using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Leap;
using Leap.Unity;
using Leap.Unity.Attributes;

/*resources: 
* https://docs.unity3d.com/ScriptReference/
* https://docs.unity3d.com/Manual/index.html
* https://developer.leapmotion.com/documentation/unity/namespaces.html
* https://github.com/leapmotion/UnityModules/wiki
* https://github.com/leapmotion/UnityModules/tree/1c6646cda7d0a71915f3907c3e930b9fcb874826/Assets/LeapMotion/Core/Scripts/DetectionUtilities
* http://gestis.itrust.de/nxt/gateway.dll/gestis_de/000000.xml?f=templates&fn=default.htm&vid=gestisdeu:sdbdeu
*/

public class TaskCompletionCheck : MonoBehaviour {

	/*This class checks if the constructed molecule is correct (due to tasks - the prototype can only check for ammoniac, methylamine, methanole).
	*Therefore both thumbs need to be upturned (ExtendedFingerDetector and FingerDirectionDetector via DetectorLogicGate).
	*When both thumbs are recognized, the molecule will be checked. If construction is correct, the UI prints out positive message, if it is not correct, the UI prints out negative message.*/

	//**************************************************************
    //detectors && handattachments:
	//-------------------------------
    [SerializeField]
	private DetectorLogicGate[] ThumbsUpDetectors;
	private int left = 0;
	private int right = 1;
    
    //components:
	//-------------------------------
	private GameObject task;
	private GameObject[] allMoleculesInHierarchy;
	private List<GameObject> qualifiedMolecules;
	private GameObject finalMolecule;
	private GameObject[] allAtoms;

	private GameObject taskCompletion;
	private GameObject taskCorrect;
	private GameObject taskIncorrect;
	private GameObject ReportAtomNum;
	private GameObject ReportMoleculeAmbiguity;
	private GameObject ReportNoMolecule;
	//**************************************************************

	void Start() {
		task = GameObject.FindGameObjectWithTag ("Task");
		taskCompletion = GameObject.FindGameObjectWithTag ("TaskCompletion");
		taskCorrect = taskCompletion.transform.GetChild (0).gameObject;
		taskIncorrect = taskCompletion.transform.GetChild (1).gameObject;

		ReportAtomNum = taskIncorrect.transform.GetChild (3).gameObject;
		ReportMoleculeAmbiguity = taskIncorrect.transform.GetChild (4).gameObject;
		ReportNoMolecule = taskIncorrect.transform.GetChild (5).gameObject;

		qualifiedMolecules = new List<GameObject> ();
	}

	public void CheckIfBothThumbsAreUp() {
		if (ThumbsUpDetectors [left].IsActive == true && ThumbsUpDetectors [right].IsActive == true) {
			OnThumbsUpDetected ();
		}
	}

	public void CheckIfBothThumbsAreDown() {
		if (ThumbsUpDetectors [left].IsActive == false && ThumbsUpDetectors [right].IsActive == false) {
			ReportMoleculeAmbiguity.SetActive (false);
			ReportAtomNum.SetActive (false);
			ReportNoMolecule.SetActive (false);

			taskCorrect.SetActive (false);
			taskIncorrect.SetActive (false);
		}
	}

	private void OnThumbsUpDetected() {
		qualifiedMolecules.Clear ();

		allMoleculesInHierarchy = GameObject.FindGameObjectsWithTag ("Molecule");
		if (allMoleculesInHierarchy.Length == 1 && allMoleculesInHierarchy[0].name == "MoleculeOrigin") {
			taskIncorrect.SetActive (true);
			//taskcompletion is INCORRECT: there is no molecule to test
			ReportNoMolecule.SetActive (true);

		} else if (allMoleculesInHierarchy.Length > 1) {
			for (int i = 0; i < allMoleculesInHierarchy.Length; i++) {
				if (allMoleculesInHierarchy [i].activeInHierarchy == true && allMoleculesInHierarchy [i].transform.parent == null && allMoleculesInHierarchy[i].transform.name != "MoleculeOrigin") {
					qualifiedMolecules.Add (allMoleculesInHierarchy [i]);
				}
			}

			if (qualifiedMolecules.Count == 1) { //for completion there has to be only one molecule in the hierarchy
				finalMolecule = qualifiedMolecules [0];
                
				switch (task.name) {
				case "Ammoniak":
					CheckForAmmoniac ();
					break;
				case "Methanol":
					CheckForMethanol ();
					break;
				case "Methylamin":
					CheckForMethylamine();
					break;
				}
			} else {
				taskIncorrect.SetActive (true);
				ReportMoleculeAmbiguity.SetActive (true);
                //taskcompletion is INCORRECT: more than one molecule - tested molecule needs to be clear
			}
		}
	}

