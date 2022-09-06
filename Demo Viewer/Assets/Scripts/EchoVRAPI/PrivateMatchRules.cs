using System;
using Newtonsoft.Json;

namespace EchoVRAPI
{
	/// <summary>
	/// Settings on the podium in Echo Arena
	/// </summary>
	[Serializable]
	public class PrivateMatchRules
	{
		public enum DiscLocation
		{
			blue,
			mid,
			orange,
		}

		public enum RoundsPlayed
		{
			all,
			best_of
		}

		public enum Overtime
		{
			round_end,
			match_end,
			none
		}

		// Page 1
		[JsonProperty("MINUTES")]
		public int minutes;
		[JsonProperty("SECONDS")]
		public int seconds;
		[JsonProperty("BLUE SCORE")]
		public int blue_score;
		[JsonProperty("ORANGE SCORE")]
		public int orange_score;

		// Page 2
		[JsonProperty("DISC LOCATION")]
		public DiscLocation disc_location;
		[JsonProperty("GOAL STOPS TIME")]
		public bool goal_stops_time;
		[JsonProperty("RESPAWN TIME")]
		public int respawn_time;
		[JsonProperty("CATAPULT TIME")]
		public int catapult_time;

		// page 3
		[JsonProperty("ROUND COUNT")]
		public int round_count;
		[JsonProperty("ROUNDS PLAYED")]
		public RoundsPlayed rounds_played;
		[JsonProperty("ROUND WAIT TIME")]
		public int round_wait_time;
		[JsonProperty("CARRY POINTS OVER")]
		public bool carry_points_over;

		// page 4
		[JsonProperty("BLUE ROUNDS WON")]
		public int blue_rounds_won;
		[JsonProperty("ORANGE ROUNDS WON")]
		public int orange_rounds_won;
		[JsonProperty("OVERTIME")]
		public Overtime overtime;
		[JsonProperty("STANDARD CHASSIS")]
		public bool standard_chassis;

		// page 5
		[JsonProperty("MERCY ENABLED")]
		public bool mercy_enabled;
		[JsonProperty("MERCY SCORE DIFF")]
		public int mercy_score_diff;
		[JsonProperty("TEAM ONLY VOICE")]
		public bool team_only_voice;
		[JsonProperty("DISC CURVE")]
		public bool disc_curve;

		// page 6
		[JsonProperty("SELF-GOALING")]
		public bool self_goaling; // this has a dash
		[JsonProperty("GOALIE PING ADV")]
		public bool goalie_ping_adv;
		
		public int err_code;

		public PrivateMatchRules()
		{
		}

		/// <summary>
		/// Copy constructor
		/// </summary>
		public PrivateMatchRules(PrivateMatchRules rules)
		{
			minutes = rules.minutes;
			seconds = rules.seconds;
			blue_score = rules.blue_score;
			orange_score = rules.orange_score;
			disc_location = rules.disc_location;
			goal_stops_time = rules.goal_stops_time;
			respawn_time = rules.respawn_time;
			catapult_time = rules.catapult_time;
			round_count = rules.round_count;
			rounds_played = rules.rounds_played;
			round_wait_time = rules.round_wait_time;
			carry_points_over = rules.carry_points_over;
			blue_rounds_won = rules.blue_rounds_won;
			orange_rounds_won = rules.orange_rounds_won;
			overtime = rules.overtime;
			standard_chassis = rules.standard_chassis;
			mercy_enabled = rules.mercy_enabled;
			mercy_score_diff = rules.mercy_score_diff;
			team_only_voice = rules.team_only_voice;
			disc_curve = rules.disc_curve;
			self_goaling = rules.self_goaling;
			goalie_ping_adv = rules.goalie_ping_adv;
		}

		public override bool Equals(object obj)
		{
			return obj is PrivateMatchRules rules && Equals(rules);
		}

		protected bool Equals(PrivateMatchRules other)
		{
			return minutes == other.minutes &&
			       seconds == other.seconds &&
			       blue_score == other.blue_score &&
			       orange_score == other.orange_score &&
			       disc_location == other.disc_location &&
			       goal_stops_time == other.goal_stops_time &&
			       respawn_time == other.respawn_time &&
			       catapult_time == other.catapult_time &&
			       round_count == other.round_count &&
			       rounds_played == other.rounds_played &&
			       round_wait_time == other.round_wait_time &&
			       carry_points_over == other.carry_points_over &&
			       blue_rounds_won == other.blue_rounds_won &&
			       orange_rounds_won == other.orange_rounds_won &&
			       overtime == other.overtime &&
			       standard_chassis == other.standard_chassis &&
			       mercy_enabled == other.mercy_enabled &&
			       mercy_score_diff == other.mercy_score_diff &&
			       team_only_voice == other.team_only_voice &&
			       disc_curve == other.disc_curve &&
			       self_goaling == other.self_goaling &&
			       goalie_ping_adv == other.goalie_ping_adv;
		}

		public override int GetHashCode()
		{
			int hashCode = minutes;
			hashCode = (hashCode * 397) ^ seconds;
			hashCode = (hashCode * 397) ^ blue_score;
			hashCode = (hashCode * 397) ^ orange_score;
			hashCode = (hashCode * 397) ^ (int)disc_location;
			hashCode = (hashCode * 397) ^ goal_stops_time.GetHashCode();
			hashCode = (hashCode * 397) ^ respawn_time;
			hashCode = (hashCode * 397) ^ catapult_time;
			hashCode = (hashCode * 397) ^ round_count;
			hashCode = (hashCode * 397) ^ (int)rounds_played;
			hashCode = (hashCode * 397) ^ round_wait_time;
			hashCode = (hashCode * 397) ^ carry_points_over.GetHashCode();
			hashCode = (hashCode * 397) ^ blue_rounds_won;
			hashCode = (hashCode * 397) ^ orange_rounds_won;
			hashCode = (hashCode * 397) ^ (int)overtime;
			hashCode = (hashCode * 397) ^ standard_chassis.GetHashCode();
			hashCode = (hashCode * 397) ^ mercy_enabled.GetHashCode();
			hashCode = (hashCode * 397) ^ mercy_score_diff;
			hashCode = (hashCode * 397) ^ team_only_voice.GetHashCode();
			hashCode = (hashCode * 397) ^ disc_curve.GetHashCode();
			hashCode = (hashCode * 397) ^ self_goaling.GetHashCode();
			hashCode = (hashCode * 397) ^ goalie_ping_adv.GetHashCode();
			return hashCode;
		}
	}
}