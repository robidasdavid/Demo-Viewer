namespace EchoVRAPI
{
	/// <summary>
	/// Detailed info about the last throw
	/// </summary>
	public class LastThrow
	{
		public float arm_speed;
		public float rot_per_sec;
		public float pot_speed_from_rot;
		public float total_speed;
		public float speed_from_arm;
		public float speed_from_wrist;
		public float speed_from_movement;
		public float off_axis_spin_deg;
		public float wrist_align_to_throw_deg;
		public float throw_align_to_movement_deg;
		public float off_axis_penalty;
		public float wrist_throw_penalty;
		public float throw_move_penalty;


		#region Equality Comparison

		protected bool Equals(LastThrow other)
		{
			return arm_speed.Equals(other.arm_speed) &&
			       rot_per_sec.Equals(other.rot_per_sec) &&
			       pot_speed_from_rot.Equals(other.pot_speed_from_rot) &&
			       total_speed.Equals(other.total_speed) &&
			       speed_from_arm.Equals(other.speed_from_arm) &&
			       speed_from_wrist.Equals(other.speed_from_wrist) &&
			       speed_from_movement.Equals(other.speed_from_movement) &&
			       off_axis_spin_deg.Equals(other.off_axis_spin_deg) &&
			       wrist_align_to_throw_deg.Equals(other.wrist_align_to_throw_deg) &&
			       throw_align_to_movement_deg.Equals(other.throw_align_to_movement_deg) &&
			       off_axis_penalty.Equals(other.off_axis_penalty) &&
			       wrist_throw_penalty.Equals(other.wrist_throw_penalty) &&
			       throw_move_penalty.Equals(other.throw_move_penalty);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj.GetType() == GetType() && Equals((LastThrow)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = arm_speed.GetHashCode();
				hashCode = (hashCode * 397) ^ rot_per_sec.GetHashCode();
				hashCode = (hashCode * 397) ^ pot_speed_from_rot.GetHashCode();
				hashCode = (hashCode * 397) ^ total_speed.GetHashCode();
				hashCode = (hashCode * 397) ^ speed_from_arm.GetHashCode();
				hashCode = (hashCode * 397) ^ speed_from_wrist.GetHashCode();
				hashCode = (hashCode * 397) ^ speed_from_movement.GetHashCode();
				hashCode = (hashCode * 397) ^ off_axis_spin_deg.GetHashCode();
				hashCode = (hashCode * 397) ^ wrist_align_to_throw_deg.GetHashCode();
				hashCode = (hashCode * 397) ^ throw_align_to_movement_deg.GetHashCode();
				hashCode = (hashCode * 397) ^ off_axis_penalty.GetHashCode();
				hashCode = (hashCode * 397) ^ wrist_throw_penalty.GetHashCode();
				hashCode = (hashCode * 397) ^ throw_move_penalty.GetHashCode();
				return hashCode;
			}
		}

		#endregion
	}
}