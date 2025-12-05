using UnityEngine;
using System.Collections;

public class Variometer : MonoBehaviour {
	private AudioSource audioSrc;
	private Rigidbody player;

	// Use this for initialization
	void Start () {
		audioSrc = GetComponent<AudioSource> ();
		player = GameObject.Find ("Player").GetComponent<Rigidbody> ();
		audioSrc.Play ();
		audioSrc.pitch = 0;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void FixedUpdate(){
		if (player.linearVelocity.y > 0 && player.linearVelocity.y < 1000) {
			audioSrc.pitch = player.linearVelocity.y / 5;
		} else {
			audioSrc.pitch = 0;
		}
		//audio.Play ();
	}
}
