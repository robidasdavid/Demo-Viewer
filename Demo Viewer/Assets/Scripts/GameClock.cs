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
		if (DemoStart.instance.playhead == null) return;
		Frame frame = DemoStart.instance.playhead.GetFrame();
		if (frame == null) return;
		
		gameClock.text = frame.game_clock_display;
		blueScore.text = padWith0 ? frame.blue_points.ToString("00") : frame.blue_points.ToString();
		orangeScore.text = padWith0 ? frame.orange_points.ToString("00") : frame.orange_points.ToString();
	}
}