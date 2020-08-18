﻿using System;
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
		deltaTime *= playbackMultiplier;

		playheadLocation += TimeSpan.FromSeconds(deltaTime * (isReverse ? -1 : 1));

		FindCurrentFrameLocation();
	}


	/// <summary>
	/// Set playing variable to start and stop auto-play of demo.
	/// </summary>
	/// <param name="value">True to play, False to pause</param>
	public void SetPlaying(bool value)
	{
		isReverse = false;
		isPlaying = value;
	}

	public Frame GetFrame()
	{
		LastFrameIndex = currentFrameIndex;
		return Frame.Lerp(GetPreviousFrame(), GetNearestFrame(), playheadLocation);
	}

	public Frame GetNearestFrame()
	{
		return game.frames[Mathf.Clamp(currentFrameIndex, 0, game.nframes - 1)];
	}

	public Frame GetPreviousFrame()
	{
		return game.frames[Mathf.Clamp(currentFrameIndex - 1, 0, game.nframes - 1)];
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
				isPlaying = false;
				CurrentFrameIndex = FrameCount - 1;
				return;
			}
			else if (currentFrameIndex <= 0)
			{
				isPlaying = false;
				CurrentFrameIndex = 0;
				return;
			}
		}
	}
}