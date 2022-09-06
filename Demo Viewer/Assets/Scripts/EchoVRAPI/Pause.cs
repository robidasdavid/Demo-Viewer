namespace EchoVRAPI
{
	/// <summary>
	/// ⏸
	/// </summary>
	public class Pause
	{
		public string paused_state;
		public string unpaused_team;
		public string paused_requested_team;
		public float unpaused_timer;
		public float paused_timer;

		#region Equality comparison

		protected bool Equals(Pause other)
		{
			return paused_state == other.paused_state &&
			       unpaused_team == other.unpaused_team &&
			       paused_requested_team == other.paused_requested_team &&
			       unpaused_timer.Equals(other.unpaused_timer) &&
			       paused_timer.Equals(other.paused_timer);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((Pause)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = (paused_state != null ? paused_state.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (unpaused_team != null ? unpaused_team.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (paused_requested_team != null ? paused_requested_team.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ unpaused_timer.GetHashCode();
				hashCode = (hashCode * 397) ^ paused_timer.GetHashCode();
				return hashCode;
			}
		}

		#endregion
	}
}