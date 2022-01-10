#if UNITY
using UnityEngine;
#else
using System.Numerics;
#endif
using System;
using System.Collections.Generic;
using System.Linq;

namespace EchoVRAPI
{

	/// <summary>
	/// 🦴🦴🦴🦴
	/// </summary>
	[Serializable]
	public class Bones
	{
		public BonePlayer[] user_bones;
		public int error_code;
		public string err_description;
		
		/// <summary>
		/// ↔ Mixes the two states with a linear interpolation based on t
		/// For binary or int values, the "from" state is preferred.
		/// </summary>
		/// <param name="from">The start state</param>
		/// <param name="to">The next state</param>
		/// <param name="t">Weighting of the two states</param>
		/// <returns>A mix of the two states</returns>
		internal static Bones Lerp(Bones from, Bones to, float t)
		{
			if (from == null) return to;
			if (to == null) return from;

			if (from.user_bones == null) return to;
			if (to.user_bones == null) return from;
			
			t = Math2.Clamp01(t);

			if (from.user_bones.Length != to.user_bones.Length) return t > 5 ? to : from;

			Bones newBones = new Bones
			{
				user_bones = new BonePlayer[@from.user_bones.Length]
			};

			for (int p = 0; p < from.user_bones.Length; p++)
			{
				// this does orientation lerping by lerping the individual floats in the quaternion. Not ideal
				newBones.user_bones[p] = new BonePlayer
				{
					bone_o = new float[from.user_bones[p].bone_o.Length],
					bone_t = new float[from.user_bones[p].bone_t.Length]
				};
				for (int oIndex = 0; oIndex < from.user_bones[p].bone_o.Length; oIndex++)
				{
					newBones.user_bones[p].bone_o[oIndex] = Math2.Lerp(from.user_bones[p].bone_o[oIndex], to.user_bones[p].bone_o[oIndex], t);
				}
				for (int tIndex = 0; tIndex < from.user_bones[p].bone_t.Length; tIndex++)
				{
					newBones.user_bones[p].bone_t[tIndex] = Math2.Lerp(from.user_bones[p].bone_t[tIndex], to.user_bones[p].bone_t[tIndex], t);
				}
			}

			return newBones;
		}
	}

	[Serializable]
	public class BonePlayer
	{
		// 23 total bones 

		// 92 values
		public float[] bone_o;

		// 69 values
		public float[] bone_t;


		public (Vector3, Quaternion)[] GetPoses()
		{
			(Vector3, Quaternion)[] poses = new (Vector3, Quaternion)[23];
			for (int i = 0; i < 23; i++)
			{
				poses[i] = GetPose(i);
			}

			return poses;
		}

		public (Vector3, Quaternion) GetPose(int index)
		{
			return (GetPosition(index), GetRotation(index));
		}

		public Quaternion GetRotation(int index)
		{
			return bone_o.Skip(index * 4).Take(4).ToArray().ToQuaternion();
		}

		public Vector3 GetPosition(int index)
		{
			return bone_t.Skip(index * 3).Take(3).ToArray().ToVector3();
		}


		public void SetPose(int index, (Vector3, Quaternion) value)
		{
			SetPosition(index, value.Item1);
			SetRotation(index, value.Item2);
		}

		public void SetRotation(int index, Quaternion value)
		{
#if UNITY
			bone_o[index * 4] = value.x;
			bone_o[index * 4 + 1] = value.y;
			bone_o[index * 4 + 2] = value.z;
			bone_o[index * 4 + 3] = value.w;
#else
			bone_o[index * 4] = value.X;
			bone_o[index * 4 + 1] = value.Y;
			bone_o[index * 4 + 2] = value.Z;
			bone_o[index * 4 + 3] = value.W;
#endif
		}

		public void SetPosition(int index, Vector3 value)
		{
#if UNITY
			bone_t[index * 3] = value.x;
			bone_t[index * 3 + 1] = value.y;
			bone_t[index * 3 + 2] = value.z;
#else
			bone_t[index * 3] = value.X;
			bone_t[index * 3 + 1] = value.Y;
			bone_t[index * 3 + 2] = value.Z;
#endif
		}
	}
}