using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour {
    public GameObject a;
    public GameObject b;

    public bool toggleOn;
	// Update is called once per frame
	void Update () {
        if (Input.GetMouseButtonDown(0)) {
            toggleOn = !toggleOn;

            b.SetActive(toggleOn);
            a.SetActive(!toggleOn);
        }	
	}
}
