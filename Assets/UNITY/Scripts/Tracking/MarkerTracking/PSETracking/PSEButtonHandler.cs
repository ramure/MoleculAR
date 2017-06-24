using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Leap.Unity;

/*resources: 
* https://docs.unity3d.com/ScriptReference/
* https://docs.unity3d.com/Manual/index.html
* https://developer.leapmotion.com/documentation/unity/namespaces.html
* https://github.com/leapmotion/UnityModules/wiki
* https://github.com/leapmotion/UnityModules/tree/1c6646cda7d0a71915f3907c3e930b9fcb874826/Assets/LeapMotion/Core/Scripts/DetectionUtilities
* http://www.rsc.org/periodic-table/
*/

public class PSEButtonHandler : MonoBehaviour {

	/*This class changes proximity-distances (increasing) as soon as periodic-table-area is triggered and changes them back to normal again (decreasing) as soon as area is not triggered any more.
	*Therefore differences and problems in tracking and depth information (when mapping both hands) should be reduced. */

	//************************************
	//detectors && handattachments:
	//-------------------------------
	[SerializeField]
	private ProximityDetector[] proximityDetectors;

    //components:
	//-------------------------------
	[SerializeField]
	private GameObject infoHandler;
	private InfoHandler info;
    
    private GameObject indexFinger;
    
    private GameObject[] elementButtons;
	private List<ElementButtonByDistance> elementsByDistance;
	private GameObject elementButtonWithSmallestDistance;
    
	private GameObject selectedButton;

    //chronological logic:
	//-------------------------------
	private bool findSmallestDistance = false;
	//************************************

	void Start() {
		info = infoHandler.GetComponent<InfoHandler> ();
		elementButtons = GameObject.FindGameObjectsWithTag ("UIButton");
	}

	public void OnTriggerEnter(Collider fingerCollider) {
		if (fingerCollider.gameObject.name == "bone3") { //bone3 is part of indexfinger that triggers collision
			indexFinger = fingerCollider.gameObject;
			findSmallestDistance = true;

			//change on-/off-distance of proximitydetectors (to ease atom-pinching within triggerArea):
			for (int i = 0; i < proximityDetectors.Length; i++) {
				/*proximityDetectors [i].OnDistance = 0.035f;
				proximityDetectors [i].OffDistance = 0.04f;*/

				proximityDetectors [i].OnDistance = 0.04f;
				proximityDetectors [i].OffDistance = 0.045f;
			}
		}
	}
	public void OnTriggerStay(Collider fingerCollider) {
		if (findSmallestDistance == true && indexFinger != null && fingerCollider.gameObject.name == "bone3") {
			elementsByDistance = GetElementButtonsSortedByDistance ();
			ShowElementButtonWithSmallestDistance (elementsByDistance [0].elementButton);
		}
	}
	public void OnTriggerExit(Collider fingerCollider) {
		if (fingerCollider.gameObject.name == "bone3") {
			findSmallestDistance = false;
			indexFinger = null;

			//change on-/off-distance of proximitydetectors back to origin:
			for (int i = 0; i < proximityDetectors.Length; i++) {
				proximityDetectors [i].OnDistance = 0.02f;
				proximityDetectors [i].OffDistance = 0.025f;
			}
		}
	}

	private List<ElementButtonByDistance> GetElementButtonsSortedByDistance() {
		List<ElementButtonByDistance> buttonList = new List<ElementButtonByDistance> ();
		float actualDistance;
		for (int i = 0; i < elementButtons.Length; i++) {
			actualDistance = Vector3.Distance (indexFinger.transform.position, elementButtons [i].transform.position);
			buttonList.Add (new ElementButtonByDistance (elementButtons [i], actualDistance));
		}
		SortElementButtonsByDistance (buttonList);
		return buttonList;
	}
	private void SortElementButtonsByDistance(List<ElementButtonByDistance> list) {
		for (int i = 1; i < list.Count; i++) {
			for (int j = 0; j < (list.Count - i); j++) {
				if (list [j].distance > list [j + 1].distance) {
					ElementButtonByDistance tempElement = list [j];
					list [j] = list [j + 1];
					list [j + 1] = tempElement;
				}
			}
		}
	}

	private void ShowElementButtonWithSmallestDistance(GameObject currentElementButton) {
		if (currentElementButton != elementButtonWithSmallestDistance && elementButtonWithSmallestDistance != null) {
			SetChildrenVisibleState (false, elementButtonWithSmallestDistance);
		}
		SetChildrenVisibleState (true, currentElementButton);
		elementButtonWithSmallestDistance = currentElementButton;
	}
	private void SetChildrenVisibleState(bool visibleState, GameObject button) {
		//only one infotext can be shown
		if (visibleState == false) {
			foreach (Transform component in button.transform) {
				component.gameObject.SetActive (visibleState);
			}
		}
		if (visibleState == true) {
			foreach (Transform component in button.transform) {
				component.gameObject.SetActive (visibleState);
			}
		}
	}

	private class ElementButtonByDistance {
		public GameObject elementButton;
		public float distance;
		public ElementButtonByDistance(GameObject actualElement, float actualDistance) {
			elementButton = actualElement;
			distance = actualDistance;
		}
	}
}
