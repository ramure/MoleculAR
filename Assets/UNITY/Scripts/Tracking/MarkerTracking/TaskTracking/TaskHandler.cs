using UnityEngine;
using System.Collections;

/*resources: 
* https://docs.unity3d.com/ScriptReference/
* https://docs.unity3d.com/Manual/index.html
* http://gestis.itrust.de/nxt/gateway.dll/gestis_de/000000.xml?f=templates&fn=default.htm&vid=gestisdeu:sdbdeu
*/

public class TaskHandler : TrackingHandler {

	//This class activates all molecule-information when the task-sign is tracked and recognized. Also the task will be stored temporarily.

	//************************************
    //components:
	//-------------------------------
	[SerializeField]
	private GameObject AmmoniakMarker;
	[SerializeField]
	private GameObject MethanolMarker;
	[SerializeField]
	private GameObject MethylaminMarker;
    
    [SerializeField]
	private GameObject Task;
    
    [SerializeField]
	private GameObject infoHandler;
	private InfoHandler info;

    //chronological logic:
	//-------------------------------
	private bool ammoniakTaskIsActive = false;
	private bool methanolTaskIsActive = false;
	private bool methylaminTaskIsActive = false;
	//************************************

	void Start () {
		info = infoHandler.GetComponent<InfoHandler> ();
	}
	
	void Update () {
		ammoniakTaskIsActive = GetTrackableState (AmmoniakMarker);
		methanolTaskIsActive = GetTrackableState (MethanolMarker);
		methylaminTaskIsActive = GetTrackableState (MethylaminMarker);

		if ((ammoniakTaskIsActive == true && methanolTaskIsActive == false && methylaminTaskIsActive == false) || (ammoniakTaskIsActive == false && methanolTaskIsActive == true && methylaminTaskIsActive == false) || (ammoniakTaskIsActive == false && methanolTaskIsActive == false && methylaminTaskIsActive == true) || (ammoniakTaskIsActive == false && methanolTaskIsActive == false && methylaminTaskIsActive == false)) {
			SetInfoAccordingToState (AmmoniakMarker, ammoniakTaskIsActive);
			SetInfoAccordingToState (MethanolMarker, methanolTaskIsActive);
			SetInfoAccordingToState (MethylaminMarker, methylaminTaskIsActive);
		} 
	}
	private void SetInfoAccordingToState(GameObject marker, bool state) {
		marker.SetActive(state);
		if (state == true) {
			SetTaskAccordingToMarker (marker);
		}
	}

	private void SetTaskAccordingToMarker(GameObject marker) {
		Task.name = marker.name;
	}
}
