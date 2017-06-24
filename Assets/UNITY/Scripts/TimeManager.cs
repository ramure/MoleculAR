using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/*resources: 
* https://docs.unity3d.com/ScriptReference/
* https://docs.unity3d.com/Manual/index.html
* https://docs.unity3d.com/ScriptReference/Coroutine.html
* https://docs.unity3d.com/Manual/Coroutines.html
* https://www.youtube.com/watch?v=f6dEj7-G-Fw
*/

public class TimeManager : MonoBehaviour {

	/*This class represents a timer that can be called by important working processes (other classes) to show and count down before starting next process-steps.
	*Counting-down is represented by a circular progressbar in cyan or red coloring (cyan when only one atom is selected (e.g. bonding-process) and red when two or more atoms are selected (e.g. deletion-process)).
	*The timer can be stopped at any time and executes transfered actions when it finishes (due to coroutine-action). */

	//**************************************************************
    //components:
	//-------------------------------
	private Action callbackAction;                                     //action that will be executed when timer finishes
	private IEnumerator coroutine;
	private List<GameObject> countDownAtomTimers;                      //each acting atom has its own timer
	
    //chronological logic:
	//-------------------------------
    private bool coroutineActive = true;
    private bool countingDown = false;
	private bool timerFinished = false;

    //visual feedback:
	//-------------------------------
	[SerializeField]
	private Material[] loadingBarColors;                               //material for timer-counting (highlights for visual feedback)
	private Material actualLoadingBarColor;                            //registered material (0: blue, 1: red)
    
    private float currentAmount = 0;
	private float speed = 70.0f;
	//**************************************************************

	void Start() {
		coroutineActive = true;
		countDownAtomTimers = new List<GameObject> ();
	}
	
	public void startTimer(List<GameObject> atoms, string state, Action onTimerFinishedCallback = null) {
		callbackAction = onTimerFinishedCallback;

		int loadingState = -1;
		if (state == "Construction") {
			loadingState = 0;
		} else if (state == "Destruction") {
			loadingState = 1;
		}
		foreach (GameObject atom in atoms) {
			actualLoadingBarColor = loadingBarColors[loadingState]; //construction shows blue (0) progressbar, destruction shows red (1) progressbar
			if (atom != null) {
				countDownAtomTimers.Add(atom.transform.Find("CountDownCanvas").GetChild(0).GetChild(0).gameObject);
			}
		}
		if (countDownAtomTimers != null) {
			ManageTransparency(countDownAtomTimers, 1.0f);
			
            //start counting-process (coroutine):
			coroutineActive = true;
			coroutine = CoroutineTimeCounting();
			StartCoroutine(coroutine);
		}
	}

	private void ManageTransparency(List<GameObject> timers, float alphaValue) {
		foreach (GameObject timer in timers) {
			GameObject actualTimer = timer;
			actualTimer.transform.GetComponent<Image>().material = actualLoadingBarColor;
			
			Image loadingBar = actualTimer.transform.GetComponent<Image>();
			Color loadingColor = loadingBar.color;
			loadingColor.a = alphaValue;
			loadingBar.color = loadingColor;
		}
	}

	void Update() {
		if (coroutineActive == false) {
			StopCoroutine (coroutine);
		}
		if (countingDown == true) {
			if (currentAmount < 100) {
				currentAmount = currentAmount + speed * Time.deltaTime;
			} else { //timer has finished
				countingDown = false;
				foreach (GameObject countDownAtomTimer in countDownAtomTimers) {
					countDownAtomTimer.transform.GetComponent<Image>().fillAmount = 0;
				}
				currentAmount = 0;
				timerFinished = true;
				ManageTransparency (countDownAtomTimers, 0.0f);
			}
			foreach (GameObject countDownAtomTimer in countDownAtomTimers) {
				countDownAtomTimer.transform.GetComponent<Image>().fillAmount = currentAmount / 100; //modulate progressbar (counting)
			}
		}
	}

	public void stopTimer() {
		if (coroutine != null) {
			coroutineActive = false;
		}
		if (countDownAtomTimers != null) {
			countingDown = false;
			timerFinished = false;
			currentAmount = 0;
			foreach (GameObject countDownAtomTimer in countDownAtomTimers) {
				countDownAtomTimer.transform.GetComponent<Image>().fillAmount = 0; //set progressbar to origin (counting)
			}
			ManageTransparency (countDownAtomTimers, 0.0f);
		}
		countDownAtomTimers.Clear ();
	}

	IEnumerator CoroutineTimeCounting() {
		countingDown = true;
		while (timerFinished == false) {
			yield return null; //coroutine is lasting until timer has finished
		}
		countDownAtomTimers.Clear ();
		if (timerFinished == true) {
			timerFinished = false;
			if (callbackAction != null) {
				callbackAction ();
			}
		}
	}
}
