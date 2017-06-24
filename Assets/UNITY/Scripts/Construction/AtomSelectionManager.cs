using System.Collections.Generic;
using Leap.Unity;
using UnityEngine;

/*resources: 
* https://docs.unity3d.com/ScriptReference/
* https://docs.unity3d.com/Manual/index.html
* https://developer.leapmotion.com/documentation/unity/namespaces.html
* https://github.com/leapmotion/UnityModules/wiki
* https://github.com/leapmotion/UnityModules/tree/1c6646cda7d0a71915f3907c3e930b9fcb874826/Assets/LeapMotion/Core/Scripts/DetectionUtilities
*/

public class AtomSelectionManager : MonoBehaviour {

	/*This class handles the selection of all atoms - the simple selection (use of ProximityDetector) as well as the selection of first and second atom in bonding-process (use of ProximityDetector and ExtendedFingerDetector (via DetectorLogicGate)).
	*The bonding-process can be initialized and the actual hand/finger and state will be registered (due to ActionStateController).
	*After timer-countdown AtomSelectionManager triggers next process-step (BondSelectionManager) to continue bonding-process.*/

	//**************************************************************
	//detectors && handattachments:
	//-------------------------------
	[SerializeField]
	private ProximityDetector[] ProximityDetectors;							//both indexfinger-proximity-detectors
	[SerializeField]
	private DetectorLogicGate[] ExtendedAndProximityDetectors;				//both indexfinger-logicGate-detectors
	private GameObject actualTriggerIndexFinger;							//indexfinger which triggered the construction-process
	[SerializeField]
	private GameObject[] IndexFingers;				
	private int left = 0;
	private int right = 1;

	//cooperating components:
	//-------------------------------
	[SerializeField]
	private GameObject timeManager;
	private TimeManager timer;                                             //component that manages timer-counting
	[SerializeField]
	private GameObject bondSelectionManager;
	private BondSelectionManager bondSelection;                            //component that manages bond-selection
	[SerializeField]
	private GameObject actionStateController;
	private ActionStateController actionState;                             //central component that manages states of all components
	
	//alternating components:
	//-------------------------------
	[SerializeField]
	private GameObject[] atomTransformations;								//handattachment-component for transformation-process
	[SerializeField]
	private GameObject[] bondDestructions;									//handattachment-component for bonddestruction-process

	//atoms && bonds:
	//-------------------------------
	private GameObject selectedAtom;										//registered, current-selected atom (once construction-process starts this atom is constantly registered as first atom)
	private GameObject rejectedAtom;                                        //atom that is rejected due to reserved bonds
	private GameObject tempAtom;											//temporary-stored second atom (can change during construction-process)
	private int minAvailableBonds = 1;										//because of simple-bonding there must be at least 1 bond

	//chronological logic:
	//-------------------------------
	private bool stopOtherSelections = false;								//manages atomselection-process
	private bool findSecondAtomForBonding = false;							//atomselection refers to second atom

	//visual feedback:
	//-------------------------------
	[SerializeField]
	private Material[] selectionMaterial;									//material for selection (highlights for visual feedback)
	private enum SelectionState{Basic, Extended, Rejected};					//states that show the actual type of selection (basic = white, extended = blue, rejected = red)
	private GameObject highlightedAtomSphere;								//colored sphere around atom for visual feedback
	private Renderer atomSphereRenderer;
	//**************************************************************

	void Start() {
		timer = timeManager.GetComponent<TimeManager> ();
		bondSelection = bondSelectionManager.GetComponent<BondSelectionManager> ();
		actionState = actionStateController.GetComponent<ActionStateController> ();
	}
    
	//call-back when proximity-detector reacts to atom
	public void SetCurrentAtom(GameObject currentAtom) {
		if (stopOtherSelections == false) {
			if (findSecondAtomForBonding == false) {
				
				if (selectedAtom != null && selectedAtom != currentAtom) {
					SetSphereAccordingToSelectionState (selectedAtom, SelectionState.Basic, false); //if new atom is selected, previous atom will be deselected
				}
				SetSphereAccordingToSelectionState (currentAtom, SelectionState.Basic, true); //new atom will be highlighted and selected
				selectedAtom = currentAtom;

			} else if (findSecondAtomForBonding == true) {
				if (selectedAtom != currentAtom && ((selectedAtom.transform.root == currentAtom.transform.root && selectedAtom.transform.root.name != "Molecule") || (selectedAtom.transform.root != currentAtom.transform.root))) {
					tempAtom = currentAtom; //after first atom was chosen, now second atom is temporary-registered
					PrepareBondSelection ();
				} else if (selectedAtom == currentAtom) {
					ClearRemainingBondSelections(selectedAtom);
					actionState.SetAllComponentsToOrigin ();
				}
			}
		}
	}
    
	private void ClearRemainingBondSelections(GameObject atom) {
        List<GameObject> bondsLeft = GetFreeBondsOfAtom(atom);
        if (bondsLeft.Count != 0) {
            foreach (GameObject bond in bondsLeft) {
                bond.transform.GetComponent<Renderer>().sharedMaterial = selectionMaterial[0];
            }
        }
        bondsLeft.Clear();
    }
	
