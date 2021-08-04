using UnityEngine;

/// <summary>
/// Enables or disables objects on keypress
/// </summary>
public class ToggleObjectOnKeypress : MonoBehaviour
{
	public KeyCode keyCode;
	public GameObject obj;
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
					obj.SetActive(!obj.activeSelf);
					break;
				case ToggleEnable.Enable:
					obj.SetActive(true);
					break;
				case ToggleEnable.Disable:
					obj.SetActive(false);
					break;
			}
		}
	}
}