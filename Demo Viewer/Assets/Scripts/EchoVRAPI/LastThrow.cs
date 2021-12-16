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
	}

}