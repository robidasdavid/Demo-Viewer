using Newtonsoft.Json;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class LiveFrameProvider : MonoBehaviour
{
	public static bool isLive;

	public static Frame frame;
	public static Frame lastFrame;

	public bool local = true;
	public string localAPIURL = "http://127.0.0.1:6721/session";
	public string networkAPIURL = "localhost:5005/live_replay/";
	public string session_id;

	float lastFrameTime = 0;

	// fps of fetching
	float updateRate = 60;

	// Update is called once per frame
	void Update()
	{
		if (isLive)
		{
			if (Time.time - lastFrameTime > (1 / updateRate))
			{
				StartCoroutine(GetNewFrame());
				lastFrameTime = Time.time;
			}
		}
	}

	IEnumerator GetNewFrame()
	{
		UnityWebRequest request;
		if (local)
		{
			request = UnityWebRequest.Get(localAPIURL);
		}
		else
		{
			request = UnityWebRequest.Get(networkAPIURL + session_id);
		}
		yield return request.SendWebRequest();

		if (request.isNetworkError)
		{
			// not in match
		}
		else
		{
			lastFrame = frame;
			try
			{
				frame = Frame.FromJSON(DateTime.Now, request.downloadHandler.text);
				if (lastFrame != null && DemoStart.playhead != null)
				{
					DemoStart.playhead.playheadLocation = lastFrame.frameTime;
				}
			}
			catch (ArgumentException)
			{
				// not in match or something idk
				// not sure when it gets invalid return data except in lobby
			}

		}

		request.Dispose();
	}


}