	private void CheckForAmmoniac() {
		int numOfNitrogen = 1;
		int numOfHydrogen = 3;
		
		FindAllAtoms();
        
		int actualNumOfAtomsInMolecule = FindAtomCountInMolecule ();
		if (actualNumOfAtomsInMolecule == (numOfNitrogen + numOfHydrogen)) {

			int actualNumOfNitrogen = FindSpecificAtomCountInMolecule ("N");
			int actualNumOfHydrogen = FindSpecificAtomCountInMolecule ("H");

			if (actualNumOfNitrogen == numOfNitrogen && actualNumOfHydrogen == numOfHydrogen) {
				taskCorrect.SetActive (true); //taskcompletion is CORRECT
			} else {
				taskIncorrect.SetActive (true); //taskcompletion is INCORRECT: number of atoms included is not correct
				ReportAtomNum.SetActive (true);
			}
		} else {
			taskIncorrect.SetActive (true); //taskcompletion is INCORRECT: number of atoms included is not correct
			ReportAtomNum.SetActive (true);
		}
	}
    
    private void CheckForMethanol() {
		int numOfOxygen = 1;
		int numOfHydrogen = 4;
		int numOfCarbon = 1;
		
		FindAllAtoms();

		int actualNumOfAtomsInMolecule = FindAtomCountInMolecule ();
		if (actualNumOfAtomsInMolecule == (numOfOxygen + numOfHydrogen+ numOfCarbon)) {
			int actualNumOfOxygen = FindSpecificAtomCountInMolecule ("O");
			int actualNumOfHydrogen = FindSpecificAtomCountInMolecule ("H");
			int actualNumOfCarbon = FindSpecificAtomCountInMolecule ("C");

			if (actualNumOfOxygen == numOfOxygen && actualNumOfHydrogen == numOfHydrogen && actualNumOfCarbon == numOfCarbon) {
				taskCorrect.SetActive (true); //taskcompletion is CORRECT
			} else {
				taskIncorrect.SetActive (true); //taskcompletion is INCORRECT: number of atoms included is not correct
				ReportAtomNum.SetActive (true);
			}
		} else {
			taskIncorrect.SetActive (true); //taskcompletion is INCORRECT: number of atoms included is not correct
			ReportAtomNum.SetActive (true);
		}
	}

	private void CheckForMethylamine() {
		int numOfNitrogen = 1;
		int numOfHydrogen = 5;
		int numOfCarbon = 1;

		FindAllAtoms();

		int actualNumOfAtomsInMolecule = FindAtomCountInMolecule ();
		if (actualNumOfAtomsInMolecule == (numOfNitrogen + numOfHydrogen+ numOfCarbon)) {
			int actualNumOfNitrogen = FindSpecificAtomCountInMolecule ("N");
			int actualNumOfHydrogen = FindSpecificAtomCountInMolecule ("H");
			int actualNumOfCarbon = FindSpecificAtomCountInMolecule ("C");

			if (actualNumOfNitrogen == numOfNitrogen && actualNumOfHydrogen == numOfHydrogen && actualNumOfCarbon == numOfCarbon) {
				taskCorrect.SetActive (true); //taskcompletion is CORRECT
			} else {
				taskIncorrect.SetActive (true); //taskcompletion is INCORRECT: number of atoms included is not correct
				ReportAtomNum.SetActive (true);
			}
		} else {
			taskIncorrect.SetActive (true); //taskcompletion is INCORRECT: number of atoms included is not correct
			ReportAtomNum.SetActive (true);
		}
	}

	private void FindAllAtoms() {
		allAtoms = GameObject.FindGameObjectsWithTag ("Atom");
	}

	private int FindAtomCountInMolecule() {
		int countAtomIncluded = 0;
		for (int i = 0; i < allAtoms.Length; i++) {
			if (allAtoms[i].transform.root.gameObject == finalMolecule) {
				countAtomIncluded = countAtomIncluded + 1;
			}
		}
		return countAtomIncluded;
	}

	private int FindSpecificAtomCountInMolecule(string atomId) {
		int countAtom = 0;
		for (int i = 0; i < allAtoms.Length; i++) {
			if (allAtoms [i].name.Substring (0, 1) == atomId && allAtoms[i].transform.root.gameObject == finalMolecule) {
				countAtom = countAtom + 1;
			}
		}
		return countAtom;
	}
}
