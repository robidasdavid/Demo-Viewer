using System;
using System.Collections;
using System.Collections.Generic;
using EchoVRAPI;
using Photon.Compression;
using UnityEngine;
using UnityEngine.Serialization;
using Transform = UnityEngine.Transform;

public class PlayerV4 : MonoBehaviour
{
	private Player playerData;
	private BonePlayer bonePlayerData;

	[FormerlySerializedAs("head")] public Transform body;
	public Transform leftHand;
	public Transform rightHand;

	private List<GameObject> flyingSkeleton = new List<GameObject>();

	public void SetPlayerData(Team.TeamColor teamColor, Player player, BonePlayer bonePlayer)
	{
		playerData = player;
		bonePlayerData = bonePlayer;

		transform.SetPositionAndRotation(player.head.Position, player.head.Rotation);

		body.SetPositionAndRotation(player.body.Position, player.body.Rotation);
		leftHand.SetPositionAndRotation(player.lhand.Position, player.lhand.Rotation);
		rightHand.SetPositionAndRotation(player.rhand.Position, player.rhand.Rotation);

		if (bonePlayer != null)
		{
			(Vector3, Quaternion)[] bones = bonePlayer.GetPoses();
			for (int i = 0; i < bones.Length; i++)
			{
				if (flyingSkeleton.Count <= i)
				{
					flyingSkeleton.Add(new GameObject("skele"));
					flyingSkeleton[i].transform.SetParent(transform);
					GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
					sphere.transform.SetParent(flyingSkeleton[i].transform);
					sphere.transform.localScale = Vector3.one * .1f;
				}

				flyingSkeleton[i].transform.localPosition = bones[i].Item1;
				flyingSkeleton[i].transform.localRotation = bones[i].Item2;
			}
		}
	}

	private void Update()
	{
		(Vector3, Quaternion)[] bones = bonePlayerData.GetPoses();
		Debug.DrawLine(transform.TransformPoint(bones[2].Item1), transform.TransformPoint(bones[4].Item1));
		Debug.DrawLine(transform.TransformPoint(bones[4].Item1), transform.TransformPoint(bones[6].Item1));
		Debug.DrawLine(transform.TransformPoint(bones[3].Item1), transform.TransformPoint(bones[5].Item1));
		Debug.DrawLine(transform.TransformPoint(bones[5].Item1), transform.TransformPoint(bones[7].Item1));
	}
}