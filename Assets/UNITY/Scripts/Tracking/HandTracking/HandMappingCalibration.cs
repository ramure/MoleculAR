using UnityEngine;
using System.Collections;

/*resources: 
* https://docs.unity3d.com/ScriptReference/
* https://docs.unity3d.com/Manual/index.html
*/

public class HandMappingCalibration : MonoBehaviour {

	/*This class handles the mapping of real hands and virtual hands (depending on fixed distance of approximately 6 cm between the center of the marker and the center of the leap motion controller (lmc))
	*Therefore the position and rotation of virtual hand-models can be mapped on real hands.*/

	//************************************
    //components:
	//-------------------------------
	[SerializeField]
	private GameObject LMCMarker;
	private Vector3 LMCPosition;
	private Quaternion LMCRotation;

	[SerializeField]
	private GameObject LMCOrigin;				//LeapHandController + HandModels
    
    //chronological logic:
	//-------------------------------
	private bool mapping = false;
	//************************************

	void Start() {
		LMCOrigin.SetActive (false);
	}

	public void InitializeMapping() {
		mapping = true;
		LMCOrigin.SetActive (true);
	}

	void Update () {
		if (mapping == true) {
			//marker for leapmotion (hand-positioning)
			LMCPosition = LMCMarker.transform.position;
			LMCRotation = LMCMarker.transform.rotation;

            //mapping leap-hands with real-hands (calculate small offset because of experimental set-up)
			LMCPosition.z = LMCPosition.z - 0.06f;

			//hand-positioning (map leap-hands according to marker-position)
			LMCOrigin.transform.position = LMCPosition;
			LMCOrigin.transform.rotation = LMCRotation;
		}
	}
}
