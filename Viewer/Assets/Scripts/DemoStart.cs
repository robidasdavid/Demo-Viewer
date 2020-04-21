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

//Serializable classes for JSON serializing from the API output.
[System.Serializable]
public class Game
{
    public float caprate;
    public long nframes;
    public Frame[] frames;
}
[System.Serializable]
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
[System.Serializable]
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
[System.Serializable]
public class Frame
{
    public Disc disc;

    public string sessionid;
    public int orange_points;
    public bool private_match;
    public string client_name;
    public string game_clock_display;
    public string game_status;
    public int game_clock;
    public string match_type;

    public Team[] teams;

    public string map_name;
    public int[] possession;
    public bool tournament_match;
    public int blue_points;

    public Last_Score last_score;


}
[System.Serializable]
public class Disc
{
    public float[] position;
    public float[] velocity;
    public int bounce_count;
}
[System.Serializable]
public class Team
{
    public Player[] players;
    public string team;
    public bool possession;
    public Stats stats;

}
[System.Serializable]
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
    public GameObject orangeScoreEffects;
    public GameObject blueScoreEffects;

    public Text frameText;
    public Text gameTimeText;
    public Slider playbackSlider;
    public Slider speedSlider;
    public int numFrames;
    public Game loadedDemo;

    public GameObject lastGoalStats;
    public static bool showingGoalStats;

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

    private bool isScored = false;

    private bool wasPlaying = false;
    public bool isPlaying = false;

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
    void Start()
    {
        //Ahh yes welcome to the code
        //Load and serialize demo file
        Debug.Log(Time.time + "Loading Demo");
        string demoFile = PlayerPrefs.GetString("fileDirector");
        string jsonText = File.ReadAllText(demoFile);
        loadedDemo = JsonUtility.FromJson<Game>(jsonText);
        Debug.Log(Time.time + "finished serializing");
        numFrames = loadedDemo.frames.Length;
        frameText.text = "Frame 0 of " + numFrames;
        demoFramerate = loadedDemo.caprate;

        //set slider values
        playbackSlider.maxValue = numFrames;

        //create arraylists for player objects and their nametags
        blueTeam.Add(blue1);
        blueTeam.Add(blue2);
        blueTeam.Add(blue3);
        blueTeam.Add(blue4);
        blueTeamText.Add(blue1text);
        blueTeamText.Add(blue2text);
        blueTeamText.Add(blue3text);
        blueTeamText.Add(blue4text);

        orangeTeam.Add(orange1);
        orangeTeam.Add(orange2);
        orangeTeam.Add(orange3);
        orangeTeam.Add(orange4);
        orangeTeamText.Add(orange1text);
        orangeTeamText.Add(orange2text);
        orangeTeamText.Add(orange3text);
        orangeTeamText.Add(orange4text);

        teamsText.Add(blueTeamText);
        teamsText.Add(orangeTeamText);

        Teams.Add(blueTeam);
        Teams.Add(orangeTeam);

        //HUD initialization
        goalEventObject.SetActive(false);
        lastGoalStats.SetActive(false);

        //Set replay settings
        discTrail.enabled = true;

    }

    // Update is called once per frame
    void Update()
    {

        //Find and declare what frame the slider is on.
        
        if (isPlaying)
        {
            playbackSlider.value = currentSliderFrame;
        }
        if (!isPlaying)
        {
            currentSliderFrame = (int)playbackSlider.value;
        }
        

        frameText.text = "Frame " + (currentSliderFrame + 1) + " of " + numFrames;
        string multi = string.Format("{0:0.##}", speedSlider.value);
        playbackFramerate.text = multi + "x";

        //Only render the next frame if it differs from the last (optimization)
        if (currentSliderFrame != currentFrame)
        {
            //Grab frame
            Frame viewingFrame = loadedDemo.frames[currentSliderFrame];
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
                if (viewingFrame.game_status == "score")
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

    //Handle instantiation of effects while playing versus not playing to minimize effects while scrubbing
    public void FXInstantiate(GameObject fx, Vector3 position, Vector3 rotation)
    {
        if(isPlaying)
        {
            Instantiate(fx, position, Quaternion.Euler(rotation));
        }
    }

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

        string gameTime = viewingFrame.game_clock_display;
        gameTimeText.text = gameTime;

        movedObjects.Clear();

        if (viewingFrame.game_status != "score")
        {
            isScored = false;
            goalEventObject.SetActive(false);
        }
        if (viewingFrame.game_status == "score" && !isScored)
        {
            goalEventObject.SetActive(true);
            isScored = true;
            FXInstantiate(blueScoreEffects, new Vector3(0.75f, 0, 0), new Vector3(0, 180, 0));
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
        if (viewingFrame.teams[0].possession)
        {
            discTrailMat.color = new Color(0, 165, 216, 0.6f);
        } else
        {
            discTrailMat.color = new Color(255, 0, 0, 0.6f);
        }


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

    public void openGoalStats()
    {
        showingGoalStats = true;
    }

    public void closeGoalStats()
    {
        showingGoalStats = false;
        
    }

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
        if (viewingFrame.game_status == "score" && !isScored)
        {
            goalEventObject.SetActive(true);
            isScored = true;
            if (viewingFrame.disc.position[0] > 0)
            {
                FXInstantiate(orangeScoreEffects, Vector3.zero, Vector3.zero);
            }
            else
            {
                FXInstantiate(blueScoreEffects, new Vector3(0.75f, 0, 0), new Vector3(0, 180, 0));
            }
        }

        if (viewingFrame.teams[0].possession)
        {
            discTrailMat.color = new Color(0f, 0.647f, 0.847f, 0.8f);
        }
        else if (viewingFrame.teams[1].possession)
        {
            discTrailMat.color = new Color(0.8235f, 0.4313f, 0.1764f, 0.8f);
        }
        else
        {
            discTrailMat.color = new Color(1f, 1f, 1f, 0.6f);
        }

        blueGoals.text = viewingFrame.teams[0].stats.points.ToString();
        orangeGoals.text = viewingFrame.teams[1].stats.points.ToString();
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
        float[] playerHeadRotation = viewingFrame.teams[j].players[i].forward;
        float[] rHandPosition = viewingFrame.teams[j].players[i].rhand;
        float[] lHandPosition = viewingFrame.teams[j].players[i].lhand;
        float[] playerVelocity = viewingFrame.teams[j].players[i].velocity;
        //Set playerObject's transform values to those stored
        playerObject.transform.position = new Vector3(playerPosition[2], playerPosition[1], playerPosition[0]);
        //Old method that rotates entire player
        //playerObject.transform.rotation = Quaternion.LookRotation(new Vector3(playerHeadRotation[0], playerHeadRotation[1], playerHeadRotation[2]));
        IKController playerIK = playerObject.GetComponent<IKController>();
        //Send Head and Hand transforms to IK script
        playerIK.headRotation = new Vector3(playerHeadRotation[2], playerHeadRotation[1], playerHeadRotation[0]);
        playerIK.rHandPosition = new Vector3(rHandPosition[2], rHandPosition[1], rHandPosition[0]);
        playerIK.lHandPosition = new Vector3(lHandPosition[2], lHandPosition[1], lHandPosition[0]);
        //Send Velocity to IK script
        playerIK.playerVelocity = new Vector3(playerVelocity[2], playerVelocity[1], playerVelocity[0]);

        if (viewingFrame.teams[j].players[i].stunned && !playerObject.GetComponent<playerVariables>().stunnedInitiated)
        {
            FXInstantiate(punchParticle, playerObject.transform.position, Vector3.zero);
            playerObject.GetComponent<playerVariables>().stunnedInitiated = true;
        }
        if (!viewingFrame.teams[j].players[i].stunned && playerObject.GetComponent<playerVariables>().stunnedInitiated)
            playerObject.GetComponent<playerVariables>().stunnedInitiated = false;

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
        //Playback speed controls
        yield return new WaitForSeconds((1/(loadedDemo.caprate + 20)) / speedSlider.value);
        yield return 0;
        currentSliderFrame++;
    }
    public ArrayList isBeingHeld(Frame viewingFrame, bool oneTeam)
    {
        /*
         * Returns arraylist
         * Index 1 (bool): isBeingHeld
         * Index 2 (bool): is it right hand?
         * Index 3 (int[2]): index 1=j index 2=i
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
        isPlaying = value;
        if (value)
            currentSliderFrame++;
    }
    //Handles new demo button (opens menu scene)
    public void openNewDemo()
    {
        SceneManager.LoadScene(0);
    }

    public void useSlider()
    {
        
        Debug.Log(isPlaying);
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
        if (isPlaying)
        {
            int newFrameNumber = Mathf.FloorToInt(playbackSlider.value);
            currentSliderFrame = newFrameNumber;
        } else
        {
            currentSliderFrame = (int)playbackSlider.value;
        }
        
    }
}
