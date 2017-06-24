using System.Collections.Generic;
using System;
using UnityEngine;
using Leap.Unity;

/*resources: 
* https://docs.unity3d.com/ScriptReference/
* https://docs.unity3d.com/Manual/index.html
* https://developer.leapmotion.com/documentation/unity/namespaces.html
* https://github.com/leapmotion/UnityModules/wiki
* https://github.com/leapmotion/UnityModules/tree/1c6646cda7d0a71915f3907c3e930b9fcb874826/Assets/LeapMotion/Core/Scripts/DetectionUtilities
*/

public class AtomTransformationManager : MonoBehaviour {

	/*This class handles the manipulation of virtual atoms by pinching the objects (via PinchDetector and ProximityDetector of PinchPoint) and then moving or rotating them in 3D-space. Objects can easily be released.
	*If the pinched atom is part of the original atoms from the periodic-system, the atom needs to be initialized by duplicating it and naming it with different id (so that it can be activated by CustomProximityDetector).
	*When pinched, the atom is attached to the pinchpoint (between thumb and indexfinger) of virtual hand-model and when released, it is bound to the 3D-space again.*/

	//**************************************************************
	//detectors && handattachments:
	//-------------------------------
	[SerializeField]
	private PinchDetector pinchDetectorLeft;
	[SerializeField]
	private PinchDetector pinchDetectorRight;
	[SerializeField]
	private DetectorLogicGate pinchDetectorProxLeft;
	[SerializeField]
	private DetectorLogicGate pinchDetectorProxRight;
	private PinchDetector actualPinchDetector;

	private enum PinchRegistry {Left, Right, None};
	private PinchRegistry pinchRegister;

	//cooperating components:
	//-------------------------------
	[SerializeField]
	private GameObject[] atomSelections;
	[SerializeField]
	private GameObject[] bondDestructions;

	//atoms && bonds:
	//-------------------------------
	private GameObject selectedAtom;
	[SerializeField]
	private GameObject transformationAnchor;
	[SerializeField]
	private GameObject atomsRoot;

	private GameObject transformedMolecule;

	private List<GameObject> allAtomsInMolecule;
	private List<GameObject> atomsInMolecule;

	//chronological logic:
	//-------------------------------
	private bool pinchActivated = false;
	private bool isAtomTransformation = false;
	private bool isMoleculeTransformation = false;

	//visual feedback:
	//-------------------------------
	private GameObject highlightedSphere;
	[SerializeField]
	private Material[] highlightedMaterial;
	[SerializeField]
	private Material searchedMaterial;
	[SerializeField]
	private Material pinchMaterial;
	//**************************************************************

	void Start () {
		allAtomsInMolecule = new List<GameObject> ();
		atomsInMolecule = new List<GameObject>();
	}
	
	public void SetAtomSelection(bool activestate) {
		for (int i = 0; i < atomSelections.Length; i++) {
			atomSelections [i].SetActive (activestate);
		}
	}
	public void SetBondDestruction(bool activeState) {
		for (int i = 0; i < bondDestructions.Length; i++) {
			bondDestructions [i].SetActive (activeState);
		}
	}
	public void SetAtomTransformationToOrigin() {
		DischargeTransformationAnchor ();
		ReleaseComponent ();
	}
	
	void Update () {
		if (pinchActivated == true) {
			switch (pinchRegister) {
			case PinchRegistry.Left:
				TransformAnchor (pinchDetectorLeft);
				break;
			case PinchRegistry.Right:
				TransformAnchor (pinchDetectorRight);
				break;
			case PinchRegistry.None:
				break;
			}
		}
	}

	public void SetCurrentAtom(GameObject currentAtom) {
		ClearAtom ();

		selectedAtom = currentAtom;
		SetSphereAccordingToSelectionState (selectedAtom, 0, true);
	}
	public void ClearAtom() {
		if (selectedAtom != null) {
			SetSphereAccordingToSelectionState (selectedAtom, 0, false);
			selectedAtom = null;
		}
	}

	public void ReleaseComponent() {

		pinchRegister = PinchRegistry.None;
		pinchActivated = false;

		if (selectedAtom != null) {
			if (isAtomTransformation == true) {
				SetSphereAccordingToSelectionState (selectedAtom, 0, true);
				selectedAtom.transform.SetParent (atomsRoot.transform);							//set atom back again as child of atoms

				isAtomTransformation = false;

			} else if (isMoleculeTransformation == true) {
				foreach (GameObject atom in atomsInMolecule) {
					SetSphereAccordingToSelectionState(atom, 0, false);
				}
				highlightedSphere = selectedAtom.transform.Find ("SelectionSphere").gameObject;
				highlightedSphere.SetActive (true);
				transformedMolecule.transform.SetParent (null);

				isMoleculeTransformation = false;
				allAtomsInMolecule.Clear ();
				atomsInMolecule.Clear();
			}
			DischargeTransformationAnchor ();
		}
		SetAtomSelection(true);
		SetBondDestruction (true);
	}

