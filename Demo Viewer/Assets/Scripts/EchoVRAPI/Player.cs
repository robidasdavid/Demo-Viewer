using System.Collections.Generic;
using Newtonsoft.Json;
#if UNITY
using UnityEngine;
#else
using System.Numerics;
#endif

namespace EchoVRAPI
{
	/// <summary>
	/// Object Containing basic player information and player stats 
	/// </summary>
	public class Player
	{
		[JsonIgnore] public Team.TeamColor team_color;

		/// <summary>
		/// Right hand position and rotation
		/// </summary>
		public Transform rhand { get; set; }

		/// <summary>
		/// Index of the player in the match, so [0-6] for 3v3 & [0-7] for 4v4
		/// </summary>
		public int playerid { get; set; }

		/// <summary>
		/// Display Name
		/// </summary>
		public string name { get; set; }

		/// <summary>
		/// Application-scoped Oculus userid
		/// </summary>
		public long userid { get; set; }

		/// <summary>
		/// Object describing a player's aggregated statistics throughout the match.
		/// </summary>
		public Stats stats { get; set; }

		public int number { get; set; }
		public int level { get; set; }

		/// <summary>
		/// Boolean of player's stunned status.
		/// </summary>
		public bool stunned { get; set; }

		public int ping { get; set; }
		public float packetlossratio { get; set; }
		public string holding_left { get; set; }
		public string holding_right { get; set; }

		/// <summary>
		/// Boolean of the player's invulnerability after being stunned.
		/// </summary>
		public bool invulnerable { get; set; }

		public Transform head;

		/// <summary>
		/// Boolean determining whether or not this player has or had possession of the disc.
		/// possession will remain true until someone else grabs the disc or for 7 seconds (maybe?)
		/// </summary>
		public bool possession { get; set; }

		public Transform body;

		/// <summary>
		/// Left hand position and rotation
		/// </summary>
		public Transform lhand { get; set; }

		public bool blocking { get; set; }

		/// <summary>
		/// A 3 element list of floats representing the player's velocity.
		/// < X, Y, Z >
		/// </summary>
		public List<float> velocity { get; set; }

		/// <summary>
		/// This is not from the api, but set afterwards in the temporal processing step
		/// </summary>
		[JsonIgnore] public Vector3 playspacePosition =
#if UNITY
			Vector3.zero;
#else
			Vector3.Zero;
#endif
		[JsonIgnore] public float distanceGained = 0;
		[JsonIgnore] public Vector3 virtualPlayspacePosition =
#if UNITY
			Vector3.zero;
#else
			Vector3.Zero;
#endif


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
			t = Math2.Clamp01(t);

			return new Player()
			{
				name = from.name,
				playerid = from.playerid,
				userid = from.userid,
				stats = from.stats,
				number = from.number,
				level = from.level,
				possession = from.possession,
				head = Transform.Lerp(from.head, to.head, t),
				body = Transform.Lerp(from.body, to.body, t),
				lhand = Transform.Lerp(from.lhand, to.lhand, t),
				rhand = Transform.Lerp(from.rhand, to.rhand, t),
				invulnerable = from.invulnerable,
				stunned = from.stunned,
				velocity = Vector3.Lerp(from.velocity.ToVector3(), to.velocity.ToVector3(), t).ToFloatList(),
				blocking = from.blocking,
				team_color = from.team_color,
			};
		}
	}
}