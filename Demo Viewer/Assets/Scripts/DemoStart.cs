/**
 * David Robidas & ZZenith 2020
 * Date: 16 April 2020
 * Purpose: Run demo viewing process
 */


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.IO.Compression;
using System;
using System.Linq;
using TMPro;
using System.Threading;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityTemplateProjects;

//Serializable classes for JSON serializing from the API output.


public class PlayerStats : Stats
{
	public PlayerStats(int pt, int points, int goals, int saves, int stuns, int interceptions, int blocks, int passes, int catches, int steals, int assists, int shots_taken)
	{
		possession_time = pt;
		this.points = points;
		this.goals = goals;
		this.saves = saves;
		this.stuns = stuns;
		this.interceptions = interceptions;
		this.blocks = blocks;
		this.passes = passes;
		this.catches = catches;
		this.steals = steals;
		this.assists = assists;
		this.shots_taken = shots_taken;
	}
}



public class DemoStart : MonoBehaviour
{
	#region Variables

	public GameObject orangeScoreEffects;
	public GameObject blueScoreEffects;

	public Text frameText;
	public Text gameTimeText;
	public Slider playbackSlider;
	public Slider temporalProcessingSlider;
	public Slider speedSlider;
	public float playbackSpeed {
		get => playhead.playbackMultiplier;
		set {
			playhead.playbackMultiplier = value;
			UpdateSpeedSlider();
		}
	}
	public Text speedMultiplierText;
	[Range(0, 1)]
	public float fileReadProgress;
	public object loadedDemoLock = new object();

	[NonSerialized] // This is to prevent the editor from becoming super slow
	public Game loadedDemo;

	public GameObject controlsOverlay;
	public static string lastDateTimeString;
	public static string lastJSON;
	public GameObject settingsScreen;
	private bool isSoundOn;
	public Toggle masterSoundToggle;

	public float maxGameTime;
	public GameObject joustReadout;
	public GameObject lastGoalStats;
	public bool showingGoalStats = false;

	public bool showGoalAnim = false;

	public GameObject goalEventObject;
	public Text blueGoals;
	public Text orangeGoals;

	public GameObject punchParticle;

	public Text playbackFramerate;

	public Transform playerObjsParent;
	public GameObject bluePlayerPrefab;
	public GameObject orangePlayerPrefab;

	public DiscController disc;

	private bool isScored = false;

	public bool wasDPADXReleased = true;
	private bool wasDPADYPressed = false;

	/// <summary>
	/// ((teamid, player ign), player character obj)
	/// </summary>
	Dictionary<(int, string), PlayerCharacter> playerObjects = new Dictionary<(int, string), PlayerCharacter>();


	public static Playhead playhead;

	public ScoreBoardController scoreBoardController;

	private string jsonStr;
	bool ready = false;

	public bool showDebugLogs;

	protected string IP = "http://69.30.197.26:5000";

	public TextMeshProUGUI replayFileNameText;

	public bool processTemporalDataInBackground = true;
	public float temporalProcessingProgress = 0;

	/// <summary>
	/// 0 for none, 1 for all players, 2 for local player
	/// </summary>
	public static int showPlayspace;

	private Transform followingPlayer;
	public SimpleCameraController camController;

	public Material pointCloudMaterial;
	
	
	private static bool loadPointCloud;

	// for the point cloud
	private static readonly List<Vector3> vertices = new List<Vector3>();
	private static readonly List<Color> colors = new List<Color>();
	private static readonly List<Vector3> normals = new List<Vector3>();
	
	private static bool finishedProcessingTemporalData = false;

	private static int loadingThreadId;



	#endregion

	private void Start()
	{
		// Ahh yes welcome to the code

		// Load and serialize demo file
#if !UNITY_WEBGL
		string demoFile = PlayerPrefs.GetString("fileDirector");
		replayFileNameText.text = Path.GetFileName(demoFile);
		SendConsoleMessage("Loading Demo: " + demoFile);
		StartCoroutine(LoadFile(demoFile));
#else
		string getFileName = "";
			int pm = Application.absoluteURL.IndexOf("=");
			if (pm != -1)
			{
				getFileName = Application.absoluteURL.Split("="[0])[1];
			}
			sendConsoleMessage("Loading: " + getFileName);

			StartCoroutine(GetText(getFileName, DoLast));
#endif

		

		
		showPlayspace = PlayerPrefs.GetInt("ShowPlayspaceVisualizers", 0);
		loadPointCloud = PlayerPrefs.GetInt("ShowPointCloud", 0) == 1;
		
		float[] options = {1, 10, 30, 50};
		GameManager.instance.vrRig.transform.localScale = Vector3.one * options[PlayerPrefs.GetInt("VRArenaScale",2)];
	}

