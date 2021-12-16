using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using EchoVRAPI;
using Photon.Pun;
using UnityEngine;

public class NetworkFrameManager : MonoBehaviourPunCallbacks, IPunObservable
{
	public int networkFrameIndex; // TODO
	public bool networkPlaying;
	public float networkPlaySpeed;
	public string networkFilename;
	public DateTime networkFrameTime = DateTime.Now;

	public DateTime CorrectedNetworkFrameTime =>
		networkFrameTime.AddSeconds((networkPlaying ? 1 : 0) * networkPlaySpeed * (Time.time - gameTimeAtLastFrame));

	public float gameTimeAtLastFrame;
	public string networkJsonData;
	public Frame lastFrame;
	public Frame frame;
	public bool IsLocalOrServer => !PhotonNetwork.InRoom || photonView.IsMine;


	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			//if (networkFrameTime == new DateTime()) networkFrameTime = DateTime.Now;
			stream.SendNext(networkPlaying);
			stream.SendNext(networkPlaySpeed);
			stream.SendNext(networkFrameIndex);
			stream.SendNext(networkFrameTime.ToFileTime());
			stream.SendNext(networkFilename);
			stream.SendNext(Zip(networkJsonData));
		}

		if (stream.IsReading)
		{
			networkPlaying = (bool)stream.ReceiveNext();
			networkPlaySpeed = (float)stream.ReceiveNext();
			networkFrameIndex = (int)stream.ReceiveNext();
			networkFrameTime = DateTime.FromFileTime((long)stream.ReceiveNext());
			networkFilename = (string)stream.ReceiveNext();
			networkJsonData = Unzip((byte[])stream.ReceiveNext());
			
			// zipping saves about 90%
			// Debug.Log($"Raw size: {Encoding.Unicode.GetByteCount(networkJsonData):N}, Compressed size: {Zip(networkJsonData).Length:N}");

			if (lastFrame == null || lastFrame.recorded_time != frame.recorded_time)
			{
				lastFrame = frame;
			}

			if (frame == null || frame.recorded_time != networkFrameTime)
			{
				frame = Frame.FromJSON(networkFrameTime, networkJsonData);
			}
			
			// frame is still null, make sure the last frame is too
			if (frame == null) lastFrame = null;

			gameTimeAtLastFrame = Time.time;
		}
	}

	public void BecomeHost()
	{
		photonView.TransferOwnership(PhotonNetwork.LocalPlayer);
	}
	
	
	public static void CopyTo(Stream src, Stream dest) {
		byte[] bytes = new byte[4096];

		int cnt;

		while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0) {
			dest.Write(bytes, 0, cnt);
		}
	}

	public static byte[] Zip(string str) {
		byte[] bytes = Encoding.UTF8.GetBytes(str);

		using (var msi = new MemoryStream(bytes))
		using (var mso = new MemoryStream()) {
			using (var gs = new GZipStream(mso, CompressionMode.Compress)) {
				//msi.CopyTo(gs);
				CopyTo(msi, gs);
			}

			return mso.ToArray();
		}
	}

	public static string Unzip(byte[] bytes) {
		using (var msi = new MemoryStream(bytes))
		using (var mso = new MemoryStream()) {
			using (var gs = new GZipStream(msi, CompressionMode.Decompress)) {
				//gs.CopyTo(mso);
				CopyTo(gs, mso);
			}

			return Encoding.UTF8.GetString(mso.ToArray());
		}
	}
}