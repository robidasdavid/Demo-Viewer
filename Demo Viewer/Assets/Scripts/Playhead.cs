using System;
using System.CodeDom;
using UnityEngine;
/// <summary>
/// Utility class for controlling playback of the game frames
/// </summary>
public class Playhead
{
	public Game game;

	public int CurrentFrameIndex {
		get => currentFrameIndex;
		set {
			currentFrameIndex = value;
			playheadLocation = GetNearestFrame().frameTime;
		}
	}
	private int currentFrameIndex;
	public int LastFrameIndex { get; private set; }

	public DateTime playheadLocation;
	public DateTime startTime;
	public DateTime endTime;

	public bool wasPlaying = false;
	public bool isPlaying = false;
	public bool isReverse = false;
	public bool wasPlayingBeforeScrub = false;
	public bool isScrubbing = false;
	public float playbackMultiplier = 1f;

	public Playhead(Game game)
	{
		this.game = game;

		// set start and end times
		startTime = game.frames[0].frameTime;
		endTime = game.frames[game.nframes - 1].frameTime;
	}

	public int FrameCount {
		get => game.nframes;
	}

	public void IncrementPlayhead(float deltaTime)
	{
		if (LiveFrameProvider.isLive)
		{
			playheadLocation += TimeSpan.FromSeconds(deltaTime);
		}
		else
		{
			deltaTime *= playbackMultiplier;

			playheadLocation += TimeSpan.FromSeconds(deltaTime * (isReverse ? -1 : 1));

			FindCurrentFrameLocation();

			// if the current playhead and the next recorded frame time are 1 second apart, just skip to the next frame
			Frame nextFrame = isReverse ? GetPreviousFrame() : GetNextFrame();
			float diff = (float)Math.Abs((nextFrame.frameTime - playheadLocation).TotalSeconds);
			if (diff > 1)
			{
				playheadLocation = nextFrame.frameTime;
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
		// if we are host of the room (or not in shared space)
		if (GameManager.instance.netFrameMan.IsLocalOrServer)
		{
			if (LiveFrameProvider.isLive)
			{
				return Frame.Lerp(LiveFrameProvider.lastFrame, LiveFrameProvider.frame, playheadLocation);
			}
			else
			{
				LastFrameIndex = currentFrameIndex;
				return Frame.Lerp(GetPreviousFrame(), GetNearestFrame(), playheadLocation);
			}
		}
		else
		{
			return Frame.FromJSON(DateTime.Now, GameManager.instance.netFrameMan.networkJsonData);
		}
	}

	public Frame GetNearestFrame()
	{
		return game.frames[Mathf.Clamp(currentFrameIndex, 0, game.nframes - 1)];
	}

	public Frame GetPreviousFrame()
	{
		if (LiveFrameProvider.isLive)
		{
			return LiveFrameProvider.lastFrame;
		}
		else
		{
			return game.frames[Mathf.Clamp(currentFrameIndex - 1, 0, game.nframes - 1)];
		}
	}

	private Frame GetNextFrame()
	{
		return game.frames[Mathf.Clamp(currentFrameIndex + 1, 0, game.nframes - 1)];
	}

	private void FindCurrentFrameLocation()
	{
		while (true)
		{
			// check if we are done searching
			if (playheadLocation >= GetPreviousFrame().frameTime &&
				playheadLocation <= GetNearestFrame().frameTime)
			{
				return;
			}

			if (GetNearestFrame().frameTime < playheadLocation)
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
			else if (currentFrameIndex <= 0)
			{
				isPlaying &= !isReverse;    // if we are trying to play forwards, just keep playing
				CurrentFrameIndex = 0;
				return;
			}
		}
	}
}
