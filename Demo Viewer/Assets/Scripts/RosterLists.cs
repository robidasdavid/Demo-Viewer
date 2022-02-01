using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EchoVRAPI;
using Transform = UnityEngine.Transform;

public class RosterLists : MonoBehaviour
{
	[Header("Order is Blue-Orange")] public GameObject[] playerPrefabs;

	public Transform[] parents;

	private readonly List<RosterRow>[] players = new List<RosterRow>[2];

	// Update is called once per frame
	private void Update()
	{
		if (DemoStart.instance.playhead == null) return;
		Frame f = DemoStart.instance.playhead.GetFrame();
		if (f == null) return;

		for (int t = 0; t < 2; t++)
		{
			players[t] ??= new List<RosterRow>();
			
			while (players[t].Count < f.teams[t].players.Count)
			{
				players[t].Add(Instantiate(playerPrefabs[t], parents[t]).GetComponent<RosterRow>());
			}

			while (players[t].Count > f.teams[t].players.Count)
			{
				RosterRow last = players[t].Last();
				players[t].RemoveAt(players[t].Count - 1);
				Destroy(last.gameObject);
			}

			for (int i = 0; i < f.teams[t].players.Count; i++)
			{
				Player p = f.teams[t].players[i];


				players[t][i].playerName.text = p.name;
				players[t][i].playerNumber.text = p.number.ToString("00");
				players[t][i].button.onClick.RemoveAllListeners();
				players[t][i].button.onClick.AddListener(() =>
				{
					DemoStart.instance.camController.FocusPlayer(p);
				});
			}
		}
	}
}