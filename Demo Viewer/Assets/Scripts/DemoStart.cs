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
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.IO.Compression;
using System;
using TMPro;
//using System.Media;

//Serializable classes for JSON serializing from the API output.
[Serializable]
public class Game
{
    public bool isNewstyle;
    public float caprate;
    public long nframes;
    public Frame[] frames;
}
[Serializable]
public class Stats
{
    public int possession_time;
    public int points;
    public int goals;
    public int saves;
    public int stuns;
    public int interceptions;
    public int blocks;
    public int passes;
    public int catches;
    public int steals;
    public int assists;
    public int shots_taken;
}
[Serializable]
public class Last_Score
{
    public float disc_speed;
    public string team;
    public string goal_type;
    public int point_amount;
    public float distance_thrown;
    public string person_scored;
    public string assist_scored;
}
[Serializable]
public class Frame
{
    public Disc disc;
    public double frameTimeOffset;

    public string sessionid;
    public int orange_points;
    public bool private_match;
    public string client_name;
    public string game_clock_display;
    public string game_status;
    public float game_clock;
    public string match_type;

    public Team[] teams;

    public string map_name;
    public int[] possession;
    public bool tournament_match;
    public int blue_points;

    public Last_Score last_score;


}
[Serializable]
public class Disc
{
    public float[] position;
    public float[] velocity;
    public int bounce_count;
}
[Serializable]
public class Team
{
    public Player[] players;
    public string team;
    public bool possession;
    public Stats stats;

}
[Serializable]
public class Player
{
    public string name;
    public float[] rhand;
    public int playerid;
    public float[] position;
    public float[] lhand;
    public long userid;
    public Stats stats;
    public int number;
    public int level;
    public bool possession;
    public float[] left;
    public bool invulnerable;
    public float[] up;
    public float[] forward;
    public bool stunned;
    public float[] velocity;
    public bool blocking;
}

