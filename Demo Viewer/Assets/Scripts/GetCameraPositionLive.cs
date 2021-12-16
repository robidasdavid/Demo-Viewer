using System;
using EchoVRAPI;
using UnityEngine;

public class GetCameraPositionLive : MonoBehaviour
{
	private void Start()
	{
		LiveFrameProvider.isLive = true;
	}

	private void Update()
	{
		if (LiveFrameProvider.frame == null) return;

		EchoVRAPI.VRPlayer player = LiveFrameProvider.frame.player;
		transform.position = player.vr_position.ToVector3();
		transform.forward = player.vr_forward.ToVector3();
		transform.up = player.vr_up.ToVector3();

		Debug.DrawRay(player.vr_position.ToVector3(), player.vr_forward.ToVector3(), Color.blue);
		Debug.DrawRay(player.vr_position.ToVector3(), player.vr_left.ToVector3(), Color.red);
		Debug.DrawRay(player.vr_position.ToVector3(), player.vr_up.ToVector3(), Color.green);
	}
}