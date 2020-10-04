using Mirror;
using Mirror.Discovery;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using unityutilities;

public class GameManager : MonoBehaviour
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
	public NetworkFrameManager netFrameMan;
	public NetworkDiscovery networkDiscovery;
	public Dictionary<long, ServerResponse> discoveredServers = new Dictionary<long, ServerResponse>();

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

		// Enable VR Mode
		if (enableVR || args.Contains("-useVR"))
		{
			enableVR = true;
			//RefreshVRObjectsVisibility(GetPresence());

			XRSettings.enabled = true;
			//XRGeneralSettings.Instance.Manager.InitializeLoaderSync();
			//XRGeneralSettings.Instance.Manager.StartSubsystems();
		}
		else
		{
			XRSettings.enabled = false;
		}
		RefreshVRObjectsVisibility(enableVR);
	}



	// Update is called once per frame
	void Update()
	{
		//// check if headset is worn
		//bool isPresent = GetPresence();
		//if (lastFrameUserPresent != isPresent)
		//{
		//	RefreshVRObjectsVisibility(isPresent);
		//}
		//lastFrameUserPresent = isPresent;

		// hide UI when connected to another user
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
	}

	private static bool GetPresence()
	{
		//InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
		//bool isPresent = false;
		//headDevice.TryGetFeatureValue(CommonUsages.userPresence, out isPresent);
		//return isPresent;

		return XRDevice.userPresence == UserPresenceState.Present;
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
