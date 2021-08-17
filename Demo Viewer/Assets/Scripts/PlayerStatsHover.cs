using TMPro;
using UnityEngine;

public class PlayerStatsHover : MonoBehaviour
{
	public Transform statsWindow;
	public TextMeshProUGUI statsTextBox;
	public TextMeshProUGUI speedTextBox;
	private float lastVisibleTime;

	public Stats Stats {
		set => statsTextBox.text = value?.ToString();
	}
	public float Speed {
		set => speedTextBox.text = $"Speed: {value:N2} m/s";
	}

	private bool visible;
	public bool Visible {
		get => visible;
		set {
			statsWindow.gameObject.SetActive(value);
			visible = value;
			lastVisibleTime = Time.time;
		}
	}

	// Update is called once per frame
	private void Update()
	{
		statsWindow.LookAt(GameManager.instance.camera.transform.position);
		if (Time.time - lastVisibleTime > .2f)
		{
			Visible = false;
		}
	}
}
