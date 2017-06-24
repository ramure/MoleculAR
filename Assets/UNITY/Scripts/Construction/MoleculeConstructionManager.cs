using UnityEngine;
using System.Collections;

/*resources: 
* https://docs.unity3d.com/ScriptReference/
* https://docs.unity3d.com/Manual/index.html
* https://www.youtube.com/watch?v=Bqcu94VuVOI
* https://docs.unity3d.com/Manual/class-LineRenderer.html
* https://docs.unity3d.com/ScriptReference/LineRenderer.html
*/

public class MoleculeConstructionManager : MonoBehaviour {

	/*This class aligns first and second selected atoms and rotates them so that they are pointing (with selected bond-tip) to each other.
	*After this alignment both bonds are connected and replaced by one, fixed bond-prefab. In the end a new molecule out of both atoms (and bonds) is registered and formed.*/

	//**************************************************************
    //cooperating components:
	//-------------------------------
	[SerializeField]
	private GameObject actionStateController;
	private ActionStateController actionState;							//central component that manages states of all components

	//atoms && bonds:
	//-------------------------------
	[SerializeField]
	private GameObject fixedBond;                                      //bond-prefab

	private GameObject firstAtom;
	private GameObject firstBond;
	private GameObject secondAtom;
	private GameObject secondBond;

	private GameObject[] atoms;
	private GameObject[] bonds;
	private int numberOfElements = 2;

	[SerializeField]
	private GameObject[] pivotHelper;									//helper for setting pivotpoint of atom (according to selected bond)
	private GameObject pivotHelperFirst;								//pivotpoint of first atom
	private GameObject pivotHelperSecond;								//pivotpoint of second atom
	private Vector3 pivotFirstBond;
	private Vector3 pivotSecondBond;
	private Quaternion pivotFirstBondRotation;
	private Quaternion pivotSecondBondRotation;

	[SerializeField]
	private GameObject moleculeOrigin;                                  //molecule-prefab
	private GameObject firstComponent;									//component that will be child of first pivotHelper
	private GameObject secondComponent;									//component that will be child of second pivotHelper

	private GameObject bondingLineManager;								//linerenderer-component
	private GameObject bondLine;										//fixed physical bond between atoms (after construction-process)

	//visual feedback:
	//-------------------------------
	private GameObject highlightedSphere;								//colored sphere around atom for visual feedback
	private Renderer atomRenderer;
	[SerializeField]
	private Material[] selectionMaterial;								//material for selection (highlights for visual feedback)
	//**************************************************************

	void Start() {
		actionState = actionStateController.GetComponent<ActionStateController> ();
	}


	public void ConnectBothAtoms(GameObject atomFirst, GameObject bondFirst, GameObject atomSecond, GameObject bondSecond, GameObject lineManager) {
		firstAtom = atomFirst;
		firstBond = bondFirst;
		secondAtom = atomSecond;
		secondBond = bondSecond;

		atoms = new GameObject[numberOfElements];
		bonds = new GameObject[numberOfElements];
		atoms [0] = firstAtom;
		atoms [1] = secondAtom;
		bonds [0] = firstBond;
		bonds [1] = secondBond;

		bondingLineManager = lineManager;
		bondLine = new GameObject();

		pivotHelperFirst = pivotHelper [0];
		pivotHelperSecond = pivotHelper [1];

		SetRotationAndPositionAccordingToPivotHelper ();
		CreateFixedBondingLineBetweenAtoms ();
		JoinBothAtomsIntoMolecule ();
	}

	private void SetRotationAndPositionAccordingToPivotHelper() {
		InitializePivotHelper ();
		SetPivotHelperInHierachy ();
		ConnectBothPivots ();
	}

	private void InitializePivotHelper() {
		pivotFirstBond = firstBond.transform.position;
		pivotFirstBondRotation = firstBond.transform.rotation;
		pivotSecondBond = secondBond.transform.position;
		pivotSecondBondRotation = secondBond.transform.rotation;

		pivotHelperFirst.transform.position = pivotFirstBond;
		pivotHelperFirst.transform.rotation = pivotFirstBondRotation;
		pivotHelperSecond.transform.position = pivotSecondBond;
		pivotHelperSecond.transform.rotation = pivotSecondBondRotation;
	}

