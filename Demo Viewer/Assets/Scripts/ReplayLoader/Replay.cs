using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ButterReplays;
using EchoVRAPI;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// For loading of files from disk and getting frame information.
/// Pretends to load the whole file, but handles temporal processing and partial loading internally.
/// This serves to abstract the whole file management from the rest of the program.
/// </summary>
public class Replay : MonoBehaviour
{
	public Action FileLoaded;
	public Action TemporalLoadingFinished;
	public Action<float> LoadProgress;
	public Action<float> TemporalLoadProgress;


	public class Game
	{
		public int nFrames;
		public string filename;
		internal List<string> rawFrames;
		internal List<Frame> frames;
	}

	[NonSerialized] // This is to prevent the editor from becoming super slow
	private Game game;

	private readonly object gameLock = new object();
	private static int loadingThreadId;

	public int FrameCount => game?.nFrames ?? 0;
	public string FileName => game?.filename;

	// for the point cloud
	public readonly List<Vector3> vertices = new List<Vector3>();
	public readonly List<Color> colors = new List<Color>();
	public readonly List<Vector3> normals = new List<Vector3>();

	private int processingProgress = 0;

	public void LoadFile(string fileName = "")
	{
		StartCoroutine(LoadFileCo(fileName));
	}

	/// <summary>
	/// Part of the process for reading the file
	/// </summary>
	/// <param name="fileName">The filename of the replay file</param>
	private IEnumerator LoadFileCo(string fileName = "")
	{
		Stopwatch sw = new Stopwatch();
		sw.Start();

		if (string.IsNullOrEmpty(fileName)) yield break;

		Debug.Log("Reading file: " + fileName);
		StreamReader reader = new StreamReader(fileName);


		float fileReadProgress = 0;
		Thread loadThread = new Thread(() => ReadReplayFile(reader, fileName, ++loadingThreadId, ref fileReadProgress));
		loadThread.Start();
		while (loadThread.IsAlive)
		{
			LoadProgress?.Invoke(fileReadProgress);
			yield return null;
		}

		FileLoaded?.Invoke();

		Debug.Log($"Fished reading file in {sw.Elapsed.TotalSeconds:N2} seconds.");


		Thread processTemporalDataThread = new Thread(() => ProcessAllTemporalData(game, ++loadingThreadId));
		processTemporalDataThread.Start();
		while (processTemporalDataThread.IsAlive)
		{
			TemporalLoadProgress?.Invoke((float)processingProgress / game.nFrames);
			yield return null;
		}

		TemporalLoadProgress?.Invoke(1);

		TemporalLoadingFinished?.Invoke();
	}

	/// <summary>
	/// Actually reads the replay file into memory
	/// This is a thread on desktop versions
	/// </summary>
	private void ReadReplayFile(StreamReader fileReader, string filename, int threadLoadingId, ref float fileReadProgress)
	{
		// butter
		if (filename.EndsWith(".butter"))
		{
			fileReader.Close();

			fileReadProgress = 0;
			List<Frame> frames;
			using (BinaryReader binaryReader = new BinaryReader(File.OpenRead(filename)))
			{
				frames = ButterFile.FromBytes(binaryReader, ref fileReadProgress);
			}

			fileReadProgress = 1;

			Game readGame = new Game
			{
				// rawFrames = // TODO,
				nFrames = frames.Count,
				filename = filename,
				frames = frames
			};

			lock (gameLock)
			{
				game = readGame;
			}
		}
		// echoreplay
		else
		{
			using (fileReader = OpenOrExtract(fileReader))
			{
				fileReadProgress = 0;
				List<string> allLines = new List<string>();
				do
				{
					allLines.Add(fileReader.ReadLine());
					fileReadProgress += .00005f;
					fileReadProgress %= 1;

					// if we started loading a different file instead, stop this one
					if (threadLoadingId != loadingThreadId) return;
				} while (!fileReader.EndOfStream);

				//string fileData = fileReader.ReadToEnd();
				//List<string> allLines = fileData.LowMemSplit("\n");

				Game readGame = new Game
				{
					rawFrames = allLines,
					nFrames = allLines.Count,
					filename = filename,
					frames = new List<Frame>(new Frame[allLines.Count])
				};

				lock (gameLock)
				{
					game = readGame;
				}
			}
		}
	}


	public static StreamReader OpenOrExtract(StreamReader reader)
	{
		char[] buffer = new char[2];
		reader.Read(buffer, 0, buffer.Length);
		reader.DiscardBufferedData();
		reader.BaseStream.Seek(0, SeekOrigin.Begin);
		if (buffer[0] != 'P' || buffer[1] != 'K') return reader;
		ZipArchive archive = new ZipArchive(reader.BaseStream);
		StreamReader ret = new StreamReader(archive.Entries[0].Open());
		return ret;
	}


