using UnityEngine;

/// <summary>
/// Enables or disables objects on keypress
/// </summary>
public class ToggleObjectsOnKeypress : MonoBehaviour
{
	public KeyCode keyCode;
	public GameObject[] obj;
	public ToggleEnable toggleOrEnable;
	public enum ToggleEnable
	{
		Toggle,
		Enable,
		Disable
	}

	private void Update()
	{
		if (Input.GetKeyDown(keyCode))
		{
			switch (toggleOrEnable)
			{
				case ToggleEnable.Toggle:
					foreach (GameObject o in obj)
					{
						o.SetActive(!o.activeSelf);						
					}
					break;
				case ToggleEnable.Enable:
					foreach (GameObject o in obj)
					{
						o.SetActive(true);						
					}
					break;
				case ToggleEnable.Disable:
					foreach (GameObject o in obj)
					{
						o.SetActive(false);						
					}
					break;
			}
		}
	}
}