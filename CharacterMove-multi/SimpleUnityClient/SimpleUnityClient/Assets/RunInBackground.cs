using UnityEngine;
using System.Collections;

public class RunInBackground : MonoBehaviour {

	// Use this for initialization
	void Start () {
        // for testing multiplayer works in one desktop.
        Application.runInBackground = true;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
