using UnityEngine;
using AOT;
using System.Collections.Generic;
using Oculus.Platform;
using Oculus.Platform.Models;


// This class coordinates communication with the Oculus Platform
// Service running in your device.
public class SocialMan : MonoBehaviour
{

	private float voiceCurrent = 0.0f;

	// Local player
	private uint packetSequence = 0;

	public OvrAvatar localAvatarPrefab;
	public OvrAvatar remoteAvatarPrefab;

	protected OvrAvatar localAvatar;
	public GameObject playerObject;

	// Remote players
	protected Dictionary<ulong, RemotePlayer> remoteUsers = new Dictionary<ulong, RemotePlayer>();

	protected State currentState;

	protected static SocialMan s_instance = null;
	protected RoomMan roomManager;
	protected P2PMan p2pManager;
	protected VoipMan voipManager;

	// my Application-scoped Oculus ID
	protected ulong myID;

	// my Oculus user name
	protected string myOculusID;


	// animating the mouth for voip
	public static readonly float VOIP_SCALE = 2f;

	public virtual void Update()
	{
		// Look for updates from remote users
		p2pManager.GetRemotePackets();

		// update avatar mouths to match voip volume
		foreach (KeyValuePair<ulong, RemotePlayer> kvp in remoteUsers)
		{
			if (kvp.Value.voipSource == null)
			{
				if (kvp.Value.RemoteAvatar.MouthAnchor != null)
				{
					kvp.Value.voipSource = kvp.Value.RemoteAvatar.MouthAnchor.AddComponent<VoipAudioSourceHiLevel>();
					kvp.Value.voipSource.senderID = kvp.Value.remoteUserID;
				}
			}

			if (kvp.Value.voipSource != null)
			{
				float remoteVoiceCurrent = Mathf.Clamp(kvp.Value.voipSource.peakAmplitude * VOIP_SCALE, 0f, 1f);
				kvp.Value.RemoteAvatar.VoiceAmplitude = remoteVoiceCurrent;
			}
		}

		if (localAvatar != null)
		{
			localAvatar.VoiceAmplitude = Mathf.Clamp(voiceCurrent * VOIP_SCALE, 0f, 1f);
		}

		Request.RunCallbacks();
	}

	#region Initialization and Shutdown

	public virtual void Awake()
	{
		LogOutputLine("Start Log.");

		// make sure only one instance of this manager ever exists
		if (s_instance != null)
		{
			Destroy(gameObject);
			return;
		}

		s_instance = this;
		DontDestroyOnLoad(gameObject);

		TransitionToState(State.INITIALIZING);

		Core.AsyncInitialize().OnComplete(InitCallback);

		roomManager = new RoomMan();
		p2pManager = new P2PMan();
		voipManager = new VoipMan();
	}

	void InitCallback(Message<PlatformInitialize> msg)
	{
		if (msg.IsError)
		{
			TerminateWithError(msg);
			return;
		}

		LaunchDetails launchDetails = ApplicationLifecycle.GetLaunchDetails();
		LogOutput("App launched with LaunchType " + launchDetails.LaunchType);

		// First thing we should do is perform an entitlement check to make sure
		// we successfully connected to the Oculus Platform Service.
		Entitlements.IsUserEntitledToApplication().OnComplete(IsEntitledCallback);
	}

	public virtual void Start()
	{
		// noop here, but is being overridden in PlayerController
	}

	void IsEntitledCallback(Message msg)
	{
		if (msg.IsError)
		{
			TerminateWithError(msg);
			return;
		}

		// Next get the identity of the user that launched the Application.
		Users.GetLoggedInUser().OnComplete(GetLoggedInUserCallback);
	}

	void GetLoggedInUserCallback(Message<User> msg)
	{
		if (msg.IsError)
		{
			TerminateWithError(msg);
			return;
		}

		myID = msg.Data.ID;
		myOculusID = msg.Data.OculusID;

		localAvatar = Instantiate(localAvatarPrefab);
		localAvatar.CanOwnMicrophone = false;

		playerObject = GameManager.instance.usingVR ? GameManager.instance.vrRig.gameObject : GameManager.instance.flatCamera.gameObject;

		localAvatar.transform.SetParent(playerObject.transform, false);
		localAvatar.transform.localPosition = new Vector3(0, 0, 0);
		localAvatar.transform.localRotation = Quaternion.identity;

		localAvatar.oculusUserID = myID.ToString();
		localAvatar.RecordPackets = true;
		localAvatar.PacketRecorded += OnLocalAvatarPacketRecorded;
		localAvatar.EnableMouthVertexAnimation = true;

		Quaternion rotation = Quaternion.identity;
		transform.localPosition = Vector3.zero;
		transform.localRotation = rotation;

		TransitionToState(State.CHECKING_LAUNCH_STATE);

		// If the user launched the app by accepting the notification, then we want to
		// join that room.  If not, try to find a friend's room to join
		if (!roomManager.CheckForInvite())
		{
			LogOutput("No invite on launch, looking for a friend to join.");
			Users.GetLoggedInUserFriendsAndRooms()
				.OnComplete(GetLoggedInUserFriendsAndRoomsCallback);
		}
		Voip.SetMicrophoneFilterCallback(MicFilter);
	}

