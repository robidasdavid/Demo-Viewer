using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class ClippingController : MonoBehaviour
{
	public Slider startPoint;
	public Slider endPoint;
	public RectTransform highlightArea;
	private RectTransform thisTransform;
	public TMP_InputField clipNameField;
	public Transform errorBox;
	public TMP_Text errorText;
	public Transform savingClipMessage;

	// Start is called before the first frame update
	private void Start()
	{
		thisTransform = GetComponent<RectTransform>();
	}

	private void OnEnable()
	{
		string currentName = PlayerPrefs.GetString("fileDirector", "new_clip");
		if (File.Exists(currentName))
		{
			currentName = Path.GetFileNameWithoutExtension(currentName);
		}

		if (currentName.Contains("rec_"))
		{
			currentName = currentName.Replace("rec_", "clip_");
		}

		clipNameField.text = currentName;
	}

	public void OnSliderChanged(float value)
	{
		float width = thisTransform.rect.width;
		float minPoint = Mathf.Min(startPoint.value, endPoint.value) * width;
		float maxPoint = (1 - Mathf.Max(startPoint.value, endPoint.value)) * width;

		highlightArea.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, minPoint, width - maxPoint - minPoint);

		highlightArea.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 8);
	}

	public void SaveClip()
	{
		
		int nFrames = GameManager.instance.demoStart.loadedDemo.nframes;
		int startFrame = (int) (startPoint.value * nFrames);
		int endFrame = (int) (endPoint.value * nFrames);

		if (startFrame > endFrame)
		{
			Debug.LogError("Start frame is after end frame. Can't clip.");
			errorText.text = "Start frame is after end frame. Can't clip.";
			errorBox.gameObject.SetActive(true);
			return;
		}

		if (endFrame >= nFrames)
		{
			Debug.LogError("End frame is after the end of the replay. Can't clip.");
			errorText.text = "End frame is after the end of the replay. Can't clip.";
			errorBox.gameObject.SetActive(true);
			return;
		}

		if (startFrame < 0)
		{
			Debug.LogError("Start frame is < 0. Can't clip.");
			errorText.text = "Start frame is < 0. Can't clip.";
			errorBox.gameObject.SetActive(true);
			return;
		}

		string fileName = clipNameField.text;
		
		if (string.IsNullOrEmpty(fileName))
		{
			Debug.LogError("Please give the clip a name. Can't clip.");
			errorText.text = "Please give the clip a name. Can't clip.";
			errorBox.gameObject.SetActive(true);
			return;
		}
		
		
		// add the current directory if it isn't specified in the file name
		if (!fileName.Contains(Path.DirectorySeparatorChar))
		{
			string defaultFolderName = Path.GetDirectoryName(PlayerPrefs.GetString("fileDirector"));
			if (!Directory.Exists(defaultFolderName))
			{
				Debug.LogError("No folder. Can't clip.");
				errorText.text = "No folder. Can't clip.";
				errorBox.gameObject.SetActive(true);
				return;
			}

			fileName = Path.Combine(defaultFolderName, fileName);
		}

		// if directory still doesn't exist, quit
		if (!Directory.Exists(Path.GetDirectoryName(fileName)))
		{
			Debug.LogError("Directory not found. Can't clip.");
			errorText.text = "Directory not found. Can't clip.";
			errorBox.gameObject.SetActive(true);
			return;
		}

		// add the extension if not specified
		if (!Path.HasExtension(fileName))
		{
			fileName += ".echoreplay";
		}
		
		if (File.Exists(fileName))
		{
			Debug.LogError("File name already exists. Can't clip.");
			errorText.text = "File name already exists. Can't clip.";
			errorBox.gameObject.SetActive(true);
			return;
		}

		StartCoroutine(SaveClipCo(fileName, startFrame, endFrame));
	}

	private IEnumerator SaveClipCo(string fileName, int startFrame, int endFrame)
	{
		
		savingClipMessage.gameObject.SetActive(true);
		errorBox.gameObject.SetActive(false);
		
		Thread saveThread = new Thread(() =>GameManager.instance.demoStart.SaveReplayClip(fileName, startFrame, endFrame));
		saveThread.Start();
		while (saveThread.IsAlive)
		{
			yield return null;
		}
		savingClipMessage.gameObject.SetActive(false);
	}
}