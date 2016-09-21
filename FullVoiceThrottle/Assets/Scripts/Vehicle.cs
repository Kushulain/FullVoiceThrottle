using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Wheel
{
	public WheelCollider collider;
	public Transform mesh;
	public float HandBrakeFactor;
	public float brakeFactor;
	public float steering;
	public float engineFactor;
	public float rotation;

	public float base_side_stiffness;
}

[System.Serializable]
public class Gear
{
	public float ratio;
	public float gasTolerance;
}

public class Vehicle : MonoBehaviour {

	public List<Wheel> wheels = new List<Wheel>();
	public List<Gear> gears = new List<Gear>();
	public AnimationCurve engineEfficiency = new AnimationCurve();
	public Vector3 localVelocity;
	public Vector3 localVelocity_kmh;
	public Vector3 localVelocity_mph;

	public float input_gasPedal = 0f;
	public float input_Steering = 0f;
	public float input_Brake = 0f;
	public float input_HandBrake = 0f;
	public float input_Clutch = 0f;
	public bool input_GearUp = false;
	public bool input_GearDown = false;
	public float steeringSensivity = 2f;

	public float brakeTorque = 800f;
	public float handBrakeTorque = 2000f;
	public float engineTorque = 1000f;
	public float engineInertia = 1f;

	public int currentGear = 0;

	public float motorRPM = 0f;
	public float wheelMotorRPM = 0f;
	public float fakeWheelMotorRPM = 0f;
	public float wantedRPM = 0f;
	public float minWantedRPM = 1000f;
	public float maxWantedRPM = 5550f;


	
	public float sound_ratioPitch_RPM = 0.001f;
	public float roadSkidVolume = 0.5f;

	public Vector3 gravCenter = new Vector3 (0f, 0.2f, 1.3f);

	int motorWheel = 0;

	List<AudioSource> enginAudio = new List<AudioSource>();

	public List<ParticleSystem> Particles = new List<ParticleSystem>();

	public float finalTime = -1f;
	public float malus = 0f;


	// Use this for initialization
	void Start () {

		enginAudio.Add (GetComponents<AudioSource> () [0]);
		enginAudio.Add (GetComponents<AudioSource> () [1]);

		for (int i=0; i<wheels.Count; i++)
		{
			wheels[i].base_side_stiffness = wheels[i].collider.sidewaysFriction.stiffness;
			wheels[i].mesh = wheels[i].collider.transform.GetChild(0);
			wheels[i].collider.motorTorque = 0f;
			wheels[i].collider.GetComponent<AudioSource>().time = Random.Range(0f,wheels[i].collider.GetComponent<AudioSource>().clip.length);
			if (wheels[i].engineFactor > 0.5f)
			{
				motorWheel = i;
			}
		}

		GetComponent<Rigidbody>().centerOfMass = gravCenter;
	}
	
	void FixedUpdate()
	{
		localVelocity = transform.InverseTransformDirection(GetComponent<Rigidbody>().velocity);
		localVelocity_kmh = localVelocity * 0.001f * 3600f;
		localVelocity_mph = localVelocity_kmh / 1.609f;

				
//		foreach (Wheel wheel in wheels)
//		{
//			if (wheel.engineFactor > 0f && wheel.collider.isGrounded)
//			{
//				float wheelSpeed = (wheel.collider.rpm * wheel.collider.radius * Mathf.PI * 2f) / 60f;
//				wheelSpeed *= Vector3.Dot(wheel.mesh.right,transform.right) < 0f ? -1f : 1f;
//
//				WheelFrictionCurve WFC = wheel.collider.sidewaysFriction;
//
//				WFC.stiffness = wheel.base_side_stiffness / 
//					Mathf.Max(1f,Mathf.Abs(wheelSpeed
//					           - localVelocity.z ) * 1f);
//
//				wheel.collider.sidewaysFriction = WFC;
//			}
//		}

	}
	
	// Update is called once per frame
	void Update () {
		GatherInputs ();
		UpdateGears ();
		UpdateWheelMesh ();
		UpdateWheelBrake ();
		UpdateWheelSteering ();
		UpdateWheelTorque ();
		UpdateSound ();

		
		float wheelSpeed = (wheels[motorWheel].collider.rpm * wheels[motorWheel].collider.radius * Mathf.PI * 2f) / 60f;
		wheelSpeed *= Vector3.Dot(wheels[motorWheel].mesh.right,transform.right) < 0f ? -1f : 1f;

		float skidZ = Mathf.Abs (wheelSpeed
						- localVelocity.z);

		if (!wheels [motorWheel].collider.isGrounded)
						skidZ = 0f;

		foreach (ParticleSystem PS in Particles)
		{
			if (skidZ>2f)
				PS.Play();
			else
				PS.Stop();
		}

		if (Input.GetAxis("RestartLevel") > 0f)
		{
			Application.LoadLevel(Application.loadedLevel);
		}
	}



