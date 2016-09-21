using UnityEngine;
using System.Collections;

public class CamScript : MonoBehaviour {


	public float curXRotation = 0f;

	Vector3 refPos;
	Quaternion refRot;

	// Use this for initialization
	void Start () {
		refPos = transform.localPosition;
		refRot = transform.localRotation;
	}
	
	// Update is called once per frame
	void Update () {
		transform.localPosition = refPos;
		transform.localRotation = refRot;
		print (Input.GetAxis ("CamRotationX"));
		curXRotation -= Input.GetAxis ("CamRotationX") * Time.deltaTime * 100f;

		transform.RotateAround (transform.parent.position, Vector3.up, curXRotation);
	
	}
}
