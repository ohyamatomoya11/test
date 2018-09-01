using UnityEngine;
using System.Collections;

public class jumpsound : MonoBehaviour {

	private AudioSource Jump2;

	void Start () {
		//AudioSourceコンポーネントを取得し、変数に格納
		Jump2 = GetComponent<AudioSource>();
	}

	void Update () {
		//指定のキーが押されたら音声ファイル再生
		if(Input.GetKeyDown(KeyCode.Space)) {
			Jump2.PlayOneShot(Jump2.clip);
		}
	}
}

