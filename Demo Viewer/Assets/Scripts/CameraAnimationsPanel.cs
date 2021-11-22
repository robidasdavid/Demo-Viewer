using System;
using System.Collections;
using System.Collections.Generic;
using Spark;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class CameraAnimationsPanel : MonoBehaviour
{
	public CameraSplineManager manager;
	public Transform scrollViewContent;
	public GameObject rowPrefab;
	
	[Header("Popup (Currently Unused")]
	public Button saveAsCopy;
	public Button saveButton;
	public GameObject savePopup;

	[Header("Save Bar")] 
	public TMP_InputField customFileName; 

	// Start is called before the first frame update
	private void Start()
	{
		Refresh();
	}

	private void OnEnable()
	{
		Refresh();
	}

	public void Refresh()
	{
		foreach (Transform child in scrollViewContent)
		{
			Destroy(child.gameObject);
		}
		CameraWriteSettings.Load();
		foreach (KeyValuePair<string, AnimationKeyframes> anim in CameraWriteSettings.animations)
		{
			GameObject row = Instantiate(rowPrefab, scrollViewContent);
			AnimationRowController rowController = row.GetComponent<AnimationRowController>();
			rowController.nameLabel.text = anim.Key;
			rowController.loadButton.onClick.AddListener(() =>
			{
				manager.editor.Animation = anim.Value;
				customFileName.text = anim.Key;
			});
			rowController.saveButton.onClick.AddListener(() =>
			{
				string fileName = rowController.nameLabel.text;
				savePopup.gameObject.SetActive(true);
				saveAsCopy.onClick.RemoveAllListeners();
				saveAsCopy.onClick.AddListener(() =>
				{
					while (CameraWriteSettings.animations.ContainsKey(fileName))
					{
						fileName += " (Copy)";
					}
					CameraWriteSettings.animations[fileName] = manager.editor.Animation;
					CameraWriteSettings.instance.Save();
					savePopup.SetActive(false);
					Refresh();
					
					// force Spark to reload settings file if it's open
					UnityWebRequest req = UnityWebRequest.Get("http://localhost:6724/api/reload_camera_settings");
					req.SendWebRequest();
				});
				saveButton.onClick.RemoveAllListeners();
				saveButton.onClick.AddListener(() =>
				{
					CameraWriteSettings.animations[fileName] = manager.editor.Animation;
					CameraWriteSettings.instance.SaveAnimation(customFileName.text);
					savePopup.SetActive(false);
					Refresh();
					
					// force Spark to reload settings file if it's open
					UnityWebRequest req = UnityWebRequest.Get("http://localhost:6724/api/reload_camera_settings");
					req.SendWebRequest();
				});
			});
		}
	}

	public void Save()
	{
		if (string.IsNullOrWhiteSpace(customFileName.text))
		{
			Debug.Log("Empty Animation name", customFileName.gameObject);
			return;
		}
		CameraWriteSettings.animations[customFileName.text] = manager.editor.Animation;
		CameraWriteSettings.instance.SaveAnimation(customFileName.text);
		savePopup.SetActive(false);
		StartCoroutine(RefreshAfterDelay());
					
		// force Spark to reload settings file if it's open
		UnityWebRequest req = UnityWebRequest.Get("http://localhost:6724/api/reload_camera_settings");
		req.SendWebRequest();
	}

	IEnumerator RefreshAfterDelay()
	{
		yield return new WaitForSeconds(1);
		Refresh();
	}

	public void UnloadAnimation()
	{
		manager.editor.Animation = null;
		customFileName.text = "";
	}
}