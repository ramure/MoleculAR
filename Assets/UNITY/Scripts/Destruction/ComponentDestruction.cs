using System.Collections.Generic;
using Leap.Unity;
using UnityEngine;

/*resources: 
* https://docs.unity3d.com/ScriptReference/
* https://docs.unity3d.com/Manual/index.html
* https://docs.unity3d.com/ScriptReference/Collider.html
*/

public class ComponentDestruction : MonoBehaviour {

	/*This class handles destruction of connected, holistic molecules or single, released atoms. 
	*After the construct is moved to the destruction-area behind the gridline, the deletion will be triggered (and shown by timer-countdown).
	*After timer-countdown the construct is permanently removed. */

	//************************************
    //cooperating components:
	//-------------------------------
	[SerializeField]
	private GameObject timeManager;
	private TimeManager timer;                         //component that manages timer-counting

    //components:
	//-------------------------------
	[SerializeField]
	private GameObject grid;                           //when atom enters barrier grid signals deletion-process
	[SerializeField]
	private GameObject trash;                          //trash for deleted atoms
	private GameObject componentToDelete;
	private GameObject triggeredAtom;                  //atom that triggeres barrier

	private List<GameObject> atoms;
	//************************************

	void Start() {
		timer = timeManager.GetComponent<TimeManager> ();
	}

	void OnTriggerExit(Collider atomCollider) {
		if (atomCollider.gameObject.transform.tag == "Atom" && atomCollider.gameObject.transform.parent.tag != "UIButton") {
			grid.SetActive (true);
			triggeredAtom = atomCollider.gameObject;
		}
	}

	void Update() {
		if (grid.activeInHierarchy == true) {

			GameObject triggeredAtomRoot = triggeredAtom.transform.root.gameObject;
			if (triggeredAtomRoot.name == "Molecule") {
				componentToDelete = triggeredAtomRoot;
				atoms = new List<GameObject> ();
				GetAllAtomsInMolecule (triggeredAtomRoot);

				timer.startTimer (atoms, "Destruction", DeleteComponent);

			} else if (triggeredAtomRoot.name == "Atoms") {
				componentToDelete = triggeredAtom;
				List<GameObject> atoms = new List<GameObject> (new GameObject[] { componentToDelete });

				timer.startTimer (atoms, "Destruction", DeleteComponent);

			} else if (triggeredAtomRoot.name == "TransformationAnchor") {
				timer.stopTimer ();
			}
		}
	}

	void OnTriggerEnter(Collider atomCollider) {
		if (atomCollider.gameObject.transform.tag == "Atom" && atomCollider.gameObject.transform.parent.tag != "UIButton") {
			grid.SetActive (false);
		}
	}

	private void DeleteComponent() {
		if (componentToDelete != null) {
			componentToDelete.transform.SetParent (trash.transform);
			componentToDelete.tag = "AtomErased";
			componentToDelete.SetActive (false);
			componentToDelete = null;

			grid.SetActive (false);
		}
	}

	private void GetAllAtomsInMolecule(GameObject rootMolecule) {
		for (int i = 0; i < rootMolecule.transform.childCount; i++) {
			if (rootMolecule.transform.GetChild (i).name == "Molecule") {
				GetAllAtomsInMolecule (rootMolecule.transform.GetChild (i).gameObject);
			}
			if (rootMolecule.transform.GetChild (i).name != "Molecule" && rootMolecule.transform.GetChild (i).tag != "FixedBond") {
				atoms.Add (rootMolecule.transform.GetChild (i).gameObject);
			}
		}
	}
}
