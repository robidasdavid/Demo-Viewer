using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderPositionIndicator : MonoBehaviour
{
	public Slider slider;
	public TMP_Text text;

	private void Start()
	{
		slider.onValueChanged.AddListener(val =>
		{
			if (DemoStart.instance.playhead != null)
			{
				text.text = (val * DemoStart.instance.playhead.FrameCount).ToString("N0");
			}
		});
	}
}