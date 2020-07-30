using TMPro;
using UnityEngine;

public class ViewReplayUIController : MonoBehaviour
{
	private string replayName;
	public string ReplayName {
		get => replayName;
		set {
			replayName = value;
			replayNameText.text = value;
		}
	}
	[SerializeField]
	private TextMeshProUGUI replayNameText;

	public bool Loading {
		set {
			loadingIndicator.gameObject.SetActive(value);
		}
	}
	[SerializeField]
	private Transform loadingIndicator;

	// TODO should show or hide this menu
	public void Show()
	{

	}
}