	//call-back when proximity-detector reacts to atom and indexfinger is extended (ExtendedFingerDetector)
	public void StartConstructionProcess() {
		if (stopOtherSelections == false && findSecondAtomForBonding == false) {
			if (selectedAtom != null && (selectedAtom.transform.root.name == "Atoms" || selectedAtom.transform.root.name == "Molecule")) {
				tempAtom = selectedAtom;
				
				List<GameObject> selectedAtoms = new List<GameObject>(new GameObject[] {tempAtom});
				timer.startTimer(selectedAtoms, "Construction", PrepareBondSelection); //when timer finishes, bondselection-process will be initialized
			}
		}
	}
	//call-back when proximity-detector is no longer reacting to atom or indexfinger is not extended (ExtendedFingerDetector)
	public void StopConstructionProcess() {
		timer.stopTimer ();
	}
	
	//call-back when timer has finished
	private void PrepareBondSelection() {
		SetAtomTransformation(false);
		SetBondDestruction (false);

		stopOtherSelections = true;

		if (findSecondAtomForBonding == false) {
			RegisterBondingFinger ();
		}
		List<GameObject> bondsAvailable = GetFreeBondsOfAtom(tempAtom);
		if (bondsAvailable.Count >= minAvailableBonds) {
			bondsAvailable.Clear ();
			if (findSecondAtomForBonding == false) {
				SetSphereAccordingToSelectionState (tempAtom, SelectionState.Extended, true);
			} else {
				SetSphereAccordingToSelectionState (tempAtom, SelectionState.Basic, true);
			}
			
			//start bond-selecting:
			bondSelection.InitializeBondSelection(tempAtom, actualTriggerIndexFinger, findSecondAtomForBonding);
			findSecondAtomForBonding = false;

		} else if (bondsAvailable.Count < minAvailableBonds) {
			bondsAvailable.Clear ();
			SetSphereAccordingToSelectionState (tempAtom, SelectionState.Rejected, true);
			rejectedAtom = tempAtom;
			stopOtherSelections = false;

			bondSelection.SetAtomRejected (rejectedAtom);
		}
    }

	private void RegisterBondingFinger() {
		if (ExtendedAndProximityDetectors [left].IsActive == true && ExtendedAndProximityDetectors [right].IsActive == false) {
			actualTriggerIndexFinger = IndexFingers [left];
			actionState.RegisterBondingHand (left);
		} else if (ExtendedAndProximityDetectors [right].IsActive == true && ExtendedAndProximityDetectors [left].IsActive == false) {
			actualTriggerIndexFinger = IndexFingers [right];
			actionState.RegisterBondingHand (right);
		}
	}

	//call-back when proximity-detector is no longer reacting to atom
	public void ClearAtom() {
		if (stopOtherSelections == false) {
			timer.stopTimer ();
		}
		if (findSecondAtomForBonding == true) {
			if (rejectedAtom != null) {
				SetSphereAccordingToSelectionState (rejectedAtom, SelectionState.Basic, false);
				rejectedAtom = null;
				tempAtom = null;
			}
		} else {
			if (rejectedAtom != null) {
				SetSphereAccordingToSelectionState (rejectedAtom, SelectionState.Basic, true);
				rejectedAtom = null;
			}
		}
	}

	private List<GameObject> GetFreeBondsOfAtom(GameObject atom) {
		List<GameObject> freeBonds = new List<GameObject> ();
		int numberOfAllBonds = atom.transform.FindChild("Bonding").childCount;

		for (int i = 0; i < numberOfAllBonds; i++) {
			if (atom.transform.FindChild ("Bonding").GetChild (i).gameObject.tag == "Bond") {
				freeBonds.Add(atom.transform.FindChild("Bonding").GetChild(i).gameObject);
			}
		}
		return freeBonds;
	}

	public void FindSecondAtomForBonding() {
		stopOtherSelections = false;
		findSecondAtomForBonding = true;
	}

	public void SetAtomTransformation(bool activeState) {
		for (int i = 0; i < atomTransformations.Length; i++) {
			atomTransformations [i].SetActive (activeState);
		}
	}

	public void SetBondDestruction(bool activeState) {
		for (int i = 0; i < bondDestructions.Length; i++) {
			bondDestructions [i].SetActive (activeState);
		}
	}

	public void SetAtomSelectionToOrigin() {
		stopOtherSelections = false;
		findSecondAtomForBonding = false;

		if (selectedAtom != null) {
			SetSphereAccordingToSelectionState (selectedAtom, SelectionState.Basic, false);
			selectedAtom = null;
		}
	}

	private void SetSphereAccordingToSelectionState(GameObject relatedAtom, SelectionState state, bool setActive) {
		if (relatedAtom != null) {
			highlightedAtomSphere = relatedAtom.transform.Find ("SelectionSphere").gameObject;
			atomSphereRenderer = highlightedAtomSphere.GetComponent<Renderer> ();

			//set color according to state:
			switch (state) {
			case SelectionState.Basic:
				atomSphereRenderer.sharedMaterial = selectionMaterial [0];	//white
				break;
			case SelectionState.Extended:
				atomSphereRenderer.sharedMaterial = selectionMaterial [1];	//blue
				break;
			case SelectionState.Rejected:
				atomSphereRenderer.sharedMaterial = selectionMaterial [2];	//red
				break;
			}

			//activate/deactivate sphere:
			highlightedAtomSphere.SetActive (setActive);
		}
	}
}