	void UpdateGears()
	{
		if (input_GearUp)
		{
			currentGear++;
			currentGear = Mathf.Min (currentGear,gears.Count-1);
		}
		if (input_GearDown)
		{
			currentGear--;
			currentGear = Mathf.Max (currentGear,0);
		}

		if (currentGear == 1)
		{
			input_Clutch = 1f;
		}
	}

	void UpdateSound()
	{
		enginAudio[0].pitch = Mathf.Abs(motorRPM * sound_ratioPitch_RPM);
		enginAudio [0].volume = Mathf.Abs (wheels [motorWheel].collider.motorTorque / 4000f) + (motorRPM/maxWantedRPM) * 0.5f;
		enginAudio[1].pitch = Mathf.Abs(motorRPM * sound_ratioPitch_RPM);
		//enginAudio [0].volume = Mathf.Abs (wheels [i].collider.motorTorque / 1000f);

		foreach (Wheel wheel in wheels)
		{
			if (wheel.collider.isGrounded)
			{

				float wheelSpeed = (wheel.collider.rpm * wheel.collider.radius * Mathf.PI * 2f) / 60f;
				wheelSpeed *= Vector3.Dot(wheel.mesh.right,transform.right) < 0f ? -1f : 1f;

				wheel.collider.GetComponent<AudioSource>().volume =  Mathf.Abs(wheelSpeed
												- localVelocity.z ) * 0.2f + 
												Mathf.Abs(localVelocity.x) * 0.1f;

				wheel.collider.GetComponent<AudioSource>().pitch = wheel.collider.GetComponent<AudioSource>().volume * 0.2f + 1f;

				wheel.collider.GetComponent<AudioSource>().volume *= roadSkidVolume;
			}
			else
			{
				wheel.collider.GetComponent<AudioSource>().volume = Mathf.Lerp(wheel.collider.GetComponent<AudioSource>().volume,0f,0.1f);
			}
		}

	}

