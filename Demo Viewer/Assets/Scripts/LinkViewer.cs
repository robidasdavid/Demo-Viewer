using System.Collections;
using System.Collections.Generic;
using EchoVRAPI;
using UnityEngine;

public class LinkViewer : MonoBehaviour
{
	public bool linkTeams;
	public bool linkClosest;
	public float linkDistance = 15;
	private LineRenderer blueTeamLine;
	private LineRenderer orangeTeamLine;
	private List<LineRenderer> linePool = new List<LineRenderer>();
	public Material lineMaterial;
	public float lineWidth = .03f;
	public Color blueColor;
	public Color orangeColor;


	// Update is called once per frame
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.L))
		{
			ToggleDistanceLinks();
		}
		if (Input.GetKeyDown(KeyCode.K))
		{
			ToggleTeamLinks();
		}
		
		Frame frame = DemoStart.instance.playhead.GetFrame();
		if (frame == null) return;

		int poolIndex = 0;

		if (linkTeams)
		{
			for (int i = 0; i < 2; i++)
			{
				foreach (Player p1 in frame.teams[i].players)
				{
					foreach (Player p2 in frame.teams[i].players)
					{
						if (p1.playerid != p2.playerid)
						{
							LineRenderer line = GetFromPool(poolIndex++);
							line.SetPositions(new[]
							{
								p1.head.Position,
								p2.head.Position,
							});
							line.startColor = i == 0 ? blueColor : orangeColor;
							line.endColor = i == 0 ? blueColor : orangeColor;
							line.material = lineMaterial;
							line.enabled = true;
							line.widthMultiplier = lineWidth;
						}
					}
				}
			}
		}

		if (linkClosest)
		{
			foreach (Player p1 in frame.GetAllPlayers())
			{
				foreach (Player p2 in frame.GetAllPlayers())
				{
					if (p1.playerid != p2.playerid && Vector3.Distance(p1.head.Position, p2.head.Position) < linkDistance)
					{
						LineRenderer line = GetFromPool(poolIndex++);

						if (line == null)
						{
							return;
						}

						line.SetPositions(new[]
						{
							p1.head.Position,
							p2.head.Position,
						});
						line.material = lineMaterial;
						line.startColor = p1.team_color == Team.TeamColor.blue ? blueColor : orangeColor;
						line.endColor = p2.team_color == Team.TeamColor.blue ? blueColor : orangeColor;
						line.enabled = true;
						line.widthMultiplier = lineWidth;
					}
				}
			}
		}

		// set all the rest to be invisible
		for (; poolIndex < linePool.Count; poolIndex++)
		{
			linePool[poolIndex].enabled = false;
		}
	}

	public void ToggleTeamLinks()
	{
		linkTeams = !linkTeams;
	}

	public void ToggleDistanceLinks()
	{
		linkClosest = !linkClosest;
	}

	private LineRenderer GetFromPool(int poolIndex)
	{
		if (linePool.Count <= poolIndex)
		{
			GameObject obj = new GameObject("LineRenderer");
			obj.transform.SetParent(transform);
			LineRenderer line = obj.AddComponent<LineRenderer>();
			line.positionCount = 2;
			linePool.Add(line);
		}

		return linePool[poolIndex];
	}
}