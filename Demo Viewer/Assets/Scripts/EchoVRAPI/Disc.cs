using System.Collections.Generic;
using UnityEngine;

namespace EchoVRAPI
{
	
	/// <summary>
	/// Object describing the disc at the given instant. 
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
			t = Math2.Clamp01(t);

			return new Disc()
			{
				position = Vector3.Lerp(from.position.ToVector3(), to.position.ToVector3(), t).ToFloatList(),
				velocity = Vector3.Lerp(from.velocity.ToVector3(), to.velocity.ToVector3(), t).ToFloatList(),
				bounce_count = from.bounce_count
			};
		}
	}

}