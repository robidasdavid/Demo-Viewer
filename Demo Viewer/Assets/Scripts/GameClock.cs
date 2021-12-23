using EchoVRAPI;
using TMPro;
using UnityEngine;

public class GameClock : MonoBehaviour
{
	public TMP_Text gameClock;
	public TMP_Text blueScore;
	public TMP_Text orangeScore;
	public bool padWith0 = true;

	// Update is called once per frame
	private void Update()
	{
		if (DemoStart.playhead == null) return;
		Frame frame = DemoStart.playhead.GetFrame();
		if (frame == null) return;
		blueScore.text = padWith0 ? frame.blue_points.ToString("D2") : frame.blue_points.ToString();
		orangeScore.text = padWith0 ? frame.orange_points.ToString("D2") : frame.orange_points.ToString();
		gameClock.text = frame.game_clock_display;
	}
}