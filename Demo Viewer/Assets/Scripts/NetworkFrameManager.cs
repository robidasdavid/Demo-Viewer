using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using ButterReplays;
using EchoVRAPI;
using Photon.Pun;
using UnityEngine;

public class NetworkFrameManager : MonoBehaviourPunCallbacks, IPunObservable
{
	public string networkFilename;
	public DateTime networkFrameTime = DateTime.Now;
	private int frameIndex = -1;
	private int lastFrameIndex = -1;

	public DateTime CorrectedNetworkFrameTime => 
		lastFrame?.recorded_time.AddSeconds((DemoStart.playhead.isPlaying ? 1 : 0) * 
		                                    DemoStart.playhead.playbackMultiplier * 
		                                    (Time.timeAsDouble - gameTimeAtLastFrame)) 
		?? DateTime.Now;

	public double gameTimeAtLastFrame;
	public string networkJsonData;
	public byte[] networkBinaryData;
	public Frame lastFrame;
	public Frame frame;
	public bool IsLocalOrServer => !PhotonNetwork.InRoom || photonView.IsMine;


	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			Frame f = DemoStart.playhead.GetNearestFrame();
			stream.SendNext(DemoStart.playhead.isPlaying);
			stream.SendNext(DemoStart.playhead.playbackMultiplier);
			stream.SendNext(DemoStart.playhead.CurrentFrameIndex);
			stream.SendNext(DemoStart.playhead.lastPlayheadLocation.ToFileTime());
			stream.SendNext(Path.GetFileNameWithoutExtension(DemoStart.playhead.game.filename));

			ButterFile bf = new ButterFile();
			bf.AddFrame(f);
			networkBinaryData = bf.GetBytes();
			List<Frame> f2 = ButterFile.FromBytes(networkBinaryData);
			stream.SendNext(networkBinaryData);
			// stream.SendNext(Zip(networkJsonData));
		}
		else if (stream.IsReading)
		{
			DemoStart.playhead.isPlaying = (bool)stream.ReceiveNext();
			DemoStart.playhead.playbackMultiplier = (float)stream.ReceiveNext();
			frameIndex = (int)stream.ReceiveNext();
			networkFrameTime = DateTime.FromFileTime((long)stream.ReceiveNext());
			networkFilename = (string)stream.ReceiveNext();
			networkBinaryData = (byte[])stream.ReceiveNext();
			// networkJsonData = ButterFile.Unzip((byte[])stream.ReceiveNext());

			// zipping saves about 90% for json
			// butter contains internal zipping
			// Debug.Log($"Raw size: {networkBinaryData.Length:N}, Compressed size: {ButterFile.Zip(networkBinaryData).Length:N}");


			if (frame == null || frameIndex != lastFrameIndex)
			{
				lastFrameIndex = frameIndex;
				lastFrame = frame;
				List<Frame> frames = ButterFile.FromBytes(networkBinaryData);
				if (frames.Count < 1) Debug.LogError("Didn't get a frame", this);
				if (frames.Count > 1) Debug.LogError("Got too many frames", this);
				frame = frames[0];
				gameTimeAtLastFrame = Time.timeAsDouble;
			}

			// frame is still null, make sure the last frame is too
			if (frame == null) lastFrame = null;
		}
	}

	public void BecomeHost()
	{
		photonView.TransferOwnership(PhotonNetwork.LocalPlayer);
	}
}