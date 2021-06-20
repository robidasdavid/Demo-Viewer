using Newtonsoft.Json;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Application = UnityEngine.Application;

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


/// <summary>
/// UI Controller for the replay browser menu
/// Prefab defaults are set up for vr, then modified for the 2d interface
/// </summary>
public class ReplaySelectionUI : MonoBehaviourPunCallbacks
{
	public bool isVR;
	public Transform mainMenu;
	public GameObject playerButtonPrefab;
	public Transform playersList;
	public GameObject replayDataRowPrefab;
	public TMP_InputField manualInputText;
	public TextMeshProUGUI folderSrcText;
	public Transform onlineReplaysList;
	public Transform localReplaysList;
	public ScrollRect scrollView;
	public Transform[] allVisualizations;

	public ViewReplayUIController viewReplayUIController;

	public string replayGetURL = "https://ignitevr.gg/cgi-bin/EchoStats.cgi/get_replays";
	public string replayFileURL = "https://ignitevr.gg/cgi-bin/EchoStats.cgi/echoreplays/";
	public string apiKey;

	public Transform panel;

	private bool showing = true;

	public enum ReplaySources
	{
		Local,
		Online
	}

	private ReplaySources replaysSource;

	public ReplaySources ReplaysSource
	{
		get => replaysSource;
		set
		{
			if (value == ReplaySources.Local)
			{
				localReplaysList.gameObject.SetActive(true);
				onlineReplaysList.gameObject.SetActive(false);
				scrollView.content = localReplaysList.GetComponent<RectTransform>();

				StartCoroutine(GetReplaysLocal());
			}
			else if (value == ReplaySources.Online)
			{
				localReplaysList.gameObject.SetActive(false);
				onlineReplaysList.gameObject.SetActive(true);
				scrollView.content = onlineReplaysList.GetComponent<RectTransform>();

				StartCoroutine(GetReplaysWeb());
			}

			replaysSource = value;
		}
	}


	[Header("Join Room Dialog")] public Transform joinRoomDialog;
	public InputField roomNameInput;
	public Text roomPlayerCountLabel;
	public Text connectedInfoLabel;
	public Button joinButton;
	public Button disconnectButton;


