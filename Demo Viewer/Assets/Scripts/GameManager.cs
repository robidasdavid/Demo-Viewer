using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using unityutilities;

public class GameManager : MonoBehaviour
{
	public static GameManager instance;
	public NetworkFrameManager netFrameMan;
	public Transform[] vrOnlyThings;
	public Transform[] flatOnlyThingsDesktop;
	public Transform[] flatOnlyThingsMobile;
	public Transform[] uiHiddenOnLive;
	public Transform[] uiShownOnLive;

	public SceneAsset[] arenaModelScenes;
	

	public Text dataSource;
	public Button becomeHostButton;
	[ReadOnly] public bool lastFrameWasOwner = true;
	[ReadOnly] public bool lastFrameUserPresent;
	[ReadOnly] public bool usingVR = false;
	public bool enableVR = false;
	public Rig vrRig;
	public Camera vrCamera;
	public Camera flatCameraDesktop;
	public Camera flatCameraMobile;

	public new Camera camera
	{
		get
		{
			return usingVR
				? vrCamera
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
				: flatCameraMobile;
#else
				: flatCameraDesktop;
#endif
		}
	}

	public DemoStart demoStart;

	[Header("Drawing Mode")] public MonoBehaviour[] drawingModeEnabled;
	public MonoBehaviour[] drawingModeDisabled;
	private bool drawingMode;

	/// <summary>
	/// True for drawing, false for not drawing
	/// </summary>
	public bool DrawingMode
	{
		set
		{
			drawingMode = value;
			foreach (MonoBehaviour obj in drawingModeEnabled)
			{
				obj.enabled = value;
			}

			foreach (MonoBehaviour obj in drawingModeDisabled)
			{
				obj.enabled = !value;
			}
		}
		get => drawingMode;
	}

	private void Awake()
	{
		instance = this;

		RefreshVRObjectsVisibility(false);
		lastFrameWasOwner = true;

		List<string> args = System.Environment.GetCommandLineArgs().ToList();

		foreach (var arg in args)
		{
			if (arg.Contains(".json") || arg.Contains(".echoreplay"))
			{
				PlayerPrefs.SetString("fileDirector", arg);
				break;
			}
		}

		DrawingMode = false;

		// Enable VR Mode
		if (enableVR || args.Contains("-useVR"))
		{
			enableVR = true;
			//RefreshVRObjectsVisibility(GetPresence());

			// XRGeneralSettings.Instance.Manager.InitializeLoader();
			XRGeneralSettings.Instance.Manager.InitializeLoaderSync();
			XRGeneralSettings.Instance.Manager.StartSubsystems();
		}
		else
		{
			XRSettings.enabled = false;
		}

		RefreshVRObjectsVisibility(enableVR);
		
		
		// arena model
		SceneManager.LoadSceneAsync(arenaModelScenes[PlayerPrefs.GetInt("ArenaModel", 0)].name, LoadSceneMode.Additive);


		// add file handling to registry for .echoreplay files
#if UNITY_STANDALONE_WIN
		FileAssociations.SetAssociation(
			".echoreplay",
			"EchoVR Replay Viewer",
			"View EchoVR replay files in a reconstructed 3D environment",
			Path.Combine(Application.dataPath, "Replay Viewer.exe"));
#endif
	}


	// Update is called once per frame
	private void Update()
	{
		//// check if headset is worn
		//bool isPresent = GetPresence();
		//if (lastFrameUserPresent != isPresent)
		//{
		//	RefreshVRObjectsVisibility(isPresent);
		//}
		//lastFrameUserPresent = isPresent;

		// hide UI when connected to another user
		if (netFrameMan != null)
		{
			if (lastFrameWasOwner != netFrameMan.IsLocalOrServer)
			{
				foreach (var item in instance.uiHiddenOnLive)
				{
					item.gameObject.SetActive(netFrameMan.IsLocalOrServer);
				}

				foreach (var item in instance.uiShownOnLive)
				{
					item.gameObject.SetActive(!netFrameMan.IsLocalOrServer);
				}
			}

			lastFrameWasOwner = netFrameMan.IsLocalOrServer;

			becomeHostButton.gameObject.SetActive(!netFrameMan.IsLocalOrServer);
			if (!netFrameMan.IsLocalOrServer)
			{
				dataSource.text = netFrameMan.networkFilename;
			}
		}
	}

	private void RefreshVRObjectsVisibility(bool present)
	{
		Debug.Log("RefreshVRObjectsVisibility: " + present);
		usingVR = present;
		if (present)
		{
			foreach (Transform thing in vrOnlyThings)
			{
				thing.gameObject.SetActive(true);
			}

			foreach (Transform thing in flatOnlyThingsDesktop)
			{
				thing.gameObject.SetActive(false);
			}

			foreach (Transform thing in flatOnlyThingsMobile)
			{
				thing.gameObject.SetActive(false);
			}
		}
		else
		{
			foreach (var thing in vrOnlyThings)
			{
				thing.gameObject.SetActive(false);
			}

			foreach (var thing in flatOnlyThingsDesktop)
			{
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
				thing.gameObject.SetActive(false);
#else
				thing.gameObject.SetActive(true);
#endif
			}

			foreach (var thing in flatOnlyThingsMobile)
			{
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
				thing.gameObject.SetActive(true);
#else
				thing.gameObject.SetActive(false);
#endif
			}
		}
	}
}