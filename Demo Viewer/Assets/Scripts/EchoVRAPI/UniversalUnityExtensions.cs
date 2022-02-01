using System;
#if UNITY
using UnityEngine;
#else
using System.Numerics;
#endif

namespace EchoVRAPI
{
	public static class UniversalUnityExtensions
	{
		public static float UniversalLength(this Vector3 v)
		{
#if UNITY
			return v.magnitude;
#else
			return v.Length();
#endif
		}

		public static Vector3 UniversalVector3Zero()
		{
#if UNITY
			return Vector3.zero;
#else
			return Vector3.Zero;
#endif
		}

		public static Quaternion UniversalQuaternionIdentity()
		{
#if UNITY
			return Quaternion.identity;
#else
			return Quaternion.Identity;
#endif
		}

		/// <summary>
		///   <para>Returns the angle in degrees between two rotations</para>
		/// </summary>
		public static float QuaternionAngle(Quaternion a, Quaternion b)
		{
#if UNITY
			return Quaternion.Angle(a, b);
#else
			float num = Quaternion.Dot(a, b);
			return IsEqualUsingDot(num)
				? 0.0f
				: (float) (Math.Acos(Math.Min(Math.Abs(num), 1f)) * 2.0 * 57.2957801818848);
#endif
		}

		private static bool IsEqualUsingDot(float dot) => dot > 0.999998986721039;
	}
}