	private void Start()
	{
		showing = panel.gameObject.activeSelf;

		manualInputText.text = PlayerPrefs.GetString("fileDirector");
		folderSrcText.text = "Files in " + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Spark","replays");

		// refresh the list
		ReplaysSource = ReplaysSource;
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.BackQuote) ||
		    Input.GetButtonDown("XboxStart"))
		{
			ShowToggle();
		}
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

				ReplaysListData replays = JsonConvert.DeserializeObject<ReplaysListData>(jsontext);

				// add the new ones
				foreach (var replay in replays.replays)
				{
					GameObject button = Instantiate(replayDataRowPrefab, onlineReplaysList);
					var replayFileInfo = button.GetComponentInChildren<ReplayFileInfo>();
					replayFileInfo.ServerFilename = replay.server_filename;
					replayFileInfo.OriginalFilename = replay.original_filename;
					replayFileInfo.CreatedBy = replay.recorded_by;
					replayFileInfo.Notes = replay.notes;

					button.GetComponentInChildren<Button>().onClick.AddListener(delegate
					{
						DownloadReplay(replay.server_filename, replay.original_filename);
					});
				}
			}
		}
	}

	private IEnumerator GetReplaysLocal()
	{
		string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Spark", "replays");
		if (Directory.Exists(path))
		{
			DirectoryInfo replaysFolder = new DirectoryInfo(path);

			// get list of files in the current folder
			FileInfo[] files = replaysFolder.GetFiles().OrderBy(p => p.CreationTime).ToArray();
			Array.Reverse(files);

			files = files.Take(200).ToArray();      // avoid lag by only loading the first 100 files

			// add the new ones
			foreach (FileInfo file in files)
			{
				if (file.Extension == ".echoreplay")
				{
					GameObject button = Instantiate(replayDataRowPrefab, localReplaysList);
					ReplayFileInfo replayFileInfo = button.GetComponentInChildren<ReplayFileInfo>();
					replayFileInfo.OriginalFilename = file.Name;
					replayFileInfo.CreatedBy = "Local";
					replayFileInfo.Size = FileLengthToString(file.Length);
					replayFileInfo.Notes = "";

					button.GetComponentInChildren<Button>().onClick.AddListener(delegate { LoadLocalReplay(file.FullName); });
				}

				// this is not necessary, but it'll create a slight animation
				yield return null;
			}
		}
	}

	public static string FileLengthToString(float length)
	{
		if (length < 1e3f)
		{
			return length + " bytes";
		}
		else if (length < 1e6f)
		{
			return (length / 1e3f).ToString("N2") + " KiB";
		}
		else if (length < 1e9f)
		{
			return (length / 1e6f).ToString("N2") + " MiB";
		}
		else
		{
			return (length / 1e9f).ToString("N2") + " GiB";
		}
	}

	public void LoadLocalReplay(string filename)
	{
		PlayerPrefs.SetString("fileDirector", filename);

		// only way to load a new replay file is to reload the scene
		SceneManager.LoadSceneAsync("Game Scene");

		viewReplayUIController.ReplayName = filename;
		viewReplayUIController.Loading = true;
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

	/// <summary>
	/// Loads the file specified in the manual input bar
	/// </summary>
	public void LoadLocalReplay()
	{
		string text = manualInputText.text;
		if (File.Exists(text))
		{
			LoadLocalReplay(text);
		}
		else
		{
			Debug.LogError("File doesn't exist: " + text);
		}
	}

	/// <summary>
	/// Shows or hides this menu
	/// </summary>
	public void ShowToggle()
	{
		panel.gameObject.SetActive(!showing);

		showing = !showing;
	}

	/// <summary>
	/// Whether to get data live from a local game session or not
	/// </summary>
	/// <param name="live">True to use live source</param>
	public void LiveToggle(bool live)
	{
		LiveFrameProvider.isLive = live;

		if (DemoStart.playhead != null)
		{
			DemoStart.playhead.SetPlaying(live);
			foreach (Transform item in GameManager.instance.uiHiddenOnLive)
			{
				item.gameObject.SetActive(!live);
			}

			foreach (Transform item in GameManager.instance.uiShownOnLive)
			{
				item.gameObject.SetActive(live);
			}

			GameManager.instance.dataSource.text = "Local Game";
		}
	}

	public void ArenaModelChanged(int selection)
	{
		foreach (Transform obj in GameManager.instance.arenaModels)
		{
			obj.gameObject.SetActive(false);
		}
		GameManager.instance.arenaModels[selection].gameObject.SetActive(true);
		PlayerPrefs.SetInt("ArenaModel", selection);
	}

	public void BlocksModelChanged(int selection)
	{
		foreach (Transform obj in GameManager.instance.blocksModels)
		{
			obj.gameObject.SetActive(false);
		}
		GameManager.instance.blocksModels[selection].gameObject.SetActive(true);
		PlayerPrefs.SetInt("BlocksModel", selection);
	}

	public void ShowPlayspaceChanged(int selection)
	{
		DemoStart.showPlayspace = selection;
		PlayerPrefs.SetInt("ShowPlayspaceVisualizers", selection);
	}
	
	public void VRArenaScaleChanged(int selection)
	{
		float[] options = {1, 10, 30, 50};
		GameManager.instance.vrRig.transform.localScale = Vector3.one * options[selection];
		PlayerPrefs.SetInt("VRArenaScale", selection);
	}

	public void HostMatch()
	{
		if (!string.IsNullOrEmpty(roomNameInput.text))
		{
			PhotonNetwork.JoinOrCreateRoom(roomNameInput.text, new RoomOptions(), TypedLobby.Default);
			joinRoomDialog.gameObject.SetActive(false);
		}
	}

	public void ShowMatches()
	{
		// TODO
	}

	public void RoomNameInputChanged(string newText)
	{
		// roomPlayerCountLabel.text = 
	}

	public void LeaveRoom()
	{
		if (PhotonNetwork.IsConnected)
		{
			PhotonNetwork.LeaveRoom();
		}
	}

	public override void OnConnectedToMaster()
	{
		connectedInfoLabel.text = "Not Connected";
		joinButton.gameObject.SetActive(true);
		disconnectButton.gameObject.SetActive(false);
	}

	public override void OnJoinedRoom()
	{
		connectedInfoLabel.text = "Connected: " + PhotonNetwork.CurrentRoom.Name;
		joinButton.gameObject.SetActive(false);
		disconnectButton.gameObject.SetActive(true);
	}
}