	private void DischargeTransformationAnchor() {
		if (transformationAnchor.transform.childCount > 0) {
			for (int i = 0; i < transformationAnchor.transform.childCount; i++) {

				if (transformationAnchor.transform.GetChild (i).name == "Molecule") {
					transformationAnchor.transform.GetChild (i).SetParent (null);
				} else {
					transformationAnchor.transform.GetChild (i).SetParent (atomsRoot.transform);
				}
			}
		}
	}
	private int GetNewDigit(GameObject atom) {
		string atomType = atom.name [0].ToString();

		//alle vorhandenen atome:
		GameObject[] allAtoms = GameObject.FindGameObjectsWithTag ("Atom");
		List<GameObject> interactableAtoms = new List<GameObject> ();
		for (int a = 0; a < allAtoms.Length; a++) {
			if (allAtoms [a].transform.parent.tag != "UIButton" && allAtoms[a].name[0].ToString() == atomType) {
				interactableAtoms.Add (allAtoms [a]);
			}
		}
		int tempDigit = 0;

		for (int i = 0; i < interactableAtoms.Count; i++) {
			string actualAtom = interactableAtoms [i].name;
			string digitString = actualAtom.Substring (1);

			int digits;
			Int32.TryParse (digitString, out digits);
			if (tempDigit < digits) {
				tempDigit = digits;
			}
		}
		tempDigit = tempDigit + 1;
		interactableAtoms.Clear ();
		return tempDigit;
	}

	public void TransformComponent() {
		
		if (selectedAtom != null) {
			SetAtomSelection (false);
			SetBondDestruction (false);

			DischargeTransformationAnchor ();

			if (pinchDetectorProxLeft.IsActive == true && pinchDetectorProxRight.IsActive == false) {
				actualPinchDetector = pinchDetectorLeft;
				pinchRegister = PinchRegistry.Left;
			} else if (pinchDetectorProxLeft.IsActive == false && pinchDetectorProxRight.IsActive == true) {
				actualPinchDetector = pinchDetectorRight;
				pinchRegister = PinchRegistry.Right;
			} else {
				pinchRegister = PinchRegistry.None;
			}
			
			if (pinchRegister != PinchRegistry.None) {
				if (selectedAtom.transform.parent.tag == "UIButton") {
					int newDigit = GetNewDigit(selectedAtom);
					GameObject instantiatedSelectedAtom = Instantiate (selectedAtom, atomsRoot.transform) as GameObject;
					instantiatedSelectedAtom.name = selectedAtom.name + newDigit.ToString ();
					selectedAtom = instantiatedSelectedAtom;
					
					InstantiateAtomTransformation();
				}
				if (selectedAtom.transform.parent.name == "Atoms") {
					InstantiateAtomTransformation();
				}
				if (selectedAtom.transform.root.name == "Molecule") {
					GetAllAtomsInMolecule (selectedAtom.transform.root.gameObject);
					atomsInMolecule = allAtomsInMolecule;
					transformedMolecule = selectedAtom.transform.root.gameObject;
					
					InstantiateMoleculeTransformation();
				}
			}
		}
	}
	private void InstantiateAtomTransformation() {
		isAtomTransformation = true;
		SetTransformationAnchor(actualPinchDetector, selectedAtom, selectedAtom.transform);
		pinchActivated = true;
	}
	private void InstantiateMoleculeTransformation() {
		isMoleculeTransformation = true;
		SetTransformationAnchor(actualPinchDetector, selectedAtom, transformedMolecule.transform);
		pinchActivated = true;
	}
	
	private void GetAllAtomsInMolecule(GameObject rootMolecule) {
		for (int i = 0; i < rootMolecule.transform.childCount; i++) {
			if (rootMolecule.transform.GetChild (i).name == "Molecule") {
				GetAllAtomsInMolecule (rootMolecule.transform.GetChild (i).gameObject);
			}
			if (rootMolecule.transform.GetChild (i).name != "Molecule" && rootMolecule.transform.GetChild (i).tag != "FixedBond") {
				allAtomsInMolecule.Add (rootMolecule.transform.GetChild (i).gameObject);
			}
		}
	}

	private void SetTransformationAnchor(PinchDetector pinchDetector, GameObject atom, Transform actualComponentTransform) {
		transformationAnchor.transform.position = atom.transform.position;
		transformationAnchor.transform.rotation = pinchDetector.Rotation;
		transformationAnchor.transform.parent = null;							//make sibling
		actualComponentTransform.SetParent (transformationAnchor.transform);	//put selectedAtom into sibling-position as a child
	}

	private void TransformAnchor(PinchDetector actualPinchDetector) {
		transformationAnchor.transform.position = actualPinchDetector.Position;
		transformationAnchor.transform.rotation = actualPinchDetector.Rotation;
	}

	private void SetSphereAccordingToSelectionState(GameObject relatedAtom, int selectionState, bool setActive) {
		if (relatedAtom != null) {
			highlightedSphere = relatedAtom.transform.Find ("SelectionSphere").gameObject;
			highlightedSphere.GetComponent<Renderer> ().sharedMaterial = highlightedMaterial [selectionState];

			//activate/deactivate sphere
			highlightedSphere.SetActive (setActive);
		}
	}
}
