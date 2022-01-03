using System.Collections;
using System.Collections.Generic;
using EchoVRAPI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MinimapPlayer : MonoBehaviour
{
    public TMP_Text text;
    public Image bg;
    public RectTransform rect;
    public Button button;

    // public Player PlayerData
    // {
    //     set
    //     {
    //         if (value == null) return;
    //         text.text = value.number.ToString("00");
    //         bg.color = value.team.color == Team.TeamColor.orange ? orangeColor : blueColor;
    //     }
    // }
}
