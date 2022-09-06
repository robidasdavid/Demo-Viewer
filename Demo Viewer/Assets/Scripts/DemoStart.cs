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
using System;
using System.Linq;
using TMPro;
using EchoVRAPI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Transform = UnityEngine.Transform;



public class DemoStart : MonoBehaviour
{
	#region Variables

	public GameObject orangeScoreEffects;
	public GameObject blueScoreEffects;

	public Text frameText;
	public Text gameTimeText;
	public List<Slider> playbackSliders;
	public Slider temporalProcessingSlider;
	public Slider speedSlider;

	public float playbackSpeed
	{
		get => playhead.playbackMultiplier;
		set
		{
			playhead.playbackMultiplier = value;
			UpdateSpeedSlider();
		}
	}

	public Text speedMultiplierText;

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
	public GameObject playerV4Prefab;

	private bool isScored = false;

	public bool inSnapUI;
	public bool wasDPADXReleased = true;
	private bool wasDPADYPressed = false;

	/// <summary>
	/// ((teamid, player ign), player character obj)
	/// </summary>
	[Obsolete("Switching to playerv4")] public static Dictionary<(int, string), PlayerCharacter> playerObjects = new Dictionary<(int, string), PlayerCharacter>();

	public static Dictionary<string, PlayerV4> playerV4Objects = new Dictionary<string, PlayerV4>();


	public Playhead playhead;
	public Replay replay;

	public TextMeshProUGUI replayFileNameText;

	/// <summary>
	/// 0 for none, 1 for all players, 2 for local player
	/// </summary>
	public static int showPlayspace;

	public SimpleCameraController camController;

	private static bool loadPointCloud;
	public Material pointCloudMaterial;
	public Mesh pointCloud;


	public static DemoStart instance;

	#endregion


	private void Start()
	{
		// Ahh yes welcome to the code

		instance = this;

		string demoFile = PlayerPrefs.GetString("fileDirector");
		
		// load arena model so it isn't empty if there is no file selected
		if (string.IsNullOrEmpty(demoFile))
		{
			SceneManager.LoadSceneAsync(GameManager.instance.arenaModelScenes[PlayerPrefs.GetInt("ArenaModel", 0)], LoadSceneMode.Additive);
		}
		
		replayFileNameText.text = Path.GetFileName(demoFile);
		replay.LoadFile(demoFile);

		// no need to deregister because replayLoader gets deleted on scene loads
		replay.FileLoaded += () =>
		{
			frameText.text = $"Frame 0 of {playhead.FrameCount}";

			//set slider values
			playbackSliders.ForEach(s => s.maxValue = playhead.FrameCount - 1);

			playerObjects = new Dictionary<(int, string), PlayerCharacter>();
			playerV4Objects = new Dictionary<string, PlayerV4>();

			//HUD initialization
			goalEventObject.SetActive(false);
			lastGoalStats.SetActive(false);

			// load a combat map if necessary
			// read the first frame
			Frame middleFrame = replay.GetFrame(playhead.FrameCount / 2);
			if (middleFrame.map_name == "mpl_arena_a")
			{
				// arena model
				SceneManager.LoadSceneAsync(GameManager.instance.arenaModelScenes[PlayerPrefs.GetInt("ArenaModel", 0)], LoadSceneMode.Additive);
			}
			else
			{
				// combat model
				SceneManager.LoadSceneAsync(GameManager.combatMapScenes[middleFrame.map_name], LoadSceneMode.Additive);
			}
		};

		replay.TemporalLoadingFinished += () =>
		{
			if (loadPointCloud)
			{
				pointCloud = new Mesh()
				{
					name = replay.FileName,
					vertices = replay.vertices.ToArray(),
					colors = replay.colors.ToArray(),
					normals = replay.normals.ToArray(),
					indexFormat = replay.vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
				};

				pointCloud.SetIndices(
					Enumerable.Range(0, replay.vertices.Count).ToArray(),
					MeshTopology.Points, 0
				);

				pointCloud.UploadMeshData(true);

				GameObject obj = new GameObject("Point Cloud");
				MeshFilter mf = obj.AddComponent<MeshFilter>();
				mf.sharedMesh = pointCloud;
				MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
				meshRenderer.sharedMaterial = GameManager.instance.demoStart.pointCloudMaterial;
			}
		};

		replay.LoadProgress += f =>
		{
			playbackSliders.ForEach(s => s.SetValueWithoutNotify(f));
		};

		replay.TemporalLoadProgress += f => { temporalProcessingSlider.value = f; };


		showPlayspace = PlayerPrefs.GetInt("ShowPlayspaceVisualizers", 0);
		loadPointCloud = PlayerPrefs.GetInt("ShowPointCloud", 0) == 1;

		float[] options = { 1, 10, 30, 50 };
		GameManager.instance.vrRig.transform.parent.localScale = Vector3.one * options[PlayerPrefs.GetInt("VRArenaScale", 2)];
	}

