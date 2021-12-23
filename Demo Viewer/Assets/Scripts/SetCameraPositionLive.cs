using System.Collections.Generic;
using System.Text;
using EchoVRAPI;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class SetCameraPositionLive : MonoBehaviour
{
	private static float fov = 1;
	public static float FOV
	{
		set => fov = value / 60f;
		get => fov;
	}
	
	private void Update()
	{
		Vector3 p = transform.localPosition;
		Quaternion r = Math2.QuaternionLookRotation(transform.localRotation.ForwardBackwards(), transform.localRotation.UpBackwards());;

		string jsonData = JsonConvert.SerializeObject(new Dictionary<string, float>()
		{
			{"px", p.z},
			{"py", p.y},
			{"pz", p.x},
			{"qx", r.x},
			{"qy", r.y},
			{"qz", r.z},
			{"qw", r.w},
			{"fovy", fov},
		});

		UnityWebRequest req = UnityWebRequest.Post("http://127.0.0.1:6721/camera_transform", jsonData);
		req.uploadHandler =
			new UploadHandlerRaw(string.IsNullOrEmpty(jsonData) ? null : Encoding.UTF8.GetBytes(jsonData));
		req.SendWebRequest();
	}
}