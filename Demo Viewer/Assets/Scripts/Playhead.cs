using System;
using System.IO;
using EchoVRAPI;
using UnityEngine;

/// <summary>
/// Utility class for controlling playback of the game frames
/// </summary>
public class Playhead : MonoBehaviour
{
	public Replay replay;

	public int CurrentFrameIndex
	{
		get => currentFrameIndex;
		set
		{
			currentFrameIndex = value;
			playheadLocation = GetNearestFrame()?.recorded_time ?? DateTime.MinValue;
		}
	}

	private int currentFrameIndex;
	public int LastFrameIndex { get; set; }

	public DateTime playheadLocation;
	public DateTime StartTime => replay.GetFrame(0)?.recorded_time ?? DateTime.MinValue;
	public DateTime EndTime => replay.GetFrame(replay.FrameCount - 1)?.recorded_time ?? DateTime.MinValue;

	public DateTime lastPlayheadLocation;
	private Frame lastFrame;

	public bool wasPlaying;
	public bool isPlaying;
	public bool isReverse;
	public bool wasPlayingBeforeScrub;
	public bool isScrubbing;
	public float playbackMultiplier = 1f;


	public int FrameCount => replay.FrameCount;

	public void IncrementPlayhead(float deltaTime)
	{
		if (LiveFrameProvider.isLive)
		{
			playheadLocation += TimeSpan.FromSeconds(deltaTime);
		}
		else
		{
			deltaTime *= playbackMultiplier;

			try
			{
				playheadLocation += TimeSpan.FromSeconds(deltaTime * (isReverse ? -1 : 1));
			}
			catch (ArgumentOutOfRangeException)
			{
				isPlaying = false;
				return;
			}

			FindCurrentFrameLocation();

			// if the current and next recorded frame time are 1 second apart, just skip to the next frame
			// this is for replay files with big gaps
			Frame nextFrame = isReverse ? GetPreviousFrame() : GetNextFrame();
			if (nextFrame != null)
			{
				float diff = (float)Math.Abs((nextFrame.recorded_time - playheadLocation).TotalSeconds);
				if (diff > 1)
				{
					playheadLocation = nextFrame.recorded_time;
				}
			}
		}
	}


	/// <summary>
	/// ⏯ Set playing variable to start and stop auto-play of demo.
	/// </summary>
	/// <param name="value">True to play, False to pause</param>
	public void SetPlaying(bool value)
	{
		isReverse = false;
		isPlaying = value;
	}

	public Frame GetFrame()
	{
		// if we are not host of the room, get the frame from the network
		if (!GameManager.instance.netFrameMan.IsLocalOrServer)
		{
			// if (GameManager.instance.netFrameMan.frame == null) GameManager.instance.netFrameMan.lastFrame = null;
			Frame lastNetFrame = GameManager.instance.netFrameMan.lastFrame;
			Frame nextNetFrame = GameManager.instance.netFrameMan.frame;
			if (lastNetFrame.recorded_time > nextNetFrame.recorded_time)
			{
				return Frame.Lerp(nextNetFrame, lastNetFrame, GameManager.instance.netFrameMan.CorrectedNetworkFrameTime);
			}
			else
			{
				return Frame.Lerp(lastNetFrame, nextNetFrame, GameManager.instance.netFrameMan.CorrectedNetworkFrameTime);
			}
		}

		if (LiveFrameProvider.isLive)
		{
			return Frame.Lerp(LiveFrameProvider.lastFrame, LiveFrameProvider.frame, playheadLocation);
		}

		if (lastPlayheadLocation == playheadLocation && lastFrame != null)
		{
			return lastFrame;
		}

		// send playhead info to other players ⬆
		GameManager.instance.netFrameMan.networkFilename = Path.GetFileNameWithoutExtension(replay.FileName);
		lastFrame = Frame.Lerp(GetPreviousFrame(), GetNearestFrame(), playheadLocation);
		lastPlayheadLocation = playheadLocation;
		return lastFrame;
	}

	public Frame GetNearestFrame()
	{
		if (LiveFrameProvider.isLive)
		{
			return LiveFrameProvider.frame;
		}

		return replay.GetFrame(Mathf.Clamp(currentFrameIndex, 0, replay.FrameCount - 1));
	}

	public Frame GetPreviousFrame()
	{
		// if we are not host of the room
		if (!GameManager.instance.netFrameMan.IsLocalOrServer)
		{
			return GameManager.instance.netFrameMan.lastFrame;
		}

		if (LiveFrameProvider.isLive)
		{
			return LiveFrameProvider.lastFrame;
		}

		return replay.GetFrame(Mathf.Clamp(currentFrameIndex - 1, 0, replay.FrameCount - 1));
	}

	private Frame GetNextFrame()
	{
		return replay.GetFrame(Mathf.Clamp(currentFrameIndex + 1, 0, replay.FrameCount - 1));
	}

	private void FindCurrentFrameLocation()
	{
		while (true)
		{
			if (replay.FrameCount == 0)
			{
				currentFrameIndex = 0;
				isPlaying = false;
				return;
			}

			// check if we are done searching
			if (playheadLocation >= GetPreviousFrame()?.recorded_time &&
			    playheadLocation <= GetNearestFrame()?.recorded_time)
			{
				return;
			}

			if (GetNearestFrame().recorded_time < playheadLocation)
			{
				currentFrameIndex++;
			}
			else
			{
				currentFrameIndex--;
			}


			// if beyond start or end, stop playing
			if (currentFrameIndex >= FrameCount - 1)
			{
				isPlaying &= isReverse; // if we are trying to play backwards, just keep playing
				CurrentFrameIndex = FrameCount - 1;
				return;
			}

			if (currentFrameIndex <= 0)
			{
				isPlaying &= !isReverse; // if we are trying to play forwards, just keep playing
				CurrentFrameIndex = 0;
				return;
			}
		}
	}
}