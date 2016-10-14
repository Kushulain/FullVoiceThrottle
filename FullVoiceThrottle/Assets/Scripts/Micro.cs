using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class Micro : MonoBehaviour {
//
//	public int device = 0;
//	public AudioClip clip;
//
//	// Use this for initialization
//	void Start () {
//		Debug.Log("Device : " + Microphone.devices[device]);
//		clip = Microphone.Start(Microphone.devices[device], true, 10, 44100);
//	}
//	
//	// Update is called once per frame
//	void Update () {
//	
//	}
//
	public AudioSource audioSauce;
	public string CurrentAudioInput = "none";
	public int deviceNum = 1;
	public int WINDOW_SIZE = 1024;
	public float sampleFreq = 12000f;
	public float[] spectrum;
	public string[] inputDevices;
	public int octaveLength = 35;
	public FFTWindow FFTType;
	public int octavePrecision = 48;
	public float signalPow = 8f;
	public float signalMul = 8f;
	public Image jauge;
	public Animation animAiguille;

	public CoolDownEvent smoothDelay = new CoolDownEvent(0.1f);
	public int[] lastFreqs;
	public int numFreqSamples = 8;
	int freqSamplesCounter = 0;
	public float smoothedFreq = 2;

	bool calibDone = false;
	float[] octaveCalibration;
	public float thresholdFreq = 0f;
	public float octaveTotalCalibrationThreshold = 0f;

	public float audibleFreqSpeed = 2f;
	public float audibleFreq = 0f;

	public float finalFreq = 0f;
	public float lastKnownFreqLerp = 0f;
	public float lastKnownFreq = 0f;
	public float freqBegin = 0f;

	public float userMinFreqDefaultAllowed = 10f;
	public float userMaxFreqDefaultAllowed = 544f;

	public float userMinFreqDefaultValue = 20f;
	public float userMaxFreqDefaultValue = 50f;

	public float userCurrentMinFreq = 20f;
	public float userCurrentMaxFreq = 50f;

	public float userFreqSpeed = 30f;

	public float freqPercent = 0f;

	public float[] shortSpectrum;

	public bool lastAudible = false;

	public float shiftDebug = -50f;
	public float multDebug = 2f;

	public Transform cam;

	public KeyCode sensivityDown = KeyCode.I;
	public KeyCode sensivityUp = KeyCode.O;
	public float sensivityShiftUI = 0f;

	int frameSinceStartCounter = -1;

	float automaticCalibration = float.PositiveInfinity;


	void Start()
	{
		animAiguille["Aiguille"].speed = 0f;
		userCurrentMinFreq = userMinFreqDefaultValue;
		userCurrentMaxFreq = userMaxFreqDefaultValue;

		lastFreqs = new int[numFreqSamples];
		spectrum = new float[WINDOW_SIZE];
		inputDevices = new string[Microphone.devices.Length];
		for (int i = 0; i < Microphone.devices.Length; i++) {
			inputDevices [i] = Microphone.devices [i].ToString ();
			Debug.Log("Device: " + inputDevices [i]);
		}

		if (deviceNum >= Microphone.devices.Length)
		{
			Vehicle[] motos = FindObjectsOfType<Vehicle>();

			foreach (Vehicle mot in motos)
			{
				if (mot != this)
				{
					mot.transform.FindChild("Main Camera").GetComponent<Camera>().rect = new Rect(0f,0f,1f,1f);
				}
			}

			Destroy(gameObject);
			Destroy(animAiguille.transform.parent.parent.gameObject);

			return;
		}

		CurrentAudioInput = Microphone.devices[deviceNum].ToString();
		StartMic ();
		octaveCalibration = new float[GetIdFromFreq(3200)];
		Debug.Log(GetIdFromFreq(3200));
	}


	public void StartMic(){
		audioSauce.clip = Microphone.Start(CurrentAudioInput, true, 8, (int) sampleFreq); 
	}

	public void OnGUI(){
//		GUI.Label (new Rect (10, 10, 400, 400), CurrentAudioInput);
		GUI.Label (new Rect (10, 10 + sensivityShiftUI * Screen.height, 400, 400), thresholdFreq.ToString());
	}



	void Update() {
		if (Input.GetKeyDown(sensivityUp))
		{
			thresholdFreq += 0.0025f;
		}
		if (Input.GetKeyDown(sensivityDown))
		{
			thresholdFreq -= 0.0025f;
		}
//		if (Input.GetKeyDown(KeyCode.E))
//		{
//			Microphone.End (CurrentAudioInput);
//			deviceNum+= 1;
//			if (deviceNum > Microphone.devices.Length - 1)
//				deviceNum = 0;
//			CurrentAudioInput = Microphone.devices[deviceNum].ToString();
//
//			StartMic ();
//		}

		float delay = 0.030f;
		int microphoneSamples = Microphone.GetPosition (CurrentAudioInput);
		//		Debug.Log ("Current samples: " + microphoneSamples);
		if (microphoneSamples / sampleFreq > delay) {
			if (!audioSauce.isPlaying) {
				Debug.Log ("Starting thing");
				frameSinceStartCounter = 1;
				audioSauce.timeSamples = (int) (microphoneSamples - (delay * sampleFreq));
				audioSauce.Play ();
			}
		}

		if (frameSinceStartCounter >= 0)
			frameSinceStartCounter++;
		
		audioSauce.GetSpectrumData(spectrum, 0, FFTType);

		shortSpectrum = new float[GetIdFromFreq(3200)];
		System.Array.Copy(spectrum,shortSpectrum,GetIdFromFreq(3200));

		//float[] octaveValues = WrapOctave(shortSpectrum);

		if (Time.timeSinceLevelLoad < 6f && Time.timeSinceLevelLoad > 2.5f)
		{
			float totalFreqPreCal = 0f;
			for (int i=0; i<shortSpectrum.Length; i++)
			{
				totalFreqPreCal += shortSpectrum[i];
			}
//			Debug.Log("totalFreqPreCal = " + totalFreqPreCal);

			if (totalFreqPreCal < automaticCalibration && totalFreqPreCal != 0f)
			{
				Debug.Log("automatic Cal : " + totalFreqPreCal);
				SetCalibration(shortSpectrum);
				automaticCalibration = totalFreqPreCal;
			}
		}


		if (Input.GetKeyDown (KeyCode.A)) {
			SetCalibration(shortSpectrum);

			userCurrentMinFreq = userMinFreqDefaultValue;
			userCurrentMaxFreq = userMaxFreqDefaultValue;
		}

		float totalFreq = 0f;
		for (int i=0; i<shortSpectrum.Length; i++)
		{
			shortSpectrum[i] -= octaveCalibration[i];
			totalFreq += shortSpectrum[i];
		}

//		spectrum = WrapOctave(shortSpectrum);


		//si on sort d'un audible < 1f alors peak le plus bas
		//sinon peak le plush proche du dernier


//		int highestFFT = GetHighestPeak(shortSpectrum);
		HashSet<int> forbiddens = new HashSet<int>();
		int forbidRadius = 8;

		int highest = GetHighestPeak(shortSpectrum,forbiddens);
		for (int i=0; i<forbidRadius; i++)
		{
			int k = highest+i-(forbidRadius/2);
			if (!forbiddens.Contains(k))
				forbiddens.Add(k);
		}

		int secondHighest = GetHighestPeak(shortSpectrum,forbiddens);
		for (int i=0; i<forbidRadius; i++)
		{
			int k = secondHighest+i-(forbidRadius/2);
			if (!forbiddens.Contains(k))
				forbiddens.Add(k);
		}
		
		int thirdHighest = GetHighestPeak(shortSpectrum,forbiddens);

		int results = highest;


//		if (secondHighest < highest && (shortSpectrum[secondHighest] / shortSpectrum[highest]) > 0.4f)
//			results = secondHighest;

		if (!lastAudible || true)
		{
			if (secondHighest < highest && (shortSpectrum[secondHighest] / shortSpectrum[highest]) > 0.3f)
				results = secondHighest;
			
			if (thirdHighest < secondHighest && thirdHighest < highest && (shortSpectrum[thirdHighest] / shortSpectrum[highest]) > 0.3f)
				results = thirdHighest;
		}
		else
		{
			float hiDist = Mathf.Abs(highest-lastKnownFreq);
			float hiDist2 = Mathf.Abs(secondHighest-lastKnownFreq);
			float hiDist3 = Mathf.Abs(thirdHighest-lastKnownFreq);

			if (hiDist < hiDist2 && hiDist < hiDist3)
			{
				results = highest;
			}
			else if (hiDist2 < hiDist3 && hiDist2 < hiDist)
			{
				results = secondHighest;
			}
			else
			{
				results = thirdHighest;
			}
		}



//		int results = Mathf.Min(highest,secondHighest);

//		if (Mathf.Abs(highest-lastKnownFreq) > Mathf.Abs(secondHighest-lastKnownFreq) && lastAudible)
//		{
//			results = secondHighest;
//		}
//		else
//		{
//		}


//		float dist = Mathf.Abs((float)(highest-secondHighest));
		//dist= Mathf.Min(dist,Mathf.Abs((float)(highest-(secondHighest+octavePrecision))));

//		if (dist < octavePrecision*0.15f)
//		{
////			float otherWayDist = Mathf.Abs((float)(highest-(secondHighest+octavePrecision)));
////			float otherWayDist2 = Mathf.Abs((float)((highest+octavePrecision)-secondHighest));
////
//			float mix = (float)(shortSpectrum[highest]/(shortSpectrum[highest]+shortSpectrum[secondHighest]));
//			//Debug.Log("mix " + mix);
//
//			if (float.IsNaN(mix))
//				mix = 0f;
////
////			if (dist < otherWayDist && dist < otherWayDist2)
////			{
//				results = (int)Mathf.Lerp(secondHighest,highest,mix);
////			}
////			else if (otherWayDist < otherWayDist2)
////			{
////				results = ((int)Mathf.Lerp(secondHighest+octavePrecision,highest,mix)) % octavePrecision;
////			}
////			else
////			{
////				results = ((int)Mathf.Lerp(secondHighest,highest+octavePrecision,mix)) % octavePrecision;
////			}
//		}
//		Debug.Log("results " + results);

//			lastFreqs[freqSamplesCounter] = results;
//			freqSamplesCounter = (freqSamplesCounter+1)%numFreqSamples;
//
//		smoothedFreq = 0f;
//		for (int k=0; k<numFreqSamples; k++)
//		{
//			smoothedFreq += lastFreqs[k];
//		}
//
//		smoothedFreq /= numFreqSamples;
//		Debug.Log("results : " + results);
		ProcessFreq(results);
		//jauge.fillAmount = 0.5f  * (relativeFreq)/octavePrecision;

		freqPercent = ((finalFreq-userCurrentMinFreq) / (userCurrentMaxFreq-userCurrentMinFreq));
		UpdateSpeedometer(freqPercent);


//		totalFreq /= octaveValues.Length;

//		Debug.Log(octaveValues[highest] + " / " + totalFreq + " = " + octaveValues[highest]/totalFreq);
//		Debug.Log("Calibration : " + totalFreq);

		if (shortSpectrum[highest]/totalFreq > thresholdFreq/* totalFreq > octaveTotalCalibrationThreshold */)
		{
			audibleFreq += Time.deltaTime * audibleFreqSpeed;
		}
		else
		{
			audibleFreq -= Time.deltaTime * audibleFreqSpeed;
		}

		audibleFreq = Mathf.Clamp01(audibleFreq);

		for (int i=10; i<shortSpectrum.Length; i++)
		{
//			float value = shortSpectrum[i];
//			float bassFilter = 2f / (i+1f) + 1;
//			value /= bassFilter;
//			if (Mathf.FloorToInt(Mathf.Log(GetFreqFromId(i),2f)) == 0)
			//				Debug.DrawLine(new Vector3(i, -50f, 0f),new Vector3(i, 0f, 0),Color.blue);
			if (highest == i)
				Debug.DrawLine((new Vector3(i, shiftDebug, 0f)),
					(new Vector3(i, 50f + shiftDebug, 0)),new Color(1f,1f,0.66f,0.2f),0.2f);
			if (secondHighest == i)
				Debug.DrawLine((new Vector3(i, shiftDebug, 0f)),
					(new Vector3(i, 50f + shiftDebug, 0)),new Color(1f,1f,0.333f,0.2f),0.2f);
			if (thirdHighest == i)
				Debug.DrawLine((new Vector3(i, shiftDebug, 0f)),
					(new Vector3(i, 50f + shiftDebug, 0)),new Color(1f,1f,0.0f,0.2f),0.2f);
			if (results == i)
				Debug.DrawLine((new Vector3(i, shiftDebug, 0f)),
					(new Vector3(i, 50f + shiftDebug, 0)),new Color(0f,1f,0f,0.5f),0.2f);
			if ((int)lastKnownFreqLerp == i)
				Debug.DrawLine((new Vector3(i, shiftDebug, 0f)),
					(new Vector3(i, 50f + shiftDebug, 0)),new Color(0f,0f,1f,0.5f),0.2f);

			Debug.DrawLine((new Vector3(i - 1, multDebug * 200f * shortSpectrum[i - 1] + shiftDebug , 0)), 
				(new Vector3(i, multDebug * 200f * shortSpectrum[i] + shiftDebug , 0)), 
				Color.magenta);


		}

//		Debug.DrawLine(new Vector3((lastKnownFreq+octavePrecision*40f)%octavePrecision, -30, 0f),new Vector3((lastKnownFreq+octavePrecision*40f)%octavePrecision, 50f, 0),Color.yellow);

//
//		for (int i=1; i<octaveValues.Length; i++)
//		{
//
////			if (highest == i)
////				Debug.DrawLine(new Vector3(i, 0f, 0f),new Vector3(i, 50f, 0),Color.green);
////			if (secondHighest == i)
////				Debug.DrawLine(new Vector3(i, 0f, 0f),new Vector3(i, 50f, 0),Color.yellow);
//			if (results == i)
//				Debug.DrawLine(new Vector3(i, -20, 0f),new Vector3(i, 50f, 0),Color.red);
//
////			if (i % octaveLength == 0)
////				Debug.DrawLine(new Vector3(i, 0f, 0f),new Vector3(i, 50000f, 0),Color.green);
////			if (i % octaveLength == 0)
//
//			Debug.DrawLine( new Vector3(i - 1, 50f * octaveValues[i - 1] , 0), 
//				new Vector3(i, 50f * octaveValues[i] , 0), 
//				Color.red);
//		}

		lastKnownFreq = results;
		lastAudible = audibleFreq == 1f;
	}

	void UpdateSpeedometer(float percentage)
	{
		percentage = ((finalFreq-userCurrentMinFreq) / (userCurrentMaxFreq-userCurrentMinFreq));
		percentage = Mathf.Clamp01(percentage);

//		Debug.Log(percentage);
		animAiguille["Aiguille"].normalizedTime = percentage;
	}


	void ProcessFreq(float curFreq)
	{
//		float way = (curFreq - lastKnownFreq) > 0f ? 1f : -1f;
//		float diff = Mathf.Abs(curFreq - lastKnownFreq);

//		float distance1 = Mathf.Abs(curFreq-((lastKnownFreq+octavePrecision*100f)%octavePrecision));
//		float distance2 = Mathf.Abs(curFreq-(((lastKnownFreq+octavePrecision*100f)%octavePrecision)+octavePrecision));
//		float distance3 = Mathf.Abs((curFreq+octavePrecision)-((lastKnownFreq+octavePrecision*100f)%octavePrecision));
//
//		float delta = 0f;
//
//		if (distance1 < distance2 && distance1 < distance3)
//		{
////			Debug.Log("1 curFreq : " + curFreq + " lastKnownFreq : " + lastKnownFreq);
//			delta = curFreq-((lastKnownFreq+octavePrecision*100f)%octavePrecision);
//		}
//		else if (distance2 < distance3)
//		{
////			Debug.Log("2 curFreq : " + curFreq + " lastKnownFreq : " + lastKnownFreq);
//			delta = curFreq-(((lastKnownFreq+octavePrecision*100f)%octavePrecision)+octavePrecision);
//		}
//		else
//		{
////			Debug.Log("3 curFreq : " + curFreq + " lastKnownFreq : " + lastKnownFreq);
//			delta = (curFreq+octavePrecision)-((lastKnownFreq+octavePrecision*100f)%octavePrecision);
//		}

		float delta = curFreq-lastKnownFreqLerp;
		float finalDelta = Mathf.Lerp(0f,delta,Time.deltaTime * 7f / Mathf.Min(10f,(1f+Mathf.Abs(curFreq-lastKnownFreq))));
		lastKnownFreqLerp += finalDelta;

//		if (audibleFreq < 1.0f)
//		{
//			freqBegin = lastKnownFreq;
//		}
		if (audibleFreq == 1f/* && lastKnownFreq > userMinFreqDefaultAllowed*/)
		{
			//userFreqSpeed = 

			if (lastKnownFreqLerp < userCurrentMinFreq)
			{
				userCurrentMinFreq -= userFreqSpeed * Time.deltaTime;
				userCurrentMinFreq = Mathf.Max(userCurrentMinFreq,lastKnownFreqLerp);
			}
			if (lastKnownFreqLerp > userCurrentMaxFreq)
			{
				userCurrentMaxFreq += userFreqSpeed * Time.deltaTime;
				userCurrentMaxFreq = Mathf.Min(userCurrentMaxFreq,lastKnownFreqLerp);
			}

//			userCurrentMaxFreq -= userFreqSpeed * Time.deltaTime * 0.01f;
//			userCurrentMinFreq += userFreqSpeed * Time.deltaTime * 0.01f;

//			userCurrentMinFreq = Mathf.Min(userCurrentMinFreq,userMinFreqDefaultValue);
//			userCurrentMaxFreq = Mathf.Max(userCurrentMaxFreq,userMaxFreqDefaultValue);

			userCurrentMinFreq = Mathf.Max(userCurrentMinFreq,userMinFreqDefaultAllowed);
			userCurrentMaxFreq = Mathf.Min(userCurrentMaxFreq,userMaxFreqDefaultAllowed);

			finalFreq = lastKnownFreqLerp;
		}
//		else if (audibleFreq == 0f)
//		{
//			relativeFreq += ((-relativeFreq)/* > 0f ? 1f : -1f*/) * Time.deltaTime * (1f-audibleFreq) * 2f;
//		}
		//lastKnownFreq += way * audibleFreq * Time.deltaTime;
	}

	void SetCalibration(float[] curOctave)
	{
		System.Array.Copy(curOctave,octaveCalibration,curOctave.Length);
		calibDone = true;
	}

	float GetFreqFromId(int id)
	{
		float idf = id;

		//idf /= AudioSettings.outputSampleRate/2;
		idf *= sampleFreq;
		idf /= WINDOW_SIZE;
		return idf;
	}

	int GetIdFromFreq(float hz)
	{
		hz *= WINDOW_SIZE;
		hz /= sampleFreq;
		return (int)hz;
	}

	int GetHighestPeak(float[] octaveSpect, HashSet<int> forbiddens, float highest = float.PositiveInfinity)
	{
		int bestId = octaveSpect.Length-1;
//		float bassFilter = 0f;

		for (int i=10; i<octaveSpect.Length; i++)
		{
			if (forbiddens.Contains(i))
				continue;

			float value = octaveSpect[i];
//			bassFilter = 2f / (i+1f) + 1;
//			value /= bassFilter;
			// / bassFilter
			if (value  < highest)
			{
				if (value > octaveSpect[bestId])
				{
					bestId = i;
				}
			}
		}

		return bestId;
	}
		

	float[] WrapOctave(float[] spectrumValues)
	{
		float[] results = new float[octavePrecision];

		for (int specID=0; specID<spectrumValues.Length; specID++)
		{
			float hz = GetFreqFromId(specID);

			if (specID == 0)
				continue;
			
			float note = Mathf.Log(hz,2f);
			int octave = Mathf.FloorToInt(note);
			note = note%1.0f;

			if (octave == 0)
				continue;

			results[(int)(octavePrecision*note)] += Mathf.Pow(spectrumValues[specID]*signalMul,signalPow);
		}


		return results;
	}
}