	// Update is called once per frame
	private void Update()
	{
		// controls help
		if (Input.GetKeyDown(KeyCode.H))
			controlsOverlay.SetActive(true);
		if (Input.GetKeyUp(KeyCode.H))
			controlsOverlay.SetActive(false);


		if (playhead.isPlaying && GameManager.instance.netFrameMan.IsLocalOrServer)
		{
			playhead.IncrementPlayhead(Time.deltaTime);
			// Find and declare what frame the slider is on.
			playbackSliders.ForEach(p => p.SetValueWithoutNotify(playhead.CurrentFrameIndex));
		}

		// process input
		CheckKeys();

		frameText.text = $"Frame {(playhead.CurrentFrameIndex + 1)} of {playhead.FrameCount}";
		playbackFramerate.text = $"{speedSlider.value:0.#}x";

		// Only render the next frame if it differs from the last (optimization)
		if (playhead.CurrentFrameIndex != playhead.LastFrameIndex || playhead.isPlaying || !GameManager.instance.netFrameMan.IsLocalOrServer)
		{
			playhead.LastFrameIndex = playhead.CurrentFrameIndex;

			// Grab frame
			Frame viewingFrame = playhead.GetFrame();
			Frame nearestFrame = playhead.GetNearestFrame();
			Frame previousFrame = playhead.GetPreviousFrame();

			if (viewingFrame != null && previousFrame != null)
			{
				// Arena-only stuff
				if (viewingFrame.map_name == "mpl_arena_a")
				{
					// Joust Readout
					if (viewingFrame.disc.position.Count != 0 &&
					    previousFrame.disc.position.Count != 0)
					{
						Vector3 currectDiscPosition = viewingFrame.disc.position.ToVector3();
						Vector3 lastDiscPosition = previousFrame.disc.position.ToVector3();
						if (lastDiscPosition == Vector3.zero && currectDiscPosition != Vector3.zero && playhead.isPlaying)
						{
							maxGameTime = replay.GetFrame(0).game_clock; // TODO this may not be correct if the recording starts midgame
							float currentTime = viewingFrame.game_clock;
							joustReadout.GetComponentInChildren<Text>().text = $"{maxGameTime - currentTime:0.##}";
							StartCoroutine(FlashInOut(joustReadout, 3));
						}
					}

					if (nearestFrame != null)
					{
						if (nearestFrame.last_score != null && previousFrame.last_score != null &&
						    !nearestFrame.last_score.Equals(previousFrame?.last_score))
						{
							GameEvents.Goal?.Invoke(nearestFrame.last_score);
						}

						if (nearestFrame.last_throw != null && previousFrame.last_throw != null &&
						    nearestFrame.last_throw.Equals(previousFrame.last_throw))
						{
							GameEvents.LocalThrow?.Invoke(nearestFrame.last_throw);
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

		bool leftMouseButtonDown = Input.GetMouseButtonDown(0);
		bool rightMouseButtonDown = Input.GetMouseButtonDown(1);
		//Debug.Log(Input.mousePosition);


		// hover over players to get stats
		if (Physics.Raycast(GameManager.instance.camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 1000f, LayerMask.GetMask("players")))
		{
			PlayerStatsHover psh = hit.collider.GetComponent<PlayerStatsHover>();
			if (!psh) return;

			psh.Visible = true;

			// clicked on a player
			// highlight player
			if (leftMouseButtonDown && Input.GetKey(KeyCode.LeftShift))
			{
				psh.ToggleHighlight();
			}
			// follow player
			else if (leftMouseButtonDown && !GameManager.instance.DrawingMode)
			{
				camController.FocusPlayer(psh.GetComponentInParent<IKController>().head);
			}
			
		}
	}

	/// <summary>
	/// This is used to see if the pointer is hovering over an UI object.
	/// </summary>
	/// <returns></returns>
	public static bool IsPointerOverUIObject()
	{
		PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
		eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
		List<RaycastResult> results = new List<RaycastResult>();
		EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
		return results.Count > 0;
	}


	/// <summary>
	/// Does input processing for keyboard and controller
	/// </summary>
	public void CheckKeys()
	{
		float triggerLinearity = 4;
		float maxScrubSpeed = 1.75f;
		float rightTrig = Input.GetAxis("RightTrig") * (inSnapUI ? 0 : 1);
		float leftTrig = Input.GetAxis("LeftTrig") * (inSnapUI ? 0 : 1);
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
			playbackSliders.ForEach(p => p.SetValueWithoutNotify(playhead.CurrentFrameIndex));
		}

		// skip forwards 1 second
		if (Input.GetKeyDown(KeyCode.RightArrow))
		{
			playhead.IncrementPlayhead(1);
			playbackSliders.ForEach(p => p.SetValueWithoutNotify(playhead.CurrentFrameIndex));
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

		for (int i = 0; i < 5; ++i)
		{
			if (Input.GetKeyDown("" + (i + 1)))
			{
				// TODO don't find players by scene name
				Transform playerByNumber = playerObjsParent.GetChild(i);
				if (playerByNumber.name == "PlayerCharacter (Blue)(Clone)")
				{
					camController.FocusPlayer(playerByNumber.GetComponent<IKController>().head);
				}
			}
		}

		for (int i = 5; i < 9; ++i)
		{
			if (Input.GetKeyDown("" + (i + 1)))
			{
				int startingIndex = 0;
				for (int j = 0; j < 5; ++j)
				{
					if (playerObjsParent.GetChild(j).name == "PlayerCharacter (Orange)(Clone)") startingIndex = j;
				}

				Transform playerByNumber = playerObjsParent.GetChild(startingIndex + (i - 5));
				if (playerByNumber)
				{
					camController.FocusPlayer(playerByNumber.GetComponent<IKController>().head);
				}
			}
		}

		if (Input.GetKeyDown(KeyCode.Escape))
		{
			switch (camController.Mode)
			{
				case SimpleCameraController.CameraMode.pov:
				case SimpleCameraController.CameraMode.follow:
				case SimpleCameraController.CameraMode.followOrbit:
					camController.FocusPlayer();
					break;
			}
		}

		if (Input.GetKeyDown(KeyCode.C))
		{
			camController.Mode = SimpleCameraController.CameraMode.free;
		}

		if (Input.GetKeyDown(KeyCode.R))
		{
			camController.Mode = SimpleCameraController.CameraMode.recorded;
		}

		if (Input.GetKeyDown(KeyCode.T))
		{
			camController.Mode = SimpleCameraController.CameraMode.sideline;
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
	public void RenderGoalStats(LastScore ls)
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

		// set 2d score board
		blueGoals.text = viewingFrame.blue_points.ToString();
		orangeGoals.text = viewingFrame.orange_points.ToString();

		List<PlayerCharacter> movedObjects = new List<PlayerCharacter>();

		// Update the playerv4 data
		// TODO work on bones
		// List<Player> playerList = viewingFrame.GetAllPlayers(true);
		// playerList.Sort((p1, p2)=> p2.team_color.CompareTo(p1.team_color));
		// for (int i = 0; i < playerList.Count; i++)
		// {
		// 	Player player = playerList[i];
		// 	if (!playerV4Objects.ContainsKey(player.name))
		// 	{
		// 		playerV4Objects[player.name] = Instantiate(playerV4Prefab, playerObjsParent).GetComponent<PlayerV4>();
		// 		playerV4Objects[player.name].name = player.name;
		// 	}
		//
		// 	BonePlayer bones = viewingFrame.bones.user_bones[i];
		// 	playerV4Objects[player.name].SetPlayerData(player.team_color, player, bones);
		// }


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
		foreach (Team team in frame.teams)
		{
			foreach (Player player in team.players)
			{
				if (player.name == name)
				{
					return player;
				}
			}
		}

		return null;
	}

	[Obsolete("Switching to playerv4")]
	private void RenderPlayer(Player player, Player lastFramePlayer, int teamIndex, EchoVRAPI.VRPlayer playspace)
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
		playerIK.headPos = player.head.Position;
		playerIK.headForward = player.head.forward.ToVector3();
		playerIK.headUp = player.head.up.ToVector3();

		// send hand pos/rot ✋🤚
		playerIK.lHandPosition = player.lhand.Position;
		playerIK.rHandPosition = player.rhand.Position;
		playerIK.lHandRotation = Quaternion.LookRotation(player.lhand.left.ToVector3(), player.lhand.forward.ToVector3());
		playerIK.rHandRotation = Quaternion.LookRotation(-player.rhand.left.ToVector3(), player.rhand.forward.ToVector3());

		// send body pos/rot 🕺
		playerIK.bodyPosition = player.body.Position;
		playerIK.bodyRotation = Quaternion.LookRotation(player.body.left.ToVector3(), player.body.up.ToVector3());
		playerIK.bodyUp = player.body.up.ToVector3();
		playerIK.bodyForward = player.body.forward.ToVector3();
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
		for (int j = 0; j < viewingFrame.teams.Count; j++)
		{
			if (viewingFrame.teams[j].players != null)
			{
				for (int i = 0; i < viewingFrame.teams[j].players.Count; i++)
				{
					Player p = viewingFrame.teams[j].players[i];
					Vector3 discPosition = viewingFrame.disc.position.ToVector3();
					Vector3 rHand = p.rhand.Position;
					Vector3 lHand = p.lhand.Position;
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
		playhead.wasPlaying = playhead.isPlaying;
		playhead.SetPlaying(false);
	}

	public void unUseSlider()
	{
		playhead.SetPlaying(false);
		playhead.SetPlaying(playhead.wasPlaying);
	}

	public void PlaybackValueChanged(float value)
	{
		// if is scrubbing with the slider
		if (!playhead.isPlaying)
		{
			playhead.CurrentFrameIndex = (int)value;
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


	public void settingController(bool value)
	{
		settingsScreen.SetActive(value);
	}

	public void setSoundBool()
	{
		isSoundOn = masterSoundToggle.isOn;
	}

	public static PlayerCharacter FindPlayerObjectByName(string name)
	{
		for (int i = 0; i < 3; i++)
		{
			if (playerObjects.ContainsKey((i, name)))
			{
				return playerObjects[(i, name)];
			}
		}

		return null;
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