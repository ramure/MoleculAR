using UnityEngine;
using System.Collections;

/*resources: 
* https://docs.unity3d.com/ScriptReference/
* https://docs.unity3d.com/Manual/index.html
*/

public class LMCHandler : TrackingHandler {

	//This class initializes mapping of the hands as soon as the lmc-marker is shown and trackable.

	//************************************
    //components:
	//-------------------------------
	[SerializeField]
	private GameObject LMCMarker;
	private HandMappingCalibration mappingHandler;

    //chronological logic:
	//-------------------------------
	private bool lmcTargetIsEnabled = false;
	private bool stateBeforeCalibration = true;
	//************************************

	void Start () {
		mappingHandler = GetComponent<HandMappingCalibration>();
	}

	void Update () {
		if (stateBeforeCalibration == true) {
			lmcTargetIsEnabled = GetTrackableState (LMCMarker);

			if (lmcTargetIsEnabled == true) {
				stateBeforeCalibration = false;
			}
		} else {
			mappingHandler.InitializeMapping(); //once lmc-marker is identifyed, hands can be mapped
		}
	}
}
