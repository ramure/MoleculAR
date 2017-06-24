using UnityEngine;
using System.Collections;

/*resources: 
* https://docs.unity3d.com/ScriptReference/
* https://docs.unity3d.com/Manual/index.html
*/

public class TrackingHandler : MonoBehaviour {

	//This class checks if trackable targets are enabled.

	public bool GetTrackableState(GameObject marker) {
		bool trackableIsEnabled = false;
		Renderer[] renderers = marker.transform.GetComponentsInChildren<Renderer>(true);
		Collider[] colliders = marker.transform.GetComponentsInChildren<Collider>(true);

		foreach (Renderer renderer in renderers) {
			if (renderer.enabled == true) {
				trackableIsEnabled = true;
			} else {
				trackableIsEnabled = false;
			}
		}
		foreach (Collider collider in colliders) {
			if (collider.enabled == true) {
				trackableIsEnabled = true;
			} else {
				trackableIsEnabled = false;
			}
		}
		return trackableIsEnabled;
	} 
}
