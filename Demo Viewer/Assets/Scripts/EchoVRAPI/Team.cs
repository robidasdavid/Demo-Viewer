using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace EchoVRAPI
{
	/// <summary>
	/// Object Containing basic team information and team stats
	/// </summary>
	public class Team
	{
		/// <summary>
		/// Enum declared for our own use.
		/// </summary>
		public enum TeamColor : byte
		{
			blue,
			orange,
			spectator
		}


		public List<Player> players { get; set; } = new List<Player>();

		/// <summary>
		/// Team name
		/// </summary>
		public string team { get; set; }

		public bool possession { get; set; }
		public Stats stats { get; set; }

		/// <summary>
		/// Not in the API, but add as soon as this frame is deserialized
		/// </summary>
		[JsonIgnore]
		public TeamColor color { get; set; }

		[JsonIgnore]
		public List<string> player_names
		{
			get { return players.Select(p => p.name).ToList(); }
		}

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
			if (from == null) return to;
			if (to == null) return from;

			t = Math2.Clamp01(t);

			Team newTeam = new Team()
			{
				team = from.team,
				possession = from.possession,
				stats = from.stats
			};

			// TODO make sure the players are in the same order. This should only be a problem when players join/leave
			int numPlayers = Math.Max(from.players.Count, to.players.Count);

			newTeam.players = new List<Player>();

			for (int i = 0; i < numPlayers; i++)
			{
				if (from.players.Count <= i &&
				    to.players.Count > i)
				{
					newTeam.players.Add(to.players[i]);
				}
				else if (to.players.Count <= i &&
				         from.players.Count > i)
				{
					newTeam.players.Add(from.players[i]);
				}
				else if (from.players.Count > i &&
				         to.players.Count > i)
				{
					// actually lerp the team
					newTeam.players.Add(Player.Lerp(from.players[i], to.players[i], t));
				}
			}

			return newTeam;
		}

		/// <summary>
		/// Creates a completely empty team, but initializes arrays and stuff to avoid null checking
		/// </summary>
		/// <returns>A Team object</returns>
		public static Team CreateEmpty(TeamColor teamColor = TeamColor.spectator)
		{
			return new Team
			{
				color = teamColor,
				players = new List<Player>(),
				stats = new Stats()
			};
		}
	}
	
	

}
