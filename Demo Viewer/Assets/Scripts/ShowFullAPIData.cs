using TMPro;
using UnityEngine;

public class ShowFullAPIData : MonoBehaviour
{
	public TMP_InputField inputField;

	private void Update()
	{
		if (DemoStart.playhead != null)
		{
			string json = DemoStart.playhead.GetNearestFrame().originalJSON;

			if (json != null)
			{
				inputField.text = $"{DemoStart.playhead.GetNearestFrame().frameTime:O}\n{json}";
			}
		}
	}
}