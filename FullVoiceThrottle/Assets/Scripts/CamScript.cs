using UnityEngine;
using System.Collections;

public class CamScript : MonoBehaviour {


	public float curXRotation = 0f;
	float baseZPos = 0f;
	public Vehicle veh;
	Camera cam;
	public float acc_fov_factor = 1f;

	Vector3 refPos;
	Quaternion refRot;

	float baseFov;

	// Use this for initialization
	void Start ()
	{
		
		cam = GetComponent<Camera>();
		baseFov = cam.fieldOfView;
		refPos = transform.localPosition;
		refRot = transform.localRotation;
	}
	
	// Update is called once per frame
	void Update () {
		transform.localPosition = refPos;
		transform.localRotation = refRot;
//		print (Input.GetAxis ("CamRotationX"));
		curXRotation -= Input.GetAxis ("CamRotationX") * Time.deltaTime * 100f;

		transform.RotateAround (transform.parent.position, Vector3.up, curXRotation);

//		Debug.Log(cam.fieldOfView);
//		Debug.Log(baseFov + veh.localAcceleration.z * acc_fov_factor);
//		Debug.Log(Time.deltaTime);
		cam.fieldOfView = Mathf.Lerp(cam.fieldOfView,baseFov + Mathf.Max(0f,veh.localAcceleration.z) * acc_fov_factor, Time.deltaTime * 0.2f);
	
	}
}
