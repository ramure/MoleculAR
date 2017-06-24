using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using Vuforia;

/*resources: 
* https://docs.unity3d.com/ScriptReference/
* https://docs.unity3d.com/Manual/index.html
*/

public class PSEHandler : TrackingHandler {

	//This class shows related atom-content (model, information) when triggering the periodic-table-area due to hand-navigation.

	//************************************
    //components:
	//-------------------------------
	[SerializeField]
	private GameObject PSEMarker;
    [SerializeField]
	private GameObject infoHandler;
	
	private List<GameObject> virtElementButtons;

    //chronological logic:
	//-------------------------------
    private bool pseTargetIsEnabled = false;
	//************************************

	void Start () {
		virtElementButtons = new List<GameObject>();
		foreach (Transform virtButton in PSEMarker.transform) {
			if (virtButton.tag == "UIButton") {

				for (int i = 0; i < virtButton.childCount; i++) {
					virtButton.GetChild (i).gameObject.SetActive (false);
				}
				virtElementButtons.Add (virtButton.gameObject);
			}
		}
	}
	
	void Update () {
		pseTargetIsEnabled = GetTrackableState (PSEMarker);

		if (pseTargetIsEnabled == true) {
			foreach (Transform virtButton in PSEMarker.transform) {
				virtButton.gameObject.SetActive (true);
			}
		} else {
			foreach (Transform virtButton in PSEMarker.transform) {
				virtButton.gameObject.SetActive (false);
			}

		}
	}
}
