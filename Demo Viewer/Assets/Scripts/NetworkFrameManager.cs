using System;
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
			stream.SendNext(networkJsonData);
		}

		if (stream.IsReading)
		{
			networkPlaying = (bool)stream.ReceiveNext();
			networkPlaySpeed = (float)stream.ReceiveNext();
			networkFrameIndex = (int)stream.ReceiveNext();
			networkFrameTime = DateTime.FromFileTime((long)stream.ReceiveNext());
			networkFilename = (string)stream.ReceiveNext();
			networkJsonData = (string)stream.ReceiveNext();

			if (lastFrame == null || lastFrame.frameTime != frame.frameTime)
			{
				lastFrame = frame;
			}

			if (frame == null || frame.frameTime != networkFrameTime)
			{
				frame = Frame.FromJSON(networkFrameTime, networkJsonData);
			}

			gameTimeAtLastFrame = Time.time;
		}
	}

	public void BecomeHost()
	{
		photonView.TransferOwnership(PhotonNetwork.LocalPlayer);
	}
}