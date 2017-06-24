using UnityEngine;
using System.Collections;

/*resources: 
* https://docs.unity3d.com/ScriptReference/
* https://docs.unity3d.com/Manual/index.html
* https://www.youtube.com/watch?v=Bqcu94VuVOI
* https://docs.unity3d.com/Manual/class-LineRenderer.html
* https://docs.unity3d.com/ScriptReference/LineRenderer.html
*/

public class BondingManager : MonoBehaviour {

	/*This class handles the start and the end of the bondline-control and the attachment of the bondline towards atoms or the indexfinger.
	*Furthermore the snapping-possibility will be handled (depending on the distance between indexfinger and second atom).
	*As soon as the bonding-process finishes, the construction of the molecule (MoleculeConstructionManager) will be initialized.*/

	//**************************************************************
	//detectors && handattachments:
	//-------------------------------
	private GameObject indexFinger;										//actual interacting indexfinger

	//cooperating components:
	//-------------------------------
	[SerializeField]
	private GameObject atomSelectionManager;
	private AtomSelectionManager atomSelection;							//component that manages atomselection-process
	[SerializeField]
	private GameObject moleculeConstructionManager;						
	private MoleculeConstructionManager moleculeConstruction;           //component that manages connecting of two atoms (molecule) 

	//atoms && bonds:
	//-------------------------------
	private GameObject firstAtom;										
	private GameObject firstBond;										
	private GameObject secondAtom;
	private GameObject secondBond;

	private GameObject bondAsTempEndPoint = null;						//bondLine ends at selected bond (snapping)

	[SerializeField]
	private GameObject bondingLineManager;								//linerenderer-component
	private BondingState bondingState;									//state of line - startpoint and endpoint

	//chronological logic:
	//-------------------------------
	private bool startBonding = false;
	private bool isBonding = false;
	private bool finishBonding = false;
	private bool proximityInSecondRadius = false;						//snapping-process inititation
	//**************************************************************

	void Start () {
		atomSelection = atomSelectionManager.GetComponent<AtomSelectionManager> ();
		moleculeConstruction = moleculeConstructionManager.GetComponent<MoleculeConstructionManager> ();
	}

	public void InitializeBonding(GameObject atom, GameObject bond, GameObject index) {	
		firstAtom = atom;
		firstBond = bond;
		indexFinger = index;
		bondingState = new BondingState (bondingLineManager);

		startBonding = true;
	}
	public void FinishBonding(GameObject atom, GameObject bond, GameObject index) {	
		secondAtom = atom;
		secondBond = bond;
		indexFinger = index;

		finishBonding = true;
	}

	void Update () {
		if (startBonding == true) {
			bondingState.startPoint = firstBond.transform.position;	//pivotpoint of bond is start-point
			bondingState.StartBondingLine();
			startBonding = false;
			isBonding = true;

			atomSelection.FindSecondAtomForBonding (); //find new second atom (previos was dismissed)
		}
		if (isBonding == true) {
			if (proximityInSecondRadius == true) {
				bondingState.tempEndPoint = bondAsTempEndPoint.transform.position; //bond-pivot is temporary end-point
			} else {
				bondingState.tempEndPoint = indexFinger.transform.position;	//tip of indexfinger is temporary end-point
			}
			bondingState.UpdateBondingLine();
		}
		if (finishBonding == true) {
			bondingState.endPoint = secondBond.transform.position; //pivotpoint of second bond is end-point
			isBonding = false;
			bondingState.EndBondingLine ();
			finishBonding = false;
			proximityInSecondRadius = false;

			moleculeConstruction.ConnectBothAtoms(firstAtom, firstBond, secondAtom, secondBond, bondingLineManager);
		}
	}

	public void SetNewProximityState() {
		proximityInSecondRadius = false;
		atomSelection.FindSecondAtomForBonding ();
	}

	public void GetRadiusProximityState(GameObject bond) {
		if (bond != null) {
			bondAsTempEndPoint = bond;
			proximityInSecondRadius = true; //snapping-condition
		}
	}

	public void SetBondingToOrigin() {
		isBonding = false;
		startBonding = false;
		finishBonding = false;

		if (bondingLineManager != null) {
			bondingLineManager.GetComponent<LineRenderer> ().SetPosition (0, Vector3.zero);
			bondingLineManager.GetComponent<LineRenderer> ().SetPosition (1, Vector3.zero);
		}
	}

    
	private class BondingState {
		private Vector3 bondingStartPoint = Vector3.zero;
		private Vector3 tempBondingEndPoint = Vector3.zero;
		private Vector3 bondingEndPoint = Vector3.zero;

		private GameObject bondingLineObjectManager;
		private LineRenderer bondingLineRenderer;

		public BondingState(GameObject bondingLineManager) {
			bondingLineObjectManager = bondingLineManager;
		}

        
		public Vector3 startPoint {
			get { return bondingStartPoint; }
			set { bondingStartPoint = value; }
		}

		public Vector3 tempEndPoint {
			get { return tempBondingEndPoint; }
			set { tempBondingEndPoint = value; }
		}

		public Vector3 endPoint {
			get { return bondingEndPoint; }
			set { bondingEndPoint = value; }
		}
        
        
		public void StartBondingLine() {
			bondingLineRenderer = bondingLineObjectManager.gameObject.GetComponent<LineRenderer> ();
			bondingLineRenderer.SetPosition (0, bondingStartPoint);	//line will start at bond-pivot (index 0)
		}

		public void UpdateBondingLine() {
			bondingLineRenderer.SetPosition (1, tempBondingEndPoint); //line will end at index-fingertip (index 1)
		}

		public void EndBondingLine() {
			bondingLineRenderer.SetPosition (1, bondingEndPoint); //final line-end will be attached to second bond from second atom
		}

	}
}
