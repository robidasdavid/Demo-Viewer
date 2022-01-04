using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNITY
using UnityEngine;
#else
using System.Numerics;
#endif
using System.Text;
using EchoVRAPI;
using Half = SystemHalf.Half;

namespace ButterReplays
{
	public static class ConverterExtensions
	{
		/// <summary>
		/// Converts a list of floats to Halfs and then to bytes
		/// </summary>
		/// <param name="values"></param>
		/// <returns></returns>
		public static byte[] GetHalfBytes(this IEnumerable<float> values)
		{
			List<byte> bytes = new List<byte>();
			foreach (float val in values)
			{
				bytes.AddRange(ButterFile.GetHalfBytes((Half)val));
			}

			return bytes.ToArray();
		}

		/// <summary>
		/// Converts a list of floats to fixed precision integer numbers of length numBytes and then to bytes
		/// </summary>
		public static byte[] GetFixedPrecisionBytes(this IEnumerable<float> values, float min, float max, int bits)
		{
			using MemoryStream mem = new MemoryStream();
			using BinaryWriter writer = new BinaryWriter(mem);
			foreach (float val in values)
			{
				double f = val - min;
				f /= max - min;
				f *= Math.Pow(2, bits);
				f -= Math.Pow(2, bits - 1);
				f = Clamp(f, -Math.Pow(2, bits - 1), Math.Pow(2, bits - 1) - 1);
				writer.WriteVarInt((long)f);
			}

			return mem.ToArray();
		}

		private static double Clamp(double value, double min, double max)
		{
			if (value < min) return min;
			if (value > max) return max;
			return value;
		}


		// /// <summary>
		// /// Converts a list of floats to fixed precision integer numbers of length numBytes and then to bytes
		// /// </summary>
		// public static IEnumerable<float> FromFixedPrecisionBytes(this IEnumerable<byte> values, float min, float max, int numBits)
		// {
		// 	using MemoryStream mem = new MemoryStream();
		// 	using BinaryWriter writer = new BinaryWriter(mem);
		// 	foreach (float val in values)
		// 	{
		// 		double f = val + min;
		// 		f /= max - min;
		// 		f *= Math.Pow(2,numBits);
		// 		f -= Math.Pow(2,numBits-1);
		// 		f = Math.Clamp(f, Math.Pow(2,numBits-1), Math.Pow(2,numBits-1)-1);
		// 		writer.WriteVarInt((long)f);
		// 	}
		//
		// 	return mem.ToArray();
		// }

		/// <summary>
		/// Converts a list of floats to fixed precision shorts and then to bytes
		/// </summary>
		public static byte[] GetFixedPrecisionShortBytes(this IEnumerable<float> values, float min, float max)
		{
			using MemoryStream mem = new MemoryStream();
			using BinaryWriter writer = new BinaryWriter(mem);
			foreach (float val in values)
			{
				double f = val + min;
				f /= max - min;
				f *= short.MaxValue - short.MinValue;
				f -= short.MinValue;
				f = Clamp(f, short.MinValue, short.MaxValue);
				writer.WriteVarInt((short)f);
			}

			return mem.ToArray();
		}

		/// <summary>
		/// Converts a Vector3 to Halfs and then to bytes
		/// </summary>
		public static byte[] GetHalfBytes(this Vector3 value)
		{
			return value.ToFloatList().GetHalfBytes();
		}

		/// <summary>
		/// Converts a Vector3 to fixed precision ints and then to bytes
		/// </summary>
		public static byte[] GetHalfBytes(this Vector3 value, float min, float max, int bits)
		{
			return value.ToFloatList().GetFixedPrecisionBytes(min, max, bits);
		}

		public static byte[] GetByteBytes(this IEnumerable<int> values)
		{
			return values.Select(val => (byte)val).ToArray();
		}

		public static byte[] GetBytes(this IEnumerable<int> values)
		{
			List<byte> bytes = new List<byte>();
			foreach (int val in values)
			{
				bytes.AddRange(BitConverter.GetBytes(val));
			}

			return bytes.ToArray();
		}


		public static byte[] GetBytes(this IEnumerable<long> values)
		{
			List<byte> bytes = new List<byte>();
			foreach (long val in values)
			{
				bytes.AddRange(BitConverter.GetBytes(val));
			}

			return bytes.ToArray();
		}

		public static byte[] GetBytes(this IEnumerable<ulong> values)
		{
			List<byte> bytes = new List<byte>();
			foreach (ulong val in values)
			{
				bytes.AddRange(BitConverter.GetBytes(val));
			}

			return bytes.ToArray();
		}


