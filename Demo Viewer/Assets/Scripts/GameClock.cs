using EchoVRAPI;
using TMPro;
using UnityEngine;

public class GameClock : MonoBehaviour
{
	public TMP_Text gameClock;
	public TMP_Text blueScore;
	public TMP_Text orangeScore;

	// Update is called once per frame
	private void Update()
	{
		if (DemoStart.playhead == null) return;
		Frame frame = DemoStart.playhead.GetFrame();
		if (frame == null) return;
		blueScore.text = frame.blue_points.ToString("D2");
		orangeScore.text = frame.orange_points.ToString("D2");
		gameClock.text = frame.game_clock_display;
	}
}