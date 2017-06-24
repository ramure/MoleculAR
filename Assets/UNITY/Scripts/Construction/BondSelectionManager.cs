using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Leap;
using Leap.Unity;
using Leap.Unity.Attributes;

/*resources: 
* https://docs.unity3d.com/ScriptReference/
* https://docs.unity3d.com/Manual/index.html
* https://developer.leapmotion.com/documentation/unity/namespaces.html
* https://github.com/leapmotion/UnityModules/wiki
* https://github.com/leapmotion/UnityModules/tree/1c6646cda7d0a71915f3907c3e930b9fcb874826/Assets/LeapMotion/Core/Scripts/DetectionUtilities
*/

public class BondSelectionManager : MonoBehaviour {

	/*This class continuously selects the bond of the chosen atom with the smallest distance to the registered indexfinger and highlights it (cyan).
	*When passing the radius (of the first atom) the bondline will be initialized (BondingManager will be called that handles bondline-control).
	*When entering the radius (of the second atom) the bond-selection musst be handled within timer-countdown.*/

	//**************************************************************
	//detectors && handattachments:
	//-------------------------------
	[SerializeField]
	private ProximityDetector[] ProximityDetectors;				    //only detecting proximity towards atom-radius
	private GameObject indexFingerTip;							    //actual interacting indexfinger

	//cooperating components:
	//-------------------------------
	[SerializeField]
	private GameObject bondingManager;
	private BondingManager bonding;								    //component that manages bonding-process (bondline)
	[SerializeField]
	private GameObject timeManager;
	private TimeManager timer;									    //component that manages timer-counting

	//atoms && bonds:
	//-------------------------------
	private GameObject selectedAtom;							    //for atom-type (on-off-distance)
	private GameObject firstAtom;
	private GameObject originalBond;
	private GameObject tempAtom;
	private GameObject actualAtom;
	private GameObject rejectedAtom;
	private List<BondByDistance> bondsByDistances;				    //sorted list of bonds (by distances)
	private GameObject[] bonds;									    //all bonds available
	private GameObject selectedBondWithSmallestDistance = null;	    //bond with smallest distance towards indexfinger

	//chronological logic:
	//-------------------------------
	private bool findingSmallestDistance = false;
	private bool isSecondIteration = false;						    //if second atom needs to be selected
	private bool isSecondIterationBond = false;
	private bool continueBondingProcess = false;				    
	private bool proximityInRadius = false;						    //for snapping bondLine between first atom and second atom
	private bool isSameAtomAgain = false;

	//visual feedback:
	//-------------------------------
	private enum SelectionState{Basic, Extended, Rejected};		    //states that show the actual type of selection (basic = white, extended = blue, rejected = red)
	private Renderer bondRenderer;
	[SerializeField]
	private Material[] selectionMaterial;						    //colors each bond can take (for visual feedback)
	//**************************************************************

	void Start() {
		bonding = bondingManager.GetComponent<BondingManager> ();
		bondsByDistances = new List<BondByDistance> ();
		timer = timeManager.GetComponent<TimeManager> ();
	}

	public void InitializeBondSelection(GameObject atom, GameObject indexFinger, bool secondSelection) {
		if (secondSelection == false) {
			firstAtom = atom;
		}
		selectedAtom = atom;

		indexFingerTip = indexFinger;
		isSecondIteration = secondSelection;
		isSecondIterationBond = isSecondIteration;
		continueBondingProcess = true;

		GetBonds ();
		findingSmallestDistance = true;
	}

	void Update () {
		if (findingSmallestDistance == true) {
			GetSortedBondsByDistances ();
			selectedBondWithSmallestDistance = GetBondWithSmallestDistance (bondsByDistances [0].bond, selectedBondWithSmallestDistance);

			//snapping-process:
			if (proximityInRadius == true && isSecondIteration == true) {
				bonding.GetRadiusProximityState (selectedBondWithSmallestDistance);

				//as soon as snapping is executed end of bonding-process is initiated
				EndConstructionProcess();
			}
		}
	}

	public void SetAtomRejected(GameObject atom) {
		rejectedAtom = atom;
	}

	public void EndConstructionProcess() {
		if (isSecondIteration == true) {
			if (actualAtom != null) {
				if (rejectedAtom != actualAtom) {
					if (proximityInRadius == true) {
						List<GameObject> atoms = new List<GameObject> (new GameObject[] { actualAtom });
						timer.startTimer (atoms, "Construction", PrepareBondingFinish);
						atoms.Clear ();
					}
				}
			}
		}
	}
	public void ContinueConstructionProcess() {
		timer.stopTimer ();
		proximityInRadius = false;
	}

	private void PrepareBondingFinish() {
		if (selectedAtom != null && selectedBondWithSmallestDistance != null && indexFingerTip != null) {
			bonding.FinishBonding (selectedAtom, selectedBondWithSmallestDistance, indexFingerTip);
			isSecondIteration = false;
			continueBondingProcess = false;
		}
		proximityInRadius = false;
	}

