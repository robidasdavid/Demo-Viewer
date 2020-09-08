using System;
using Oculus.Platform;
using Oculus.Platform.Models;


/// <summary>
/// 🏠 Helper class to manage Room creation, membership and invites.
/// Rooms are a mechanism to help Oculus users create a shared experience.
/// Users can only be in one Room at a time.  If the Owner of a room
/// leaves, then ownership is transferred to some other member.
/// Here we use rooms to create the notion of a 'call' to help us
/// invite a Friend and establish a VOIP and P2P connection.
/// </summary>
public class RoomMan
{
	// the ID of the Room that I'm in
	public ulong roomID;

	// the ID of the Room that I'm invited to
	private ulong invitedRoomID;

	// Am I the server?
	public bool amIServer;

	// The current owner name
	public string roomOwnerName;
	public Action<string> OwnerChanged;

	// Have we already gone through the startup?
	private bool startupDone;

	public RoomMan()
	{
		amIServer = false;
		roomOwnerName = "";
		startupDone = false;
		Rooms.SetRoomInviteAcceptedNotificationCallback(AcceptingInviteCallback);
		Rooms.SetUpdateNotificationCallback(RoomUpdateCallback);
	}

	#region Launched Application from Accepting Invite

	// Callback to check whether the User accepted an invite
	void AcceptingInviteCallback(Message<string> msg)
	{
		if (msg.IsError)
		{
			SocialMan.TerminateWithError(msg);
			return;
		}

		SocialMan.LogOutput("Launched Invite to join Room: " + msg.Data);

		invitedRoomID = Convert.ToUInt64(msg.GetString());

		if (startupDone)
		{
			CheckForInvite();
		}
	}

	// Check to see if the App was launched by accepting the Notication from the main Oculus app.
	// If so, we can directly join that room.  (If it's still available.)
	public bool CheckForInvite()
	{
		startupDone = true;

		if (invitedRoomID != 0)
		{
			JoinExistingRoom(invitedRoomID);
			return true;
		}
		else
		{
			return false;
		}
	}

	#endregion

	#region Create a Room and Invite Friend(s) from the Oculus Universal Menu

	public void CreateRoom()
	{
		Rooms.CreateAndJoinPrivate(RoomJoinPolicy.Everyone, 4, true)
			 .OnComplete(CreateAndJoinPrivateRoomCallback);
	}

	void CreateAndJoinPrivateRoomCallback(Message<Oculus.Platform.Models.Room> msg)
	{
		if (msg.IsError)
		{
			SocialMan.TerminateWithError(msg);
			return;
		}

		roomID = msg.Data.ID;
		if (roomOwnerName != msg.Data.OwnerOptional.OculusID) OwnerChanged?.Invoke(msg.Data.OwnerOptional.OculusID);
		roomOwnerName = msg.Data.OwnerOptional.OculusID;

		if (msg.Data.OwnerOptional != null && msg.Data.OwnerOptional.ID == SocialMan.MyID)
		{
			amIServer = true;
		}
		else
		{
			amIServer = false;
		}

		SocialMan.TransitionToState(SocialMan.State.WAITING_IN_A_ROOM);
	}

	void OnLaunchInviteWorkflowComplete(Message msg)
	{
		if (msg.IsError)
		{
			SocialMan.TerminateWithError(msg);
			return;
		}
	}

	#endregion

	#region Accept Invite

	public void JoinExistingRoom(ulong roomID)
	{
		SocialMan.TransitionToState(SocialMan.State.JOINING_A_ROOM);
		Rooms.Join(roomID, true).OnComplete(JoinRoomCallback);
	}

	void JoinRoomCallback(Message<Room> msg)
	{
		if (msg.IsError)
		{
			// is reasonable if caller called more than 1 person, and I didn't answer first
			return;
		}

		var ownerOculusId = msg.Data.OwnerOptional != null ? msg.Data.OwnerOptional.OculusID : "null";
		var userCount = msg.Data.UsersOptional != null ? msg.Data.UsersOptional.Count : 0;

		if (roomOwnerName != msg.Data.OwnerOptional.OculusID) OwnerChanged?.Invoke(msg.Data.OwnerOptional.OculusID);
		roomOwnerName = msg.Data.OwnerOptional.OculusID;
		SocialMan.LogOutput("Joined Room " + msg.Data.ID + " owner: " + ownerOculusId + " count: " + userCount);
		roomID = msg.Data.ID;
		ProcessRoomData(msg);
	}

	#endregion

	#region Room Updates

	void RoomUpdateCallback(Message<Room> msg)
	{
		if (msg.IsError)
		{
			SocialMan.TerminateWithError(msg);
			return;
		}

		string ownerOculusId = msg.Data.OwnerOptional != null ? msg.Data.OwnerOptional.OculusID : "null";
		int userCount = msg.Data.UsersOptional != null ? msg.Data.UsersOptional.Count : 0;

		if (roomOwnerName != msg.Data.OwnerOptional.OculusID) OwnerChanged?.Invoke(msg.Data.OwnerOptional.OculusID);
		roomOwnerName = msg.Data.OwnerOptional.OculusID;
		SocialMan.LogOutput("Room Update " + msg.Data.ID + " owner: " + ownerOculusId + " count: " + userCount);
		ProcessRoomData(msg);
	}

	#endregion

	#region Room Exit

	public void LeaveCurrentRoom()
	{
		if (roomID != 0)
		{
			Rooms.Leave(roomID);
			roomID = 0;
		}
		SocialMan.TransitionToState(SocialMan.State.LEAVING_A_ROOM);
	}

	#endregion

	#region Process Room Data

	void ProcessRoomData(Message<Room> msg)
	{
		if (roomOwnerName != msg.Data.OwnerOptional.OculusID) OwnerChanged?.Invoke(msg.Data.OwnerOptional.OculusID);
		roomOwnerName = msg.Data.OwnerOptional.OculusID;
		if (msg.Data.OwnerOptional != null && msg.Data.OwnerOptional.ID == SocialMan.MyID)
		{
			amIServer = true;
		}
		else
		{
			amIServer = false;
		}

		// if the caller left while I was in the process of joining, just use that as our new room
		if (msg.Data.UsersOptional != null && msg.Data.UsersOptional.Count == 1)
		{
			SocialMan.TransitionToState(SocialMan.State.WAITING_IN_A_ROOM);
		}
		else
		{
			SocialMan.TransitionToState(SocialMan.State.CONNECTED_IN_A_ROOM);
		}

		// Look for users that left
		SocialMan.MarkAllRemoteUsersAsNotInRoom();

		if (msg.Data.UsersOptional != null)
		{
			foreach (User user in msg.Data.UsersOptional)
			{
				if (user.ID != SocialMan.MyID)
				{
					if (!SocialMan.IsUserInRoom(user.ID))
					{
						SocialMan.AddRemoteUser(user.ID);
					}
					else
					{
						SocialMan.MarkRemoteUserInRoom(user.ID);
					}
				}
			}
		}

		SocialMan.ForgetRemoteUsersNotInRoom();
	}

	#endregion
}
