using UnityEngine;
using System.Collections;

/*resources: 
* http://wiki.unity3d.com/index.php?title=CameraFacingBillboard
*/

public class FacingCameraManager : MonoBehaviour {

	void Update () {
		transform.LookAt (transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
	}
}
