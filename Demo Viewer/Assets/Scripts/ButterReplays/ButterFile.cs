using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
#if UNITY
using UnityEngine;
#else
using System.Numerics;
#endif
using System.Text;
using EchoVRAPI;
using Half = SystemHalf.Half;
#if ZSTD
using ZstdNet;
#endif
using Transform = EchoVRAPI.Transform;

namespace ButterReplays
{
	/// <summary>
	/// 🧈🧈🧈🧈🧈
	/// </summary>
	public class ButterFile
	{
		public readonly ButterHeader header;

		private int frameCount;

		/// <summary>
		/// These always belong to a single chunk
		/// </summary>
		private readonly ConcurrentQueue<ButterFrame> unprocessedFrames = new ConcurrentQueue<ButterFrame>();

		private readonly ConcurrentQueue<byte[]> chunkData = new ConcurrentQueue<byte[]>();

#if ZSTD
		private readonly Compressor compressor;
#endif

		/// <summary>
		/// Creates a new butter file instance.
		/// </summary>
		/// <param name="keyframeInterval">Keyframes indicate the size of the independent chunks.</param>
		/// <param name="compressionFormat">Chunk compression format</param>
		public ButterFile(ushort keyframeInterval = 300, CompressionFormat compressionFormat = CompressionFormat.gzip)
		{
			header = new ButterHeader(keyframeInterval, compressionFormat);
			if (compressionFormat.ToZstdLevel() > 0)
			{
#if ZSTD
				compressor = new Compressor(new CompressionOptions(header.compression.ToZstdLevel()));
#endif
			}
		}


		public enum CompressionFormat : byte
		{
			none,
			gzip,
			zstd_3,
			zstd_7,
			zstd_15,
			zstd_22,
			zstd_7_dict,
		}

		private enum GameStatus : byte
		{
			_ = 0,
			pre_match = 1,
			round_start = 2,
			playing = 3,
			score = 4,
			round_over = 5,
			post_match = 6,
			pre_sudden_death = 7,
			sudden_death = 8,
			post_sudden_death = 9
		}


		public enum MapName : byte
		{
			uncoded = 0,
			mpl_lobby_b2 = 1,
			mpl_arena_a = 2,
			mpl_combat_fission = 3,
			mpl_combat_combustion = 4,
			mpl_combat_dyson = 5,
			mpl_combat_gauss = 6,
			mpl_tutorial_arena = 7,
		}

		/// <summary>
		/// This can be stored as 2 bits
		/// </summary>
		private enum PausedState : byte
		{
			unpaused = 0,
			paused = 1,
			unpausing = 2,
			pausing = 3, // TODO is this a thing?
		}

		/// <summary>
		/// This can be stored as 2 bits
		/// </summary>
		private enum TeamIndex : byte
		{
			Blue = 0,
			Orange = 1,
			Spectator = 2,
			None = 3
		}

		public enum GoalType : byte
		{
			unknown,
			BOUNCE_SHOT,
			INSIDE_SHOT,
			LONG_BOUNCE_SHOT,
			LONG_SHOT,
			SELF_GOAL,
			SLAM_DUNK,
			BUMPER_SHOT,
			HEADBUTT,
			// TODO contains more
		}

		// TODO complete this
		public static string MatchType(string mapName, bool privateMatch)
		{
			// "mpl_lobby_b2" => privateMatch ? "Private Match" : "Public Match",
			switch (mapName)
			{
				case "mpl_arena_a":
					return privateMatch ? "Echo_Arena_Private" : "Echo_Arena";
				case "mpl_combat_fission":
				case "mpl_combat_combustion":
				case "mpl_combat_dyson":
				case "mpl_combat_gauss":
					return privateMatch ? "Echo_Combat_Private" : "Echo_Combat";
				case "mpl_tutorial_arena":
				case "mpl_lobby_b":
				default:
					return "Unknown";
			}
		}


		public void AddFrame(Frame frame)
		{
			// if there is no data yet, add this frame to the file header
			if (header == null)
			{
				throw new Exception("Header undefined");
			}
			else
			{
				header.ConsiderNewFrame(frame);
			}

			ButterFrame butterFrame = new ButterFrame(frame, frameCount++, unprocessedFrames.LastOrDefault(), header);

			// if chunk is finished
			if (butterFrame.IsKeyframe && !unprocessedFrames.IsEmpty)
			{
				chunkData.Enqueue(ChunkUnprocessedFrames());
				unprocessedFrames.Clear();
			}

			unprocessedFrames.Enqueue(butterFrame);
		}

		public int NumChunks()
		{
			return chunkData.Count;
		}


		private byte[] CompressChunk(byte[] uncompressed)
		{
			byte[] compressed;
			switch (header.compression)
			{
				case CompressionFormat.none:
					compressed = uncompressed;
					break;
				case CompressionFormat.gzip:
					compressed = Zip(uncompressed);
					break;
				case CompressionFormat.zstd_3:
				case CompressionFormat.zstd_7:
				case CompressionFormat.zstd_15:
				case CompressionFormat.zstd_22:
				case CompressionFormat.zstd_7_dict:
#if ZSTD
					compressed = compressor.Wrap(uncompressed);
#else
					throw new Exception("Zstd not available.");
#endif
					break;
				default:
					throw new Exception("Unknown compression.");
			}

			return compressed;
		}

