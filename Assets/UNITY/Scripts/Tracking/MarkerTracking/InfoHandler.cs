using UnityEngine;
using System.Collections;
using UnityEngine.UI;

/*resources: 
* https://docs.unity3d.com/ScriptReference/
* https://docs.unity3d.com/Manual/index.html
* https://docs.unity3d.com/ScriptReference/Collider.html
*/

public class InfoHandler : MonoBehaviour {

	//This class handles the visability of info-data from the periodic table.

	//************************************
    //visual feedback:
	//-------------------------------
	[SerializeField]
	private Material[] infoStateMaterial;
	//************************************

	public void ActivateInfo(GameObject infoSign, GameObject infoText) {
		infoSign.transform.FindChild ("InfoSign_Color").GetComponent<RawImage> ().material = infoStateMaterial [1];
		infoText.SetActive(true);
	}
	public void DeactivateInfo(GameObject infoSign, GameObject infoText) {
		infoSign.transform.FindChild ("InfoSign_Color").GetComponent<RawImage> ().material = infoStateMaterial [0];
		infoText.SetActive(false);
	}

	public void OnTriggerEnter(Collider fingerCollider) {
		if (fingerCollider.gameObject.name == "bone3") { //bone3 is part of indexfinger that triggers collision
			GameObject infoSign = this.transform.GetChild (1).gameObject;
			GameObject infoText = this.transform.GetChild (2).gameObject;

			if (infoText.activeInHierarchy == false) {
				ActivateInfo (infoSign, infoText);

			} else if (infoText.activeInHierarchy == true) {
				DeactivateInfo (infoSign, infoText);
			}
		}
	}
}
