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
using TMPro;
using System.Threading;

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
	public Slider speedSlider;
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

	public Light discLight;
	public TrailRenderer discTrail;

	public GameObject punchParticle;

	public Material discTrailMat;

	public Text playbackFramerate;

	public Transform playerObjsParent;
	public GameObject bluePlayerPrefab;
	public GameObject orangePlayerPrefab;

	public GameObject disc;
	public Material autograbBubble;
	private Color autograbColor;

	private bool isScored = false;

	public bool wasDPADXReleased = true;

	/// <summary>
	/// player ign, player character obj
	/// </summary>
	Dictionary<string, PlayerCharacter> playerObjects = new Dictionary<string, PlayerCharacter>();


	public Playhead playhead;

	public static Color blueTeamColor = new Color(0, 165, 216);
	public static Color orangeTeamColor = new Color(210, 110, 45);

	public Shader discThroughWall;

	public ScoreBoardController scoreBoardController;

	private string jsonStr;
	bool ready = false;

	public bool showDebugLogs;

	protected string IP = "http://69.30.197.26:5000";

	public TextMeshProUGUI replayFileNameText;
	#endregion

	void Start()
	{
		// Ahh yes welcome to the code

		// Load and serialize demo file
#if !UNITY_WEBGL
		string demoFile = PlayerPrefs.GetString("fileDirector");
		replayFileNameText.text = Path.GetFileName(demoFile);
		SendConsoleMessage("Loading Demo: " + demoFile);

		StartCoroutine(DoLast(demoFile));
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
	}

	// Update is called once per frame
	void Update()
	{
		if (ready)
		{
			if (playhead.isPlaying)
			{
				playhead.IncrementPlayhead(Time.deltaTime);
			}

			// Find and declare what frame the slider is on.
			if (playhead.isPlaying)
				playbackSlider.value = playhead.CurrentFrameIndex;
			else
				playhead.CurrentFrameIndex = (int)playbackSlider.value;

			// process input
			CheckKeys();

			frameText.text = string.Format("Frame {0} of {1}", (playhead.CurrentFrameIndex + 1), playhead.FrameCount);
			playbackFramerate.text = string.Format("{0:0.#}x", speedSlider.value);

			// Only render the next frame if it differs from the last (optimization)
			if (playhead.CurrentFrameIndex != playhead.LastFrameIndex || playhead.isPlaying)
			{
				// Grab frame
				Frame viewingFrame = playhead.GetFrame();
				Frame previousFrame = playhead.GetPreviousFrame();

				// Joust Readout
				Vector3 currectDiscPosition = viewingFrame.disc.position.ToVector3();
				Vector3 lastDiscPosition = previousFrame.disc.position.ToVector3();
				if (lastDiscPosition == Vector3.zero && currectDiscPosition != Vector3.zero && playhead.isPlaying)
				{
					maxGameTime = loadedDemo.frames[0].game_clock;  // TODO this may not be correct if the recording starts midgame
					float currentTime = viewingFrame.game_clock;
					joustReadout.GetComponentInChildren<Text>().text = string.Format("{0:0.##}", maxGameTime - currentTime);
					StartCoroutine(FlashInOut(joustReadout, 3));
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

				// Render this frame
				RenderFrame(viewingFrame, previousFrame);
			}
		}
	}


	/// <summary>
	/// Used in webgl mode. idk why I haven't looked. Maybe consolidate this with other webrequest file loading?
	/// </summary>
	IEnumerator GetText(string fn, Action doLast)
	{
		UnityWebRequest req = new UnityWebRequest();
		req = UnityWebRequest.Get(string.Format("{0}/file?name={1}", IP, fn));
		yield return req.SendWebRequest();

		DownloadHandler dh = req.downloadHandler;

		//this.jsonStr = dh.text;
		StreamReader read = new StreamReader(new MemoryStream(dh.data));
		ReadReplayFile(read);
		doLast();
	}

	private static StreamReader OpenOrExtract(StreamReader reader)
	{
		char[] buffer = new char[2];
		reader.Read(buffer, 0, buffer.Length);
		reader.DiscardBufferedData();
		reader.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);
		if (buffer[0] == 'P' && buffer[1] == 'K')
		{
			ZipArchive archive = new ZipArchive(reader.BaseStream);
			StreamReader ret = new StreamReader(archive.Entries[0].Open());
			//reader.Close();
			return ret;
		}
		return reader;
	}


	Game ReadReplayFile(StreamReader fileReader)
	{
		bool fileFinishedReading = false;
		List<Frame> readFrames = new List<Frame>();
		Game readGame = new Game();

		using (fileReader = OpenOrExtract(fileReader))
		{

			while (!fileFinishedReading)
			{
				if (fileReader != null)
				{
					string rawJSON = fileReader.ReadLine();
					if (rawJSON == null)
					{
						fileFinishedReading = true;
						fileReader.Close();
					}
					else
					{
						string[] splitJSON = rawJSON.Split('\t');
						string onlyJSON, onlyTime;
						if (splitJSON.Length == 2)
						{
							onlyJSON = splitJSON[1];
							onlyTime = splitJSON[0];
						}
						else
						{
							Debug.LogError("Row doesn't include both a time and API JSON");
							continue;
						}
						DateTime frameTime = DateTime.Parse(onlyTime);

						// if this is actually valid arena data
						if (onlyJSON.Length > 300)
						{
							try
							{
								Frame foundFrame = JsonUtility.FromJson<Frame>(onlyJSON);
								foundFrame.frameTime = frameTime;
								readFrames.Add(foundFrame);
							}
							catch (Exception)
							{
								Debug.LogError("Couldn't read frame. File is corrupted.");
							}
						}
					}
				}
			}
		}
		readGame.frames = readFrames.ToArray();
		readGame.nframes = readGame.frames.Length;
		return readGame;
	}

	/// <summary>
	/// Part of the process for reading the file
	/// </summary>
	/// <param name="demoFile">The filename of the replay file</param>
	IEnumerator DoLast(string demoFile = "")
	{
		if (!string.IsNullOrEmpty(demoFile))
		{
			Debug.Log("Reading file: " + demoFile);
			StreamReader reader = new StreamReader(demoFile);

			loadedDemo = ReadReplayFile(reader);
			Thread loadThread = new Thread(() => ReadReplayFile(reader));
			loadThread.Start();
			while (loadThread.IsAlive)
			{
				// maybe put a progress bar here
				yield return null;
			}
		}

		playhead = new Playhead(loadedDemo);

		frameText.text = string.Format("Frame 0 of {0}", playhead.FrameCount);

		//set slider values
		playbackSlider.maxValue = playhead.FrameCount - 1;

		//HUD initialization
		goalEventObject.SetActive(false);
		lastGoalStats.SetActive(false);

		//Set replay settings
		discTrail.enabled = true;

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
		SendConsoleMessage(Input.GetAxis("RightTrig").ToString());
		SendConsoleMessage(Input.GetAxis("LeftTrig").ToString());
		float rightTrig = Input.GetAxis("RightTrig") * 1.75f;
		float leftTrig = Input.GetAxis("LeftTrig") * 1.75f;
		float combinedTrigs = rightTrig - leftTrig;
		if (combinedTrigs == 0)
		{
			if (playhead.isScrubbing)
			{
				playhead.isScrubbing = false;
				playhead.isPlaying = playhead.wasPlayingBeforeScrub;
				playhead.isReverse = false;
				playhead.playbackMultiplier = 1f;
			}
		}
		else if (combinedTrigs < 0)
		{
			if (!playhead.isScrubbing)
			{
				playhead.wasPlayingBeforeScrub = playhead.isPlaying;
			}
			playhead.isPlaying = true;
			playhead.playbackMultiplier = 1f / (combinedTrigs * -1f);
			playhead.isReverse = true;
			playhead.isScrubbing = true;
		}
		else
		{
			if (!playhead.isScrubbing)
			{
				playhead.wasPlayingBeforeScrub = playhead.isPlaying;
			}
			playhead.isPlaying = true;
			playhead.playbackMultiplier = 1f / combinedTrigs;
			playhead.isReverse = false;
			playhead.isScrubbing = true;

		}
		if (Input.GetKeyDown(KeyCode.H))
			controlsOverlay.SetActive(true);

		if (Input.GetKeyUp(KeyCode.H))
			controlsOverlay.SetActive(false);

		if (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("XboxA"))
		{
			if (playhead.isScrubbing)
			{
				playhead.wasPlayingBeforeScrub = !playhead.wasPlayingBeforeScrub;
			}
			if (playhead.playbackMultiplier != 1f && playhead.isPlaying || playhead.isReverse)
			{
				playhead.isReverse = false;
				playhead.playbackMultiplier = 1f;
			}
			else
			{
				playhead.SetPlaying(!playhead.isPlaying);
				playhead.playbackMultiplier = 1f;
			}
		}
		if (Input.GetButtonDown("XboxSelect"))
		{
			// showGoalAnim = !showGoalAnim;

			GUIUtility.systemCopyBuffer = currentFrame.ToString();
		}

		if (Input.GetButtonDown("XboxY"))
		{
			showingGoalStats = !showingGoalStats;
		}
		if (Input.GetButtonDown("XboxStart"))
		{
			// handled in ReplaySelectionUI
		}

		if (Input.GetKeyDown(KeyCode.UpArrow))
			speedSlider.value += .2f;

		float dpadX = Input.GetAxis("XboxDpadX");
		if (!wasDPADXReleased && dpadX == 0)
		{
			wasDPADXReleased = true;
		}
		else if (wasDPADXReleased && dpadX == -1)
		{
			wasDPADXReleased = false;
			playhead.isPlaying = true;
			if (playhead.isReverse == true)
			{
				if (playhead.playbackMultiplier > 1 / 10f)
				{
					playhead.playbackMultiplier /= 2f;
				}
			}
			else
			{
				playhead.isReverse = true;
				playhead.playbackMultiplier = 1f;
			}

		}
		else if (wasDPADXReleased && dpadX == 1)
		{
			wasDPADXReleased = false;
			playhead.isPlaying = true;
			if (playhead.isReverse == false)
			{
				if (playhead.playbackMultiplier >= 1 / 10f)
				{
					playhead.playbackMultiplier /= 2f;
				}
			}
			else
			{
				playhead.isReverse = false;
				playhead.playbackMultiplier = 1f;
			}
		}


		if (Input.GetKeyDown(KeyCode.DownArrow))
		{
			speedSlider.value -= .2f;
			playhead.playbackMultiplier = Mathf.Clamp(playhead.playbackMultiplier - .2f, .01f, 8);
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

		string gameTime = viewingFrame.game_clock_display;
		gameTimeText.text = gameTime;

		if (!IsBeingHeld(viewingFrame).Item1)
		{
			if (!discTrail.enabled)
				discTrail.Clear();
			discTrail.enabled = true;
		}
		else
		{
			discTrail.enabled = false;
		}

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
		if (viewingFrame.teams[0].possession)
		{
			discTrailMat.color = new Color(0f, 0.647f, 0.847f, 0.8f);
			discLight.color = new Color(0f, 0.647f, 0.847f, 0.8f);
			autograbColor = new Color(0f, 0.647f, 0.847f, 0.08f);
			autograbBubble.SetColor("Color_605CC4B0", autograbColor);
		}
		// orange team possession effects
		else if (viewingFrame.teams[1].possession)
		{
			discTrailMat.color = new Color(0.8235f, 0.4313f, 0.1764f, 0.8f);
			discLight.color = new Color(0.8235f, 0.4313f, 0.1764f, 0.8f);
			autograbColor = new Color(0.8235f, 0.4313f, 0.1764f, 0.08f);
			autograbBubble.SetColor("Color_605CC4B0", autograbColor);
		}
		// no team possession effects
		else
		{
			discTrailMat.color = new Color(1f, 1f, 1f, 0.8f);
			discLight.color = new Color(1f, 1f, 1f, 0.8f);
			autograbColor = new Color(1f, 1f, 1f, 0.08f);
			autograbBubble.SetColor("Color_605CC4B0", autograbColor);
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
		for (int i = 0; i < viewingFrame.teams.Length; i++)
		{
			if (viewingFrame.teams[i].players != null)
			{
				foreach (var player in viewingFrame.teams[i].players)
				{
					// get the matching player from the previous frame.
					// Searching through all the players is the only way, since they may have 
					// switched teams or been reorganized in the list when another player joins
					Player previousPlayer = FindPlayerOnTeam(previousFrame.teams[i], player.name);

					// if the player was on the other team last frame, remove it
					if (previousPlayer == null && playerObjects.ContainsKey(player.name))
					{
						Destroy(playerObjects[player.name].gameObject);
						playerObjects.Remove(player.name);
					}

					RenderPlayer(player, previousPlayer, i);

					movedObjects.Add(playerObjects[player.name]);
				}
			}
		}

		// remove players that weren't accessed this frame
		List<string> playersToRemove = new List<string>();
		foreach (var playerName in playerObjects.Keys)
		{
			if (!movedObjects.Contains(playerObjects[playerName]))
			{
				Destroy(playerObjects[playerName].gameObject);
				playersToRemove.Add(playerName);
			}
		}
		foreach (var p in playersToRemove)
		{
			playerObjects.Remove(p);
		}

		DiscController discScript = disc.GetComponent<DiscController>();
		discScript.discVelocity = viewingFrame.disc.velocity.ToVector3();
		discScript.discPosition = viewingFrame.disc.position.ToVector3();
		//discScript.isGrabbed = isBeingHeld(viewingFrame, false);
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

	public void RenderPlayer(Player player, Player lastFramePlayer, int teamIndex)
	{
		// if this player doesn't exist in the scene yet
		if (!playerObjects.ContainsKey(player.name))
		{
			// instantiate it
			playerObjects.Add(player.name, Instantiate(teamIndex == 0 ? bluePlayerPrefab : orangePlayerPrefab, playerObjsParent).GetComponent<PlayerCharacter>());
		}

		PlayerCharacter p = playerObjects[player.name];

		// Set names above player heads
		p.playerName.text = player.name;

		//Get player transform values of current iteration
		Vector3 playerVelocityVector = player.velocity.ToVector3();
		Vector3 previousVelocityVector = Vector3.zero;
		if (lastFramePlayer != null)
		{
			previousVelocityVector = lastFramePlayer.velocity.ToVector3();
		}

		//Set playerObject's transform values to those stored
		p.transform.position = player.position.ToVector3();
		//Old method that rotates entire player
		//playerObject.transform.rotation = Quaternion.LookRotation(new Vector3(playerHeadForward[0], playerHeadForward[1], playerHeadForward[2]));
		IKController playerIK = p.ikController;
		//Send Head and Hand transforms to IK script
		playerIK.headForward = player.forward.ToVector3();
		//playerIK.headForward = new Vector3(playerHeadForward[0], playerHeadForward[1], playerHeadForward[2]);

		playerIK.headUp = player.up.ToVector3();
		playerIK.rHandPosition = player.rhand.ToVector3();
		playerIK.lHandPosition = player.lhand.ToVector3();
		//Send Velocity to IK script
		playerIK.playerVelocity = playerVelocityVector;

		if (player.stunned && !p.stunnedInitiated)
		{
			FXInstantiate(punchParticle, p.transform.position, Vector3.zero);
			p.stunnedInitiated = true;
		}
		if (!player.stunned && p.stunnedInitiated)
			p.stunnedInitiated = false;

		//kinda jank but it works (back thruster)
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
					Vector3 discPosition = viewingFrame.disc.position.ToVector3Backwards();
					Vector3 rHand = p.rhand.ToVector3Backwards();
					Vector3 lHand = p.lhand.ToVector3Backwards();
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
		//playhead.CurrentFrameIndex = playhead.isPlaying ? Mathf.FloorToInt(playbackSlider.value) : (int)playbackSlider.value;
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
