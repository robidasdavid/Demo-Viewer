using System;

namespace EchoVRAPI
{
		
	/// <summary>
	/// Object Containing basic relavant information on who scored last.
	/// 🥅 🥅 🥅 🥅 
	/// </summary>
	public class LastScore
	{
		public float disc_speed { get; set; }
		public string team { get; set; }
		public string goal_type { get; set; }
		public int point_amount { get; set; }
		public float distance_thrown { get; set; }

		/// <summary>
		/// Name of person who scored last.
		/// </summary>
		public string person_scored { get; set; }

		/// <summary>
		/// Name of person who assisted in the resulting goal.
		/// </summary>
		public string assist_scored { get; set; }

		public override bool Equals(object o)
		{
			LastScore s = (LastScore) o;
			return
				//Math.Abs(s.disc_speed - disc_speed) < .01f &&
				s.team == team &&
				s.goal_type == goal_type &&
				s.point_amount == point_amount &&
				Math.Abs(s.distance_thrown - distance_thrown) < .01f &&
				s.person_scored == person_scored &&
				s.assist_scored == assist_scored;
		}

		public override int GetHashCode()
		{
			int hash = 17;
			hash = hash * 23 + disc_speed.GetHashCode();
			hash = hash * 29 + goal_type.GetHashCode();
			hash = hash * 31 + point_amount.GetHashCode();
			hash = hash * 37 + distance_thrown.GetHashCode();
			hash = hash * 41 + person_scored.GetHashCode();
			hash = hash * 43 + assist_scored.GetHashCode();
			return hash;
		}
	}

}