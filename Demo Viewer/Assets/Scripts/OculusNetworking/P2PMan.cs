using UnityEngine;
using System;
using Oculus.Platform;
using Oculus.Platform.Models;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

// Helper class to manage a Peer-to-Peer connection to the other user.
// The connection is used to send and received the Transforms for the
// Avatars.  The Transforms are sent via unreliable UDP at a fixed
// frequency.
public class P2PMan
{
	public Frame lastReceivedFrame;

	// packet header is a message type byte
	public enum MessageType : byte
	{
		Update = 1,
		Frame = 2,
		Update2D = 3
	};

	public P2PMan()
	{
		Net.SetPeerConnectRequestCallback(PeerConnectRequestCallback);
		Net.SetConnectionStateChangedCallback(ConnectionStateChangedCallback);
	}

	#region Connection Management

	public void ConnectTo(ulong userID)
	{
		// ID comparison is used to decide who calls Connect and who calls Accept
		if (SocialMan.MyID < userID)
		{
			Net.Connect(userID);
			SocialMan.LogOutput("P2P connect to " + userID);
		}
	}

	public void Disconnect(ulong userID)
	{
		if (userID != 0)
		{
			Net.Close(userID);

			RemotePlayer remote = SocialMan.GetRemoteUser(userID);
			if (remote != null)
			{
				remote.p2pConnectionState = PeerConnectionState.Unknown;
			}
		}
	}

	void PeerConnectRequestCallback(Message<NetworkingPeer> msg)
	{
		SocialMan.LogOutput("P2P request from " + msg.Data.ID);

		RemotePlayer remote = SocialMan.GetRemoteUser(msg.Data.ID);
		if (remote != null)
		{
			SocialMan.LogOutput("P2P request accepted from " + msg.Data.ID);
			Net.Accept(msg.Data.ID);
		}
	}

	void ConnectionStateChangedCallback(Message<NetworkingPeer> msg)
	{
		SocialMan.LogOutput("P2P state to " + msg.Data.ID + " changed to  " + msg.Data.State);

		RemotePlayer remote = SocialMan.GetRemoteUser(msg.Data.ID);
		if (remote != null)
		{
			remote.p2pConnectionState = msg.Data.State;

			if (msg.Data.State == PeerConnectionState.Timeout &&
				// ID comparison is used to decide who calls Connect and who calls Accept
				SocialMan.MyID < msg.Data.ID)
			{
				// keep trying until hangup!
				Net.Connect(msg.Data.ID);
				SocialMan.LogOutput("P2P re-connect to " + msg.Data.ID);
			}
		}
	}

	#endregion

	#region Message Sending

	public void SendAvatarUpdate(ulong userID, Transform rootTransform, uint sequence, byte[] avatarPacket)
	{
		const int UPDATE_DATA_LENGTH = 41;
		byte[] sendBuffer = new byte[avatarPacket.Length + UPDATE_DATA_LENGTH];

		int offset = 0;
		PackByte((byte)MessageType.Update, sendBuffer, ref offset);

		PackULong(SocialMan.MyID, sendBuffer, ref offset);

		PackVector3(rootTransform.position, sendBuffer, ref offset);
		PackQuaternion(rootTransform.rotation, sendBuffer, ref offset);

		PackUInt32(sequence, sendBuffer, ref offset);

		Debug.Assert(offset == UPDATE_DATA_LENGTH);

		Buffer.BlockCopy(avatarPacket, 0, sendBuffer, offset, avatarPacket.Length);
		Net.SendPacket(userID, sendBuffer, SendPolicy.Unreliable);
	}

	/// <summary>
	/// Adds the necessary headers and sends the byte array over the network
	/// </summary>
	/// <param name="userID"></param>
	/// <param name="messageType"></param>
	/// <param name="data"></param>
	public void SendBytes(ulong userID, MessageType messageType, byte[] data)
	{
		byte[] sendBuffer = new byte[data.Length + sizeof(byte) + sizeof(ulong)];

		int offset = 0;
		PackByte((byte)messageType, sendBuffer, ref offset);

		PackULong(SocialMan.MyID, sendBuffer, ref offset);
		Buffer.BlockCopy(data, 0, sendBuffer, offset, data.Length);
		Net.SendPacket(userID, sendBuffer, SendPolicy.Unreliable);
	}

	#endregion

	#region Message Receiving

	public void GetRemotePackets()
	{
		Packet packet;

		while ((packet = Net.ReadPacket()) != null)
		{
			byte[] receiveBuffer = new byte[packet.Size];
			packet.ReadBytes(receiveBuffer);

			int offset = 0;
			MessageType messageType = (MessageType)ReadByte(receiveBuffer, ref offset);

			ulong remoteUserID = ReadULong(receiveBuffer, ref offset);
			RemotePlayer remote = SocialMan.GetRemoteUser(remoteUserID);
			if (remote == null)
			{
				SocialMan.LogOutput("Unknown remote player: " + remoteUserID);
				continue;
			}

			if (messageType == MessageType.Update)
			{
				processAvatarPacket(remote, ref receiveBuffer, ref offset);
			}
			else if (messageType == MessageType.Frame)
			{
				lastReceivedFrame = Frame.FromJSON(DateTime.Now, Encoding.ASCII.GetString(receiveBuffer, offset, receiveBuffer.Length - offset));
			}
			else if (messageType == MessageType.Update2D)
			{
				// TODO
			}
			else
			{
				SocialMan.LogOutput("Invalid packet type: " + packet.Size);
				continue;
			}

		}
	}

