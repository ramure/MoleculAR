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

public class ActionStateController : MonoBehaviour {

	/*This class manages the state of essential working processes which are handled in different classes (stopping processes and setting them back to origin).
	*So e.g. while pinching-process there is no other process possible (like bonding-process). When pinching-process stops, each process will be set to origin (ActionStateController), so that other processes can be registered.
	*Furthermore the actual hand that instantiates actions will be registered/deregistered (e.g. users cannot use two hands to pinch two objects at the same time).*/
	
	//**************************************************************
    //cooperating components:
	//-------------------------------
	[SerializeField]
	private GameObject constructionManager;
	private AtomSelectionManager atomSelection;
	private BondSelectionManager bondSelection;
	private BondingManager bonding;
	[SerializeField]
	private GameObject transformationManager;
	private AtomTransformationManager transformation;
	[SerializeField]
	private GameObject destructionManager;
	private BondDestructionManager bondDestruction;

    //handattachments:
    //-------------------------------
	[SerializeField]
	private GameObject[] handAttachments;                              //0: left hand, 1: right hand (index)
	private GameObject registeredHandAttachment = null;                //only one hand executing bonding-process
	//**************************************************************

	void Start () {
		atomSelection = constructionManager.GetComponent<AtomSelectionManager> ();
		bondSelection = constructionManager.GetComponent<BondSelectionManager> ();
		bonding = constructionManager.GetComponent<BondingManager> ();

		transformation = transformationManager.GetComponent<AtomTransformationManager> ();

		bondDestruction = destructionManager.GetComponent<BondDestructionManager> ();
	}

	public void RegisterBondingHand(int registeredHand) {
		registeredHandAttachment = handAttachments[registeredHand];
	}
    
    void Update () {
		if (registeredHandAttachment != null) {
			if (registeredHandAttachment.activeInHierarchy == false) {   //all bonding-processes will be stopped when hand is inactive
				SetAllComponentsToOrigin();
				registeredHandAttachment = null;
			}
		}
	}
    
	public void SetAllComponentsToOrigin() {
		atomSelection.SetAtomSelectionToOrigin();
		atomSelection.SetAtomTransformation (true);
		atomSelection.SetBondDestruction (true);

		bondSelection.SetBondSelectionToOrigin ();
		bonding.SetBondingToOrigin ();

		transformation.SetAtomTransformationToOrigin ();
		transformation.SetAtomSelection (true);
		transformation.SetBondDestruction (true);

		bondDestruction.SetBondDestructionToOrigin ();
		bondDestruction.SetAtomSelection(true);
		bondDestruction.SetAtomTransformation(true);
	}
}