	void UpdateWheelTorque()
	{
//		wantedRPM = Mathf.Lerp (minWantedRPM, maxWantedRPM , input_gasPedal);
//
//		wheelMotorRPM = (wheels[motorWheel].collider.rpm * gears [currentGear].ratio * (1f/wheels[motorWheel].collider.radius));
//		fakeWheelMotorRPM += Mathf.Clamp((wantedRPM-fakeWheelMotorRPM),
//			-Time.deltaTime * engineInertia * maxWantedRPM,
//			Time.deltaTime * engineInertia * maxWantedRPM);
//
//		float engineCommand = wantedRPM - wheelMotorRPM;
//
//		if (engineCommand > 0f)
//		{
//			engineCommand /= maxWantedRPM;
//			engineCommand = Mathf.Pow(maxWantedRPM,2f);
//		}
//		else
//		{
//			engineCommand /= maxWantedRPM;
//			engineCommand = Mathf.Pow(-maxWantedRPM,2f);
//			engineCommand = -maxWantedRPM;
//		}
//		engineCommand = Mathf.Clamp(engineCommand,-1f,1f);
//		motorRPM = (1f-input_Clutch) * (wheelMotorRPM) + 
//			input_Clutch * fakeWheelMotorRPM;
//		
//		for (int i=0; i<wheels.Count; i++)
//		{
//			float engineEff = ((motorRPM/maxWantedRPM) < 1f ? engineEfficiency.Evaluate(motorRPM/maxWantedRPM) : 2f);
//			Debug.Log("engineEff : " + engineEff);
//			float curTorque = wheels[i].engineFactor * gears[currentGear].ratio * engineEff;
//			Debug.Log("curTorque1 : " + curTorque);
//			float way = Vector3.Dot(wheels[i].mesh.right,transform.right) < 0f ? -1f : 1f;
//			//wheels[i].collider.motorTorque = (curTorque *  wheels[i].engineFactor) * 0.9f + newTorque * 0.1f;
//
//			curTorque *= way * engineCommand *  (1f-input_Clutch) * (1f/wheels[i].collider.radius);
//			Debug.Log("curTorque2 : " + curTorque);
//			curTorque *= engineTorque;
//			wheels[i].collider.motorTorque = curTorque;
//
//			Debug.Log("motorTorque : " + wheels[i].collider.motorTorque);
//
//		}



		wantedRPM = Mathf.Lerp (minWantedRPM, maxWantedRPM , input_gasPedal);
		//wantedRPM = (5500.0f * input_gasPedal) * 0.1f + wantedRPM * 0.9f;
		//motorRPM = 0.95f * motorRPM + 0.05f * (-wheels[motorWheel].collider.rpm * gears[currentGear].ratio);
		wheelMotorRPM = (wheels [motorWheel].collider.rpm * gears [currentGear].ratio * (1f/wheels[motorWheel].collider.radius));
		//Debug.Log(wheelMotorRPM);
		fakeWheelMotorRPM = Mathf.Lerp(minWantedRPM, maxWantedRPM, input_gasPedal);
		
		float calculatedMotorRPM = (1f-input_Clutch) * (wheelMotorRPM) + 
									input_Clutch * fakeWheelMotorRPM ;


//		motorRPM = calculatedMotorRPM;
		//Debug.Log((wantedRPM - wheelMotorRPM)/gears[currentGear].gasTolerance);
//		motorRPM = Mathf.Lerp (motorRPM, calculatedMotorRPM, Time.deltaTime * engineInertia);

		float motorRPMDelta = calculatedMotorRPM - motorRPM;
		motorRPMDelta = Mathf.Clamp(motorRPMDelta,
			-Time.deltaTime * engineInertia * maxWantedRPM * Mathf.Abs(motorRPMDelta/maxWantedRPM),
			Time.deltaTime * engineInertia * maxWantedRPM * Mathf.Abs(motorRPMDelta/maxWantedRPM));
		
		motorRPM += motorRPMDelta;

		//float newTorque = engineTorque * gears [currentGear].ratio; // * efficiencyTable[index];

		for (int i=0; i<wheels.Count; i++)
		{
			float engineEff = ((motorRPM/maxWantedRPM) < 1f ? engineEfficiency.Evaluate(motorRPM/maxWantedRPM) : 2f);
			float curTorque = engineTorque * wheels[i].engineFactor * gears[currentGear].ratio * engineEff;
			float way = Vector3.Dot(wheels[i].mesh.right,transform.right) < 0f ? -1f : 1f;
			//wheels[i].collider.motorTorque = (curTorque *  wheels[i].engineFactor) * 0.9f + newTorque * 0.1f;


			wheels[i].collider.motorTorque = way * Mathf.Clamp(((wantedRPM - wheelMotorRPM)/gears[currentGear].gasTolerance),-1f,1f) * curTorque *  (1f-input_Clutch) * (1f/wheels[i].collider.radius);


//			if (i == motorWheel)
//				print (" wheel i:" + i + " troque : " + wheels[i].collider.motorTorque + " engineTorque : " + engineTorque
//					+ " engineTorque : " + engineTorque
//					+ " (wantedRPM - wheelMotorRPM)/gears[currentGear].gasTolerance : " + (wantedRPM - wheelMotorRPM)/gears[currentGear].gasTolerance
//					+ " wantedRPM : " + wantedRPM
//					+ " wheelMotorRPM : " + wheelMotorRPM
//					+ " (motorRPM/maxWantedRPM) : " + (motorRPM/maxWantedRPM)
//					+ " engineEfficiency.Evaluate(wheelMotorRPM/maxWantedRPM) : " + engineEfficiency.Evaluate(wheelMotorRPM/maxWantedRPM)
//					+ " gears[currentGear].gasTolerance : " + gears[currentGear].gasTolerance
//					+ " curTorque : " + curTorque
//					+ " (1f-input_Clutch) : " + (1f-input_Clutch)
//					+ " (1f/wheels[i].collider.radius) : " + (1f/wheels[i].collider.radius));


		}
	}

	void UpdateWheelBrake()
	{
		for (int i=0; i<wheels.Count; i++)
		{
			float brakeTorBuf = input_Brake * brakeTorque * wheels[i].brakeFactor;
			brakeTorBuf = Mathf.Max(brakeTorBuf, input_HandBrake * handBrakeTorque * wheels[i].HandBrakeFactor);
			wheels[i].collider.brakeTorque = brakeTorBuf;

			if (i == motorWheel && brakeTorBuf > 0)
			{
				input_Clutch = 1f;
			}
		}
	}

	void UpdateWheelSteering()
	{
		foreach (Wheel wheel in wheels)
		{
			wheel.collider.steerAngle = input_Steering * wheel.steering;
		}
	}

