using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SetCameraPositionLive : MonoBehaviour
{
	[Serializable]
	public class SendCameraPosData
	{
		[Serializable]
		public class Vector3Json
		{
			public Vector3Json(Vector3 v)
			{
				X = -v.x;
				Y = v.y;
				Z = v.z;
			}

			public float X;
			public float Y;
			public float Z;
		}

		[Serializable]
		public class QuaternionJson
		{
			public QuaternionJson(Quaternion q)
			{
				X = q.x;
				Y = -q.y;
				Z = -q.z;
				W = q.w;
			}

			public float X;
			public float Y;
			public float Z;
			public float W;
		}

		public Vector3Json position;
		public QuaternionJson rotation;
	}

	private void Update()
	{
		string jsonData = JsonUtility.ToJson(new SendCameraPosData
		{
			position = new SendCameraPosData.Vector3Json(transform.localPosition),
			rotation = new SendCameraPosData.QuaternionJson(transform.localRotation)
		});

		UnityWebRequest req = UnityWebRequest.Post("http://127.0.0.1:6723", jsonData);
		req.uploadHandler = new UploadHandlerRaw(string.IsNullOrEmpty(jsonData) ? null : Encoding.UTF8.GetBytes(jsonData));
		req.SendWebRequest();
	}
}