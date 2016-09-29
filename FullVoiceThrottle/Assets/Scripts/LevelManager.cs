using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour {

	public Image maskCountdown;
	public Text textCountdown;
	public Text textCountdown2;

	public float arrivalLimit = -2000f;

	public Vehicle[] motos;

	public bool alreadyWin = false;

	// Use this for initialization
	void Start () {
		motos = FindObjectsOfType<Vehicle>();
	}

	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown (KeyCode.P))
		{
			SceneManager.LoadScene(SceneManager.GetActiveScene().name);
		}

		float timeLevel = Time.timeSinceLevelLoad * 0.5f;

		for (int i=0; i<motos.Length; i++)
		{
			if (motos[i].gameObject.transform.position.x < arrivalLimit && motos[i].Win_GO.activeSelf == false &&  motos[i].Loose_GO.activeSelf == false)
			{
				if (!alreadyWin)
				{
					motos[i].Win_GO.SetActive(true);
					motos[i].Win_GO_Text.text = "Bravo ! " + (timeLevel-4f).ToString("0.0") + "s";
					alreadyWin = true;
				}
				else
				{
					motos[i].Loose_GO.SetActive(true);
					motos[i].Loose_GO_Text.text = "Perdu ! " + (timeLevel-4f).ToString("0.0") + "s";
				}
			}
//			
		}
	
		if (timeLevel*0.5f < 4f)
		{
			if (timeLevel < 3f)
				maskCountdown.fillAmount = 1f-timeLevel%1f;
			else
				maskCountdown.fillAmount = 1f;
				

			if (timeLevel < 1)
			{
				textCountdown.text = "3";
				textCountdown2.text = "3";
			}
			else if (timeLevel < 2)
			{
				textCountdown.text = "2";
				textCountdown2.text = "2";
			}
			else if (timeLevel < 3)
			{
				textCountdown.text = "1";
				textCountdown2.text = "1";
			}
			else
			{
				textCountdown.text = "GO!";
				textCountdown2.text = "GO!";
			}
		}
		else
		{
			maskCountdown.fillAmount = 0f;
			textCountdown.text = "";
			textCountdown2.text = "";
		}
	}
}
