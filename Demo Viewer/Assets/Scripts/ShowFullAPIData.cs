using Newtonsoft.Json;
using TMPro;
using UnityEngine;

public class ShowFullAPIData : MonoBehaviour
{
	public TMP_InputField inputField;

	private void Update()
	{
		if (DemoStart.playhead == null) return;
		
		string json = JsonConvert.SerializeObject(DemoStart.playhead.GetNearestFrame());

		if (json != null)
		{
			inputField.text = $"{DemoStart.playhead.GetNearestFrame().recorded_time:O}\n{json}";
		}
	}
}