	// Update is called once per frame
	private void Update()
	{

		// controls help
		if (Input.GetKeyDown(KeyCode.H))
			controlsOverlay.SetActive(true);
		if (Input.GetKeyUp(KeyCode.H))
			controlsOverlay.SetActive(false);


		if (ready)
		{
			if (playhead.isPlaying)
			{
				playhead.IncrementPlayhead(Time.deltaTime);
			}

			// Find and declare what frame the slider is on.
			if (playhead.isPlaying)
				playbackSlider.value = playhead.CurrentFrameIndex;

			// process input
			CheckKeys();

			frameText.text = $"Frame {(playhead.CurrentFrameIndex + 1)} of {playhead.FrameCount}";
			playbackFramerate.text = $"{speedSlider.value:0.#}x";

			// Only render the next frame if it differs from the last (optimization)
			if (playhead.CurrentFrameIndex != playhead.LastFrameIndex || playhead.isPlaying || !GameManager.instance.netFrameMan.IsLocalOrServer)
			{
				// Grab frame
				Frame viewingFrame = playhead.GetFrame();
				Frame previousFrame = playhead.GetPreviousFrame();

				if (viewingFrame != null && previousFrame != null)
				{
					// Arena-only stuff
					if (viewingFrame.map_name == "mpl_arena_a")
					{
						// Joust Readout
						if (viewingFrame.disc.position.Length != 0 &&
						    previousFrame.disc.position.Length != 0)
						{
							Vector3 currectDiscPosition = viewingFrame.disc.position.ToVector3();
							Vector3 lastDiscPosition = previousFrame.disc.position.ToVector3();
							if (lastDiscPosition == Vector3.zero && currectDiscPosition != Vector3.zero && playhead.isPlaying)
							{
								maxGameTime = loadedDemo.GetFrame(0).game_clock; // TODO this may not be correct if the recording starts midgame
								float currentTime = viewingFrame.game_clock;
								joustReadout.GetComponentInChildren<Text>().text = $"{maxGameTime - currentTime:0.##}";
								StartCoroutine(FlashInOut(joustReadout, 3));
							}
						}

						// Handle goal stat visibility
						if (showingGoalStats)
						{
							goalEventObject.SetActive(false);
							lastGoalStats.SetActive(true);
							RenderGoalStats(viewingFrame.last_score);
						}
						else if (!showingGoalStats && lastGoalStats.activeSelf)
						{
							lastGoalStats.SetActive(false);
							if (viewingFrame.game_status == "score" && showGoalAnim)
							{
								goalEventObject.SetActive(true);
							}
						}
					}

					// Render this frame
					RenderFrame(viewingFrame, previousFrame);
				}
			}


			// hover over players to get stats
			if (Physics.Raycast(GameManager.instance.camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 1000f, LayerMask.GetMask("players")))
			{
				PlayerStatsHover psh = hit.collider.GetComponent<PlayerStatsHover>();
				if (!psh) return;

				psh.Visible = true;

				// clicked on a player - follow player
				if (Input.GetMouseButtonDown(0) && !GameManager.instance.DrawingMode)
				{
					if (followingPlayer == psh.transform)
					{
						followingPlayer = null;
					}
					else
					{
						followingPlayer = psh.transform;

						if (camController != null)
						{
							camController.Origin = followingPlayer.position;
							Vector3 playerPos = psh.transform.position;
							camController.transform.position = playerPos + Vector3.forward * 4 + Vector3.up * 2;
							camController.transform.LookAt(playerPos);
							camController.ApplyPosition();
						}
					}
				}
			}
		}
		else
		{
			playbackSlider.value = fileReadProgress;
		}

		if (followingPlayer != null)
		{
			camController.Origin = followingPlayer.position;
		}

		if (finishedProcessingTemporalData)
		{
			finishedProcessingTemporalData = false;
			

			if (loadPointCloud)
			{
				loadedDemo.pointCloud = new Mesh()
				{
					name = loadedDemo.filename,
					vertices =  vertices.ToArray(),
					colors =  colors.ToArray(),
					normals =  normals.ToArray(),
					indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
				};
			
				loadedDemo.pointCloud.SetIndices(
					Enumerable.Range(0, vertices.Count).ToArray(),
					MeshTopology.Points, 0
				);
			
				loadedDemo.pointCloud.UploadMeshData(true);

				GameObject obj = new GameObject("Point Cloud");
				MeshFilter mf = obj.AddComponent<MeshFilter>();
				mf.sharedMesh = loadedDemo.pointCloud;
				MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
				meshRenderer.sharedMaterial = GameManager.instance.demoStart.pointCloudMaterial;
			}
		}

		temporalProcessingSlider.value = temporalProcessingProgress;
	}


