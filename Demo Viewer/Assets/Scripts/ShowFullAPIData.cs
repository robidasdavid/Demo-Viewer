using Newtonsoft.Json;
using TMPro;
using UnityEngine;

public class ShowFullAPIData : MonoBehaviour
{
	public TMP_InputField inputField;

	private void Update()
	{
		if (DemoStart.instance.playhead == null) return;
		
		string json = JsonConvert.SerializeObject(DemoStart.instance.playhead.GetNearestFrame());

		if (json != null)
		{
			inputField.text = $"{DemoStart.instance.playhead.GetNearestFrame().recorded_time:O}\n{json}";
		}
	}
}