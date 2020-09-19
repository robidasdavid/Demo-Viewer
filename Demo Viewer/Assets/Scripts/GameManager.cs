using Mirror;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using unityutilities;

public class GameManager : NetworkBehaviour
{
	public static GameManager instance;
	public Transform[] vrOnlyThings;
	public Transform[] flatOnlyThings;
	public Transform[] uiHiddenOnLive;
	public Transform[] uiShownOnLive;
	public Text dataSource;
	[ReadOnly]
	public bool lastFrameWasOwner = true;
	[ReadOnly]
	public bool lastFrameUserPresent;
	[ReadOnly]
	public bool usingVR = false;
	public bool enableVR = false;
	public Rig vrRig;
	public Camera vrCamera;
	public Camera flatCamera;
	public new Camera camera {
		get {
			return usingVR ? vrCamera : flatCamera;
		}
	}
	public DemoStart demoStart;

	private void Awake()
	{
		instance = this;

		RefreshVRObjectsVisibility(false);
		lastFrameWasOwner = true;

		bool printArgs = false;
		string[] args = System.Environment.GetCommandLineArgs();
		for (int i = 0; i < args.Length; i++)
		{
			if (printArgs) Debug.Log("ARG " + i + ": " + args[i]);
			if (args[i].Contains(".json") || args[i].Contains(".echoreplay"))
			{
				PlayerPrefs.SetString("fileDirector", args[i]);
				break;
			}

			if (enableVR || args[i] == "-useVR")
			{
				enableVR = true;
				RefreshVRObjectsVisibility(GetPresence());
				XRGeneralSettings.Instance.Manager.InitializeLoaderSync();
				XRGeneralSettings.Instance.Manager.StartSubsystems();
			}
		}

		// TODO add back
		//SocialMan.instance.roomManager.OwnerChanged += (newName) =>
		//{
		//	dataSource.text = newName;
		//};
	}



	// Update is called once per frame
	void Update()
	{
		// check if headset is worn
		bool isPresent = GetPresence();
		if (lastFrameUserPresent != isPresent)
		{
			RefreshVRObjectsVisibility(isPresent);
		}
		lastFrameUserPresent = isPresent;

		// hide UI when connected to another user
		if (lastFrameWasOwner != isServer)
		{
			foreach (var item in instance.uiHiddenOnLive)
			{
				item.gameObject.SetActive(isServer);
			}
			foreach (var item in instance.uiShownOnLive)
			{
				item.gameObject.SetActive(!isServer);
			}
		}
		lastFrameWasOwner = isServer;
	}

	private static bool GetPresence()
	{
		InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
		bool isPresent = false;
		headDevice.TryGetFeatureValue(CommonUsages.userPresence, out isPresent);
		return isPresent;
	}

	private void RefreshVRObjectsVisibility(bool present)
	{
		Debug.Log("RefreshVRObjectsVisibility: " + present);
		usingVR = present;
		if (present)
		{
			foreach (var thing in vrOnlyThings)
			{
				thing.gameObject.SetActive(true);
			}
			foreach (var thing in flatOnlyThings)
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
			foreach (var thing in flatOnlyThings)
			{
				thing.gameObject.SetActive(true);
			}
		}
	}
}