	/// <summary>
	/// Used in webgl mode. idk why I haven't looked. Maybe consolidate this with other webrequest file loading?
	/// </summary>
	private IEnumerator GetText(string fn, Action doLast)
	{
		UnityWebRequest req = UnityWebRequest.Get($"{IP}/file?name={fn}");
		yield return req.SendWebRequest();

		DownloadHandler dh = req.downloadHandler;

		//this.jsonStr = dh.text;
		StreamReader read = new StreamReader(new MemoryStream(dh.data));
		ReadReplayFile(read, fn, ++loadingThreadId);
		doLast();
	}

	public static StreamReader OpenOrExtract(StreamReader reader)
	{
		char[] buffer = new char[2];
		reader.Read(buffer, 0, buffer.Length);
		reader.DiscardBufferedData();
		reader.BaseStream.Seek(0, SeekOrigin.Begin);
		if (buffer[0] == 'P' && buffer[1] == 'K')
		{
			ZipArchive archive = new ZipArchive(reader.BaseStream);
			StreamReader ret = new StreamReader(archive.Entries[0].Open());
			//reader.Close();
			return ret;
		}
		return reader;
	}

	/// <summary>
	/// Actually reads the replay file into memory
	/// This is a thread on desktop versions
	/// </summary>
	private void ReadReplayFile(StreamReader fileReader, string filename, int threadLoadingId)
	{
		using (fileReader = OpenOrExtract(fileReader))
		{
			fileReadProgress = 0;
			List<string> allLines = new List<string>();
			do
			{
				allLines.Add(fileReader.ReadLine());
				fileReadProgress += .0001f;
				fileReadProgress %= 1;

				// if we started loading a different file instead, stop this one
				if (threadLoadingId != loadingThreadId) return;
			} while (!fileReader.EndOfStream);

			//string fileData = fileReader.ReadToEnd();
			//List<string> allLines = fileData.LowMemSplit("\n");

			Game readGame = new Game
			{
				rawFrames = allLines,
				nframes = allLines.Count,
				filename = filename,
				frames = new List<Frame>(new Frame[allLines.Count])
			};

			lock (loadedDemoLock)
			{
				loadedDemo = readGame;
			}
		}

	}


