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
*/

public class BondDestructionManager : MonoBehaviour {
	
	/*This class handles the destruction of a fixed bond in a molecule (between two connected atoms).
	*The bond must be aimed with proximity-detector and extended index-/middle-finger to initialize the bond-destruction (triggers timer-countdown).
	*After the timers have finished, the fixed bond needs to be removed - so the molecule (connection between atoms) that contains this bond needs to be dissolved.
	*This can be done with separation and going through each branch in the main-molecule-history (repositioning of components) - so that in the end two parts are made of one. */

	//**************************************************************
	//cooperating components:
	//-------------------------------
	[SerializeField]
	private GameObject timeManager;
	private TimeManager timer;												//component that manages timer-counting
	
	//alternating components:
	//-------------------------------
	[SerializeField]
	private GameObject[] atomTransformations;								//handattachment-component for transformation-process
	[SerializeField]
	private GameObject[] atomSelections;									//handattachment-component for atomselection-process
	
	//atoms && bonds:
	//-------------------------------
	private GameObject bondToDelete = null;
	private GameObject molecule;
	private GameObject firstAtom;
	private GameObject secondAtom;
	private GameObject firstBond;
	private GameObject secondBond;
	private GameObject[] allAtoms;
	private GameObject[] bondSiblings;                                      //component-siblings of molecule in hierarchy
	
	[SerializeField]
	private GameObject atoms;
	
	//chronological logic:
	//-------------------------------
	private bool stopBondToDeleteSelection = false;
	
	//visual feedback:
	//-------------------------------
	[SerializeField]
	private Material deletionMaterial;
	//**************************************************************

	void Start() {
		timer = timeManager.GetComponent<TimeManager> ();
	}

	//call-back when proximity-detector reacts to fixed bond
	public void SetCurrentBond(GameObject currentBond) {
		if (stopBondToDeleteSelection == false) {
			bondToDelete = currentBond;
		}
	}

	//call-back when proximity-detector is no longer reacting to fixed bond
	public void ClearBond() {
		if (stopBondToDeleteSelection == false) {
			timer.stopTimer ();

			SetAtomSelection (true);
			SetAtomTransformation (true);
			SetBondDestructionToOrigin ();
		}
	}

	//call-back when proximity-detector reacts to fixed bond while index-/middle-finger are extended (ExtendedFingerDetector)
	public void PrepareBondDeletion() {
		if (stopBondToDeleteSelection == false) {
			if (bondToDelete != null) {
				molecule = bondToDelete.transform.root.gameObject;

				FindAllAtoms ();
				ExtractComponentsFromBondName (bondToDelete.name); //bond-id inlcudes name of both connected atoms and bonds

				if (firstAtom != null && secondAtom != null && firstBond != null && secondBond != null) {
					List<GameObject> bondAtoms = new List<GameObject> (new GameObject[] {firstAtom, secondAtom});
					timer.startTimer (bondAtoms, "Destruction", DeleteBond);
					bondAtoms.Clear ();
				}
			}
		}
	}

	//when timer finishes
	public void DeleteBond() {
		if (bondToDelete != null && firstAtom != null && secondAtom != null) {
			stopBondToDeleteSelection = true;

			SetAtomSelection (false);
			SetAtomTransformation (false);

			bondToDelete.tag = "BondToDelete";
			firstBond.tag = "Bond";
			secondBond.tag = "Bond";

			//iterate through molecule to suspend atom that is not part of molecule anymore:
			StartResortingOfMoleculeComponents (bondToDelete, firstAtom, secondAtom);
			stopBondToDeleteSelection = false;
		}
	}

	private void StartResortingOfMoleculeComponents(GameObject bond, GameObject first, GameObject second) {
		//get bond-siblings (both need to be separated):
		bondSiblings = new GameObject[bond.transform.parent.childCount - 1];
		int bondSiblingIndex = 0;
		for (int i = 0; i < bond.transform.parent.childCount; i++) {
			if (bond.transform.parent.GetChild (i).name != bond.name) {
				bondSiblings [bondSiblingIndex] = bond.transform.parent.GetChild (i).gameObject;
				bondSiblingIndex = bondSiblingIndex + 1;
			}
		}

		//iterate separated atoms (up in hierarchy):
		Transform bondParent = bond.transform.parent;
		Transform bondGrandParent = bondParent.transform.parent;

		for (int j = 0; j < bondSiblings.Length; j++) {
			if (bondSiblings [j].transform.parent == bondSiblings [j].transform.root) {
				if (bondSiblings [j].name != "Molecule") {
					bondSiblings [j].transform.SetParent (atoms.transform);
				} else {
					bondSiblings [j].transform.SetParent (bondGrandParent);
				}
			} else {
				bondSiblings [j].transform.SetParent (bondGrandParent);
			}
		}

		//separate component:
		bondParent.SetParent (null);
		bond.SetActive (false);
		bond = null;
		bondParent.gameObject.SetActive (false);
		bondParent = null; 

		if (bondGrandParent != null) {
			IterateThroughMoleculeStructureAndMoveDisplacedComponent (bondSiblings, bondGrandParent); //check if there is any left connection in molecule (for both siblings)
		} 
		SetAtomSelection (true);
		SetAtomTransformation (true);
	}