public class PlayerStats : Stats
{
    public PlayerStats(int pt, int points, int goals, int saves, int stuns, int interceptions, int blocks, int passes, int catches, int steals, int assists, int shots_taken)
    {
        this.possession_time = pt;
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
    private float timer = 0.0f;
    public GameObject orangeScoreEffects;
    public GameObject blueScoreEffects;

    public Text frameText;
    public Text gameTimeText;
    public Slider playbackSlider;
    public Slider speedSlider;
    public int numFrames;
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

    public float demoFramerate;

    public Text playbackFramerate;

    public GameObject blue1;
    public Text blue1text;
    public GameObject blue2;
    public Text blue2text;
    public GameObject blue3;
    public Text blue3text;
    public GameObject blue4;
    public Text blue4text;

    public GameObject orange1;
    public Text orange1text;
    public GameObject orange2;
    public Text orange2text;
    public GameObject orange3;
    public Text orange3text;
    public GameObject orange4;
    public Text orange4text;

    public GameObject disc;
    public Material autograbBubble;
    private Color autograbColor;

    private bool isScored = false;

    private bool wasPlaying = false;
    float timeDelay = 0f;
    public bool isPlaying = false;
    public bool wasDPADXReleased = true;

    public bool isReverse = false;
    public bool wasPlayingBeforeScrub = false;
    public bool isScrubbing = false;
    public float playbackMultiplier = 1f;

    ArrayList blueTeam = new ArrayList();
    ArrayList orangeTeam = new ArrayList();
    ArrayList Teams = new ArrayList();
    ArrayList blueTeamText = new ArrayList();
    ArrayList orangeTeamText = new ArrayList();
    ArrayList teamsText = new ArrayList();
    ArrayList movedObjects = new ArrayList();


    public int currentFrame = 0;
    public int currentSliderFrame = 0;

    public static Color blueTeamColor = new Color(0, 165, 216);
    public static Color orangeTeamColor = new Color(210, 110, 45);

    Shader standard;
    public Shader discThroughWall;

    private Frame previousFrame;
    private Frame nextFrame;
    public ScoreBoardController scoreBoardController;

    private string jsonStr;
    bool ready = false;

    public bool showDebugLogs;

    protected bool ISWEBGL = false;
    protected string IP = "http://69.30.197.26:5000";

    public TextMeshProUGUI replayFileNameText;

    IEnumerator GetText(string fn, Action doLast)
    {
        UnityWebRequest req = new UnityWebRequest();
        req = UnityWebRequest.Get(string.Format("{0}/file?name={1}", IP, fn));
        yield return req.SendWebRequest();

        DownloadHandler dh = req.downloadHandler;
        
        //this.jsonStr = dh.text;
        StreamReader read = new StreamReader( new MemoryStream(dh.data));
        loadNewStyle(read);
        doLast();
    }

    private static StreamReader OpenOrExtract(StreamReader reader)
        {
            char[] buffer = new char[2];
            reader.Read(buffer, 0, buffer.Length);
            reader.DiscardBufferedData();
            reader.BaseStream.Seek(0, System.IO.SeekOrigin.Begin); 
            if(buffer[0] == 'P' && buffer[1] == 'K'){                
                ZipArchive archive = new ZipArchive(reader.BaseStream);
                StreamReader ret = new StreamReader(archive.Entries[0].Open());
                //reader.Close();
                return ret;
            }
            return reader;
        }
    

    Game loadNewStyle(StreamReader fileReader){
        bool fileFinishedReading = false;
        List<Frame> readFrames = new List<Frame>();
        Game readGame = new Game();
        readGame.caprate = 60;

        DateTime? currentLoadDateTimeFrame = null;
                //filesInFolder = Directory.GetFiles(readFromFolder, "*.zip").ToList();
                //filesInFolder.Sort();
                using(fileReader = OpenOrExtract(fileReader)){

            while (!fileFinishedReading)
            {
                    if (fileReader != null)
                    {
                        string rawJSON = fileReader.ReadLine();
                        if (rawJSON == null)
                        {
                            fileFinishedReading = true;
                            fileReader.Close();
                            // if (readFromFolderIndex >= filesInFolder.Count)
                            // {
                            // 	fileFinishedReading = true;
                            // }
                            // else
                            // {
                            // 	fileReader = ExtractFile(fileReader, filesInFolder[readFromFolderIndex++]);
                            // }
                        }
                        else
                        {
                            string[] splitJSON = rawJSON.Split('\t');
                            string onlyJSON, onlyTime;
                            double frameTimeOffset = 0;
                            if (splitJSON.Length > 1)
                            {
                                onlyJSON = splitJSON[1];
                                onlyTime = splitJSON[0];
                            }
                            else
                            {
                                onlyJSON = splitJSON[0];
                                onlyTime = currentLoadDateTimeFrame.ToString();
                                // onlyTime = fileName.Substring(4, fileName.Length - 8);
                            }
                            DateTime frameTime = DateTime.Parse(onlyTime);
                            if(currentLoadDateTimeFrame != null){
                                TimeSpan frameOffsetTS = frameTime - currentLoadDateTimeFrame.Value;
                                frameTimeOffset = frameOffsetTS.TotalMilliseconds;
                            }
                            currentLoadDateTimeFrame = frameTime;

                            Frame foundFrame = JsonUtility.FromJson<Frame>(onlyJSON);
                            foundFrame.frameTimeOffset = frameTimeOffset;
                            readFrames.Add(foundFrame);

                            // if (readIntoQueue)
                            // {
                            // 	lastJSONQueue.Enqueue(onlyJSON);
                            // 	frameTimeOffset.Enqueue(frameTimeOffset);
                            // }
                            // else
                            // {
                            // 	lock (lastJSONLock)
                            // 	{
                            // 		lastDateTimeString = onlyTime;
                            // 		lastJSON = onlyJSON;
                            // 		lastJSONUsed = false;
                            // 	}
                            // }
                        }
                    }
                }
                }
                readGame.frames = readFrames.ToArray();
                readGame.nframes = readGame.frames.Length;
                return readGame;
    }

    void DoLast(){
        DoLast("");
    }

    void DoLast(string demoFile)
    {
        //Debug.Log(this.jsonStr);
        if(demoFile.Contains(".echoreplayold")){
            jsonStr = File.ReadAllText(demoFile);
            loadedDemo = JsonUtility.FromJson<Game>(this.jsonStr);
            loadedDemo.isNewstyle = false;
            sendConsoleMessage("Finished Serializing.");
        }else if(demoFile == ""){
            loadedDemo.isNewstyle = true;
        }
        else{
            Debug.Log("Reading file: " + demoFile);
            StreamReader reader = new StreamReader(demoFile);
            loadedDemo = loadNewStyle(reader);
            loadedDemo.isNewstyle = true;
        }
        


        numFrames = loadedDemo.frames.Length;
        frameText.text = string.Format("Frame 0 of {0}", numFrames);
        demoFramerate = loadedDemo.caprate;// * 2.6f;

        standard = Shader.Find("standard");

        //set slider values
        playbackSlider.maxValue = numFrames - 1;

        //create arraylists for player objects and their nametags
        for (int i = 1; i < 5; i++)
        {
            blueTeam.Add(this.GetType().GetField(string.Format("blue{0}", i)).GetValue(this));
            blueTeamText.Add(this.GetType().GetField(string.Format("blue{0}text", i)).GetValue(this));
            orangeTeam.Add(this.GetType().GetField(string.Format("orange{0}", i)).GetValue(this));
            orangeTeamText.Add(this.GetType().GetField(string.Format("orange{0}text", i)).GetValue(this));
        }

        teamsText.Add(blueTeamText);
        teamsText.Add(orangeTeamText);

        Teams.Add(blueTeam);
        Teams.Add(orangeTeam);

        //HUD initialization
        goalEventObject.SetActive(false);
        lastGoalStats.SetActive(false);

        //Set replay settings
        discTrail.enabled = true;

        //Make the previous frame a thing
        previousFrame = loadedDemo.frames[0];
        nextFrame = loadedDemo.frames[2];

        //Set timing
        // Time.fixedDeltaTime = 1f / (demoFramerate*3.5f);
        Application.targetFrameRate = 300;
        Time.fixedDeltaTime = 1f / (demoFramerate*1f);
        timeDelay = 1f / (60f);

        ready = true;
    }

    /// <summary>
    /// Loads the currently set file (set in playerprefs beforehand)
    /// </summary>
    public void ReloadFile()
    {

    }

    void Start()
    {
        //Ahh yes welcome to the code

        //Load and serialize demo file
        if (!ISWEBGL)
        {
            string demoFile = PlayerPrefs.GetString("fileDirector");
            replayFileNameText.text = Path.GetFileName(demoFile);
            sendConsoleMessage("Loading Demo: " + demoFile);
            
            DoLast(demoFile);
        }
        else
        {
            string getFileName = "";
            int pm = Application.absoluteURL.IndexOf("=");
            if (pm != -1)
            {
                getFileName = Application.absoluteURL.Split("="[0])[1];
            }
            sendConsoleMessage("Loading: " + getFileName);

            StartCoroutine(GetText(getFileName, DoLast));
        }       
    }

    // Update is called once per frame
    void Update()
    {

        if (ready)
        {
            if(isPlaying){
                timer += Time.deltaTime;
                //Playback speed controls
                if (!isReverse && currentSliderFrame == numFrames - 1 || isReverse && currentSliderFrame == 0)
                {
                    setPlaying(false);
                } else
                {
                    if(timer > ((float)((nextFrame.frameTimeOffset-4)*playbackMultiplier)/1000f)){
                        int incrementFrameCount = 0;
                        double totalFrameTimeOffset = nextFrame.frameTimeOffset - 3;
                        while(timer > ((float)((totalFrameTimeOffset)*playbackMultiplier)/1000f)){
                            incrementFrameCount ++;
                            if(isReverse){
                                if(currentFrame - incrementFrameCount == 0){
                                    break;
                                }
                                totalFrameTimeOffset += loadedDemo.frames[currentFrame - incrementFrameCount + 1].frameTimeOffset - 3;
                            }else{
                                if(currentFrame + incrementFrameCount == numFrames - 1){
                                    break;
                                }
                                totalFrameTimeOffset += loadedDemo.frames[currentFrame + incrementFrameCount + 1].frameTimeOffset - 3;
                            }
                        }
                        if(isReverse){
                            currentSliderFrame = currentSliderFrame - incrementFrameCount;
                        }else{
                            currentSliderFrame = currentSliderFrame + incrementFrameCount;
                        }
                        timer = 0.0f;
                    }
                }
            }
            
            //update previous frame
            
            //Find and declare what frame the slider is on.
            if (isPlaying)
                playbackSlider.value = currentSliderFrame;
            else
                currentSliderFrame = (int)playbackSlider.value;

            checkKeys();

            frameText.text = string.Format("Frame {0} of {1}", (currentSliderFrame + 1), numFrames);
            playbackFramerate.text = string.Format("{0:0.#}x", speedSlider.value);

            //Only render the next frame if it differs from the last (optimization)
            if (currentSliderFrame != currentFrame)
            {
                if (currentFrame != 0){
                    if(isReverse){
                        previousFrame = loadedDemo.frames[currentFrame + 1];
                    }else{
                        previousFrame = loadedDemo.frames[currentFrame - 1];
                    }
                }

                if (!isReverse && currentFrame != numFrames - 1){
                    nextFrame = loadedDemo.frames[currentFrame + 1];
                }else if(isReverse && currentFrame != 0){
                    nextFrame = loadedDemo.frames[currentFrame];
                }
                //Grab frame
                Frame viewingFrame = loadedDemo.frames[currentSliderFrame];
                //Joust Readout
                Vector3 currectDiscPosition = new Vector3(viewingFrame.disc.position[2], viewingFrame.disc.position[1], viewingFrame.disc.position[0]);
                Vector3 lastDiscPosition = new Vector3(previousFrame.disc.position[2], previousFrame.disc.position[1], previousFrame.disc.position[0]);
                if (lastDiscPosition == Vector3.zero && currectDiscPosition != Vector3.zero && isPlaying)
                {
                    maxGameTime = loadedDemo.frames[0].game_clock;
                    float currentTime = loadedDemo.frames[currentSliderFrame].game_clock;
                    joustReadout.transform.GetChild(0).GetComponent<Text>().text = string.Format("{0:0.##}", maxGameTime - currentTime);
                    IEnumerator coroutine = FlashInOut(joustReadout, 3);
                    StartCoroutine(coroutine);
                }

                //Handle goal stat visibility
                if (showingGoalStats)
                {
                    goalEventObject.SetActive(false);
                    lastGoalStats.SetActive(true);
                    renderGoalStats(viewingFrame);
                }
                else if (!showingGoalStats && lastGoalStats.activeSelf)
                {
                    lastGoalStats.SetActive(false);
                    if (viewingFrame.game_status == "score" && showGoalAnim)
                    {
                        goalEventObject.SetActive(true);
                    }
                }
                //Playing or paused? 
                if (isPlaying)
                {
                    //Start coroutine on framerate tick
                    currentFrame = currentSliderFrame;
                    IEnumerator coroutine = startPlayback(viewingFrame);
                    StartCoroutine(coroutine);
                }
                else
                {
                    //if not playing continue with single-frame rendering                 
                    if (viewingFrame.teams.Length > 1 && viewingFrame.teams[0].players != null && viewingFrame.teams[1].players != null)
                    {
                        renderFrame(viewingFrame);
                    }
                    else
                    {
                        renderFrameOneTeam(viewingFrame);
                    }
                }
            }
        }
    }

    public void checkKeys()
    {
        sendConsoleMessage(Input.GetAxis("RightTrig").ToString());
        sendConsoleMessage(Input.GetAxis("LeftTrig").ToString());
        float rightTrig = Input.GetAxis("RightTrig") * 1.75f;
        float leftTrig = Input.GetAxis("LeftTrig") * 1.75f;
        float combinedTrigs = rightTrig - leftTrig;
        if(combinedTrigs == 0){
            if(isScrubbing){
                isScrubbing = false;
                isPlaying = wasPlayingBeforeScrub;
                isReverse = false;
                playbackMultiplier = 1f;
            }
        }else if(combinedTrigs < 0){
            if(!isScrubbing){
                wasPlayingBeforeScrub = isPlaying;
            }
            isPlaying = true;
            playbackMultiplier = 1f / (combinedTrigs * -1f);
            isReverse = true;
            isScrubbing = true;
        }else{
            if(!isScrubbing){
                wasPlayingBeforeScrub = isPlaying;
            }
            isPlaying = true;
            playbackMultiplier = 1f / combinedTrigs;
            isReverse = false;
            isScrubbing = true;

        }
        if (Input.GetKeyDown(KeyCode.H))
            controlsOverlay.SetActive(true);

        if (Input.GetKeyUp(KeyCode.H))
            controlsOverlay.SetActive(false);

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("XboxA")){
            if(isScrubbing){
                wasPlayingBeforeScrub = !wasPlayingBeforeScrub;
            }
            if(playbackMultiplier != 1f && isPlaying || isReverse){
                isReverse = false;
                playbackMultiplier = 1f;
            }
            else{
                setPlaying(!isPlaying);
            }
        }
        if(Input.GetButtonDown("XboxSelect")){
            showGoalAnim = !showGoalAnim;
        }

        if(Input.GetButtonDown("XboxY")){
            showingGoalStats = !showingGoalStats;
        }
        if(Input.GetButtonDown("XboxStart")){
            GUIUtility.systemCopyBuffer = currentFrame.ToString();
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
            speedSlider.value += .2f;
        
        float dpadX = Input.GetAxis("XboxDpadX");
        if(!wasDPADXReleased && dpadX == 0){
            wasDPADXReleased = true;
        }else if(wasDPADXReleased && dpadX == -1){
            wasDPADXReleased = false;
            isPlaying = true;
            if(isReverse == true){
                if(playbackMultiplier > 1/10f){
                    playbackMultiplier = playbackMultiplier / 2f;
                }
            }else{
                isReverse = true;
                playbackMultiplier = 1f;
            }

        }else if(wasDPADXReleased && dpadX == 1){
            wasDPADXReleased = false;
            isPlaying = true;
            if(isReverse == false){
                if(playbackMultiplier >= 1/10f){
                    playbackMultiplier = playbackMultiplier / 2f;
                }
            }else{
                isReverse = false;
                playbackMultiplier = 1f;
            }
        }


        if (Input.GetKeyDown(KeyCode.DownArrow))
            speedSlider.value -= .2f;

        if(Input.GetKeyDown(KeyCode.Backspace)){
            SceneManager.LoadScene(0);
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            int newFrameNumber = Mathf.FloorToInt(playbackSlider.value) - (Mathf.FloorToInt(demoFramerate) * 5);
            currentSliderFrame = newFrameNumber < 0 ? 0 : newFrameNumber;

            playbackSlider.value = currentSliderFrame;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            int newFrameNumber = Mathf.FloorToInt(playbackSlider.value) + (Mathf.FloorToInt(demoFramerate) * 5);
            currentSliderFrame = newFrameNumber > numFrames ? numFrames : newFrameNumber;

            playbackSlider.value = currentSliderFrame;
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
        if(isPlaying)
            Instantiate(fx, position, Quaternion.Euler(rotation));
    }

    //public void soundHandler(SoundPlayer s, float vol, string sound)
    //{
        //todo
    //}

    //Handle goal stats GUI
    public void renderGoalStats(Frame viewingFrame)
    {
        Last_Score ls = viewingFrame.last_score;
        goalEventObject.SetActive(false);
        //Debug.Log(ls.disc_speed.ToString());
        lastGoalStats.transform.GetChild(1).GetComponent<Text>().text = ls.goal_type + string.Format(" : {0} PTS", ls.point_amount);
        lastGoalStats.transform.GetChild(2).GetComponent<Text>().text = ls.person_scored;
        //If no assister, hide assistedbytext and assistedbyname
        if (ls.assist_scored.Equals("[INVALID]"))
        {
            lastGoalStats.transform.GetChild(3).GetComponent<Text>().text = "";
            lastGoalStats.transform.GetChild(7).GetComponent<Text>().text = "";
        } else
        {
            lastGoalStats.transform.GetChild(3).GetComponent<Text>().text = ls.assist_scored;
        }

        lastGoalStats.transform.GetChild(4).GetComponent<Text>().text = string.Format("{0:0.##m/s}", ls.disc_speed);
        lastGoalStats.transform.GetChild(5).GetComponent<Text>().text = string.Format("{0:0m}", ls.distance_thrown);
    }

    public void renderFrameOneTeam(Frame viewingFrame)
    {
        int numBluePlayers = viewingFrame.teams[0].players.Length;

        gameTimeText.text = viewingFrame.game_clock_display;

        movedObjects.Clear();

        if (viewingFrame.game_status != "score")
        {
            isScored = false;
            goalEventObject.SetActive(false);
        }
        if (viewingFrame.game_status == "score" && !isScored && showGoalAnim)
        {
            goalEventObject.SetActive(true);
            isScored = true;
            //Why tf is this here
            //FXInstantiate(blueScoreEffects, new Vector3(0.75f, 0, 0), new Vector3(0, 180, 0));
        }

        if (!(bool)isBeingHeld(viewingFrame, true)[0])
        {
            if (!discTrail.enabled)
                discTrail.Clear();
            discTrail.enabled = true;
        }
        else
        {
            discTrail.enabled = false;
        }

        

        discTrailMat.color = viewingFrame.teams[0].possession ? new Color(0f, 0.647f, 0.847f, 0.8f) : new Color(1, 1, 1, 0.8f);
        discLight.color = viewingFrame.teams[0].possession ? new Color(0f, 0.647f, 0.847f, 0.8f) : new Color(1, 1, 1, 0.8f);
        autograbColor = viewingFrame.teams[0].possession ? new Color(0f, 0.647f, 0.847f, 0.08f) : new Color(1, 1, 1, 0.08f);
        autograbBubble.SetColor("Color_605CC4B0", autograbColor);
        



        blueGoals.text = viewingFrame.teams[0].stats.points.ToString();
        orangeGoals.text = "0";

        for (int i = 0; i < viewingFrame.teams[0].players.Length; i++)
        {
            renderPlayer(0, i, viewingFrame);
        }
        foreach (GameObject b in blueTeam)
        {
            if (!movedObjects.Contains(b))
            {
                b.transform.position = new Vector3(100, 100, 100);
            }
        }
        foreach (GameObject o in orangeTeam)
        {
            if (!movedObjects.Contains(o))
            {
                o.transform.position = new Vector3(100, 100, 100);
            }
        }
    }
    /*
    public void openGoalStats()
    {
        showingGoalStats = true;
    }

    public void closeGoalStats()
    {
        showingGoalStats = false;  
    }
    */
    public void renderFrame(Frame viewingFrame)
    {

        int numBluePlayers = viewingFrame.teams[0].players.Length;
        int numOrangePlayers = viewingFrame.teams[1].players.Length;

        string gameTime = viewingFrame.game_clock_display;
        gameTimeText.text = gameTime;

        movedObjects.Clear();

        if (!(bool)isBeingHeld(viewingFrame, false)[0])
        {
            if (!discTrail.enabled)
                discTrail.Clear();
            discTrail.enabled = true;
        } else
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

        if (viewingFrame.teams[0].possession) 
        {
            discTrailMat.color = new Color(0f, 0.647f, 0.847f, 0.8f);
            discLight.color = new Color(0f, 0.647f, 0.847f, 0.8f);
            autograbColor = new Color(0f, 0.647f, 0.847f, 0.08f);
            autograbBubble.SetColor("Color_605CC4B0", autograbColor);
            
        }
        else if (viewingFrame.teams[1].possession)
        {
            discTrailMat.color = new Color(0.8235f, 0.4313f, 0.1764f, 0.8f);
            discLight.color = new Color(0.8235f, 0.4313f, 0.1764f, 0.8f);
            autograbColor = new Color(0.8235f, 0.4313f, 0.1764f, 0.08f);
            autograbBubble.SetColor("Color_605CC4B0", autograbColor);
            
        }
        else
        {
            discTrailMat.color = new Color(1f, 1f, 1f, 0.8f);
            discLight.color = new Color(1f, 1f, 1f, 0.8f);
            autograbColor = new Color(1f, 1f, 1f, 0.08f);
            autograbBubble.SetColor("Color_605CC4B0", autograbColor);
            
        }

        blueGoals.text = viewingFrame.blue_points.ToString();
        orangeGoals.text = viewingFrame.orange_points.ToString();
        //set in game scoreboard elements
        scoreBoardController.blueScore = viewingFrame.blue_points;
        scoreBoardController.orangeScore = viewingFrame.orange_points;
        scoreBoardController.gameTime = viewingFrame.game_clock_display;

        for (int j = 0; j < 2; j++)
        {
            for (int i = 0; i < viewingFrame.teams[j].players.Length; i++)
            {
                renderPlayer(j, i, viewingFrame);
            }
        }
        foreach (GameObject b in blueTeam)
        {
            if (!movedObjects.Contains(b))
            {
                b.transform.position = new Vector3(100, 100, 100);
            }
        }
        foreach (GameObject o in orangeTeam)
        {
            if (!movedObjects.Contains(o))
            {
                o.transform.position = new Vector3(100, 100, 100);
            }
        }
        DiscController discScript = disc.GetComponent<DiscController>();
        discScript.discVelocity = new Vector3(viewingFrame.disc.velocity[2], viewingFrame.disc.velocity[1], viewingFrame.disc.velocity[0]);
        discScript.discPosition = new Vector3(viewingFrame.disc.position[2], viewingFrame.disc.position[1], viewingFrame.disc.position[0]);
        //discScript.isGrabbed = isBeingHeld(viewingFrame, false);
    }

    public void renderPlayer(int j, int i, Frame viewingFrame)
    {
        //Create temp gameObject "playerObject" to set transform of later.
        GameObject playerObject = (GameObject)(((ArrayList)Teams[j])[i]);
        int[] temp = new int[2] { j, i };
        /*Debug.Log(temp);
        if(temp == (int[])isBeingHeld(viewingFrame, false)[2])
        {
            disc.GetComponent<DiscController>().playerHolding = playerObject;
        }*/

        //Set names above player heads
        ((Text)((ArrayList)teamsText[j])[i]).text = viewingFrame.teams[j].players[i].name;

        //Get player transform values of current iteration
        float[] playerPosition = viewingFrame.teams[j].players[i].position;
        float[] playerHeadForward = viewingFrame.teams[j].players[i].forward;
        float[] playerHeadUp = viewingFrame.teams[j].players[i].up;
        float[] rHandPosition = viewingFrame.teams[j].players[i].rhand;
        float[] lHandPosition = viewingFrame.teams[j].players[i].lhand;
        float[] playerVelocity = viewingFrame.teams[j].players[i].velocity;
        float[] previousVelocity = previousFrame.teams[j].players[i].velocity;
        Vector3 playerVelocityVector = new Vector3(playerVelocity[2], playerVelocity[1], playerVelocity[0]);
        Vector3 previousVelocityVector = new Vector3(previousVelocity[2], previousVelocity[1], previousVelocity[0]);

        //Set playerObject's transform values to those stored
        playerObject.transform.position = new Vector3(playerPosition[2], playerPosition[1], playerPosition[0]);
        //Old method that rotates entire player
        //playerObject.transform.rotation = Quaternion.LookRotation(new Vector3(playerHeadForward[0], playerHeadForward[1], playerHeadForward[2]));
        IKController playerIK = playerObject.GetComponent<IKController>();
        //Send Head and Hand transforms to IK script
        playerIK.headForward = new Vector3(playerHeadForward[2], playerHeadForward[1], playerHeadForward[0]);
        //playerIK.headForward = new Vector3(playerHeadForward[0], playerHeadForward[1], playerHeadForward[2]);

        playerIK.headUp = new Vector3(playerHeadUp[2], playerHeadUp[1], playerHeadUp[0]);
        playerIK.rHandPosition = new Vector3(rHandPosition[2], rHandPosition[1], rHandPosition[0]);
        playerIK.lHandPosition = new Vector3(lHandPosition[2], lHandPosition[1], lHandPosition[0]);
        //Send Velocity to IK script
        playerIK.playerVelocity = playerVelocityVector;
        //assign playerVariables so you dont have to getcomponent multiple times because getcomponent is expensive SNEAKY (it could still be more efficient but that would be a lot of work)
        playerVariables playerVariablesComponent = playerObject.GetComponent<playerVariables>();

        if (viewingFrame.teams[j].players[i].stunned && !playerObject.GetComponent<playerVariables>().stunnedInitiated)
        {
            FXInstantiate(punchParticle, playerObject.transform.position, Vector3.zero);
            playerVariablesComponent.stunnedInitiated = true;
        }
        if (!viewingFrame.teams[j].players[i].stunned && playerObject.GetComponent<playerVariables>().stunnedInitiated)
            playerVariablesComponent.stunnedInitiated = false;

        //kinda jank but it works (back thruster)
        if (playerVelocityVector.magnitude - previousVelocityVector.magnitude > 1)
        {
            playerVariablesComponent.boost.SetActive(false);
            playerVariablesComponent.boost.SetActive(true);
        }

        //Player Blocking
        GameObject blockingEffect = playerVariablesComponent.playerShield;
        if (viewingFrame.teams[j].players[i].blocking)
        {
            blockingEffect.gameObject.SetActive(true);
        } else
        {
            blockingEffect.gameObject.SetActive(false);
        }

        movedObjects.Add(playerObject);
    }
    //Same as rendering above, just in a coroutine.
    private IEnumerator startPlayback(Frame viewingFrame)
    {
        if (viewingFrame.teams.Length > 1 && viewingFrame.teams[0].players != null && viewingFrame.teams[1].players != null)
        {
            renderFrame(viewingFrame);
        } else
        {
            renderFrameOneTeam(viewingFrame);
        }
        // if(loadedDemo.isNewstyle && ((float)nextFrame.frameTimeOffset /1000f) > (timeDelay*1.8)){
        //     //yield return new WaitForSeconds(((float)nextFrame.frameTimeOffset/1000f));
        // }else{
            yield return 0;
        // }
    }
    public ArrayList isBeingHeld(Frame viewingFrame, bool oneTeam)
    {
        /*
         * Returns arraylist
         * Index 1 (bool): isBeingHeld
         * Index 2 (bool): is it right hand?
         * Index 3 (int[2]): index 1=j index 2=i ... this is the player index
         */
        ArrayList returnArray = new ArrayList();
        //bool[] returnArray = new bool[2]{ false, false };
        if (!oneTeam)
        {
            for (int j = 0; j < 2; j++)
            {
                for (int i = 0; i < viewingFrame.teams[j].players.Length; i++)
                {
                    Player p = viewingFrame.teams[j].players[i];
                    Vector3 discPosition = new Vector3(viewingFrame.disc.position[0], viewingFrame.disc.position[1], viewingFrame.disc.position[2]);
                    Vector3 rHand = new Vector3(p.rhand[0], p.rhand[1], p.rhand[2]);
                    Vector3 lHand = new Vector3(p.lhand[0], p.lhand[1], p.lhand[2]);
                    float rHandDis = Vector3.Distance(discPosition, rHand);
                    float lHandDis = Vector3.Distance(discPosition, lHand);
                    if (rHandDis < 0.2f)
                    {
                        returnArray.Add(true);
                        returnArray.Add(true);
                        returnArray.Add(new int[2]{ j, i });
                        return returnArray;
                    }
                    if (lHandDis < 0.2f)
                    {
                        returnArray.Add(true);
                        returnArray.Add(false);
                        returnArray.Add(new int[2] { j, i });
                        return returnArray;
                    }
                }
            }

        }
        if (oneTeam)
        {
            for (int i = 0; i < viewingFrame.teams[0].players.Length; i++)
            {
                Player p = viewingFrame.teams[0].players[i];
                Vector3 discPosition = new Vector3(viewingFrame.disc.position[0], viewingFrame.disc.position[1], viewingFrame.disc.position[2]);
                Vector3 rHand = new Vector3(p.rhand[0], p.rhand[1], p.rhand[2]);
                Vector3 lHand = new Vector3(p.lhand[0], p.lhand[1], p.lhand[2]);
                float rHandDis = Vector3.Distance(discPosition, rHand);
                float lHandDis = Vector3.Distance(discPosition, lHand);

                if (rHandDis < 0.2f)
                {
                    returnArray.Add(true);
                    returnArray.Add(true);
                    returnArray.Add(new int[2] { 0, i });
                    return returnArray;
                }
                if (lHandDis < 0.2f)
                {
                    returnArray.Add(true);
                    returnArray.Add(false);
                    returnArray.Add(new int[2] { 0, i });
                    return returnArray;
                }
            }
        }
        returnArray.Add(false);
        returnArray.Add(false);
        returnArray.Add(new int[2] { -1, -1 });
        return returnArray;
    }
  
    //Function to set playing variable to start and stop auto-play of demo.
    public void setPlaying(bool value)
    {
        isReverse = false;
        isPlaying = value;
        if (value && currentSliderFrame != numFrames - 1) {
            playbackMultiplier = 1f;
            currentSliderFrame++;
        }
    }
    //Handles new demo button (opens menu scene)
    public void openNewDemo()
    {
        SceneManager.LoadScene(0);
    }

    public void useSlider()
    {
        
        sendConsoleMessage(isPlaying.ToString());
        wasPlaying = isPlaying;
        setPlaying(false);
        
    }

    public void unUseSlider()
    {
        
        setPlaying(false);
        setPlaying(wasPlaying);
        
    }

    public void playbackValueChanged()
    {
        currentSliderFrame = isPlaying ? Mathf.FloorToInt(playbackSlider.value) : (int)playbackSlider.value;
    }

    public void sendConsoleMessage(string msg)
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