	//call-back when proximity-detector is no longer reacting to atom - bondline needs to be shown
	public void PrepareBonding() {
		proximityInRadius = false;
		tempAtom = selectedAtom;
		if (continueBondingProcess == true) {
			if (selectedAtom != null && selectedBondWithSmallestDistance != null && indexFingerTip != null) {

				findingSmallestDistance = false;
				if (isSameAtomAgain == false && isSecondIteration == true) {
					
					//set bonds and sphere back to origin:
					selectedAtom.transform.Find ("SelectionSphere").GetComponent<Renderer> ().sharedMaterial = selectionMaterial [0];
					selectedAtom.transform.Find ("SelectionSphere").gameObject.SetActive (false);
					SetBondAccordingToSelectionState (selectedBondWithSmallestDistance, SelectionState.Basic);
					rejectedAtom = null;
					selectedAtom = null;

					bonding.SetNewProximityState (); //new second atom needs to be selected

				} else if (isSameAtomAgain == true && isSecondIteration == true) {
					//set bonds and sphere back to basic
					selectedAtom.transform.Find ("SelectionSphere").GetComponent<Renderer> ().sharedMaterial = selectionMaterial [0];
					selectedAtom.transform.Find ("SelectionSphere").gameObject.SetActive (false);
					SetBondAccordingToSelectionState (selectedBondWithSmallestDistance, SelectionState.Basic);
					rejectedAtom = null;

					bonding.SetNewProximityState (); //new second atom needs to be selected

				} else if (isSecondIteration == false) {
					originalBond = selectedBondWithSmallestDistance;
					bonding.InitializeBonding (selectedAtom, selectedBondWithSmallestDistance, indexFingerTip); //start bonding (create bondLine)
				}
			}
		}
	}
	public void PrepareSnapping() {
        proximityInRadius = true;
	}
	public void CheckSameAtom(GameObject currentAtom) {
		if (tempAtom == currentAtom && tempAtom != null) {
			isSameAtomAgain = true;
		} else if (tempAtom != currentAtom && tempAtom != null) {
			isSameAtomAgain = false;
		}
		actualAtom = currentAtom;
	}

	private void GetBonds() {
		int numberOfBonds = selectedAtom.transform.FindChild("Bonding").childCount;
		bonds = new GameObject[numberOfBonds];
		for (int i = 0; i < numberOfBonds; i++) {
			if (selectedAtom.transform.FindChild ("Bonding").GetChild (i).gameObject.tag == "Bond") {
				bonds [i] = selectedAtom.transform.FindChild ("Bonding").GetChild (i).gameObject;
			}
		}
	}

	private void GetSortedBondsByDistances() {
		bondsByDistances.Clear ();

		float actualDistance;
		for (int i = 0; i < bonds.Length; i++) {
			if (bonds [i] != null && indexFingerTip != null) {
				actualDistance = Vector3.Distance (indexFingerTip.transform.position, bonds [i].transform.position);
				bondsByDistances.Add(new BondByDistance(bonds[i], actualDistance));
			}
		}
		SortBondsByDistances (bondsByDistances);
	}
	private void SortBondsByDistances(List<BondByDistance> bondList) {
		for (int i = 1; i < bondList.Count; i++) {
			for (int j = 0; j < (bondList.Count - i); j++) {
				if (bondList [j].distance > bondList [j + 1].distance) {
					BondByDistance tempBond = bondList [j];
					bondList [j] = bondList [j + 1];
					bondList [j + 1] = tempBond;
				}
			}
		}
	}

	private GameObject GetBondWithSmallestDistance(GameObject bond, GameObject selectedBond) {
		GameObject currentBond = bond;
		SetBondAccordingToSelectionState (currentBond, SelectionState.Extended);
        
		if (selectedBond != currentBond && selectedBond != null) {
			if (isSecondIterationBond == false) {
				SetBondAccordingToSelectionState (selectedBond, SelectionState.Basic);
			} else {
				isSecondIterationBond = false;
			}
		}
		selectedBond = currentBond;
		return selectedBond;
	}

	private void SetBondAccordingToSelectionState(GameObject bond, SelectionState state) {
		if (bond != null) {
			bondRenderer = bond.GetComponent<Renderer> ();

			//set color according to state:
			switch (state) {
			case SelectionState.Basic:
				bondRenderer.sharedMaterial = selectionMaterial [0]; //white
				break;
			case SelectionState.Extended:
				bondRenderer.sharedMaterial = selectionMaterial [1]; //blue
				break;
			}
		}
	}

	public void SetBondSelectionToOrigin() {
		findingSmallestDistance = false;
		continueBondingProcess = false;
		isSecondIteration = false;
		isSecondIterationBond = false;

		SetBondAccordingToSelectionState (selectedBondWithSmallestDistance, SelectionState.Basic);
		SetBondAccordingToSelectionState (originalBond, SelectionState.Basic);
		
		originalBond = null;
		selectedAtom = null;
		selectedBondWithSmallestDistance = null;
		tempAtom = null;
	}

	private class BondByDistance {
		public GameObject bond;
		public float distance;
		public BondByDistance(GameObject actualBond, float actualDistance) {
			bond = actualBond;
			distance = actualDistance;
		}
	}
}
