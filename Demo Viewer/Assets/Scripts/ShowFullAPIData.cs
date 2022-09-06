using Newtonsoft.Json;
using TMPro;
using UnityEngine;

public class ShowFullAPIData : MonoBehaviour
{
	public TMP_InputField inputField;

	private void Update()
	{
		if (DemoStart.instance.playhead == null) return;
		
		string json = JsonConvert.SerializeObject(DemoStart.instance.playhead.GetFrame(), Formatting.Indented);

		inputField.text = $"{DemoStart.instance.playhead.GetFrame().recorded_time:O}\n{json}";
	}
}