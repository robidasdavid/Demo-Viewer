using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;
using System.Text;
using EchoVRAPI;
using Photon.Compression.HalfFloat;
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
		private readonly ButterHeader header;

		// private readonly List<ButterFrame> frames;
		private int frameCount = 0;
		private readonly string filePath;

		/// <summary>
		/// These always belong to a single chunk
		/// </summary>
		private readonly List<ButterFrame> unprocessedFrames = new List<ButterFrame>();

		private readonly List<byte[]> chunkData = new List<byte[]>();

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
			if (butterFrame.IsKeyframe && unprocessedFrames.Count > 0)
			{
				chunkData.Add(ChunkUnprocessedFrames());
				unprocessedFrames.Clear();
			}

			unprocessedFrames.Add(butterFrame);
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

		// public byte[] GetBytes(out Dictionary<string, double> sizeBreakdown)
		// {
		// 	List<byte> fullFileBytes = new List<byte>();
		// 	List<byte> lastChunkBytes = new List<byte>();
		// 	sizeBreakdown = new Dictionary<string, double>();
		// 	Stopwatch sw = new Stopwatch();
		// 	sw.Start();
		//
		// 	byte[] headerBytes = header.GetBytes();
		// 	fullFileBytes.AddRange(headerBytes);
		// 	sizeBreakdown["HeaderBytes"] = headerBytes.Length;
		// 	uint numChunks = (uint) (frames.Count / header.keyframeInterval + 1);
		//
		// 	// save the int for the number of chunks
		// 	fullFileBytes.AddRange(BitConverter.GetBytes(numChunks));
		//
		// 	// allocate space for the chunk sizes
		// 	fullFileBytes.AddRange(new byte[sizeof(uint) * numChunks]);
		//
		// 	sizeBreakdown["ChunkSizes"] = fullFileBytes.Count - headerBytes.Length;
		//
		//
		// 	Compressor compressor;
		//
		// 	// this generates a new dict for each file - wouldn't be useful like this
		// 	if (header.compression.UsingZstdDict())
		// 	{
		// 		// generate dictionary
		// 		List<byte[]> zstdDictionary = new List<byte[]>();
		// 		foreach (ButterFrame frame in frames)
		// 		{
		// 			zstdDictionary.Add(frame.GetBytes());
		// 		}
		//
		// 		compressor = new Compressor(new CompressionOptions(DictBuilder.TrainFromBuffer(zstdDictionary), header.compression.ToZstdLevel()));
		// 	}
		// 	else
		// 	{
		// 		compressor = new Compressor(new CompressionOptions(header.compression.ToZstdLevel()));
		// 	}
		//
		// 	sizeBreakdown["CompressionLevel"] = header.compression.ToZstdLevel();
		// 	sizeBreakdown["UsingZstdDict"] = header.compression.UsingZstdDict() ? 1 : 0;
		// 	sizeBreakdown["KeyframeInterval"] = header.keyframeInterval;
		//
		// 	double byteTotal = 0;
		// 	double chunkBytesTotal = 0;
		// 	double chunkBytesTotalUncompressed = 0;
		// 	int chunkFrames = 0;
		// 	int chunkIndex = 0;
		//
		// 	void CompressChunk()
		// 	{
		// 		byte[] uncompressed = lastChunkBytes.ToArray();
		// 		byte[] compressed;
		// 		switch (header.compression)
		// 		{
		// 			case CompressionFormat.none:
		// 				compressed = uncompressed;
		// 				break;
		// 			case CompressionFormat.gzip:
		// 				compressed = Zip(uncompressed);
		// 				break;
		// 			case CompressionFormat.zstd_3:
		// 			case CompressionFormat.zstd_7:
		// 			case CompressionFormat.zstd_15:
		// 			case CompressionFormat.zstd_22:
		// 			case CompressionFormat.zstd_7_dict:
		// 				compressed = compressor.Wrap(uncompressed);
		// 				break;
		// 			default:
		// 				throw new Exception("Unknown compression.");
		// 		}
		//
		// 		chunkBytesTotal += compressed.Length;
		// 		chunkBytesTotalUncompressed += uncompressed.Length;
		// 		fullFileBytes.AddRange(compressed);
		// 		// Console.WriteLine($"Chunk size:\t{compressed.Length:N0} bytes\traw: {lastChunkBytes.Count:N0}\tratio: {(float) compressed.Length / lastChunkBytes.Count:P}\tnframes: {chunkFrames:N}");
		// 		byte[] chunkSizeBytes = BitConverter.GetBytes((uint) compressed.Length);
		// 		fullFileBytes[headerBytes.Length + sizeof(uint) + chunkIndex * sizeof(uint)] = chunkSizeBytes[0];
		// 		fullFileBytes[headerBytes.Length + sizeof(uint) + chunkIndex * sizeof(uint) + 1] =
		// 			chunkSizeBytes[1];
		// 		fullFileBytes[headerBytes.Length + sizeof(uint) + chunkIndex * sizeof(uint) + 2] =
		// 			chunkSizeBytes[2];
		// 		fullFileBytes[headerBytes.Length + sizeof(uint) + chunkIndex * sizeof(uint) + 3] =
		// 			chunkSizeBytes[3];
		// 		chunkFrames = 0;
		// 		chunkIndex++;
		// 		lastChunkBytes.Clear();
		// 	}
		//
		//
		// 	foreach (ButterFrame frame in frames)
		// 	{
		// 		// is a keyframe but not the first frame in the file
		// 		if (frame.IsKeyframe && lastChunkBytes.Count > 0)
		// 		{
		// 			CompressChunk();
		// 		}
		//
		// 		byte[] newBytes = frame.GetBytes();
		// 		lastChunkBytes.AddRange(newBytes);
		// 		byteTotal += newBytes.Length;
		// 		chunkFrames++;
		// 	}
		//
		// 	// compress the last chunk
		// 	CompressChunk();
		//
		// 	sizeBreakdown["AllFramesUncompressed"] = byteTotal;
		// 	sizeBreakdown["AllFramesCompressed"] = chunkBytesTotal;
		// 	sizeBreakdown["AverageFrameSizeUncompressed"] = byteTotal / frames.Count;
		// 	sizeBreakdown["AverageFrameSizeCompressed"] = chunkBytesTotal / frames.Count;
		// 	sizeBreakdown["NumChunks"] = chunkIndex;
		// 	sizeBreakdown["AverageChunkSize"] = chunkBytesTotal / chunkIndex;
		//
		// 	// Console.WriteLine();
		//
		// 	// Console.WriteLine($"Included:\t{includedCount:N}\tExcluded:\t{excludedCount:N}\tPerc:\t{(float) includedCount / (excludedCount + includedCount):P}");
		// 	// Console.WriteLine($"Average frame size:\t{includedCount/excludedCount:P} bytes");
		// 	sizeBreakdown["ConversionTime"] = sw.Elapsed.TotalSeconds;
		//
		// 	return fullFileBytes.ToArray();
		// }

		public static List<Frame> FromBytes(BinaryReader fileInput)
		{
			List<Frame> l = new List<Frame>();

			byte formatVersion = fileInput.ReadByte();
			ushort keyframeInterval = fileInput.ReadUInt16();
			CompressionFormat compressionFormat = (CompressionFormat)fileInput.ReadByte();

			ButterFile b = new ButterFile(keyframeInterval, compressionFormat);
			b.header.formatVersion = formatVersion;

			Frame firstFrame = new Frame
			{
				client_name = fileInput.ReadASCIIString(),
				sessionid = fileInput.ReadSessionId(),
				sessionip = fileInput.ReadIpAddress()
			};
			byte playerCount = fileInput.ReadByte();


			for (int i = 0; i < playerCount; i++)
			{
				b.header.players.Add(fileInput.ReadASCIIString());
			}

			for (int i = 0; i < playerCount; i++)
			{
				b.header.userids.Add(fileInput.ReadInt64());
			}

			for (int i = 0; i < playerCount; i++)
			{
				b.header.numbers.Add(fileInput.ReadByte());
			}

			for (int i = 0; i < playerCount; i++)
			{
				b.header.levels.Add(fileInput.ReadByte());
			}

			firstFrame.total_round_count = fileInput.ReadByte();
			byte roundScores = fileInput.ReadByte();
			firstFrame.blue_round_score = roundScores & 0xF;
			firstFrame.orange_round_score = (roundScores >> 4) & 0xF;

			byte mapByte = fileInput.ReadByte();
			firstFrame.private_match = (mapByte & 1) == 1;
			firstFrame.map_name = ((MapName)(mapByte >> 1)).ToString();
			firstFrame.match_type = MatchType(firstFrame.map_name, firstFrame.private_match);


			// read the chunk sizes
			uint numChunks = fileInput.ReadUInt32();
			uint[] chunkSizes = new uint[numChunks];
			for (int i = 0; i < numChunks; i++)
			{
				chunkSizes[i] = fileInput.ReadUInt32();
			}

			b.header.firstFrame = firstFrame;

			Frame lastKeyframe = null;
			Frame lastFrame = null;
			
#if ZSTD
			Decompressor decompressor = new Decompressor();
#endif

			// reads one frame at a time
			// while (!fileInput.EOF())
			for (int chunkIndex = 0; chunkIndex < numChunks; chunkIndex++)
			{
				// if the last chunk is empty - nframes was divisible by chunk size
				if (chunkSizes[chunkIndex] == 0 && chunkSizes.Length - 2 <= chunkIndex) break;
				byte[] compressedChunk = fileInput.ReadBytes((int)chunkSizes[chunkIndex]);
				byte[] uncompressedChunk;
				switch (b.header.compression)
				{
					case CompressionFormat.none:
						uncompressedChunk = compressedChunk;
						break;
					case CompressionFormat.gzip:
						uncompressedChunk = UnzipBytes(compressedChunk);
						break;
					case CompressionFormat.zstd_3:
					case CompressionFormat.zstd_7:
					case CompressionFormat.zstd_15:
					case CompressionFormat.zstd_22:
					case CompressionFormat.zstd_7_dict:
#if ZSTD
						uncompressedChunk = decompressor.Unwrap(compressedChunk);
#else
						throw new Exception("Zstd not available.");
#endif
						break;
					default:
						throw new Exception("Compression format unknown");
				}

				using MemoryStream memoryStream = new MemoryStream(uncompressedChunk);
				using BinaryReader input = new BinaryReader(memoryStream);

				// read through each of the frames in this chunk
				while (!input.EOF())
				{
					ushort headerByte = input.ReadUInt16();
					if (headerByte != 0xFEFC && headerByte != 0xFEFE)
					{
						throw new Exception("Not reading at beginning of frame, maybe wrong frame size");
					}

					bool isKeyframe = headerByte == 0xFEFC;

					if (isKeyframe) lastFrame = null;

					if (!isKeyframe && lastKeyframe == null)
					{
						throw new Exception("This isn't a keyframe, but no previous keyframe found.");
					}

					DateTime time = isKeyframe
						? DateTimeOffset.FromUnixTimeMilliseconds(input.ReadInt64()).DateTime
						: lastFrame.recorded_time.AddMilliseconds(input.ReadUInt16());


					// Frame f = isKeyframe ? new Frame() : lastKeframe.Copy();
					Frame f = new Frame
					{
						recorded_time = time,
						client_name = b.header.firstFrame.client_name,
						sessionid = b.header.firstFrame.sessionid,
						sessionip = b.header.firstFrame.sessionip,
						total_round_count = b.header.firstFrame.total_round_count,
						blue_round_score = b.header.firstFrame.blue_round_score,
						orange_round_score = b.header.firstFrame.orange_round_score,
						private_match = b.header.firstFrame.private_match,
						map_name = b.header.firstFrame.map_name,
						match_type = b.header.firstFrame.match_type,
						game_clock = (float)input.ReadSingle() + (isKeyframe ? 0 : lastFrame.game_clock)
					};

					f.game_clock_display = f.game_clock.ToGameClockDisplay();
					List<bool> inclusionBitmask = input.ReadByte().GetBitmaskValues();

					f.game_status = inclusionBitmask[0]
						? ButterFrame.ByteToGameStatus(input.ReadByte())
						: lastFrame.game_status;

					if (inclusionBitmask[1])
					{
						f.blue_points = input.ReadByte();
						f.orange_points = input.ReadByte();
					}
					else
					{
						f.blue_points = lastFrame.blue_points;
						f.orange_points = lastFrame.orange_points;
					}

					// Pause and restarts
					if (inclusionBitmask[2])
					{
						byte pauses = input.ReadByte();
						f.blue_team_restart_request = (pauses & 0b1) > 0;
						f.orange_team_restart_request = (pauses & 0b10) > 0;
						f.pause = new Pause
						{
							paused_requested_team = ButterFrame.TeamIndexToTeam((byte)((pauses & 0b1100) >> 2)),
							unpaused_team = ButterFrame.TeamIndexToTeam((byte)((pauses & 0b110000) >> 4)),
							paused_state = ButterFrame.ByteToPausedState((byte)((pauses & 0b11000000) >> 6)),
							paused_timer = (float)input.ReadHalf(),
							unpaused_timer = (float)input.ReadHalf(),
						};
					}
					else
					{
						f.pause = lastFrame.pause;
					}

					// Inputs
					if (inclusionBitmask[3])
					{
						List<bool> inputs = input.ReadByte().GetBitmaskValues();

						f.left_shoulder_pressed = inputs[0];
						f.right_shoulder_pressed = inputs[1];
						f.left_shoulder_pressed2 = inputs[2];
						f.right_shoulder_pressed2 = inputs[3];
					}
					else
					{
						f.left_shoulder_pressed = lastFrame.left_shoulder_pressed;
						f.right_shoulder_pressed = lastFrame.right_shoulder_pressed;
						f.left_shoulder_pressed2 = lastFrame.left_shoulder_pressed2;
						f.right_shoulder_pressed2 = lastFrame.right_shoulder_pressed2;
					}

					// Last Score
					if (inclusionBitmask[4])
					{
						byte lastScoreByte = input.ReadByte();
						f.last_score = new LastScore
						{
							team = ButterFrame.TeamIndexToTeam((byte)(lastScoreByte & 0b11)),
							point_amount = (lastScoreByte & 0b100) > 0 ? 3 : 2,
							goal_type = ((GoalType)((lastScoreByte & 0b11111000) >> 3)).ToString()
								.Replace("_", " "),
							person_scored = b.header.GetPlayerName(input.ReadByte()),
							assist_scored = b.header.GetPlayerName(input.ReadByte()),
							disc_speed = (float)input.ReadHalf(),
							distance_thrown = (float)input.ReadHalf(),
						};
					}
					else
					{
						f.last_score = lastFrame.last_score;
					}

					// Last Throw
					if (inclusionBitmask[5])
					{
						f.last_throw = new LastThrow
						{
							arm_speed = (float)input.ReadHalf(),
							total_speed = (float)input.ReadHalf(),
							off_axis_spin_deg = (float)input.ReadHalf(),
							wrist_throw_penalty = (float)input.ReadHalf(),
							rot_per_sec = (float)input.ReadHalf(),
							pot_speed_from_rot = (float)input.ReadHalf(),
							speed_from_arm = (float)input.ReadHalf(),
							speed_from_movement = (float)input.ReadHalf(),
							speed_from_wrist = (float)input.ReadHalf(),
							wrist_align_to_throw_deg = (float)input.ReadHalf(),
							throw_align_to_movement_deg = (float)input.ReadHalf(),
							off_axis_penalty = (float)input.ReadHalf(),
							throw_move_penalty = (float)input.ReadHalf()
						};
					}
					else
					{
						f.last_throw = lastFrame.last_throw;
					}

					// VR Player
					if (inclusionBitmask[6])
					{
						(Vector3 p, Quaternion q) = input.ReadPose();
						f.player = new VRPlayer
						{
							vr_position = p.ToFloatList(),
							vr_forward = q.Forward().ToFloatList(),
							vr_left = q.Left().ToFloatList(),
							vr_up = q.Up().ToFloatList(),
						};
						// TODO get diff from previous frames
					}
					else
					{
						f.player = lastFrame.player;
					}

					// Disc
					if (inclusionBitmask[7])
					{
						(Vector3 p, Quaternion q) = input.ReadPose();

						p += (lastFrame?.disc.position.ToVector3() ?? Vector3.zero);


						f.disc = new Disc
						{
							position = p.ToFloatArray().ToList(),
							forward = q.Forward().ToFloatArray().ToList(),
							left = q.Left().ToFloatArray().ToList(),
							up = q.Up().ToFloatArray().ToList(),
							velocity = new List<float>()
							{
								(float)input.ReadHalf() + (lastFrame?.disc.velocity[0] ?? 0),
								(float)input.ReadHalf() + (lastFrame?.disc.velocity[1] ?? 0),
								(float)input.ReadHalf() + (lastFrame?.disc.velocity[2] ?? 0),
							}
						};
					}
					else
					{
						f.disc = lastFrame.disc;
					}

					byte teamDataBitmask = input.ReadByte();
					f.teams = new List<Team>()
					{
						new Team(),
						new Team(),
						new Team(),
					};
					f.teams[0].possession = (teamDataBitmask & 0b1) > 0;
					f.teams[1].possession = (teamDataBitmask & 0b10) > 0;

					// Team stats included
					bool[] teamStatsIncluded = new bool[3];
					teamStatsIncluded[0] = (teamDataBitmask & 0b100) > 0;
					teamStatsIncluded[1] = (teamDataBitmask & 0b1000) > 0;
					teamStatsIncluded[2] = (teamDataBitmask & 0b10000) > 0;

					// add team data
					for (int i = 0; i < 3; i++)
					{
						if (teamStatsIncluded[i])
						{
							f.teams[i].stats = input.ReadStats();
							// TODO diff
						}

						int teamPlayerCount = input.ReadByte();

						f.teams[i].players = new List<Player>();
						for (int j = 0; j < teamPlayerCount; j++)
						{
							// TODO match to previous keyframe and diff

							byte fileIndex = input.ReadByte();

							Player p = new Player
							{
								name = b.header.GetPlayerName(fileIndex),
								playerid = input.ReadByte(),
								level = b.header.GetPlayerLevel(fileIndex),
								number = b.header.GetPlayerNumber(fileIndex),
								userid = b.header.GetUserId(fileIndex),
							};

							List<bool> playerStateBitmask = input.ReadByte().GetBitmaskValues();
							p.possession = playerStateBitmask[0];
							p.blocking = playerStateBitmask[1];
							p.stunned = playerStateBitmask[2];
							p.invulnerable = playerStateBitmask[3];

							if (playerStateBitmask[4])
							{
								p.stats = input.ReadStats();
								Stats oldStats = lastFrame?.GetPlayer(p.userid)?.stats;
								if (oldStats != null)
								{
									p.stats += oldStats;
								}
							}
							else
							{
								p.stats = lastFrame.GetPlayer(p.userid).stats;
							}

							if (playerStateBitmask[5])
							{
								p.ping = input.ReadUInt16() + (lastFrame?.GetPlayer(p.userid)?.ping ?? 0);
								p.packetlossratio = (float)input.ReadHalf() +
								                    (lastFrame?.GetPlayer(p.userid)?.packetlossratio ?? 0);
							}
							else
							{
								p.ping = lastFrame.GetPlayer(p.userid).ping;
								p.packetlossratio = lastFrame.GetPlayer(p.userid).packetlossratio;
							}

							if (playerStateBitmask[6])
							{
								p.holding_left = b.header.ByteToHolding(input.ReadByte());
								p.holding_right = b.header.ByteToHolding(input.ReadByte());
							}
							else
							{
								p.holding_left = lastFrame.GetPlayer(p.userid).holding_left;
								p.holding_right = lastFrame.GetPlayer(p.userid).holding_right;
							}

							if (playerStateBitmask[7])
							{
								p.velocity = (input.ReadVector3Half() + (lastFrame?.GetPlayer(p.userid)?.velocity?.ToVector3() ?? Vector3.zero)).ToFloatArray().ToList();
							}
							else
							{
								p.velocity = lastFrame.GetPlayer(p.userid).velocity;
							}

							List<bool> playerPoseBitmask = input.ReadByte().GetBitmaskValues();

							p.head = new Transform();
							p.body = new Transform();
							p.lhand = new Transform();
							p.rhand = new Transform();

							if (playerPoseBitmask[0])
							{
								p.head.position =
									(input.ReadVector3Half() +
									 (lastFrame?.GetPlayer(p.userid)?.head.Position ?? Vector3.zero))
									.ToFloatList();
							}
							else
							{
								p.head.position = lastFrame.GetPlayer(p.userid).head.position;
							}

							p.head.Rotation = playerPoseBitmask[1]
								? input.ReadSmallestThree()
								: lastFrame.GetPlayer(p.userid).head.Rotation;

							if (playerPoseBitmask[2])
							{
								p.body.position =
									(input.ReadVector3Half() +
									 (lastFrame?.GetPlayer(p.userid)?.body.Position ?? Vector3.zero)).ToFloatList();
							}
							else
							{
								p.body.position = lastFrame.GetPlayer(p.userid).body.position;
							}


							p.body.Rotation = playerPoseBitmask[3]
								? input.ReadSmallestThree()
								: lastFrame.GetPlayer(p.userid).body.Rotation;


							if (playerPoseBitmask[4])
							{
								p.lhand.pos =
									(input.ReadVector3Half() +
									 (lastFrame?.GetPlayer(p.userid)?.lhand.Position ?? Vector3.zero))
									.ToFloatArray()
									.ToList();
							}
							else
							{
								p.lhand.pos = lastFrame.GetPlayer(p.userid).lhand.pos;
							}

							p.lhand.Rotation = playerPoseBitmask[5]
								? input.ReadSmallestThree()
								: lastFrame.GetPlayer(p.userid).lhand.Rotation;

							if (playerPoseBitmask[6])
							{
								p.rhand.pos =
									(input.ReadVector3Half() +
									 (lastFrame?.GetPlayer(p.userid)?.rhand.Position ?? Vector3.zero))
									.ToFloatArray()
									.ToList();
							}
							else
							{
								p.rhand.pos = lastFrame.GetPlayer(p.userid).rhand.pos;
							}

							p.rhand.Rotation = playerPoseBitmask[7]
								? input.ReadSmallestThree()
								: lastFrame.GetPlayer(p.userid).rhand.Rotation;

							f.teams[i].players.Add(p);
						}
					}

					if (isKeyframe) lastKeyframe = f;
					lastFrame = f;
					l.Add(f);
				}
			}

			return l;
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
	}

	public static class BitConverterExtensions
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
				bytes.AddRange(BitConverter.GetBytes((Half)val));
			}

			return bytes.ToArray();
		}

		/// <summary>
		/// Converts a Vector3 to Halfs and then to bytes
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static byte[] GetHalfBytes(this Vector3 value)
		{
			List<byte> bytes = new List<byte>();
			bytes.AddRange(BitConverter.GetBytes((Half)value.x));
			bytes.AddRange(BitConverter.GetBytes((Half)value.y));
			bytes.AddRange(BitConverter.GetBytes((Half)value.z));
			return bytes.ToArray();
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
			Vector3 p = new Vector3(
				(float)reader.ReadHalf(),
				(float)reader.ReadHalf(),
				(float)reader.ReadHalf()
			);
			(p.z, p.x) = (p.x, p.z);

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
				possession_time = (float)reader.ReadHalf(),
				stuns = reader.ReadUInt16(),
			};
			return stats;
		}


		public static Vector3 ReadVector3Half(this BinaryReader reader)
		{
			Vector3 p = new Vector3
			{
				x = (float)reader.ReadHalf(),
				y = (float)reader.ReadHalf(),
				z = (float)reader.ReadHalf()
			};
			(p.x, p.z) = (p.z, p.x);

			return p;
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
			
			float f4 = Mathf.Sqrt(1 - f1 * f1 - f2 * f2 - f3 * f3);
			return maxIndex switch
			{
				0 => new Quaternion(f4, f1, f2, f3),
				1 => new Quaternion(f1, f4, f2, f3),
				2 => new Quaternion(f1, f2, f4, f3),
				3 => new Quaternion(f1, f2, f3, f4),
				_ => throw new Exception("Invalid index")
			};
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
		
		public static Half ReadHalf(this BinaryReader reader)
		{
			ushort s = reader.ReadUInt16();
			Half h = new Half
			{
				RawValue = s
			};
			return h;
		}
	}
}
