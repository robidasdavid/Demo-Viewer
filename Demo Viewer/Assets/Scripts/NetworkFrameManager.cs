using System;
using Photon.Pun;

public class NetworkFrameManager : MonoBehaviourPunCallbacks, IPunObservable
{
	public int networkFrameIndex; // TODO
	public bool networkPlaying;
	public string networkFilename;
	public DateTime networkFrameTime;
	public string networkJsonData;
	public Frame lastFrame;
	public Frame frame;
	public bool IsLocalOrServer => !PhotonNetwork.InRoom || photonView.IsMine;


	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(networkPlaying);
			stream.SendNext(networkFrameIndex);
			stream.SendNext(networkFrameTime.ToFileTime());
			stream.SendNext(networkFilename);
			stream.SendNext(networkJsonData);
		}

		if (stream.IsReading)
		{
			networkPlaying = (bool) stream.ReceiveNext();
			networkFrameIndex = (int) stream.ReceiveNext();
			networkFrameTime = DateTime.FromFileTime((long) stream.ReceiveNext());
			networkFilename = (string) stream.ReceiveNext();
			networkJsonData = (string) stream.ReceiveNext();

			if (lastFrame == null || lastFrame.frameTime != frame.frameTime)
			{
				lastFrame = frame;
			}

			if (frame == null || frame.frameTime != networkFrameTime)
			{
				frame = Frame.FromJSON(networkFrameTime, networkJsonData);
			}
		}
	}

	public void BecomeHost()
	{
		photonView.TransferOwnership(PhotonNetwork.LocalPlayer);
	}
}