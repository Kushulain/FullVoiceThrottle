using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

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
	public float maxRPM;
}

public class Vehicle : MonoBehaviour {

	public List<Wheel> wheels = new List<Wheel>();
	public Transform drivingWheel;
	public List<Gear> gears = new List<Gear>();
	public AnimationCurve engineEfficiency = new AnimationCurve();
	public Vector3 localVelocity;
	public Vector3 localAcceleration;
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

//	List<AudioSource> enginAudio = new List<AudioSource>();
	public AudioSource skidingAudioS;
	public AudioSource engineAudioS;
//	public AudioSource micAudioS;

	public Micro mic;
//	public Animation animAiguille;

	public List<ParticleSystem> Particles = new List<ParticleSystem>();

	public float finalTime = -1f;
	public float malus = 0f;


	public RectTransform LatSkid;
	public RectTransform LongSkid;

	public Image Jauge;


	public Image nextGearMax;
	public Image nextGearMin;

	public Image curGearMax;
	public Image curGearMin;

	public Image prevGearMax;

	public float nextGearThreshold = 0.1f;
	public float prevGearThreshold = 0.1f;
	public float tolerance = 0.1f;

	public float lossGasSpeed = 1f;

	public float gearScale = 1f;


	public Animation animAiguille;

	CoolDownEvent GearDownDelay = new CoolDownEvent(0.5f);
	bool gearUpAllowed = false;
	bool gearDownAllowed = false;
	public bool rpmMiddle = false;


	public Text speedUI;
	public Text gearUI;

	public GameObject Win_GO;
	public GameObject Loose_GO;
	public Text Win_GO_Text;
	public Text Loose_GO_Text;
	public Animation animArrow;
	// Use this for initialization
	void Start () {

//		enginAudio.Add (GetComponents<AudioSource> () [0]);
//		enginAudio.Add (GetComponents<AudioSource> () [1]);


		animArrow["Aiguille"].speed = 0f;
		animAiguille["Aiguille"].speed = 0f;
//		animAiguille["Aiguille"].speed = 0f;
		mic = GetComponent<Micro>();
		Vector3 averagePos = Vector3.zero;
		for (int i=0; i<wheels.Count; i++)
		{
			averagePos += wheels[i].collider.transform.position;
			wheels[i].base_side_stiffness = wheels[i].collider.sidewaysFriction.stiffness;
//			wheels[i].mesh = wheels[i].collider.transform.GetChild(0);
			wheels[i].collider.motorTorque = 0f;
			wheels[i].collider.GetComponent<AudioSource>().time = Random.Range(0f,wheels[i].collider.GetComponent<AudioSource>().clip.length);
			if (wheels[i].engineFactor > 0.5f)
			{
				motorWheel = i;
			}
		}

		averagePos /= wheels.Count;

		GetComponent<Rigidbody>().centerOfMass = gravCenter ;
	}
	