	/// <summary>
	/// Loops through the whole file in the background and generates temporal data like playspace location
	/// </summary>
	private static void ProcessAllTemporalData(Game game, int threadLoadingId)
	{
		GameManager.instance.demoStart.temporalProcessingProgress = 0;
		Frame lastFrame = null;
		
		vertices.Clear();
		colors.Clear();
		normals.Clear();
		
		for (int i = 0; i < game.nframes; i++)
		{
			// if we started loading a different file instead, stop this one
			if (threadLoadingId != loadingThreadId) return;
			
			GameManager.instance.demoStart.temporalProcessingProgress = (float)i / game.nframes;
			
			// this converts the frame from raw json data to a deserialized object
			Frame frame = game.GetFrame(i);
			
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
				for (int p = 0; p < team.players.Length; p++)
				{
					Player player = team.players[p];
					
					if (loadPointCloud)
					{
						vertices.Add(player.Head.Position);
						colors.Add(t == 1 ? new Color(1, 136/255f, 0, 1) : new Color(0, 123/255f, 1, 1));
						normals.Add(player.velocity.ToVector3());
					}
					
					
					Player[] lastPlayers = lastFrame.teams[t]?.players;
					if (lastPlayers == null) continue;
					if (lastPlayers.Length <= p + 1) continue;
					Player lastPlayer = lastPlayers[p];
					if (lastPlayer == null) continue;

					if (deltaTime == 0)
					{
						// just copy the playspace position from last time
						player.playspacePosition = lastPlayer.playspacePosition;
						continue;
					}
					
					// how far the player's position moved this frame (m)
					Vector3 posDiff = player.Head.Position - lastPlayer.Head.Position;
					
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
			if (loadPointCloud && frame.disc != null)
			{
				vertices.Add(frame.disc.position.ToVector3());
				colors.Add(new Color(1, 1, 1, 1));
				normals.Add(frame.disc.velocity.ToVector3());
			}

			lastFrame = frame;
		}

		finishedProcessingTemporalData = true;
		Debug.Log("Fished processing temporal data.");
	}


	/// <summary>
	/// Part of the process for reading the file
	/// </summary>
	/// <param name="demoFile">The filename of the replay file</param>
	private IEnumerator LoadFile(string demoFile = "")
	{
		if (!string.IsNullOrEmpty(demoFile))
		{
			Debug.Log("Reading file: " + demoFile);
			StreamReader reader = new StreamReader(demoFile);

			Thread loadThread = new Thread(() => ReadReplayFile(reader, demoFile, ++loadingThreadId));
			loadThread.Start();
			while (loadThread.IsAlive)
			{
				// maybe put a progress bar here
				yield return null;
			}
			
			if (processTemporalDataInBackground)
			{
				Thread processTemporalDataThread = new Thread(() => ProcessAllTemporalData(loadedDemo, ++loadingThreadId));
				processTemporalDataThread.Start();		
			}
		}

		playhead = new Playhead(loadedDemo);
		frameText.text = $"Frame 0 of {playhead.FrameCount}";

		//set slider values
		playbackSlider.maxValue = playhead.FrameCount - 1;

		//HUD initialization
		goalEventObject.SetActive(false);
		lastGoalStats.SetActive(false);
		
		
		// load a combat map if necessary
		// read the first frame
		Frame middleFrame = loadedDemo.GetFrame(loadedDemo.nframes/2);
		if (middleFrame.map_name != "mpl_arena_a")
		{
			SceneManager.UnloadSceneAsync(GameManager.instance.arenaModelScenes[PlayerPrefs.GetInt("ArenaModel", 0)]);
			SceneManager.LoadSceneAsync(GameManager.combatMapScenes[middleFrame.map_name], LoadSceneMode.Additive);
			scoreBoardController.gameObject.SetActive(false);
		}

		ready = true;
	}

	/// <summary>
	/// Loads the currently set file (set in playerprefs beforehand)
	/// Something should be put here so files can be changed without reloading the scene
	/// </summary>
	public void ReloadFile()
	{

	}


	/// <summary>
	/// Does input processing for keyboard and controller
	/// </summary>
	public void CheckKeys()
	{
		float triggerLinearity = 4;
		float maxScrubSpeed = 1.75f;
		SendConsoleMessage(Input.GetAxis("RightTrig").ToString());
		SendConsoleMessage(Input.GetAxis("LeftTrig").ToString());
		float rightTrig = Input.GetAxis("RightTrig");
		float leftTrig = Input.GetAxis("LeftTrig");
		float combinedTrigs = rightTrig - leftTrig;
		if (combinedTrigs == 0)
		{
			// stop scrubbing
			if (playhead.isScrubbing)
			{
				playhead.isScrubbing = false;
				playhead.isPlaying = playhead.wasPlayingBeforeScrub;
				playhead.isReverse = false;
				playbackSpeed = 1f;
			}
		}
		// scrubbing backwards ⏪
		else if (combinedTrigs < 0)
		{
			if (!playhead.isScrubbing)
			{
				playhead.wasPlayingBeforeScrub = playhead.isPlaying;
			}
			playhead.isPlaying = true;
			playbackSpeed = Mathf.Pow(2, -(1 - Mathf.Abs(combinedTrigs)) * triggerLinearity) * maxScrubSpeed;
			playhead.isReverse = true;
			playhead.isScrubbing = true;
		}
		// scrubbing forwards ⏩
		else
		{
			if (!playhead.isScrubbing)
			{
				playhead.wasPlayingBeforeScrub = playhead.isPlaying;
			}
			playhead.isPlaying = true;
			playbackSpeed = Mathf.Pow(2, -(1 - Mathf.Abs(combinedTrigs)) * triggerLinearity) * maxScrubSpeed;
			playhead.isReverse = false;
			playhead.isScrubbing = true;
		}

		// play/pause ⏯
		if (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("XboxA"))
		{
			if (playhead.isScrubbing)
			{
				playhead.wasPlayingBeforeScrub = !playhead.wasPlayingBeforeScrub;
			}
			if (playhead.isReverse)
			{
				playhead.isReverse = false;
			}
			else
			{
				playhead.SetPlaying(!playhead.isPlaying);
			}
		}
		if (Input.GetButtonDown("XboxSelect"))
		{
			// showGoalAnim = !showGoalAnim;

			GUIUtility.systemCopyBuffer = playhead.CurrentFrameIndex.ToString();
		}

		if (Input.GetButtonDown("XboxY"))
		{
			showingGoalStats = !showingGoalStats;
		}
		if (Input.GetButtonDown("XboxStart"))
		{
			// handled in ReplaySelectionUI
		}
		float dpadX = Input.GetAxis("XboxDpadX");
		switch (wasDPADXReleased)
		{
			case false when dpadX == 0:
				wasDPADXReleased = true;
				break;
			case true when dpadX == -1:
			{
				wasDPADXReleased = false;
				playhead.isPlaying = true;
				if (playhead.isReverse == true)
				{
					playbackSpeed = Mathf.Clamp(playbackSpeed / 2, 0.03125f, 32f);
				}
				else
				{
					playhead.isReverse = true;
					playbackSpeed = 1f;
				}

				break;
			}
			case true when dpadX == 1:
			{
				wasDPADXReleased = false;
				playhead.isPlaying = true;
				if (playhead.isReverse == false)
				{
					playbackSpeed = Mathf.Clamp(playbackSpeed / 2, 0.03125f, 32f);
				}
				else
				{
					playhead.isReverse = false;
					playbackSpeed = 1f;
				}

				break;
			}
		}



		if (Input.GetKeyDown(KeyCode.UpArrow))
		{
			playbackSpeed = Mathf.Clamp(playbackSpeed * 2, 0.03125f, 32f);
		}

		if (Input.GetKeyDown(KeyCode.DownArrow))
		{
			playbackSpeed = Mathf.Clamp(playbackSpeed / 2, 0.03125f, 16f);
		}

		// skip backwards 1 second
		if (Input.GetKeyDown(KeyCode.LeftArrow))
		{
			playhead.IncrementPlayhead(-1);
			playbackSlider.value = playhead.CurrentFrameIndex;
		}

		// skip forwards 1 second
		if (Input.GetKeyDown(KeyCode.RightArrow))
		{
			playhead.IncrementPlayhead(1);
			playbackSlider.value = playhead.CurrentFrameIndex;
		}

		// skip backwards one frame
		if (Input.GetKeyDown(KeyCode.Comma) ||
			(Input.GetAxis("XboxDpadY") == -1 && !wasDPADYPressed))
		{
			playhead.CurrentFrameIndex--;
			wasDPADYPressed = true;
		}

		// skip forwards one frame
		if (Input.GetKeyDown(KeyCode.Period) ||
			(Input.GetAxis("XboxDpadY") == 1 && !wasDPADYPressed))
		{
			playhead.CurrentFrameIndex++;
			wasDPADYPressed = true;
		}

		if (Input.GetAxis("XboxDpadY") == 0)
		{
			wasDPADYPressed = false;
		}
	}

	private void UpdateSpeedSlider()
	{
		speedSlider.value = Mathf.Log(playbackSpeed, 2);
		speedMultiplierText.text = playbackSpeed.ToString("0.####") + "x";
	}

	public IEnumerator FlashInOut(GameObject flashObject, float time)
	{
		flashObject.SetActive(true);
		yield return new WaitForSeconds(time);
		flashObject.SetActive(false);
	}
	/* 
	public bool isDiscVisible()
	{
		RaycastHit ObstacleHit;
		return (Physics.Raycast(Camera.main.transform.position, disc.transform.position - Camera.main.transform.position, out ObstacleHit, Mathf.Infinity) && ObstacleHit.transform != Camera.main && ObstacleHit.transform == disc);
	}
	*/

	//Handle instantiation of effects while playing versus not playing to minimize effects while scrubbing
	public void FXInstantiate(GameObject fx, Vector3 position, Vector3 rotation)
	{
		if (playhead.isPlaying)
			Instantiate(fx, position, Quaternion.Euler(rotation));
	}

	//public void soundHandler(SoundPlayer s, float vol, string sound)
	//{
	//todo
	//}

	/// <summary>
	/// Handle goal stats GUI
	/// </summary>
	/// <param name="ls">Data about the last score from the API</param>
	public void RenderGoalStats(Last_Score ls)
	{
		goalEventObject.SetActive(false);
		//Debug.Log(ls.disc_speed.ToString());
		lastGoalStats.transform.GetChild(1).GetComponent<Text>().text = ls.goal_type + string.Format(" : {0} PTS", ls.point_amount);
		lastGoalStats.transform.GetChild(2).GetComponent<Text>().text = ls.person_scored;
		//If no assister, hide assistedbytext and assistedbyname
		if (ls.assist_scored.Equals("[INVALID]"))
		{
			lastGoalStats.transform.GetChild(3).GetComponent<Text>().text = "";
			lastGoalStats.transform.GetChild(7).GetComponent<Text>().text = "";
		}
		else
		{
			lastGoalStats.transform.GetChild(3).GetComponent<Text>().text = ls.assist_scored;
		}

		lastGoalStats.transform.GetChild(4).GetComponent<Text>().text = string.Format("{0:0.##m/s}", ls.disc_speed);
		lastGoalStats.transform.GetChild(5).GetComponent<Text>().text = string.Format("{0:0m}", ls.distance_thrown);
	}

	public void RenderFrame(Frame viewingFrame, Frame previousFrame)
	{
		//if (viewingFrame.teams == null ||
		//	viewingFrame.teams[0].players == null ||
		//	viewingFrame.teams[1].players == null)
		//{
		//	return;
		//}

		if (viewingFrame == null || string.IsNullOrEmpty(viewingFrame.client_name)) return;

		string gameTime = viewingFrame.game_clock_display;
		gameTimeText.text = gameTime;

		//Activate goal score effects when a team scores
		if (viewingFrame.game_status != "score")
		{
			isScored = false;
			goalEventObject.SetActive(false);
		}
		if (viewingFrame.game_status == "score" && !isScored && showGoalAnim)
		{
			goalEventObject.SetActive(true);
			isScored = true;
			if (viewingFrame.disc.position[2] < 0)
			{
				FXInstantiate(orangeScoreEffects, new Vector3(-.68f, 0, 0), Vector3.zero);
			}
			else
			{
				FXInstantiate(blueScoreEffects, new Vector3(0.75f, 0, 0), new Vector3(0, 180, 0));
			}
		}

		// blue team possession effects
		if (viewingFrame.teams[0] != null && viewingFrame.teams[0].possession)
		{
			disc.TeamIndex = TeamColor.orange;
		}
		// orange team possession effects
		else if (viewingFrame.teams[1] != null && viewingFrame.teams[1].possession)
		{
			disc.TeamIndex = TeamColor.blue;
		}
		// no team possession effects
		else
		{
			disc.TeamIndex = TeamColor.spectator;
		}

		// set 2d score board
		blueGoals.text = viewingFrame.blue_points.ToString();
		orangeGoals.text = viewingFrame.orange_points.ToString();

		// set in game scoreboard elements
		scoreBoardController.blueScore = viewingFrame.blue_points;
		scoreBoardController.orangeScore = viewingFrame.orange_points;
		scoreBoardController.gameTime = viewingFrame.game_clock_display;

		var movedObjects = new List<PlayerCharacter>();

		// Update the players
		for (int t = 0; t < 2; t++)
		{
			if (viewingFrame.teams[t].players != null)
			{
				foreach (Player player in viewingFrame.teams[t].players)
				{
					// get the matching player from the previous frame.
					// Searching through all the players is the only way, since they may have 
					// switched teams or been reorganized in the list when another player joins
					Player previousPlayer = FindPlayerOnTeam(previousFrame.teams[t], player.name);

					RenderPlayer(player, previousPlayer, t, viewingFrame.client_name == player.name ? viewingFrame.player : null);

					movedObjects.Add(playerObjects[(t, player.name)]);
				}
			}
		}

		// remove players that weren't accessed this frame
		List<(int, string)> playersToRemove = new List<(int, string)>();
		foreach ((int, string) playerIndex in playerObjects.Keys)
		{
			if (!movedObjects.Contains(playerObjects[playerIndex]))
			{
				Destroy(playerObjects[playerIndex].gameObject);
				playersToRemove.Add(playerIndex);
			}
		}
		playersToRemove.ForEach(p => playerObjects.Remove(p));

		if (viewingFrame.map_name == "mpl_arena_a")
		{
			disc.discVelocity = viewingFrame.disc.velocity.ToVector3();
			disc.discPosition = viewingFrame.disc.position.ToVector3();
			if (viewingFrame.disc.forward != null)
			{
				Debug.DrawRay(viewingFrame.disc.position.ToVector3(), viewingFrame.disc.up.ToVector3(), Color.green);
				Debug.DrawRay(viewingFrame.disc.position.ToVector3(), viewingFrame.disc.forward.ToVector3(), Color.blue);
				Debug.DrawRay(viewingFrame.disc.position.ToVector3(), viewingFrame.disc.left.ToVector3(), Color.red);
				disc.discRotation = Quaternion.LookRotation(viewingFrame.disc.forward.ToVector3(), viewingFrame.disc.up.ToVector3());
			}
			//discScript.isGrabbed = isBeingHeld(viewingFrame, false);
		}
	}

	private Player FindPlayerOnTeam(Team team, string name)
	{
		if (team == null || team.players == null) return null;

		foreach (var player in team.players)
		{
			if (player.name == name)
			{
				return player;
			}
		}

		return null;
	}

	private Player FindPlayerInFrame(Frame frame, string name)
	{
		foreach (var team in frame.teams)
		{
			foreach (var player in team.players)
			{
				if (player.name == name)
				{
					return player;
				}
			}
		}

		return null;
	}

	private void RenderPlayer(Player player, Player lastFramePlayer, int teamIndex, Playspace playspace)
	{
		// don't show spectators
		if (teamIndex == 2) return;

		// if this player doesn't exist in the scene yet
		if (!playerObjects.ContainsKey((teamIndex, player.name)))
		{
			// instantiate it
			playerObjects.Add((teamIndex, player.name), Instantiate(teamIndex == 0 ? bluePlayerPrefab : orangePlayerPrefab, playerObjsParent).GetComponent<PlayerCharacter>());
		}

		PlayerCharacter p = playerObjects[(teamIndex, player.name)];

		// Set names above player heads
		p.playerName.text = player.name;

		//Get player transform values of current iteration
		Vector3 playerVelocityVector = player.velocity.ToVector3();
		Vector3 previousVelocityVector = Vector3.zero;
		if (lastFramePlayer != null)
		{
			previousVelocityVector = lastFramePlayer.velocity.ToVector3();
		}

		//Old method that rotates entire player
		IKController playerIK = p.ikController;

		//Send Head and Hand transforms to IK script

		// send head rotation 😏
		playerIK.headPos = player.Head.Position;
		playerIK.headForward = player.Head.forward.ToVector3();
		playerIK.headUp = player.Head.up.ToVector3();

		// send hand pos/rot ✋🤚
		playerIK.lHandPosition = player.leftHand.Position;
		playerIK.rHandPosition = player.rightHand.Position;
		playerIK.lHandRotation = Quaternion.LookRotation(-player.leftHand.left.ToVector3(), player.leftHand.forward.ToVector3());
		playerIK.rHandRotation = Quaternion.LookRotation(player.rightHand.left.ToVector3(), player.rightHand.forward.ToVector3());

		// send body pos/rot 🕺
		playerIK.bodyPosition = player.body.Position;
		playerIK.bodyRotation = Quaternion.LookRotation(player.body.left.ToVector3(), player.body.up.ToVector3());

		// send velocity 💨
		playerIK.playerVelocity = playerVelocityVector;

		// send stun info 🤜
		if (player.stunned && !p.stunnedInitiated)
		{
			FXInstantiate(punchParticle, p.transform.position, Vector3.zero);
			p.stunnedInitiated = true;
		}
		if (!player.stunned && p.stunnedInitiated)
			p.stunnedInitiated = false;

		// kinda jank but it works (back thruster)
		if (playerVelocityVector.magnitude - previousVelocityVector.magnitude > 1)
		{
			p.boost.SetActive(false);
			p.boost.SetActive(true);
		}

		//Player Blocking
		GameObject blockingEffect = p.playerShield;
		if (player.blocking)
		{
			blockingEffect.gameObject.SetActive(true);
		}
		else
		{
			blockingEffect.gameObject.SetActive(false);
		}
		
		// show playspace position
		switch (showPlayspace)
		{
			case 0:
				p.playspaceVisualizer.gameObject.SetActive(false);
				break;
			// all players
			case 1:
				p.PlayspaceLocation = player.playspacePosition;
				p.playspaceVisualizer.gameObject.SetActive(true);
				break;
			// only local player
			case 2:
				if (playspace != null)
				{
					p.PlayspaceLocation = playspace.vr_position.ToVector3();
					p.playspaceVisualizer.gameObject.SetActive(true);
				}
				else
				{
					p.playspaceVisualizer.gameObject.SetActive(false);
				}
				break;
		}

		p.trailRenderer.gameObject.SetActive(PlayerPrefs.GetInt("ShowPlayerTrails", 0) == 1);

		// show player stats on player stats board 🧮
		p.hoverStats.Stats = player.stats;
		p.hoverStats.Speed = player.velocity.ToVector3().magnitude;
	}

	/// <summary>
	/// Index 1 (bool): isBeingHeld
	/// Index 2 (bool): is it right hand?
	/// Index 3 (int[2]): index 1=j index 2=i ... this is the player index
	/// </summary>
	public (bool, bool, int[]) IsBeingHeld(Frame viewingFrame)
	{
		for (int j = 0; j < viewingFrame.teams.Length; j++)
		{
			if (viewingFrame.teams[j].players != null)
			{
				for (int i = 0; i < viewingFrame.teams[j].players.Length; i++)
				{
					Player p = viewingFrame.teams[j].players[i];
					Vector3 discPosition = viewingFrame.disc.position.ToVector3();
					Vector3 rHand = p.rightHand.Position;
					Vector3 lHand = p.leftHand.Position;
					float rHandDis = Vector3.Distance(discPosition, rHand);
					float lHandDis = Vector3.Distance(discPosition, lHand);
					if (rHandDis < 0.2f)
					{
						return (true, true, new int[2] { j, i });
					}
					if (lHandDis < 0.2f)
					{
						return (true, false, new int[2] { j, i });
					}
				}
			}
		}
		return (false, false, new int[2] { -1, -1 });
	}

	public void PlaybackSpeedMultiplierSliderChanged()
	{
		playhead.playbackMultiplier = Mathf.Pow(2, speedSlider.value);
		speedMultiplierText.text = playbackSpeed.ToString("0.#####") + "x";
	}

	public void useSlider()
	{
		SendConsoleMessage(playhead.isPlaying.ToString());
		playhead.wasPlaying = playhead.isPlaying;
		playhead.SetPlaying(false);
	}

	public void unUseSlider()
	{
		playhead.SetPlaying(false);
		playhead.SetPlaying(playhead.wasPlaying);
	}

	public void playbackValueChanged()
	{
		// if is scrubbing with the slider
		if (ready && !playhead.isPlaying)
		{
			playhead.CurrentFrameIndex = (int)playbackSlider.value;
		}
	}

	/// <summary>
	/// Only use from UI buttons
	/// </summary>
	/// <param name="value"></param>
	public void setPlaying(bool value)
	{
		playhead.SetPlaying(value);
	}

	public void SendConsoleMessage(string msg)
	{
		if (showDebugLogs)
		{
			float currentRuntime = Time.time;
			Debug.Log(string.Format("Time: {0:0.#} -- {1}", currentRuntime, msg));
		}
	}

	public void settingController(bool value)
	{
		settingsScreen.SetActive(value);
	}

	public void setSoundBool()
	{
		isSoundOn = masterSoundToggle.isOn;
	}
}

/// <summary>
/// Copied directly from: https://stackoverflow.com/a/28601805
/// </summary>
public static class StringExtensions
{

	// the string.Split() method from .NET tend to run out of memory on 80 Mb strings. 
	// this has been reported several places online. 
	// This version is fast and memory efficient and return no empty lines. 
	public static List<string> LowMemSplit(this string s, string seperator)
	{
		List<string> list = new List<string>();
		int lastPos = 0;
		int pos = s.IndexOf(seperator);
		while (pos > -1)
		{
			while (pos == lastPos)
			{
				lastPos += seperator.Length;
				pos = s.IndexOf(seperator, lastPos);
				if (pos == -1)
					return list;
			}

			string tmp = s.Substring(lastPos, pos - lastPos);
			if (tmp.Trim().Length > 0)
				list.Add(tmp);
			lastPos = pos + seperator.Length;
			pos = s.IndexOf(seperator, lastPos);
		}

		if (lastPos < s.Length)
		{
			string tmp = s.Substring(lastPos, s.Length - lastPos);
			if (tmp.Trim().Length > 0)
				list.Add(tmp);
		}

		return list;
	}
}