		public static byte[] GetBytes(this IEnumerable<float> values)
		{
			List<byte> bytes = new List<byte>();
			foreach (float val in values)
			{
				bytes.AddRange(BitConverter.GetBytes(val));
			}

			return bytes.ToArray();
		}

		/// <summary>
		/// Compresses the list of bools into bytes using a bitmask
		/// </summary>
		public static byte[] GetBitmasks(this List<bool> values)
		{
			List<byte> bytes = new List<byte>();
			for (int b = 0; b < Math.Ceiling(values.Count / 8f); b++)
			{
				byte currentByte = 0;
				for (int bit = 0; bit < 8; bit++)
				{
					if (values.Count > b * 8 + bit)
					{
						currentByte |= (byte)((values[b * 8 + bit] ? 1 : 0) << bit);
					}
				}

				bytes.Add(currentByte);
			}

			return bytes.ToArray();
		}

		public static bool SameAs(this byte[] b1, byte[] b2)
		{
			if (b1 == null) return false;
			if (b2 == null) return false;
			if (b1.Length != b2.Length) return false;
			for (int i = 0; i < b1.Length; i++)
			{
				if (b1[i] != b2[i]) return false;
			}

			return true;
		}

		public static bool IsZero(this byte[] b)
		{
			if (b == null) throw new ArgumentException("Input array null");
			return b.All(t => t == 0);
		}

		public static string ReadASCIIString(this BinaryReader reader, int maxLength = 1024)
		{
			List<byte> str = new List<byte>();
			for (int i = 0; i < maxLength; i++)
			{
				byte lastByte = reader.ReadByte();
				if (lastByte == 0)
				{
					return Encoding.ASCII.GetString(str.ToArray());
				}

				str.Add(lastByte);
			}

			return Encoding.ASCII.GetString(str.ToArray());
		}

		public static string ReadSessionId(this BinaryReader reader)
		{
			string str = ButterHeader.ByteArrayToString(reader.ReadBytes(16));
			str = str.Insert(8, "-");
			str = str.Insert(13, "-");
			str = str.Insert(18, "-");
			str = str.Insert(23, "-");
			return str;
		}

		public static string ReadIpAddress(this BinaryReader reader)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < 4; i++)
			{
				sb.Append(reader.ReadByte());
				if (i < 3) sb.Append('.');
			}