	private void SetPivotHelperInHierachy() {
		if (firstAtom.transform.root.name == "Atoms") {
			firstComponent = firstAtom;
			firstAtom.transform.SetParent (pivotHelperFirst.transform, true);
			
		} else if (firstAtom.transform.root.name == "Molecule") {
			firstComponent = firstAtom.transform.root.gameObject;
			firstAtom.transform.root.SetParent (pivotHelperFirst.transform, true);
		}
		if (secondAtom.transform.root.name == "Atoms") {
			secondComponent = secondAtom;
			secondAtom.transform.SetParent (pivotHelperSecond.transform, true);
			
		} else if (secondAtom.transform.root.name == "Molecule") {
			secondComponent = secondAtom.transform.root.gameObject;
			secondAtom.transform.root.SetParent (pivotHelperSecond.transform, true);
			
		}
	}

	private void ConnectBothPivots() {
		pivotHelperSecond.transform.rotation = pivotFirstBondRotation;
		pivotHelperSecond.transform.rotation = Quaternion.LookRotation (-pivotHelperSecond.transform.forward);	//turn around 180 degree (both atoms facing each other)
		pivotHelperSecond.transform.position = pivotHelperFirst.transform.position;
	}

	private void CreateFixedBondingLineBetweenAtoms() {
		float distanceBetweenAtoms = 0;
		distanceBetweenAtoms = Vector3.Distance(firstAtom.transform.position, secondAtom.transform.position);

		bondLine = Instantiate (fixedBond);
		bondLine.name = "FixedBond_" + firstAtom.name + "-" + firstBond.name + "_" + secondAtom.name + "-" + secondBond.name; //nomenclature (id) for identifying fixed bond in hierarchy
		bondLine.transform.localScale += new Vector3 (0.01f, (distanceBetweenAtoms / 2.0f), 0.01f);
		bondLine.transform.position = pivotHelperFirst.transform.position;
		bondLine.transform.rotation = pivotHelperSecond.transform.localRotation;
		bondLine.transform.Rotate (90, 0, 0);
	}

	private void JoinBothAtomsIntoMolecule() {
		for (int i = 0; i < numberOfElements; i++) {
			SetBondAssignedState (bonds [i]);
			RepositionHighlighting (atoms [i]);
		}
		CreateMoleculeOutOfBothComponentsAndBond ();
		ClearLineRenderer ();
	}

	private void SetBondAssignedState(GameObject bond) {
		bond.tag = "BondAssigned"; //set new tag to exclude bond from selection
	}

	private void RepositionHighlighting(GameObject atom) {
		highlightedSphere = atom.transform.Find("SelectionSphere").gameObject;
		atomRenderer = highlightedSphere.GetComponent<Renderer> ();
		atomRenderer.enabled = true;
		atomRenderer.sharedMaterial = selectionMaterial [0];
		highlightedSphere.SetActive (false);
	}

	private void CreateMoleculeOutOfBothComponentsAndBond() {
		GameObject molecule = Instantiate(moleculeOrigin).gameObject;
		molecule.name = "Molecule";
		molecule.transform.SetParent (null);
		
		firstComponent.transform.SetParent (molecule.transform);
		secondComponent.transform.SetParent (molecule.transform);

		bondLine.transform.SetParent (molecule.transform);

		//after construction-process finally finishes all components (for processing) are set to origin
		actionState.SetAllComponentsToOrigin();
	}

	private void ClearLineRenderer() {
		//set linerenderer back to origin:
		bondingLineManager.GetComponent<LineRenderer>().SetPosition(0, Vector3.zero);
		bondingLineManager.GetComponent<LineRenderer>().SetPosition(1, Vector3.zero);
	}
}