	public void processAvatarPacket(RemotePlayer remote, ref byte[] packet, ref int offset)
	{
		if (remote == null)
			return;

		remote.receivedRootPositionPrior = remote.receivedRootPosition;
		remote.receivedRootPosition = ReadVector3(packet, ref offset);

		remote.receivedRootRotationPrior = remote.receivedRootRotation;
		remote.receivedRootRotation = ReadQuaternion(packet, ref offset);

		remote.RemoteAvatar.transform.position = remote.receivedRootPosition;
		remote.RemoteAvatar.transform.rotation = remote.receivedRootRotation;

		// forward the remaining data to the avatar system
		int sequence = (int)ReadUInt32(packet, ref offset);

		byte[] remainingAvatarBuffer = new byte[packet.Length - offset];
		Buffer.BlockCopy(packet, offset, remainingAvatarBuffer, 0, remainingAvatarBuffer.Length);

		IntPtr avatarPacket = Oculus.Avatar.CAPI.ovrAvatarPacket_Read((UInt32)remainingAvatarBuffer.Length, remainingAvatarBuffer);

		var ovravatarPacket = new OvrAvatarPacket { ovrNativePacket = avatarPacket };
		remote.RemoteAvatar.GetComponent<OvrAvatarRemoteDriver>().QueuePacket(sequence, ovravatarPacket);
	}
	#endregion

	#region Serialization

	// Convert an object to a byte array
	public static byte[] ObjectToByteArray(object obj)
	{
		BinaryFormatter bf = new BinaryFormatter();
		using (var ms = new MemoryStream())
		{
			bf.Serialize(ms, obj);
			return ms.ToArray();
		}
	}

	// Convert a byte array to an Object
	public static object ByteArrayToObject(ref byte[] arrBytes, ref int offset)
	{
		using (var memStream = new MemoryStream())
		{
			var binForm = new BinaryFormatter();
			memStream.Write(arrBytes, offset, arrBytes.Length);
			memStream.Seek(0, SeekOrigin.Begin);
			var obj = binForm.Deserialize(memStream);
			return obj;
		}
	}

	void PackByte(byte b, byte[] buf, ref int offset)
	{
		buf[offset] = b;
		offset += sizeof(byte);
	}
	byte ReadByte(byte[] buf, ref int offset)
	{
		byte val = buf[offset];
		offset += sizeof(byte);
		return val;
	}

	void PackQuaternion(Quaternion q, byte[] buf, ref int offset)
	{
		PackFloat(q.x, buf, ref offset);
		PackFloat(q.y, buf, ref offset);
		PackFloat(q.z, buf, ref offset);
		PackFloat(q.w, buf, ref offset);
	}

	Quaternion ReadQuaternion(byte[] buf, ref int offset)
	{
		return new Quaternion(
			ReadFloat(buf, ref offset),
			ReadFloat(buf, ref offset),
			ReadFloat(buf, ref offset),
			ReadFloat(buf, ref offset));
	}

	public void PackVector3(Vector3 v, byte[] buf, ref int offset)
	{
		PackFloat(v.x, buf, ref offset);
		PackFloat(v.y, buf, ref offset);
		PackFloat(v.z, buf, ref offset);
	}

	Vector3 ReadVector3(byte[] buf, ref int offset)
	{
		return new Vector3(
			ReadFloat(buf, ref offset),
			ReadFloat(buf, ref offset),
			ReadFloat(buf, ref offset));
	}

	void PackFloat(float f, byte[] buf, ref int offset)
	{
		Buffer.BlockCopy(BitConverter.GetBytes(f), 0, buf, offset, sizeof(float));
		offset += sizeof(float);
	}
	float ReadFloat(byte[] buf, ref int offset)
	{
		float val = BitConverter.ToSingle(buf, offset);
		offset += sizeof(float);
		return val;
	}

	void PackULong(ulong u, byte[] buf, ref int offset)
	{
		Buffer.BlockCopy(BitConverter.GetBytes(u), 0, buf, offset, sizeof(ulong));
		offset += sizeof(ulong);
	}
	ulong ReadULong(byte[] buf, ref int offset)
	{
		ulong val = BitConverter.ToUInt64(buf, offset);
		offset += sizeof(ulong);
		return val;
	}

	void PackUInt32(UInt32 u, byte[] buf, ref int offset)
	{
		Buffer.BlockCopy(BitConverter.GetBytes(u), 0, buf, offset, sizeof(UInt32));
		offset += sizeof(UInt32);
	}
	UInt32 ReadUInt32(byte[] buf, ref int offset)
	{
		UInt32 val = BitConverter.ToUInt32(buf, offset);
		offset += sizeof(UInt32);
		return val;
	}

	#endregion
}
