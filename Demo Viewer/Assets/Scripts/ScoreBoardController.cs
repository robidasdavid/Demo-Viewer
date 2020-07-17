using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreBoardController : MonoBehaviour
{
    //Sent from master script
    public string gameTime;
    public int blueScore;
    public int orangeScore;

    //scoreboard elements
    public Text[] timeText;
    public Text[] blueScoreText;
    public Text[] orangeScoreText;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        for(int i = 0; i < timeText.Length; i++)
        {
            timeText[i].text = gameTime;
            blueScoreText[i].text = string.Format("{0:00}", blueScore);
            orangeScoreText[i].text = string.Format("{0:00}", orangeScore);
        }
    }
}
