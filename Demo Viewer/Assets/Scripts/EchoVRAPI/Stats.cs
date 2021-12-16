namespace EchoVRAPI
{
	
	/// <summary>
	/// Object containing the player's stats in the match.
	/// </summary>
	public class Stats
	{
		public float possession_time;
		public int points;
		public int saves;
		public int goals;
		public int stuns;
		public int passes;
		public int catches;
		public int steals;
		public int blocks;
		public int interceptions;
		public int assists;
		public int shots_taken;

		public static Stats operator +(Stats a, Stats b)
		{
			Stats stats = new Stats
			{
				possession_time = a.possession_time + b.possession_time,
				points = a.points + b.points,
				passes = a.passes + b.passes,
				catches = a.catches + b.catches,
				steals = a.steals + b.steals,
				stuns = a.stuns + b.stuns,
				blocks = a.blocks + b.blocks,
				interceptions = a.interceptions + b.interceptions,
				assists = a.assists + b.assists,
				saves = a.saves + b.saves,
				goals = a.goals + b.goals,
				shots_taken = a.shots_taken + b.shots_taken
			};
			return stats;
		}

		public static Stats operator -(Stats a, Stats b)
		{
			Stats stats = new Stats
			{
				possession_time = a.possession_time - b.possession_time,
				points = a.points - b.points,
				passes = a.passes - b.passes,
				catches = a.catches - b.catches,
				steals = a.steals - b.steals,
				stuns = a.stuns - b.stuns,
				blocks = a.blocks - b.blocks,
				interceptions = a.interceptions - b.interceptions,
				assists = a.assists - b.assists,
				saves = a.saves - b.saves,
				goals = a.goals - b.goals,
				shots_taken = a.shots_taken - b.shots_taken
			};
			return stats;
		}
	}

}