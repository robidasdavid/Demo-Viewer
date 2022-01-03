using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EchoVRAPI;
using UnityEngine;

public class Minimap : MonoBehaviour
{
	public RectTransform playerParent;
	public GameObject playerPrefab;
	private readonly List<MinimapPlayer> players = new List<MinimapPlayer>();
	public Color orangeColor = Color.red;
	public Color blueColor = Color.blue;
	public float playerPositionScale = 4.5f;


	// Update is called once per frame
	private void Update()
	{
		if (DemoStart.playhead == null) return;
		Frame f = DemoStart.playhead.GetFrame();
		if (f == null) return;
		List<Player> framePlayers = f.GetAllPlayers();

		// add new player objects (player joined)
		while (players.Count < framePlayers.Count)
		{
			players.Add(Instantiate(playerPrefab, playerParent).GetComponent<MinimapPlayer>());
		}

		// remove extra player objects (player left)
		while (players.Count > framePlayers.Count)
		{
			Destroy(players.Last().gameObject);
			players.RemoveAt(players.Count - 1);
		}

		// set player data
		int offset = 0;
		for (int t = 0; t < 2; t++)
		{
			if (f.teams.Count <= t) break;
			Team team = f.teams[t];
			if (team == null) break;
			for (int p = 0; p < team.players.Count; p++)
			{
				if (team.players[p] == null)
				{
					Debug.LogError("Null player");
					break;
				}

				players[p + offset].text.text = team.players[p].number.ToString("00");
				players[p + offset].bg.color = t == 0 ? blueColor : orangeColor;
				Vector3 pos = team.players[p].head.Position;
				players[p + offset].rect.anchoredPosition = new Vector2(-pos.x, -pos.z) * playerPositionScale;
				players[p + offset].button.onClick.RemoveAllListeners();
				Player localPlayer = team.players[p];
				players[p + offset].button.onClick.AddListener(() => { DemoStart.instance.camController.FocusPlayer(localPlayer); });
			}

			offset += team.players.Count;
		}
	}
}