	void GetLoggedInUserFriendsAndRoomsCallback(Message<UserAndRoomList> msg)
	{
		if (msg.IsError)
		{
			return;
		}

		foreach (UserAndRoom el in msg.Data)
		{
			// see if any friends are in a joinable room
			if (el.User == null) continue;
			if (el.RoomOptional == null) continue;
			if (el.RoomOptional.IsMembershipLocked == true) continue;
			if (el.RoomOptional.Joinability != RoomJoinability.CanJoin) continue;
			if (el.RoomOptional.JoinPolicy == RoomJoinPolicy.None) continue;

			LogOutput("Trying to join room " + el.RoomOptional.ID + ", friend " + el.User.OculusID);
			roomManager.JoinExistingRoom(el.RoomOptional.ID);
			return;
		}

		LogOutput("No friend to join. Creating my own room.");
		// didn't find any open rooms, start a new room
		roomManager.CreateRoom();
		TransitionToState(State.CREATING_A_ROOM);
	}

	public void OnLocalAvatarPacketRecorded(object sender, OvrAvatar.PacketEventArgs args)
	{
		var size = Oculus.Avatar.CAPI.ovrAvatarPacket_GetSize(args.Packet.ovrNativePacket);
		byte[] toSend = new byte[size];

		Oculus.Avatar.CAPI.ovrAvatarPacket_Write(args.Packet.ovrNativePacket, size, toSend);

		foreach (KeyValuePair<ulong, RemotePlayer> kvp in remoteUsers)
		{
			//LogOutputLine("Sending avatar Packet to  " + kvp.Key);
			// Root is local tracking space transform
			p2pManager.SendAvatarUpdate(kvp.Key, playerObject.transform, packetSequence, toSend);
		}

		packetSequence++;
	}

	public void OnApplicationQuit()
	{
		roomManager.LeaveCurrentRoom();

		foreach (KeyValuePair<ulong, RemotePlayer> kvp in remoteUsers)
		{
			p2pManager.Disconnect(kvp.Key);
			voipManager.Disconnect(kvp.Key);
		}
		LogOutputLine("End Log.");
	}

	public void AddUser(ulong userID, ref RemotePlayer remoteUser)
	{
		remoteUsers.Add(userID, remoteUser);
	}

	public void LogOutputLine(string line)
	{
		Debug.Log(Time.time + ": " + line);
	}

	// For most errors we terminate the Application since this example doesn't make
	// sense if the user is disconnected.
	public static void TerminateWithError(Message msg)
	{
		s_instance.LogOutputLine("Error: " + msg.GetError().Message);
		UnityEngine.Application.Quit();
	}

	#endregion

	#region Properties

	public static State CurrentState {
		get {
			return s_instance.currentState;
		}
	}

	public static ulong MyID {
		get {
			if (s_instance != null)
			{
				return s_instance.myID;
			}
			else
			{
				return 0;
			}
		}
	}

	public static string MyOculusID {
		get {
			if (s_instance != null && s_instance.myOculusID != null)
			{
				return s_instance.myOculusID;
			}
			else
			{
				return string.Empty;
			}
		}
	}

	#endregion

	#region State Management

	public enum State
	{
		// loading platform library, checking application entitlement,
		// getting the local user info
		INITIALIZING,

		// Checking to see if we were launched from an invite
		CHECKING_LAUNCH_STATE,

		// Creating a room to join
		CREATING_A_ROOM,

		// in this state we've create a room, and hopefully
		// sent some invites, and we're waiting people to join
		WAITING_IN_A_ROOM,

		// in this state we're attempting to join a room from an invite
		JOINING_A_ROOM,

		// we're in a room with others
		CONNECTED_IN_A_ROOM,

