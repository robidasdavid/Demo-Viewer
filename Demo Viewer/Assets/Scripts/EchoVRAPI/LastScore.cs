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


		#region Equality comparison

		protected bool Equals(LastScore other)
		{
			return disc_speed.Equals(other.disc_speed) &&
			       team == other.team &&
			       goal_type == other.goal_type &&
			       point_amount == other.point_amount &&
			       distance_thrown.Equals(other.distance_thrown) &&
			       person_scored == other.person_scored &&
			       assist_scored == other.assist_scored;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((LastScore)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = disc_speed.GetHashCode();
				hashCode = (hashCode * 397) ^ (team != null ? team.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (goal_type != null ? goal_type.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ point_amount;
				hashCode = (hashCode * 397) ^ distance_thrown.GetHashCode();
				hashCode = (hashCode * 397) ^ (person_scored != null ? person_scored.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (assist_scored != null ? assist_scored.GetHashCode() : 0);
				return hashCode;
			}
		}

		#endregion
	}
}