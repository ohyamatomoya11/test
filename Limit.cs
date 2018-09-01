﻿using UnityEngine;
using System.Collections;

public class Limit : MonoBehaviour {

	public float life_time = 8.5f;
	float time = 0f;

	// Use this for initialization
	void Start () {
		time = 0;
	}

	// Update is called once per frame
	void Update () {
		time += Time.deltaTime;
		print (time);
		if(time>life_time){
			Destroy(gameObject);
		}
	}
}