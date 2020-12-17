using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

public class LoadReplayIntoPointCloud : MonoBehaviour
{

	public float fileReadProgress;
	public object loadedDemoLock = new object();
	public Game loadedDemo;

	// Start is called before the first frame update
	IEnumerator Start()
	{
		string demoFile = PlayerPrefs.GetString("fileDirector");
		if (!string.IsNullOrEmpty(demoFile))
		{
			Debug.Log("Reading file: " + demoFile);
			StreamReader reader = new StreamReader(demoFile);

			Thread loadThread = new Thread(() => ReadReplayFile(reader));
			loadThread.Start();
			while (loadThread.IsAlive)
			{
				// maybe put a progress bar here
				yield return null;
			}
		}


	}

	// Update is called once per frame
	void Update()
	{

	}

	void ProcessGameFrames()
	{
		for (int i = 0; i < loadedDemo.rawFrames.Count; i++)
		{
			loadedDemo.GetFrame(i);
		}
	}


	/// <summary>
	/// Actually reads the replay file into memory
	/// This is a thread on desktop versions
	/// </summary>
	void ReadReplayFile(StreamReader fileReader)
	{
		using (fileReader = DemoStart.OpenOrExtract(fileReader))
		{
			fileReadProgress = 0;
			List<string> allLines = new List<string>();
			do
			{
				allLines.Add(fileReader.ReadLine());
				fileReadProgress += .0001f;
				fileReadProgress %= 1;
			} while (!fileReader.EndOfStream);

			//string fileData = fileReader.ReadToEnd();
			//List<string> allLines = fileData.LowMemSplit("\n");

			Game readGame = new Game
			{
				rawFrames = allLines,
				nframes = allLines.Count,
				frames = new List<Frame>(new Frame[allLines.Count])
			};

			lock (loadedDemoLock)
			{
				loadedDemo = readGame;
			}
		}

	}
}
