using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using UnityEngine.Networking;

public class SetCameraPositionLive : MonoBehaviour
{
	private void Update()
	{
		Vector3 localPosition = transform.localPosition;
		Quaternion localRotation = transform.localRotation;
		string jsonData = JsonConvert.SerializeObject(new Dictionary<string, float>()
		{
			{"px", -localPosition.x},
			{"py", localPosition.y},
			{"pz", localPosition.z},
			{"qx", localRotation.x},
			{"qy", localRotation.y},
			{"qz", localRotation.z},
			{"qw", localRotation.w},
			{"fovy", 1},
		});

		UnityWebRequest req = UnityWebRequest.Post("http://127.0.0.1:6721/camera_transform", jsonData);
		req.uploadHandler = new UploadHandlerRaw(string.IsNullOrEmpty(jsonData) ? null : Encoding.UTF8.GetBytes(jsonData));
		req.SendWebRequest();
	}
}