	void GatherInputs()
	{
		input_gasPedal = Input.GetAxis ("Gas");
		input_Steering = Mathf.Lerp(input_Steering, Input.GetAxis ("Steering") / Mathf.Max(1f,localVelocity.z* 0.1f), Time.deltaTime * steeringSensivity);
		input_Brake = Input.GetAxis ("Brake");
		input_HandBrake = Input.GetButton ("HandBrake") ? 1f : 0f;
		input_Clutch = Input.GetAxis ("Clutch");

		input_GearUp = Input.GetButtonDown ("GearUp");
		input_GearDown = Input.GetButtonDown ("GearDown");

	}

	void UpdateWheelMesh()
	{
		foreach (Wheel wheel in wheels)
		{
			Vector3 posBuf = wheel.mesh.localPosition;

			RaycastHit[] hits = Physics.RaycastAll(wheel.collider.transform.position,
			                                    -transform.up,
			                                    wheel.collider.suspensionDistance + wheel.collider.radius);
			int res = MathsAndOperations.GetClosestHit(hits,wheel.collider.transform.position);

			if (res != -1)
			{
				posBuf.y = wheel.collider.transform.InverseTransformPoint(hits[res].point).y + wheel.collider.radius;
				wheel.mesh.localPosition = posBuf;
			}
			else
			{
				posBuf.y = -wheel.collider.suspensionDistance;
				wheel.mesh.localPosition = posBuf;
			}

			wheel.rotation = Mathf.Repeat(wheel.rotation + Time.deltaTime * wheel.collider.rpm * 360.0f / 60.0f,360f);

			wheel.mesh.localRotation = Quaternion.Euler(wheel.rotation,
			                                            wheel.collider.steerAngle,
			                                            0.0f);

			//wheel.mesh.RotateAround(wheel.mesh.transform.position, transform.right, Time.deltaTime * wheel.collider.rpm * 360.0f / 60.0f);

		}
	}

	void OnDrawGizmos() {
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.TransformPoint(gravCenter), 0.4f);
	}

	
	public void OnGUI() {
		if (true) {
			// calculate actual speed in Km/H (SI metrics rule, so no inch, yard, foot,
			// stone, or other stupid length measure!)
			
			// message to display
			string msg = "Speed " + localVelocity_kmh.z.ToString("f0") + "Km/H, " + motorRPM.ToString("f0") + "RPM, gear " + (currentGear-1); //  + " torque " + newTorque.ToString("f2") + ", efficiency " + table[index].ToString("f2");
			
			GUILayout.BeginArea(new Rect(Screen.width -250 - 32, 32, 250, 40), GUI.skin.window);
			GUILayout.Label(msg);
			GUILayout.EndArea();


			if (finalTime == -1f)
			{
				GUILayout.BeginArea(new Rect( 32, 32, 400, 40), GUI.skin.window);
				GUILayout.Label(Time.timeSinceLevelLoad + malus + " s");
				GUILayout.EndArea();
			}
			else if (finalTime > 60f)
			{
				GUILayout.BeginArea(new Rect( 32, 32, 400, 40), GUI.skin.window);
				GUILayout.Label(finalTime + " s, c nul ! (r -> recommencer)");
				GUILayout.EndArea();
			}
			else if (finalTime > 50f)
			{
				GUILayout.BeginArea(new Rect( 32, 32, 400, 40), GUI.skin.window);
				GUILayout.Label(finalTime + " s, c pas trop mal ! (r -> recommencer)");
				GUILayout.EndArea();
			}
			else if (finalTime > 45f)
			{
				GUILayout.BeginArea(new Rect( 32, 32, 400, 40), GUI.skin.window);
				GUILayout.Label(finalTime + " s, oh yeah ! (r -> recommencer)");
				GUILayout.EndArea();
			}
			else if (finalTime > 40f)
			{
				GUILayout.BeginArea(new Rect( 32, 32, 400, 40), GUI.skin.window);
				GUILayout.Label(finalTime + " s, bravo tu gères! (r -> recommencer)");
				GUILayout.EndArea();
			}
			else if (finalTime > 35f)
			{
				GUILayout.BeginArea(new Rect( 32, 32, 400, 40), GUI.skin.window);
				GUILayout.Label(finalTime + " s, T TRO UN AS DU DRIFT KWA §§§ (r)");
				GUILayout.EndArea();
			}
			else if (finalTime > 33f)
			{
				GUILayout.BeginArea(new Rect( 32, 32, 400, 40), GUI.skin.window);
				GUILayout.Label(finalTime + " s, tu as battu le créateur de ce jeu... (r)");
				GUILayout.EndArea();
			}
			else
			{
				GUILayout.BeginArea(new Rect( 32, 32, 400, 40), GUI.skin.window);
				GUILayout.Label(finalTime + " s, tu triche ? (r)");
				GUILayout.EndArea();
			}
		}
	}
}
