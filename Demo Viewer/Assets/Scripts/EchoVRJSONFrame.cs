using System;
using System.Text;
using UnityEngine;

[Serializable]
public class Game
{
	public bool isNewstyle;
	public int nframes;
	public Frame[] frames;
}
[Serializable]
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

	public override string ToString()
	{
		StringBuilder s = new StringBuilder();
		s.Append("Possession Time: ");
		s.Append(possession_time);
		s.Append("\nPoints: ");
		s.Append(points);
		s.Append("\nGoals: ");
		s.Append(goals);
		s.Append("\nSaves: ");
		s.Append(saves);
		s.Append("\nStuns: ");
		s.Append(stuns);
		s.Append("\nAssists: ");
		s.Append(assists);
		s.Append("\nShots Taken: ");
		s.Append(shots_taken);
		return s.ToString();
	}
}
[Serializable]
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
[Serializable]
public class Frame
{
	/// <summary>
	/// Time of this frame as saved in the replay file
	/// </summary>
	public DateTime frameTime;

	public Disc disc;
	public string sessionid;
	public int orange_points;
	public bool private_match;
	public string client_name;
	public string game_clock_display;
	public string game_status;
	public float game_clock;
	public string match_type;

	public Team[] teams;

	public string map_name;
	public int[] possession;
	public bool tournament_match;
	public int blue_points;

	public Last_Score last_score;

	/// <summary>
	/// ↔ Mixes the two frames with a linear interpolation based on t
	/// For binary or int values, the "from" frame is preferred.
	/// </summary>
	/// <param name="from">The start frame</param>
	/// <param name="to">The next frame</param>
	/// <param name="t">The DateTime of the playhead</param>
	/// <returns>A mix of the two frames</returns>
	internal static Frame Lerp(Frame from, Frame to, DateTime t)
	{
		if (from.frameTime == to.frameTime)
		{
			return from;
		}
		else if (from.frameTime > to.frameTime)
		{
			Debug.LogError("From frame is after To frame");
			return null;
		}
		else if (from.frameTime > t)
		{
			return from;
		}
		else if (to.frameTime < t)
		{
			return to;
		}
		else
		{
			// the ratio between the frames
			float lerpValue = (float)((t - from.frameTime).TotalSeconds / (to.frameTime - from.frameTime).TotalSeconds);

			Frame newFrame = new Frame()
			{
				frameTime = t,


				disc = Disc.Lerp(from.disc, to.disc, lerpValue),
				sessionid = from.sessionid,
				orange_points = from.orange_points,
				private_match = from.private_match,
				client_name = from.client_name,
				game_clock_display = from.game_clock_display, // TODO this could be interpolated
				game_status = from.game_status,
				game_clock = Mathf.Lerp(from.game_clock, to.game_clock, lerpValue),
				match_type = from.match_type,

				map_name = from.map_name,
				possession = from.possession,
				tournament_match = from.tournament_match,
				blue_points = from.blue_points,
				last_score = from.last_score

			};

			int numTeams = Math.Max(from.teams.Length, to.teams.Length);

			newFrame.teams = new Team[numTeams];

			for (int i = 0; i < numTeams; i++)
			{
				if (from.teams.Length <= i &&
					to.teams.Length > i)
				{
					newFrame.teams[i] = to.teams[i];
				}
				else if (to.teams.Length <= i &&
				  from.teams.Length > i)
				{
					newFrame.teams[i] = from.teams[i];
				}
				else if (from.teams.Length > i &&
				  to.teams.Length > i)
				{
					// actually lerp the team
					newFrame.teams[i] = Team.Lerp(from.teams[i], to.teams[i], lerpValue);
				}
			}

			return newFrame;
		}
	}
}

[Serializable]
public class Disc
{
	public float[] position;
	public float[] velocity;
	public int bounce_count;

