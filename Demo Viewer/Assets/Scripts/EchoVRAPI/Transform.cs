using System;
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
	/// Object for position and rotation
	/// </summary>
	public class Transform
	{
		
		/// <summary>
		/// Don't get this value. Use Position property instead
		/// </summary>
		public List<float> pos { get; set; }

		/// <summary>
		/// Don't get this value. Use Position property instead
		/// </summary>
		public List<float> position { get; set; }

		public List<float> forward;
		public List<float> left;
		public List<float> up;
		
		
		[JsonIgnore]
		public Vector3 Position
		{
			get
			{
				if (pos != null) return pos.ToVector3();
				if (position != null) return position.ToVector3();
				throw new NullReferenceException("Neither pos nor position are set");
			}
			set
			{
				// only set the one with prior data
				if (pos != null) pos = value.ToFloatList();
				if (position != null) position = value.ToFloatList();
				else position = value.ToFloatList();
			}
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

		public static Transform operator +(Transform t1, Transform t2)
		{
			if (t2 == null) return t1;
			Transform ret = new Transform
			{
				Position = t1.Position + t2.Position,
				// ret.Rotation = Quaternion.Multiply(t1.Rotation, t2.Rotation);
				Rotation = t1.Rotation
			};
			return ret;
		}
		
		public static Transform operator -(Transform t1, Transform t2)
		{
			if (t2 == null) return t1;
			Transform ret = new Transform
			{
				Position = t1.Position - t2.Position,
				// ret.Rotation = Quaternion.Multiply(t1.Rotation, Quaternion.Inverse(t2.Rotation));
				Rotation = t1.Rotation
			};
			return ret;
		}

		/// <summary>
		/// ↔ Mixes the two states with a linear interpolation based on t
		/// For binary or int values, the "from" state is preferred.
		/// </summary>
		/// <param name="from">The start state</param>
		/// <param name="to">The next state</param>
		/// <param name="t">Weighting of the two states</param>
		/// <returns>A mix of the two states</returns>
		internal static Transform Lerp(Transform from, Transform to, float t)
		{
			t = Math2.Clamp01(t);

			Transform newTransform = new Transform()
			{
				pos = from.pos,
				position = from.position,
				left = from.left,
				forward = from.forward,
				up = from.up,
			};

			newTransform.Position = Vector3.Lerp(from.Position, to.Position, t);
			newTransform.Rotation = Quaternion.Slerp(from.Rotation, to.Rotation, t);

			return newTransform;
		}
	}

}