	/// <summary>
	/// Loops through the whole file in the background and generates temporal data like playspace location
	/// </summary>
	private void ProcessAllTemporalData(Game game, int threadLoadingId)
	{
		processingProgress = 0;
		Frame lastFrame = null;

		Stopwatch sw = new Stopwatch();
		sw.Start();

		vertices.Clear();
		colors.Clear();
		normals.Clear();

		Parallel.For(0, game.nFrames,
			new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount - 1 },
			i =>
			{
				GetFrame(i);
				Interlocked.Increment(ref processingProgress);
			});

		for (int i = 0; i < game.nFrames; i++)
		{
			// if we started loading a different file instead, stop this one
			if (threadLoadingId != loadingThreadId) return;

			processingProgress = i;

			// this converts the frame from raw json data to a deserialized object
			Frame frame = GetFrame(i);

			if (frame == null) continue;

			if (lastFrame == null) lastFrame = frame;

			float deltaTime = lastFrame.game_clock - frame.game_clock;

			#region Local Playspace

			#endregion

			// loop through the two player teams
			for (int t = 0; t < 2; t++)
			{
				Team team = frame.teams[t];
				if (team?.players == null) continue;

				// loop through all the players on the team
				for (int p = 0; p < team.players.Count; p++)
				{
					Player player = team.players[p];


					vertices.Add(player.head.Position);
					colors.Add(t == 1 ? new Color(1, 136 / 255f, 0, 1) : new Color(0, 123 / 255f, 1, 1));
					normals.Add(player.velocity.ToVector3());


					List<Player> lastPlayers = lastFrame.teams[t]?.players;
					if (lastPlayers == null) continue;
					if (lastPlayers.Count <= p + 1) continue;
					Player lastPlayer = lastPlayers[p];
					if (lastPlayer == null) continue;

					if (deltaTime == 0)
					{
						// just copy the playspace position from last time
						player.playspacePosition = lastPlayer.playspacePosition;
						continue;
					}

					// how far the player's position moved this frame (m)
					Vector3 posDiff = player.head.Position - lastPlayer.head.Position;

					// how far the player should have moved by velocity this frame (m)
					Vector3 velDiff = player.velocity.ToVector3() * deltaTime;

					// -
					Vector3 movement = posDiff - velDiff;

					// move the player in the playspace
					player.playspacePosition = lastPlayer.playspacePosition + movement;

					// add a "recentering force" to correct longterm inaccuracies
					player.playspacePosition -= player.playspacePosition.normalized * (.05f * deltaTime);
				}
			}

			// combat replays don't have disc position
			if (frame.disc != null)
			{
				vertices.Add(frame.disc.position.ToVector3());
				colors.Add(new Color(1, 1, 1, 1));
				normals.Add(frame.disc.velocity.ToVector3());
			}

			lastFrame = frame;
		}

		processingProgress = 1;
		Debug.Log($"Fished processing temporal data in {sw.Elapsed.TotalSeconds:N2} seconds.");
	}

	/// <summary>
	/// Gets or converts the requested frame.
	/// May return null if the frame can't be converted.
	/// </summary>
	public Frame GetFrame(int index)
	{
		if (game == null) return null;
		if (game.frames[index] != null) return game.frames[index];

		// repeat because maybe the requested frame needs to be discarded.
		while (game.rawFrames.Count > 0)
		{
			Frame newFrame = Frame.FromEchoReplayString(game.rawFrames[index]);
			if (newFrame != null)
			{
				game.frames[index] = newFrame;
				game.rawFrames[index] = null; // free up the memory, since the raw frames take up a lot more
				return game.frames[index];
			}

			Debug.LogError($"Discarded frame {index}");
			game.frames.RemoveAt(index);
			game.rawFrames.RemoveAt(index);
			game.nFrames--;
		}

		Debug.LogError("File contains no valid arena frames.");
		return null;
	}


	/// <summary>
	/// Saves a replay clip
	/// </summary>
	public void SaveReplayClip(string fileName, int startFrame, int endFrame)
	{
		if (game == null)
		{
			Debug.LogError("No replay loaded. Can't clip.");
			return;
		}

		List<Frame> frames = game.frames.Skip(startFrame).Take(endFrame - startFrame).ToList();
		EchoReplay.SaveReplay(fileName, frames);
		ButterFile butterFile = new ButterFile();
		frames.ForEach(f => butterFile.AddFrame(f));
		File.WriteAllBytes(fileName.Replace(".echoreplay", ".butter"), butterFile.GetBytes());
	}
}