using System;
using System.Collections.Generic;
using EchoVRAPI;
using UnityEngine;

[Serializable]
public class Game
{
	public bool isNewstyle;
	public int nframes;
	public string filename;
	public List<string> rawFrames;
	public List<Frame> frames { get; set; }
	public Mesh pointCloud;

	/// <summary>
	/// Gets or converts the requested frame.
	/// May return null if the frame can't be converted.
	/// </summary>
	public Frame GetFrame(int index)
	{
		if (frames[index] != null) return frames[index];

		// repeat because maybe the requested frame needs to be discarded.
		while (rawFrames.Count > 0)
		{
			Frame newFrame = Frame.FromEchoReplayString(rawFrames[index]);
			if (newFrame != null)
			{
				frames[index] = newFrame;
				// rawFrames[index] = null;    // free up the memory, since the raw frames take up a lot more
				return frames[index];
			}

			Debug.LogError($"Discarded frame {index}");
			frames.RemoveAt(index);
			rawFrames.RemoveAt(index);
			nframes--;
		}

		Debug.LogError("File contains no valid arena frames.");
		return null;
	}
}