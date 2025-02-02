﻿using Newtonsoft.Json;
using System;
using System.Collections;
using EchoVRAPI;
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

	double lastFetchTime = 0;

	// fps of fetching
	public float updateRate = 60;

	// Update is called once per frame
	void Update()
	{
		if (isLive)
		{
			if (Time.timeAsDouble - lastFetchTime > (1 / updateRate))
			{
				StartCoroutine(GetNewFrame());
				lastFetchTime = Time.timeAsDouble;
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

		if (request.result == UnityWebRequest.Result.Success)
		{
			lastFrame = frame;
			try
			{
				frame = Frame.FromJSON(DateTime.Now, request.downloadHandler.text, null);
				if (lastFrame != null && DemoStart.instance.playhead != null)
				{
					DemoStart.instance.playhead.playheadLocation = lastFrame.recorded_time;
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
