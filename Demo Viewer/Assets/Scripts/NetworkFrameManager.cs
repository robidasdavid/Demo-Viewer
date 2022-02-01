using System;
using System.Collections.Generic;
using System.IO;
using ButterReplays;
using EchoVRAPI;
using UnityEngine;
using VelNet;

public class NetworkFrameManager : NetworkSerializedObjectStream
{
	public string networkFilename;
	public DateTime networkFrameTime = DateTime.Now;
	private int frameIndex = -1;
	private int lastFrameIndex = -1;

	public DateTime CorrectedNetworkFrameTime => 
		lastFrame?.recorded_time.AddSeconds((DemoStart.instance.playhead.isPlaying ? 1 : 0) * 
		                                    DemoStart.instance.playhead.playbackMultiplier * 
		                                    (Time.timeAsDouble - gameTimeAtLastFrame)) 
		?? DateTime.Now;

	public double gameTimeAtLastFrame;
	public int networkNFrames = 0;
	public string networkJsonData;
	public byte[] networkBinaryData;
	public Frame lastFrame;
	public Frame frame;
	public bool IsLocalOrServer => !VelNetManager.InRoom || IsMine;

	public void BecomeHost()
	{
		networkObject.TakeOwnership();
	}
	protected override void SendState(BinaryWriter binaryWriter)
	{
		Frame f = DemoStart.instance.playhead.GetNearestFrame();
		binaryWriter.Write(DemoStart.instance.playhead.isPlaying);
		binaryWriter.Write(DemoStart.instance.replay.FrameCount);
		binaryWriter.Write(DemoStart.instance.playhead.playbackMultiplier);
		binaryWriter.Write(DemoStart.instance.playhead.CurrentFrameIndex);
		binaryWriter.Write(DemoStart.instance.playhead.lastPlayheadLocation.ToFileTimeUtc());
		binaryWriter.Write(Path.GetFileNameWithoutExtension(DemoStart.instance.replay.FileName));

		ButterFile bf = new ButterFile();
		bf.AddFrame(f);
		networkBinaryData = bf.GetBytes();
		binaryWriter.Write(networkBinaryData.Length);
		binaryWriter.Write(networkBinaryData);
	}

	protected override void ReceiveState(BinaryReader binaryReader)
	{
		DemoStart.instance.playhead.isPlaying = binaryReader.ReadBoolean();
		networkNFrames = binaryReader.ReadInt32();
		DemoStart.instance.playhead.playbackMultiplier = binaryReader.ReadSingle();
		frameIndex = binaryReader.ReadInt32();
		networkFrameTime = DateTime.FromFileTimeUtc(binaryReader.ReadInt64());
		networkFilename = binaryReader.ReadString();
		networkBinaryData = binaryReader.ReadBytes(binaryReader.ReadInt32());

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