			return sb.ToString();
		}

		public static bool EOF(this BinaryReader binaryReader)
		{
			Stream bs = binaryReader.BaseStream;
			return (bs.Position == bs.Length);
		}

		public static (Vector3, Quaternion) ReadPose(this BinaryReader reader)
		{
			Vector3 p = reader.ReadVector3Half();
			Quaternion q = reader.ReadSmallestThree();

			return (p, q);
		}

		public static Stats ReadStats(this BinaryReader reader)
		{
			Stats stats = new Stats
			{
				assists = reader.ReadByte(),
				blocks = reader.ReadByte(),
				catches = reader.ReadByte(),
				goals = reader.ReadByte(),
				interceptions = reader.ReadByte(),
				passes = reader.ReadByte(),
				points = reader.ReadByte(),
				saves = reader.ReadByte(),
				steals = reader.ReadByte(),
				shots_taken = reader.ReadByte(),
				possession_time = (float)reader.ReadSystemHalf(),
				stuns = reader.ReadUInt16(),
			};
			return stats;
		}


		public static float ReadFixedPrecisionFloat(this BinaryReader reader, float min, float max, int bits)
		{
			double val = reader.ReadVarInt();
			val += Math.Pow(2, bits - 1);
			val /= Math.Pow(2, bits);
			val *= max - min;
			val += min;
			return (float)val;
		}

		public static Half ReadSystemHalf(this BinaryReader reader)
		{
			Half val = new Half
			{
				Value = reader.ReadUInt16()
			};
			return val;
		}

		public static void WriteHalf(this BinaryWriter writer, Half h)
		{
			writer.Write(h.Value);
		}


		// compress ulong varint.
		// same result for int, short and byte. only need one function.
		// https://github.com/vis2k/Mirror/blob/master/Assets/Mirror/Runtime/Compression.cs
		public static void WriteVarUInt(this BinaryWriter writer, ulong value)
		{
			if (value <= 240)
			{
				writer.Write((byte)value);
				return;
			}

			if (value <= 2287)
			{
				writer.Write((byte)(((value - 240) >> 8) + 241));
				writer.Write((byte)((value - 240) & 0xFF));
				return;
			}

			if (value <= 67823)
			{
				writer.Write((byte)249);
				writer.Write((byte)((value - 2288) >> 8));
				writer.Write((byte)((value - 2288) & 0xFF));
				return;
			}

			if (value <= 16777215)
			{
				writer.Write((byte)250);
				writer.Write((byte)(value & 0xFF));
				writer.Write((byte)((value >> 8) & 0xFF));
				writer.Write((byte)((value >> 16) & 0xFF));
				return;
			}

			if (value <= 4294967295)
			{
				writer.Write((byte)251);
				writer.Write((byte)(value & 0xFF));
				writer.Write((byte)((value >> 8) & 0xFF));
				writer.Write((byte)((value >> 16) & 0xFF));
				writer.Write((byte)((value >> 24) & 0xFF));
				return;
			}

			if (value <= 1099511627775)
			{
				writer.Write((byte)252);
				writer.Write((byte)(value & 0xFF));
				writer.Write((byte)((value >> 8) & 0xFF));
				writer.Write((byte)((value >> 16) & 0xFF));
				writer.Write((byte)((value >> 24) & 0xFF));
				writer.Write((byte)((value >> 32) & 0xFF));
				return;
			}

			if (value <= 281474976710655)
			{
				writer.Write((byte)253);
				writer.Write((byte)(value & 0xFF));
				writer.Write((byte)((value >> 8) & 0xFF));
				writer.Write((byte)((value >> 16) & 0xFF));
				writer.Write((byte)((value >> 24) & 0xFF));
				writer.Write((byte)((value >> 32) & 0xFF));
				writer.Write((byte)((value >> 40) & 0xFF));
				return;
			}

			if (value <= 72057594037927935)
			{
				writer.Write((byte)254);
				writer.Write((byte)(value & 0xFF));
				writer.Write((byte)((value >> 8) & 0xFF));
				writer.Write((byte)((value >> 16) & 0xFF));
				writer.Write((byte)((value >> 24) & 0xFF));
				writer.Write((byte)((value >> 32) & 0xFF));
				writer.Write((byte)((value >> 40) & 0xFF));
				writer.Write((byte)((value >> 48) & 0xFF));
				return;
			}

			// all others
			{
				writer.Write((byte)255);
				writer.Write((byte)(value & 0xFF));
				writer.Write((byte)((value >> 8) & 0xFF));
				writer.Write((byte)((value >> 16) & 0xFF));
				writer.Write((byte)((value >> 24) & 0xFF));
				writer.Write((byte)((value >> 32) & 0xFF));
				writer.Write((byte)((value >> 40) & 0xFF));
				writer.Write((byte)((value >> 48) & 0xFF));
				writer.Write((byte)((value >> 56) & 0xFF));
			}
		}

		// zigzag encoding https://gist.github.com/mfuerstenau/ba870a29e16536fdbaba
		public static void WriteVarInt(this BinaryWriter writer, long i)
		{
			ulong zigzagged = (ulong)((i >> 63) ^ (i << 1));
			writer.WriteVarUInt(zigzagged);
		}

		public static ulong ReadVarUInt(this BinaryReader reader)
		{
			byte a0 = reader.ReadByte();
			if (a0 < 241)
			{
				return a0;
			}

			byte a1 = reader.ReadByte();
			if (a0 <= 248)
			{
				return 240 + ((a0 - (ulong)241) << 8) + a1;
			}

			byte a2 = reader.ReadByte();
			if (a0 == 249)
			{
				return 2288 + ((ulong)a1 << 8) + a2;
			}

			byte a3 = reader.ReadByte();
			if (a0 == 250)
			{
				return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16);
			}

			byte a4 = reader.ReadByte();
			if (a0 == 251)
			{
				return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16) + (((ulong)a4) << 24);
			}

			byte a5 = reader.ReadByte();
			if (a0 == 252)
			{
				return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16) + (((ulong)a4) << 24) + (((ulong)a5) << 32);
			}

			byte a6 = reader.ReadByte();
			if (a0 == 253)
			{
				return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16) + (((ulong)a4) << 24) + (((ulong)a5) << 32) + (((ulong)a6) << 40);
			}

			byte a7 = reader.ReadByte();
			if (a0 == 254)
			{
				return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16) + (((ulong)a4) << 24) + (((ulong)a5) << 32) + (((ulong)a6) << 40) + (((ulong)a7) << 48);
			}

			byte a8 = reader.ReadByte();
			return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16) + (((ulong)a4) << 24) + (((ulong)a5) << 32) + (((ulong)a6) << 40) + (((ulong)a7) << 48) + (((ulong)a8) << 56);
		}

		// zigzag decoding https://gist.github.com/mfuerstenau/ba870a29e16536fdbaba
		public static long ReadVarInt(this BinaryReader reader)
		{
			ulong data = ReadVarUInt(reader);
			return ((long)(data >> 1)) ^ -((long)data & 1);
		}

		public static Vector3 ReadVector3FixedPrecision(this BinaryReader reader, float min, float max, int bits)
		{
			return reader.ReadFixedPrecisionFloats(3, min, max, bits).ToVector3();
		}

		public static Vector3 ReadVector3Half(this BinaryReader reader)
		{
			return reader.ReadHalfs(3).ToVector3();
		}

		public static float[] ReadFixedPrecisionFloats(this BinaryReader reader, int num, float min, float max, int bits)
		{
			float[] halfs = new float[num];
			for (int i = 0; i < num; i++)
			{
				halfs[i] = reader.ReadFixedPrecisionFloat(min, max, bits);
			}

			return halfs;
		}

		public static float[] ReadHalfs(this BinaryReader reader, int num)
		{
			float[] halfs = new float[num];
			for (int i = 0; i < num; i++)
			{
				halfs[i] = reader.ReadSystemHalf();
			}

			return halfs;
		}


		public static Quaternion ReadSmallestThree(this BinaryReader reader)
		{
			uint st = reader.ReadUInt32();

			uint maxIndex = st & 0b11;
			float f1 = Uncompress((st & (0b1111111111 << 2)) >> 2);
			float f2 = Uncompress((st & (0b1111111111 << 12)) >> 12);
			float f3 = Uncompress((st & (0b1111111111 << 22)) >> 22);

			float Uncompress(float input)
			{
				return (float)(input / 1023 * 1.41421356 - 0.70710678);
			}

#if UNITY
			float f4 = Mathf.Sqrt(1 - f1 * f1 - f2 * f2 - f3 * f3);
#else
			float f4 = MathF.Sqrt(1 - f1 * f1 - f2 * f2 - f3 * f3);
#endif
			Quaternion ret = maxIndex switch
			{
				0 => new Quaternion(f4, f1, f2, f3),
				1 => new Quaternion(f1, f4, f2, f3),
				2 => new Quaternion(f1, f2, f4, f3),
				3 => new Quaternion(f1, f2, f3, f4),
				_ => throw new Exception("Invalid index")
			};

#if UNITY
			ret = Quaternion.LookRotation(ret.ForwardBackwards(), ret.UpBackwards());
#endif
			return ret;
		}

		// converts time in seconds to a string in the format "mm:ss.ms"
		public static string ToGameClockDisplay(this float time)
		{
			int minutes = (int)time / 60;
			int seconds = (int)time % 60;
			int milliseconds = (int)((time - (int)time) * 100);
			return $"{minutes:D2}:{seconds:D2}.{milliseconds:D2}";
		}

		public static bool GetBitmaskValue(this byte b, int index)
		{
			return (b & (1 << index)) != 0;
		}

		public static List<bool> GetBitmaskValues(this IEnumerable<byte> bytes)
		{
			List<bool> l = new List<bool>();
			foreach (byte b in bytes)
			{
				l.AddRange(b.GetBitmaskValues());
			}

			return l;
		}

		public static List<bool> GetBitmaskValues(this byte b)
		{
			List<bool> l = new List<bool>();
			for (int i = 0; i < 8; i++)
			{
				l.Add(b.GetBitmaskValue(i));
			}

			return l;
		}

		public static int ToZstdLevel(this ButterFile.CompressionFormat level)
		{
			return level switch
			{
				ButterFile.CompressionFormat.zstd_3 => 3,
				ButterFile.CompressionFormat.zstd_7 => 7,
				ButterFile.CompressionFormat.zstd_15 => 15,
				ButterFile.CompressionFormat.zstd_22 => 22,
				ButterFile.CompressionFormat.zstd_7_dict => 7,
				_ => -1
			};
		}

		public static bool UsingZstdDict(this ButterFile.CompressionFormat level)
		{
			return level switch
			{
				ButterFile.CompressionFormat.zstd_7_dict => true,
				_ => false
			};
		}
	}
}