		// Leaving a room
		LEAVING_A_ROOM,

		// shutdown any connections and leave the current room
		SHUTDOWN,
	};

	public static void TransitionToState(State newState)
	{
		if (s_instance)
		{
			s_instance.LogOutputLine("State " + s_instance.currentState + " -> " + newState);
		}

		if (s_instance && s_instance.currentState != newState)
		{
			s_instance.currentState = newState;

			// state transition logic
			switch (newState)
			{
				case State.SHUTDOWN:
					s_instance.OnApplicationQuit();
					break;

				default:
					break;
			}
		}
	}

	public static void MarkAllRemoteUsersAsNotInRoom()
	{
		foreach (KeyValuePair<ulong, RemotePlayer> kvp in s_instance.remoteUsers)
		{
			kvp.Value.stillInRoom = false;
		}
	}

	public static void MarkRemoteUserInRoom(ulong userID)
	{
		RemotePlayer remoteUser = new RemotePlayer();

		if (s_instance.remoteUsers.TryGetValue(userID, out remoteUser))
		{
			remoteUser.stillInRoom = true;
		}
	}

	public static void ForgetRemoteUsersNotInRoom()
	{
		List<ulong> toPurge = new List<ulong>();

		foreach (KeyValuePair<ulong, RemotePlayer> kvp in s_instance.remoteUsers)
		{
			if (kvp.Value.stillInRoom == false)
			{
				toPurge.Add(kvp.Key);
			}
		}

		foreach (ulong key in toPurge)
		{
			RemoveRemoteUser(key);
		}
	}

	public static void LogOutput(string line)
	{
		s_instance.LogOutputLine(Time.time + ": " + line);
	}

	public static bool IsUserInRoom(ulong userID)
	{
		return s_instance.remoteUsers.ContainsKey(userID);
	}

	public static void AddRemoteUser(ulong userID)
	{
		RemotePlayer remoteUser = new RemotePlayer();

		remoteUser.RemoteAvatar = Instantiate(s_instance.remoteAvatarPrefab);
		remoteUser.RemoteAvatar.oculusUserID = userID.ToString();
		remoteUser.RemoteAvatar.ShowThirdPerson = true;
		remoteUser.RemoteAvatar.EnableMouthVertexAnimation = true;
		remoteUser.p2pConnectionState = PeerConnectionState.Unknown;
		remoteUser.voipConnectionState = PeerConnectionState.Unknown;
		remoteUser.stillInRoom = true;
		remoteUser.remoteUserID = userID;

		s_instance.AddUser(userID, ref remoteUser);
		s_instance.p2pManager.ConnectTo(userID);
		s_instance.voipManager.ConnectTo(userID);

		s_instance.LogOutputLine("Adding User " + userID);
	}

	public static void RemoveRemoteUser(ulong userID)
	{
		RemotePlayer remoteUser = new RemotePlayer();

		if (s_instance.remoteUsers.TryGetValue(userID, out remoteUser))
		{
			Destroy(remoteUser.RemoteAvatar.MouthAnchor.GetComponent<VoipAudioSourceHiLevel>(), 0);
			Destroy(remoteUser.RemoteAvatar.gameObject, 0);
			s_instance.remoteUsers.Remove(userID);

			s_instance.LogOutputLine("Removing User " + userID);
		}
	}

	public void UpdateVoiceData(short[] pcmData, int numChannels)
	{
		if (localAvatar != null)
		{
			localAvatar.UpdateVoiceData(pcmData, numChannels);
		}

		float voiceMax = 0.0f;
		float[] floats = new float[pcmData.Length];
		for (int n = 0; n < pcmData.Length; n++)
		{
			float cur = floats[n] = (float)pcmData[n] / (float)short.MaxValue;
			if (cur > voiceMax)
			{
				voiceMax = cur;
			}
		}
		voiceCurrent = voiceMax;
	}

	[MonoPInvokeCallback(typeof(Oculus.Platform.CAPI.FilterCallback))]
	public static void MicFilter(short[] pcmData, System.UIntPtr pcmDataLength, int frequency, int numChannels)
	{
		s_instance.UpdateVoiceData(pcmData, numChannels);
	}


	public static RemotePlayer GetRemoteUser(ulong userID)
	{
		RemotePlayer remoteUser = new RemotePlayer();

		if (s_instance.remoteUsers.TryGetValue(userID, out remoteUser))
		{
			return remoteUser;
		}
		else
		{
			return null;
		}
	}

	#endregion

}
