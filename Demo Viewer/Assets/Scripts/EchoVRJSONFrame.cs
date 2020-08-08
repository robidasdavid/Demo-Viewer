using System;
using UnityEngine;

[Serializable]
public class Game
{
	public bool isNewstyle;
	public float caprate;
	public long nframes;
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
	public Disc disc;
	public double frameTimeOffset;

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


}
[Serializable]
public class Disc
{
	public float[] position;
	public float[] velocity;
	public int bounce_count;
}
[Serializable]
public class Team
{
	public Player[] players;
	public string team;
	public bool possession;
	public Stats stats;

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
}

static class FloatArrayExtension
{
	public static Vector3 ToVector3(this float[] array)
	{
		return new Vector3(array[2], array[1], array[0]);
	}
}