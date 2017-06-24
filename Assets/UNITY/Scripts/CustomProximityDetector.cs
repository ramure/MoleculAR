using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using Leap.Unity.Attributes;

/*resources: 
* https://docs.unity3d.com/ScriptReference/
* https://docs.unity3d.com/Manual/index.html
* https://github.com/leapmotion/UnityModules/blob/1c6646cda7d0a71915f3907c3e930b9fcb874826/Assets/LeapMotion/Core/Scripts/DetectionUtilities/ProximityDetector.cs
*/

namespace Leap.Unity{

	public class CustomProximityDetector : ProximityDetector {
		
		/*This class inherits from Leap-ProximityDetector and overrides and expands rooted facilities.
		*Because originally all objects that could be detected by proximitydetectors were registered when starting, newly generated objects (per runtime) couldn't be perceived by proximitydetectors. 
		*Therefore CustomProximityDetector searches for all (even newly generated) tagged objects in the game-scene and reacts to them.*/

		private List<GameObject> targetsOnUpdate;

		void Update() {
			if (TagName != "") {
				GameObject[] allTaggedObjectsOnUpdate = GameObject.FindGameObjectsWithTag (TagName);
				targetsOnUpdate = new List<GameObject> ();

				if (allTaggedObjectsOnUpdate.Length != TargetObjects.Length) {
					for (int i = 0; i < TargetObjects.Length; i++) {
						targetsOnUpdate.Add (TargetObjects [i]);
					}
					for (int i = 0; i < allTaggedObjectsOnUpdate.Length; i++) {
						int counter = 0;
						GameObject newTargetOnUpdate = allTaggedObjectsOnUpdate [i];

						for (int j = 0; j < TargetObjects.Length; j++) {
							if (newTargetOnUpdate == TargetObjects [j]) {
								counter = counter + 1;
							}
						}
						if (counter == 0) {
							targetsOnUpdate.Add (newTargetOnUpdate);
						}
					}
					TargetObjects = targetsOnUpdate.ToArray ();
					targetsOnUpdate.Clear ();
				}
			}
		}
	}
}