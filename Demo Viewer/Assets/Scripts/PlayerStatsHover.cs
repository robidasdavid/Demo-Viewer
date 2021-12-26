using EchoVRAPI;
using TMPro;
using UnityEngine;
using Transform = UnityEngine.Transform;

public class PlayerStatsHover : MonoBehaviour
{
	public Transform statsWindow;
	public TextMeshProUGUI statsTextBox;
	public TextMeshProUGUI speedTextBox;
	private double lastVisibleTime;

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
			lastVisibleTime = Time.timeAsDouble;
		}
	}

	// Update is called once per frame
	private void Update()
	{
		statsWindow.LookAt(GameManager.instance.camera.transform.position);
		if (Time.timeAsDouble - lastVisibleTime > .2f)
		{
			Visible = false;
		}
	}
}