		private byte[] ChunkUnprocessedFrames()
		{
			List<byte> lastChunkBytes = new List<byte>();

			if (unprocessedFrames.Count > header.keyframeInterval)
			{
				throw new Exception("Chunk too large.");
			}


			foreach (ButterFrame frame in unprocessedFrames)
			{
				byte[] newBytes = frame.GetBytes();
				lastChunkBytes.AddRange(newBytes);
			}

			// compress the last chunk
			return CompressChunk(lastChunkBytes.ToArray());
		}

		public byte[] GetBytes()
		{
			return GetBytes(out Dictionary<string, double> _);
		}

		public byte[] GetBytes(out Dictionary<string, double> sizeBreakdown)
		{
			List<byte> fullFileBytes = new List<byte>();
			sizeBreakdown = new Dictionary<string, double>();

			if (frameCount == 0) return fullFileBytes.ToArray();

			Stopwatch sw = new Stopwatch();
			sw.Start();

			byte[] headerBytes = header.GetBytes();
			fullFileBytes.AddRange(headerBytes);
			sizeBreakdown["HeaderBytes"] = headerBytes.Length;

			// save the int for the number of chunks
			fullFileBytes.AddRange(BitConverter.GetBytes(chunkData.Count + 1));

			foreach (byte[] chunk in chunkData)
			{
				fullFileBytes.AddRange(BitConverter.GetBytes((uint)chunk.Length));
			}

			byte[] tailChunk = ChunkUnprocessedFrames();
			fullFileBytes.AddRange(BitConverter.GetBytes((uint)tailChunk.Length));

			sizeBreakdown["ChunkSizes"] = fullFileBytes.Count - headerBytes.Length;
			sizeBreakdown["CompressionLevel"] = header.compression.ToZstdLevel();
			sizeBreakdown["UsingZstdDict"] = header.compression.UsingZstdDict() ? 1 : 0;
			sizeBreakdown["KeyframeInterval"] = header.keyframeInterval;


			foreach (byte[] chunk in chunkData)
			{
				fullFileBytes.AddRange(chunk);
			}

			// add the unfinished chunk at the end
			fullFileBytes.AddRange(tailChunk);

			sizeBreakdown["ConversionTime"] = sw.Elapsed.TotalSeconds;

			return fullFileBytes.ToArray();
		}

		public static List<Frame> FromBytes(byte[] bytes)
		{
			float progress = 0;
			using MemoryStream mem = new MemoryStream(bytes);
			using BinaryReader fileInput = new BinaryReader(mem);
			return FromBytes(fileInput, ref progress);
		}

		public static List<Frame> FromBytes(BinaryReader fileInput)
		{
			float progress = 0;
			return FromBytes(fileInput, ref progress);
		}

		public static List<Frame> FromBytes(BinaryReader fileInput, ref float readProgress)
		{
			byte formatVersion = fileInput.ReadByte();
			return formatVersion switch
			{
				1 => Decompressor_v1.FromBytes(formatVersion, fileInput, ref readProgress),
				// 2 => Decompressor_v2.FromBytes(formatVersion, fileInput, ref readProgress),
				_ => null
			};
		}

		public static void CopyTo(Stream src, Stream dest)
		{
			byte[] bytes = new byte[4096];

			int cnt;

			while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
			{
				dest.Write(bytes, 0, cnt);
			}
		}

		public static byte[] Zip(string str)
		{
			return Zip(Encoding.UTF8.GetBytes(str));
		}

		public static byte[] Zip(byte[] bytes)
		{
			using MemoryStream msi = new MemoryStream(bytes);
			using MemoryStream mso = new MemoryStream();
			using (GZipStream gs = new GZipStream(mso, CompressionMode.Compress))
			{
				CopyTo(msi, gs);
			}

			mso.Flush();
			byte[] data = mso.ToArray();
			mso.Dispose();
			return data;
		}

		public static byte[] UnzipBytes(byte[] bytes)
		{
			using MemoryStream msi = new MemoryStream(bytes);
			using MemoryStream mso = new MemoryStream();
			using (GZipStream gs = new GZipStream(msi, CompressionMode.Decompress))
			{
				CopyTo(gs, mso);
			}

			mso.Flush();
			byte[] data = mso.ToArray();
			mso.Dispose();
			return data;
		}

		public static string UnzipStr(byte[] bytes)
		{
			return Encoding.UTF8.GetString(UnzipBytes(bytes));
		}


		public static byte[] GetHalfBytes(Half half)
		{
			return BitConverter.GetBytes(half.Value);
		}
	}

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
			return IsEqualUsingDot(num) ? 0.0f : (float)((double)MathF.Acos(MathF.Min(MathF.Abs(num), 1f)) * 2.0 * 57.2957801818848);
#endif
		}

		private static bool IsEqualUsingDot(float dot) => dot > 0.999998986721039;
	}

#if UNITY
	internal static class ConcurrentQueueExtensions
	{
		public static void Clear<T>(this ConcurrentQueue<T> queue)
		{
			while (queue.TryDequeue(out T _))
			{
			}
		}
	}
#endif
}