using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class GetBonesWow : MonoBehaviour
{
	// Start is called before the first frame update
	void Start()
	{
	}

	// Update is called once per frame
	void Update()
	{
		StartCoroutine(GetNewFrame());
	}

	private IEnumerator GetNewFrame()
	{
		
		using (UnityWebRequest req = UnityWebRequest.Get("http://localhost:6721/player_bones"))
		{
			yield return req.SendWebRequest();
			string strData = req.downloadHandler.text;
			Debug.Log(strData);
			JObject data = JsonConvert.DeserializeObject<JObject>(strData);

			Debug.Log(data["user_bones"]);
			
		}
	}

	// private class Bones
	// {
	// 	public Bone
	// }
}