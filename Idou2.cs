﻿using UnityEngine;
using System.Collections;
using Novel;

public class Idou2: MonoBehaviour {

	// Use this for initialization
	void Start () {


	}

	// Update is called once per frame
	void Update () {

	}

	void OnCollisionEnter(Collision collision)
	{
		if (collision.gameObject.name == "Player" +
			"")
		{
			//Application.LoadLevel("veeee");
			NovelSingleton.StatusManager.callJoker("wide/scene2", "");
		}
	}
}