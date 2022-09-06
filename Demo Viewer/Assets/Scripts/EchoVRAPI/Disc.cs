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
	/// Object describing the disc at the given instant.
	/// 💿 💿 💿 💿 
	/// </summary>
	public class Disc
	{
		/// <summary>
		/// A 3 element list of floats representing the disc's position relative to the center of the map.
		/// < X, Y, Z >
		/// </summary>
		public List<float> position { get; set; }

		public List<float> forward { get; set; }
		public List<float> left { get; set; }
		public List<float> up { get; set; }

		/// <summary>
		/// A 3 element list of floats representing the disc's velocity.
		/// < X, Y, Z >
		/// </summary>
		public List<float> velocity { get; set; }

		public int bounce_count { get; set; }

		[JsonIgnore]
		public Vector3 Position
		{
#if UNITY
			get => position?.ToVector3() ?? Vector3.zero;
#else
			get => position?.ToVector3() ?? Vector3.Zero;
#endif
			set => position = value.ToFloatList();
		}
		
		[JsonIgnore]
		internal Quaternion? rot;

		[JsonIgnore]
		public Quaternion Rotation
		{
			get
			{
				if (rot != null) return (Quaternion) rot;

				rot = Math2.QuaternionLookRotation(forward.ToVector3(), up.ToVector3());
				return (Quaternion) rot;
			}
			set
			{
				rot = value;
				forward = value.Forward().ToFloatList();
				up = value.Up().ToFloatList();
				left = value.Left().ToFloatList();
			}
		}

		/// <summary>
		/// ↔ Mixes the two states with a linear interpolation based on t
		/// For binary or int values, the "from" state is preferred.
		/// </summary>
		/// <param name="from">The start state</param>
		/// <param name="to">The next state</param>
		/// <param name="t">Weighting of the two states</param>
		/// <returns>A mix of the two frames</returns>
		internal static Disc Lerp(Disc from, Disc to, float t)
		{
			if (from == null) return to;
			if (to == null) return from;
			
			t = Math2.Clamp01(t);

			return new Disc()
			{
				position = Vector3.Lerp(from.position.ToVector3(), to.position.ToVector3(), t).ToFloatList(),
				velocity = Vector3.Lerp(from.velocity.ToVector3(), to.velocity.ToVector3(), t).ToFloatList(),
				rot = Quaternion.Slerp(from.Rotation, to.Rotation, t),
				bounce_count = from.bounce_count
			};
		}
		
		

		/// <summary>
		/// Creates a completely empty disc, but initializes arrays and stuff to avoid null checking
		/// </summary>
		/// <returns>A Disc object</returns>
		public static Disc CreateEmpty()
		{
			return new Disc
			{
				position = new List<float> { 0, 0, 0 },
				left = new List<float> { 0, 0, 0 },
				forward = new List<float> { 0, 0, 0 },
				up = new List<float> { 0, 0, 0 },
				velocity = new List<float> { 0, 0, 0 },
			};
		}
	}

}