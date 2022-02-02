using System.Text;
using EchoVRAPI;
using TMPro;
using UnityEngine;
using Transform = UnityEngine.Transform;

public class ScoreboardController : MonoBehaviour
{
	public enum StatsStyle
	{
		Default,
		Full
	}

	public TMP_Text[] scoreboards;
	public StatsStyle statsStyle;

	// Update is called once per frame
	private void Update()
	{
		Frame f = DemoStart.instance.playhead.GetFrame();
		if (f == null) return;

		for (int t = 0; t < 2; t++)
		{
			StringBuilder sb = new StringBuilder();
			f.teams[t].players.ForEach(p =>
			{
				switch (statsStyle)
				{
					case StatsStyle.Default:
					{
						sb.Append(new string(' ', 2));
						sb.Append(p.name);
						sb.Append(new string(' ', 21 - p.name.Length));

						// if Combat
						if (p.stats == null) return;

						sb.Append(p.stats.points);
						sb.Append(new string(' ', 7 - p.stats.points.ToString().Length));

						sb.Append(p.stats.assists);
						sb.Append(new string(' ', 7 - p.stats.assists.ToString().Length));

						sb.Append(p.stats.saves);
						sb.Append(new string(' ', 7 - p.stats.saves.ToString().Length));

						sb.Append(p.stats.stuns);
						sb.Append(new string(' ', 7 - p.stats.stuns.ToString().Length));

						sb.Append(p.ping);
						sb.AppendLine();
						break;
					}
					case StatsStyle.Full:
					{
						sb.Append(new string(' ', 1));
						sb.Append(p.name);
						sb.Append(new string(' ', 21 - p.name.Length));

						// if Combat
						if (p.stats == null) return;

						sb.Append(p.stats.points);
						sb.Append(new string(' ', 4 - p.stats.points.ToString().Length));

						sb.Append(p.stats.assists);
						sb.Append(new string(' ', 4 - p.stats.assists.ToString().Length));

						sb.Append(p.stats.saves);
						sb.Append(new string(' ', 4 - p.stats.saves.ToString().Length));

						sb.Append(p.stats.stuns);
						sb.Append(new string(' ', 4 - p.stats.stuns.ToString().Length));

						// sb.Append(p.stats.passes);
						// sb.Append(new string(' ', 4 - p.stats.passes.ToString().Length));
						//
						// sb.Append(p.stats.catches);
						// sb.Append(new string(' ', 4 - p.stats.catches.ToString().Length));

						sb.Append(p.stats.steals);
						sb.Append(new string(' ', 4 - p.stats.steals.ToString().Length));

						// sb.Append(p.stats.blocks);
						// sb.Append(new string(' ', 4 - p.stats.blocks.ToString().Length));
						//
						// sb.Append(p.stats.interceptions);
						// sb.Append(new string(' ', 4 - p.stats.interceptions.ToString().Length));

						sb.Append(p.stats.shots_taken);
						sb.Append(new string(' ', 4 - p.stats.shots_taken.ToString().Length));

						sb.Append(p.stats.possession_time.ToString("N0"));
						sb.Append(new string(' ', Mathf.Clamp(5 - p.stats.possession_time.ToString("N0").Length, 0, 100)));

						sb.Append(p.ping);
						sb.AppendLine();
						break;
					}
				}
			});

			scoreboards[t].text = sb.ToString();
		}
	}
}