	private void IterateThroughMoleculeStructureAndMoveDisplacedComponent(GameObject[] components, Transform grandParent) {
		//new bond to compare:
		GameObject comparativeBond = new GameObject();
		GameObject movedComponent = new GameObject ();
		
		for (int i = 0; i < grandParent.childCount; i++) {
			if (grandParent.GetChild (i).tag == "FixedBond") {
				comparativeBond = grandParent.GetChild (i).gameObject;
			}
		}
		//extract new atom-/bond-names
		ExtractComponentsFromBondName(comparativeBond.name);

		//check if components are included in new bond (for comparison)
		for (int j = 0; j < components.Length; j++) {
			bool isIncluded = CheckComponentIfAtomsAreIncluded (firstAtom, secondAtom, components[j]);

			if (isIncluded == false) {
				movedComponent = components [j]; //component moves up in hierarchy
			}
		}
		if (movedComponent != null) {
			GameObject[] movedComponents = new GameObject[1];
			movedComponents [0] = movedComponent;

			if (movedComponent.transform.parent != movedComponent.transform.root) {
				movedComponent.transform.SetParent(movedComponent.transform.parent.parent);

				if (movedComponent.transform.parent.parent != null) {
					IterateThroughMoleculeStructureAndMoveDisplacedComponent (movedComponents, movedComponent.transform.parent.parent);
				}
                
			} else if (movedComponent.transform.parent == movedComponent.transform.root) {
                if (movedComponent.transform.name != "Molecule") {
					movedComponent.transform.SetParent (atoms.transform);
				} else {
					movedComponent.transform.SetParent (null);
				}
			}
		}
	}

	private bool CheckComponentIfAtomsAreIncluded(GameObject first, GameObject second, GameObject component) {
		bool isIncludedInComponent = false;
		for (int i = 0; i < allAtoms.Length; i++) {
			if ((allAtoms [i].name == first.name || allAtoms[i].name == second.name) && (allAtoms[i].transform.root.gameObject == molecule)) {
				
                if (component.name == allAtoms[i].name) {
					isIncludedInComponent = true;
				}
				Transform[] allIncludedAtoms = component.GetComponentsInChildren<Transform>();
				foreach (Transform element in allIncludedAtoms) {
					if (element.name == allAtoms[i].name) {
						isIncludedInComponent = true;
					}
				}
			}
		}
		return isIncludedInComponent;
	}

	private void ExtractComponentsFromBondName(string bond) {
		int[] separativeIndices = new int[2];
		int j = 0;

		for (int i = 0; i < bond.Length; i++) {
			string str = bond [i].ToString ();
			if(str.Equals("_")) {
				separativeIndices [j] = i + 1;
				j = j + 1;
			}
		}
		string secondAtomComponents = bond.Substring(separativeIndices[1]);
		string firstAtomComponents = bond.Substring(separativeIndices[0], ((separativeIndices[1] - 2) - separativeIndices[0]) + 1);
		
		string[] firstComponents = ExtractComponents (firstAtomComponents);
		string[] secondComponents = ExtractComponents (secondAtomComponents);

		firstAtom = null;
		secondAtom = null;
		firstAtom = FindAtomInMolecule (firstAtom, firstComponents[0]);
		secondAtom = FindAtomInMolecule (secondAtom, secondComponents[0]);

		if (firstAtom != null) {
			firstBond = firstAtom.transform.GetChild(0).Find(firstComponents[1]).gameObject;
		}
		if (secondAtom != null) {
			secondBond = secondAtom.transform.GetChild(0).Find (secondComponents [1]).gameObject;
		}
	}

	private string[] ExtractComponents(string components) {
		string atom;
		string bond;
		string[] extractedComponents = new string[2];
		for (int i = 0; i < components.Length; i++) {
			string str = components [i].ToString ();
			if (str.Equals("-")) {
                
				atom = components.Substring (0, i);
				bond = components.Substring (i + 1);

				extractedComponents [0] = atom;
				extractedComponents [1] = bond;
			}
		}
		return extractedComponents;
	}

	private void FindAllAtoms() {
		allAtoms = GameObject.FindGameObjectsWithTag ("Atom");
	}
		
	private GameObject FindAtomInMolecule(GameObject searchedAtom, string atomId) {
		for (int i = 0; i < allAtoms.Length; i++) {
			if (allAtoms [i].name == atomId && allAtoms[i].transform.root.gameObject == molecule) {
				searchedAtom = allAtoms [i].gameObject;
			}
		}
		return searchedAtom;
	}

	public void SetAtomTransformation(bool activeState) {
		for (int i = 0; i < atomTransformations.Length; i++) {
			atomTransformations [i].SetActive (activeState);
		}
	}

	public void SetAtomSelection(bool activeState) {
		for (int i = 0; i < atomSelections.Length; i++) {
			atomSelections [i].SetActive (activeState);
		}
	}

	public void SetBondDestructionToOrigin() {
		stopBondToDeleteSelection = false;
		bondToDelete = null;

		firstAtom = null;
		firstBond = null;
		secondAtom = null;
		secondBond = null;
	}
}