	/// <summary>
	/// ↔ Mixes the two states with a linear interpolation based on t
	/// For binary or int values, the "from" state is preferred.
	/// </summary>
	/// <param name="from">The start state</param>
	/// <param name="to">The next state</param>
	/// <param name="t">Weighting of the two states</param>
	/// <returns>A mix of the two frames</returns>
	internal static Disc Lerp(Disc from, Disc to, float t)
	{
		t = Mathf.Clamp01(t);

		return new Disc()
		{
			position = Vector3.Lerp(from.position.ToVector3(), to.position.ToVector3(), t).ToFloatArray(),
			velocity = Vector3.Lerp(from.velocity.ToVector3(), to.velocity.ToVector3(), t).ToFloatArray(),
			bounce_count = from.bounce_count
		};
	}
}

[Serializable]
public class Team
{
	public Player[] players;
	public string team;
	public bool possession;
	public Stats stats;

	/// <summary>
	/// ↔ Mixes the two states with a linear interpolation based on t
	/// For binary or int values, the "from" state is preferred.
	/// </summary>
	/// <param name="from">The start state</param>
	/// <param name="to">The next state</param>
	/// <param name="t">Weighting of the two states</param>
	/// <returns>A mix of the two frames</returns>
	internal static Team Lerp(Team from, Team to, float t)
	{
		t = Mathf.Clamp01(t);

		Team newTeam = new Team()
		{
			team = from.team,
			possession = from.possession,
			stats = from.stats

		};

		if (from.players == null)
		{
			newTeam.players = null;
		}
		else if (to.players == null)
		{
			newTeam.players = from.players;
		}
		else
		{
			// TODO make sure the players are in the same order. This should only be a problem when players join/leave
			int numPlayers = Math.Max(from.players.Length, to.players.Length);

			newTeam.players = new Player[numPlayers];

			for (int i = 0; i < numPlayers; i++)
			{
				if (from.players.Length <= i &&
					to.players.Length > i)
				{
					newTeam.players[i] = to.players[i];
				}
				else if (to.players.Length <= i &&
				  from.players.Length > i)
				{
					newTeam.players[i] = from.players[i];
				}
				else if (from.players.Length > i &&
				  to.players.Length > i)
				{
					// actually lerp the team
					newTeam.players[i] = Player.Lerp(from.players[i], to.players[i], t);
				}
			}
		}

		return newTeam;
	}

}
[Serializable]
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


	/// <summary>
	/// ↔ Mixes the two states with a linear interpolation based on t
	/// For binary or int values, the "from" state is preferred.
	/// </summary>
	/// <param name="from">The start state</param>
	/// <param name="to">The next state</param>
	/// <param name="t">Weighting of the two states</param>
	/// <returns>A mix of the two frames</returns>
	internal static Player Lerp(Player from, Player to, float t)
	{
		t = Mathf.Clamp01(t);

		return new Player()
		{
			name = from.name,
			rhand = Vector3.Lerp(from.rhand.ToVector3(), to.rhand.ToVector3(), t).ToFloatArray(),
			playerid = from.playerid,
			position = Vector3.Lerp(from.position.ToVector3(), to.position.ToVector3(), t).ToFloatArray(),
			lhand = Vector3.Lerp(from.lhand.ToVector3(), to.lhand.ToVector3(), t).ToFloatArray(),
			userid = from.userid,
			stats = from.stats,
			number = from.number,
			level = from.level,
			possession = from.possession,
			left = Vector3.Lerp(from.left.ToVector3(), to.left.ToVector3(), t).ToFloatArray(),
			invulnerable = from.invulnerable,
			up = Vector3.Lerp(from.up.ToVector3(), to.up.ToVector3(), t).ToFloatArray(),
			forward = Vector3.Lerp(from.forward.ToVector3(), to.forward.ToVector3(), t).ToFloatArray(),
			stunned = from.stunned,
			velocity = Vector3.Lerp(from.velocity.ToVector3(), to.velocity.ToVector3(), t).ToFloatArray(),
			blocking = from.blocking
		};
	}
}

static class FloatArrayExtension
{
	public static Vector3 ToVector3(this float[] array)
	{
		return new Vector3(array[2], array[1], array[0]);
	}
	public static Vector3 ToVector3Backwards(this float[] array)
	{
		return new Vector3(array[0], array[1], array[2]);
	}

	public static float[] ToFloatArray(this Vector3 vector3)
	{
		return new float[]
		{
			vector3.z,
			vector3.y,
			vector3.x
		};
	}
}