	void FixedUpdate()
	{
		Vector3 prevVel = localVelocity;
		localVelocity = transform.InverseTransformDirection(GetComponent<Rigidbody>().velocity);
		localAcceleration = (localVelocity-prevVel) / Mathf.Max(0.001f, Time.fixedTime);
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



		speedUI.text = localVelocity_kmh.z.ToString("f0");
		gearUI.text = (currentGear-1).ToString();
		
//		float wheelSpeed = (wheels[motorWheel].collider.rpm * wheels[motorWheel].collider.radius * Mathf.PI * 2f) / 60f;
//		wheelSpeed *= Vector3.Dot(wheels[motorWheel].mesh.right,transform.right) < 0f ? -1f : 1f;
//
//		float skidZ = Mathf.Abs (wheelSpeed
//						- localVelocity.z);
//
//		if (!wheels [motorWheel].collider.isGrounded)
//						skidZ = 0f;
//
//		foreach (ParticleSystem PS in Particles)
//		{
//			if (skidZ>2f)
//				PS.Play();
//			else
//				PS.Stop();
//		}

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
		engineAudioS.pitch = Mathf.Abs(motorRPM * sound_ratioPitch_RPM);
		engineAudioS.volume = Mathf.Abs (wheels [motorWheel].collider.motorTorque / 4000f) + (motorRPM/maxWantedRPM) * 0.5f;
//		enginAudio[1].pitch = Mathf.Abs(motorRPM * sound_ratioPitch_RPM);
		//enginAudio [0].volume = Mathf.Abs (wheels [i].collider.motorTorque / 1000f);

//		foreach (Wheel wheel in wheels)
//		{
//			if (wheel.collider.isGrounded)
//			{
//
//				float wheelSpeed = (wheel.collider.rpm * wheel.collider.radius * Mathf.PI * 2f) / 60f;
//				wheelSpeed *= Vector3.Dot(wheel.mesh.right,transform.right) < 0f ? -1f : 1f;
//
//				wheel.collider.GetComponent<AudioSource>().volume =  Mathf.Abs(wheelSpeed
//												- localVelocity.z ) * 0.2f + 
//												Mathf.Abs(localVelocity.x) * 0.1f;
//
//				wheel.collider.GetComponent<AudioSource>().pitch = wheel.collider.GetComponent<AudioSource>().volume * 0.2f + 1f;
//
//				wheel.collider.GetComponent<AudioSource>().volume *= roadSkidVolume;
//			}
//			else
//			{
//				wheel.collider.GetComponent<AudioSource>().volume = Mathf.Lerp(wheel.collider.GetComponent<AudioSource>().volume,0f,0.1f);
//			}
//		}

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


//		Jauge.fillAmount = 0.028f +  0.625f * motorRPM/maxWantedRPM;
			
//		input_gasPedal = mic.freqPercent - motorRPM/maxWantedRPM;
//		Mathf.Clamp(input_gasPedal,-1f,1f);
//		animAiguille["Aiguille"].normalizedTime = motorRPM/maxWantedRPM;

		if (currentGear == 0)
			currentGear = 1;
		
		float prevGearThresholdMax = ((1f+prevGearThreshold) * (gears[currentGear-1].maxRPM/maxWantedRPM)*(gears[currentGear].ratio/gears[currentGear-1].ratio)) * 0.66f;
		float nextGearThresholdMin =  ((1f - nextGearThreshold) * gears[currentGear].maxRPM/maxWantedRPM);

		nextGearMax.fillAmount = 1f;
		nextGearMin.fillAmount = 0.028f +  0.625f * nextGearThresholdMin;

		curGearMax.fillAmount = 0.028f +  0.625f * (motorRPM/maxWantedRPM + tolerance);
		curGearMin.fillAmount = 0.028f +  0.625f * motorRPM/maxWantedRPM;

		animArrow["Aiguille"].normalizedTime = (motorRPM/maxWantedRPM + tolerance * 0.8f);


		if (currentGear > 2 && currentGear < (gears.Count))
			prevGearMax.fillAmount = 0.028f + 0.625f * prevGearThresholdMax;
		else
			prevGearMax.fillAmount = 0f;


		if (mic.freqPercent > motorRPM/maxWantedRPM)
		{
			float diff = mic.freqPercent - motorRPM/maxWantedRPM;
			diff /= 3f;
			diff /= tolerance;
			input_gasPedal = 1f - diff;
			input_gasPedal *= 1.5f;
		}
		else
		{
			input_gasPedal = 0f;
		}

//		if (mic.freqPercent > motorRPM/maxWantedRPM && mic.freqPercent < (motorRPM/maxWantedRPM + tolerance))
//			input_gasPedal = 1f;
//		else if (mic.freqPercent > motorRPM/maxWantedRPM)
//			input_gasPedal += -lossGasSpeed * Time.deltaTime;

		input_gasPedal = Mathf.Clamp01(input_gasPedal);

		if (mic.audibleFreq < 1f)
		{
//			gearUpAllowed = false;
			gearDownAllowed = false;
		}
		if (mic.audibleFreq == 1f)
		{
			GearDownDelay.Go();
		}

		if (GearDownDelay.Available() && mic.audibleFreq < 1f)
		{
			rpmMiddle = false;
			gearDownAllowed = false;
		}
//		Debug.Log(gearDownAllowed + " + " + (mic.freqPercent < prevGearThresholdMax) );

		if (Time.timeSinceLevelLoad > 6f && gearUpAllowed && (motorRPM/maxWantedRPM + tolerance) > nextGearThresholdMin)
		{
			animArrow["Aiguille"].normalizedTime = nextGearThresholdMin * 0.5f;
		}

		if (Time.timeSinceLevelLoad > 6f && gearUpAllowed && (motorRPM/maxWantedRPM + tolerance*1.5f) > nextGearThresholdMin && 
			(mic.freqPercent < (motorRPM/maxWantedRPM - 0.04f) || (mic.audibleFreq < 1f && mic.lastAudible)))
		{
			gearUpAllowed = false;
			gearDownAllowed = false;
			rpmMiddle = false;
			currentGear++;
			currentGear = Mathf.Clamp(currentGear,2,gears.Count-1);
		}


		if (Time.timeSinceLevelLoad > 6f && gearDownAllowed && (motorRPM/maxWantedRPM) < prevGearThresholdMax && 
			(mic.freqPercent > (motorRPM/maxWantedRPM + 0.15f) && mic.freqPercent > prevGearThresholdMax))
		{
			gearUpAllowed = false;
			gearDownAllowed = false;
			rpmMiddle = false;
			currentGear--;
			currentGear = Mathf.Clamp(currentGear,2,gears.Count-1);
		}

//		Debug.Log(mic.freqPercent + " > " + prevGearThresholdMax);
//		Debug.Log(mic.freqPercent + " <" + nextGearThresholdMin);
		if (mic.freqPercent < nextGearThresholdMin && mic.freqPercent > prevGearThresholdMax)
		{
			rpmMiddle = true;
		}

		if (rpmMiddle && mic.audibleFreq == 1f)
		{
			if (mic.freqPercent > nextGearThresholdMin || (motorRPM/maxWantedRPM + tolerance) >  nextGearThresholdMin)
				gearUpAllowed = true;
			if (mic.freqPercent < prevGearThresholdMax )
				gearDownAllowed = true;
				
		}
			


//		motorRPM = mic.freqPercent * maxWantedRPM;

		//mic.freqPercent 


		//wantedRPM = (5500.0f * input_gasPedal) * 0.1f + wantedRPM * 0.9f;
		//motorRPM = 0.95f * motorRPM + 0.05f * (-wheels[motorWheel].collider.rpm * gears[currentGear].ratio);
		wheelMotorRPM = (wheels [motorWheel].collider.rpm * gearScale * gears [currentGear].ratio * (1f/wheels[motorWheel].collider.radius));
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
//		Debug.Log(" wantedRPM : " + wantedRPM);
//		Debug.Log(" wheelMotorRPM : " + wheelMotorRPM);
//		Debug.Log(Mathf.Clamp(((wantedRPM - wheelMotorRPM)/gears[currentGear].gasTolerance),-1f,1f));

		for (int i=0; i<wheels.Count; i++)
		{
			wantedRPM = Mathf.Lerp (minWantedRPM, gears[currentGear].maxRPM , input_gasPedal);
//			float engineEff = ((motorRPM/maxWantedRPM) < 1f ? engineEfficiency.Evaluate(motorRPM/maxWantedRPM) : 2f);
//			float engineEff = ((motorRPM/gears[currentGear].maxRPM) < 1f ? engineEfficiency.Evaluate(motorRPM/gears[currentGear].maxRPM) : 2f);

			bool overRPM = motorRPM/gears[currentGear].maxRPM < 1f;

			float engineEff = overRPM ? engineEfficiency.Evaluate(motorRPM/gears[currentGear].maxRPM) : 2f;

			float curTorque = engineTorque * wheels[i].engineFactor * gears[currentGear].ratio * gearScale * engineEff;
			float way = 1f ; //Vector3.Dot(wheels[i].mesh.right,transform.right) < 0f ? -1f : 1f;
			//wheels[i].collider.motorTorque = (curTorque *  wheels[i].engineFactor) * 0.9f + newTorque * 0.1f;

			float engineDiff = Mathf.Clamp(((wantedRPM - wheelMotorRPM)/gears[currentGear].gasTolerance),-1f,1f);
//			if (engineDiff < 0f)
//				engineDiff *= 2f;


			float newTorque = way * engineDiff * curTorque *  (1f-input_Clutch) * (1f/wheels[i].collider.radius);
			wheels[i].collider.motorTorque =  Mathf.Lerp(wheels[i].collider.motorTorque,newTorque,Time.deltaTime * 2f);
			//			wheels[i].collider.motorTorque = 0f;
//			if (wheels[i].engineFactor > 0f)
//				Debug.Log("gas : " + input_gasPedal);

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
		input_Clutch = Time.timeSinceLevelLoad < 3f ? 1f : 0f;
//		input_gasPedal = Input.GetAxis ("Gas");
		input_Steering = Mathf.Lerp(input_Steering, Input.GetAxis ("Steering") / Mathf.Max(1f,localVelocity.z* 0.1f), Time.deltaTime * steeringSensivity);
		input_Brake = Input.GetAxis ("Brake");
		input_HandBrake = Input.GetButton ("HandBrake") ? 1f : 0f;

		input_GearUp = Input.GetButtonDown ("GearUp");
		input_GearDown = Input.GetButtonDown ("GearDown");

	}

	void UpdateWheelMesh()
	{
		for (int i=0; i<wheels.Count; i+=2)
		{
			Transform Wmesh = wheels[i].mesh ??  wheels[i+1].mesh;
			Vector3 averagePos = wheels[i].collider.transform.position + wheels[i+1].collider.transform.position;
			averagePos /= 2f;


			Vector3 posBuf = Wmesh.localPosition;
//			Vector3 posBuf = Wmesh.position;

			RaycastHit[] hits = Physics.RaycastAll(averagePos,
			                                    -transform.up,
								wheels[i].collider.suspensionDistance + wheels[i].collider.radius);
			int res = MathsAndOperations.GetClosestHit(hits,averagePos);

			if (res != -1)
			{

				//posBuf.y =  - transform.up * ( wheels[i].collider.radius ) wheels[i].mesh.parent.InverseTransformPoint(hits[res].point + transform.up * wheels[i].collider.radius).y;
				posBuf.y = wheels[i].mesh.parent.InverseTransformPoint(hits[res].point + transform.up * wheels[i].collider.radius).y;
				Wmesh.localPosition = posBuf;
			}
//			else
//			{
//				posBuf.y = wheels[i].mesh.parent.InverseTransformPoint(- transform.up * wheels[i].collider.suspensionDistance).y;
//				Wmesh.localPosition = posBuf;
//			}

			wheels[i].rotation = Mathf.Repeat(wheels[i].rotation + Time.deltaTime * wheels[i].collider.rpm * 360.0f / 60.0f,360f);

			Wmesh.localRotation = Quaternion.Euler(wheels[i].rotation,
				-90f,
				0.0f);

			//wheel.mesh.RotateAround(wheel.mesh.transform.position, transform.right, Time.deltaTime * wheel.collider.rpm * 360.0f / 60.0f);

		}

		drivingWheel.localRotation = Quaternion.Euler(0f,
				wheels[0].collider.steerAngle,
				0.0f);
	}

	void OnDrawGizmos() {
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.TransformPoint(gravCenter), 0.4f);
	}

	
//	public void OnGUI() {
//		if (true) {
//			// calculate actual speed in Km/H (SI metrics rule, so no inch, yard, foot,
//			// stone, or other stupid length measure!)
//			
//			// message to display
//			string msg = "Speed " + localVelocity_kmh.z.ToString("f0") + "Km/H, " + motorRPM.ToString("f0") + "RPM, gear " + (currentGear-1); //  + " torque " + newTorque.ToString("f2") + ", efficiency " + table[index].ToString("f2");
//			
//			GUILayout.BeginArea(new Rect(Screen.width -250 - 32, 32, 250, 40), GUI.skin.window);
//			GUILayout.Label(msg);
//			GUILayout.EndArea();
//
//
//			if (finalTime == -1f)
//			{
//				GUILayout.BeginArea(new Rect( 32, 32, 400, 40), GUI.skin.window);
//				GUILayout.Label(Time.timeSinceLevelLoad + malus + " s");
//				GUILayout.EndArea();
//			}
//			else if (finalTime > 60f)
//			{
//				GUILayout.BeginArea(new Rect( 32, 32, 400, 40), GUI.skin.window);
//				GUILayout.Label(finalTime + " s, c nul ! (r -> recommencer)");
//				GUILayout.EndArea();
//			}
//			else if (finalTime > 50f)
//			{
//				GUILayout.BeginArea(new Rect( 32, 32, 400, 40), GUI.skin.window);
//				GUILayout.Label(finalTime + " s, c pas trop mal ! (r -> recommencer)");
//				GUILayout.EndArea();
//			}
//			else if (finalTime > 45f)
//			{
//				GUILayout.BeginArea(new Rect( 32, 32, 400, 40), GUI.skin.window);
//				GUILayout.Label(finalTime + " s, oh yeah ! (r -> recommencer)");
//				GUILayout.EndArea();
//			}
//			else if (finalTime > 40f)
//			{
//				GUILayout.BeginArea(new Rect( 32, 32, 400, 40), GUI.skin.window);
//				GUILayout.Label(finalTime + " s, bravo tu gères! (r -> recommencer)");
//				GUILayout.EndArea();
//			}
//			else if (finalTime > 35f)
//			{
//				GUILayout.BeginArea(new Rect( 32, 32, 400, 40), GUI.skin.window);
//				GUILayout.Label(finalTime + " s, T TRO UN AS DU DRIFT KWA §§§ (r)");
//				GUILayout.EndArea();
//			}
//			else if (finalTime > 33f)
//			{
//				GUILayout.BeginArea(new Rect( 32, 32, 400, 40), GUI.skin.window);
//				GUILayout.Label(finalTime + " s, tu as battu le créateur de ce jeu... (r)");
//				GUILayout.EndArea();
//			}
//			else
//			{
//				GUILayout.BeginArea(new Rect( 32, 32, 400, 40), GUI.skin.window);
//				GUILayout.Label(finalTime + " s, tu triche ? (r)");
//				GUILayout.EndArea();
//			}
//		}
//	}
}
