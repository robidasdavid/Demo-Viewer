using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class ReplaysListData
{
	public ReplaysListItem[] replays;

	[Serializable]
	public class ReplaysListItem
	{
		public string match_id;
		public string match_time;
		public string notes;
		public string original_filename;
		public string server_filename;
		public string recorded_by;
		public string upload_time;
	}
}

public class ReplaySelectionUI : MonoBehaviour
{
	public Transform mainMenu;
	public GameObject playerButtonPrefab;
	public Transform playersList;
	public GameObject replayDataRowPrefab;
	public Transform onlineReplaysList;
	public Transform[] allVisualizations;

	public ViewReplayUIController viewReplayUIController;

	public string replayGetURL = "https://ignitevr.gg/cgi-bin/EchoStats.cgi/get_replays";
	public string replayFileURL = "https://ignitevr.gg/cgi-bin/EchoStats.cgi/echoreplays/";
	public string apiKey;

	void Start()
	{
		StartCoroutine(GetReplaysWeb());
	}

	IEnumerator GetReplaysWeb()
	{
		// get the json data about what replays are available
		using (UnityWebRequest webRequest = UnityWebRequest.Get(replayGetURL))
		{
			webRequest.SetRequestHeader("x-api-key", apiKey);

			// Request and wait for the desired page.
			yield return webRequest.SendWebRequest();

			if (webRequest.isNetworkError)
			{
				Debug.Log("Error: " + webRequest.error);
			}
			else
			{
				string jsontext = Regex.Unescape(webRequest.downloadHandler.text);

				ReplaysListData replays = JsonUtility.FromJson<ReplaysListData>(jsontext);

				// add the new ones
				foreach (var replay in replays.replays)
				{
					GameObject button = Instantiate(replayDataRowPrefab, onlineReplaysList);
					var replayFileInfo = button.GetComponentInChildren<ReplayFileInfo>();
					replayFileInfo.ServerFilename = replay.server_filename;
					replayFileInfo.OriginalFilename = replay.original_filename;
					replayFileInfo.CreatedBy = replay.recorded_by;
					replayFileInfo.Notes = replay.notes;

					button.GetComponentInChildren<Button>().onClick.AddListener(delegate { DownloadReplay(replay.server_filename, replay.original_filename); });
				}
			}
		}
	}

	public void DownloadReplay(string server_filename, string original_filename)
	{
		StartCoroutine(DownloadReplayCo(server_filename, original_filename));
	}

	public IEnumerator DownloadReplayCo(string server_filename, string original_filename)
	{
		viewReplayUIController.Show();

		// get the json data about what replays are available


		Debug.Log("Downloading: " + replayFileURL + server_filename);
		using (UnityWebRequest webRequest = UnityWebRequest.Get(replayFileURL + server_filename))
		{
			// Request and wait for the desired page.
			yield return webRequest.SendWebRequest();

			if (webRequest.isNetworkError)
			{
				Debug.Log("Error: " + webRequest.error);
			}
			else
			{
				string savePath = Path.Combine(Application.persistentDataPath, server_filename);
				File.WriteAllBytes(savePath, webRequest.downloadHandler.data);


				PlayerPrefs.SetString("fileDirector", savePath);

				// only way to load a new replay file is to reload the scene
				SceneManager.LoadSceneAsync("Game Scene");

				viewReplayUIController.ReplayName = original_filename;
				viewReplayUIController.Loading = true;

			}
		}
	}

}
