using System;
using System.IO;
using ButterReplays;
using EchoVRAPI;
using Newtonsoft.Json;
using Spark;
using UnityEngine;

/// <summary>
/// Utility class for controlling playback of the game frames
/// </summary>
public class Playhead
{
	public Game game;

	public int CurrentFrameIndex
	{
		get => currentFrameIndex;
		set
		{
			currentFrameIndex = value;
			playheadLocation = GetNearestFrame().recorded_time;
		}
	}

	private int currentFrameIndex;
	public int LastFrameIndex { get; private set; }

	public DateTime playheadLocation;
	public DateTime startTime;
	public DateTime endTime;

	public DateTime lastPlayheadLocation;
	private Frame lastFrame;

	public bool wasPlaying = false;
	public bool isPlaying;
	public bool isReverse;
	public bool wasPlayingBeforeScrub = false;
	public bool isScrubbing = false;
	public float playbackMultiplier = 1f;

	public Playhead(Game game)
	{
		this.game = game;

		if (game == null) return;

		// set start and end times
		startTime = game.GetFrame(0).recorded_time;
		endTime = game.GetFrame(game.nframes - 1).recorded_time;
	}

	public int FrameCount => game?.nframes ?? 0;

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
				//
			}

			FindCurrentFrameLocation();

			// if the current playhead and the next recorded frame time are 1 second apart, just skip to the next frame
			Frame nextFrame = isReverse ? GetPreviousFrame() : GetNextFrame();
			float diff = (float) Math.Abs((nextFrame.recorded_time - playheadLocation).TotalSeconds);
			if (diff > 1)
			{
				playheadLocation = nextFrame.recorded_time;
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
			return Frame.Lerp(GameManager.instance.netFrameMan.lastFrame, GameManager.instance.netFrameMan.frame,
				GameManager.instance.netFrameMan.CorrectedNetworkFrameTime);
		}

		if (LiveFrameProvider.isLive)
		{  
			return Frame.Lerp(LiveFrameProvider.lastFrame, LiveFrameProvider.frame, playheadLocation);
		}

		if (lastPlayheadLocation == playheadLocation && lastFrame != null)
		{
			return lastFrame;
		}

		LastFrameIndex = currentFrameIndex;
		// send playhead info to other players ⬆
		GameManager.instance.netFrameMan.networkFilename = Path.GetFileNameWithoutExtension(game.filename);
		lastFrame = Frame.Lerp(GetPreviousFrame(), GetNearestFrame(), playheadLocation);
		lastPlayheadLocation = playheadLocation;
		return lastFrame;
	}

	public Frame GetNearestFrame()
	{
		return game.GetFrame(Mathf.Clamp(currentFrameIndex, 0, game.nframes - 1));
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

		return game.GetFrame(Mathf.Clamp(currentFrameIndex - 1, 0, game.nframes - 1));
	}

	private Frame GetNextFrame()
	{
		return game.GetFrame(Mathf.Clamp(currentFrameIndex + 1, 0, game.nframes - 1));
	}

	private void FindCurrentFrameLocation()
	{
		while (true)
		{
			// check if we are done searching
			if (playheadLocation >= GetPreviousFrame().recorded_time &&
			    playheadLocation <= GetNearestFrame().recorded_time)
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