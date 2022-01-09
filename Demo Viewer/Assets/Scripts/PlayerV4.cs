using System.Collections.Generic;
using EchoVRAPI;
using UnityEngine;
using UnityEngine.Serialization;
using Transform = UnityEngine.Transform;

public class PlayerV4 : MonoBehaviour
{
	private Player playerData;
	private BonePlayer bonePlayerData;

	[FormerlySerializedAs("body")] public Transform head;
	public Transform leftHand;
	public Transform rightHand;

	private GameObject skeleParent;
	private List<GameObject> flyingSkeleton = new List<GameObject>();

	public GameObject[] boneMapping = new GameObject[23];

	public void SetPlayerData(Team.TeamColor teamColor, Player player, BonePlayer bonePlayer)
	{
		playerData = player;
		bonePlayerData = bonePlayer;

		Vector3 thisPos = player.body.Position;
		// (thisPos.x, thisPos.z) = (thisPos.z, thisPos.x);
		transform.SetPositionAndRotation(thisPos, player.body.Rotation);

		head.SetPositionAndRotation(player.head.Position, player.head.Rotation);
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
					if (skeleParent == null)
					{
						skeleParent = new GameObject("Skeleton Parent");
						skeleParent.transform.SetParent(transform);
						skeleParent.transform.localEulerAngles = new Vector3(0, -90, 0);
						// skeleParent.transform.localEulerAngles = new Vector3(0,0,0);
						skeleParent.transform.localPosition = Vector3.zero;
					}

					flyingSkeleton[i].transform.SetParent(skeleParent.transform);
					GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Cube);
					sphere.transform.SetParent(flyingSkeleton[i].transform);
					sphere.transform.localScale = new Vector3(.1f,.05f,.05f);
				}

				Vector3 pos = bones[i].Item1;
				// (pos.x, pos.z) = (pos.z, pos.x);
				flyingSkeleton[i].transform.localPosition = pos;
				// flyingSkeleton[i].transform.RotateAround(transform.position, Vector3.up, 90f);
				flyingSkeleton[i].transform.localRotation = bones[i].Item2;


				// actually set rig bones
				if (boneMapping[i] != null)
				{
					boneMapping[i].transform.rotation = skeleParent.transform.rotation * bones[i].Item2;
					// boneMapping[i].transform.position = skeleParent.transform.TransformPoint(bones[i].Item1);
				}
			}
		}

		// transform.SetPositionAndRotation(player.body.Position, player.body.Rotation);
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