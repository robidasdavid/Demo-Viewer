using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadSetting : MonoBehaviour
{
	public Component target;
	public string key;

	private void Start()
	{
		Load();
		SetupListeners();
	}

	private void SetupListeners()
	{
		if (target == null) return;

		switch (target)
		{
			case InputField inputField:
				inputField.onValueChanged.AddListener(val => { PlayerPrefs.SetString(key, val); });
				break;
			case Toggle toggle:
				toggle.onValueChanged.AddListener(val => { PlayerPrefs.SetInt(key, val ? 1 : 0); });
				break;
			case Dropdown dropdown:
				dropdown.onValueChanged.AddListener(val => { PlayerPrefs.SetInt(key, val); });
				break;
			case TMP_Dropdown dropdown:
				dropdown.onValueChanged.AddListener(val => { PlayerPrefs.SetInt(key, val); });
				break;
		}
	}

	private void Load()
	{
		if (target == null) return;

		switch (target)
		{
			case InputField inputField:
				if (PlayerPrefs.HasKey(key))
					inputField.text = PlayerPrefs.GetString(key);
				break;
			case Toggle toggle:
				if (PlayerPrefs.HasKey(key))
					toggle.isOn = PlayerPrefs.GetInt(key) == 1;
				break;
			case Dropdown dropdown:
				if (PlayerPrefs.HasKey(key))
					dropdown.value = PlayerPrefs.GetInt(key);
				break;
			case TMP_Dropdown dropdown:
				if (PlayerPrefs.HasKey(key))
					dropdown.value = PlayerPrefs.GetInt(key);
				break;
		}
	}
}