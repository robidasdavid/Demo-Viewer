using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerStatsHover : MonoBehaviour
{
	public Transform statsWindow;
	public TextMeshProUGUI statsTextBox;
	public TextMeshProUGUI speedTextBox;
	private float lastVisibleTime = 0;

	public Stats Stats {
		set {
			statsTextBox.text = value.ToString();
		}
	}
	public float Speed {
		set {
			speedTextBox.text = "Speed: " + value.ToString("N2") + " m/s";
		}
	}

	private bool visible = false;
	public bool Visible {
		get => visible;
		set {
			statsWindow.gameObject.SetActive(value);
			visible = value;
			lastVisibleTime = Time.time;
		}
	}

	// Update is called once per frame
	void Update()
	{
		statsWindow.LookAt(GameManager.instance.camera.transform.position);
		if (Time.time - lastVisibleTime > .2f)
		{
			Visible = false;
		}
	}
}
