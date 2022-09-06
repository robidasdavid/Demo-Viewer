using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
#if UNITY
using UnityEngine;
using Transform = EchoVRAPI.Transform;
#else
using System.Numerics;
#endif
using EchoVRAPI;

namespace ButterReplays
{
	public static class DecompressorV2
	{
		private const byte version = 2;
		public static List<Frame> FromBytes(byte formatVersion, BinaryReader fileInput, ref float readProgress)
		{
			if (formatVersion != version)
			{
				// Debug.WriteLine("Wrong version.");
				return null;
			}
			
			List<Frame> l = new List<Frame>();

			ushort keyframeInterval = fileInput.ReadUInt16();
			ButterFile.CompressionFormat compressionFormat = (ButterFile.CompressionFormat) fileInput.ReadByte();

			ButterFile b = new ButterFile(keyframeInterval, compressionFormat);
			b.header.formatVersion = formatVersion;

			Frame firstFrame = new Frame
			{
				client_name = fileInput.ReadASCIIString(),
				sessionid = fileInput.ReadSessionId(),
				sessionip = fileInput.ReadIpAddress()
			};

			byte playerCount = fileInput.ReadByte();
			for (int i = 0;
			     i < playerCount;
			     i++)
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
			firstFrame.map_name = ((ButterFile.MapName) (mapByte >> 1)).ToString();
			firstFrame.match_type = ButterFile.MatchType(firstFrame.map_name, firstFrame.private_match);


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
			ZstdNet.Decompressor decompressor = new ZstdNet.Decompressor();
#endif

			// reads one frame at a time
			// while (!fileInput.EOF())
			for (int chunkIndex = 0; chunkIndex < numChunks; chunkIndex++)
			{
				// if the last chunk is empty - nframes was divisible by chunk size
				if (chunkSizes[chunkIndex] == 0 && chunkSizes.Length - 2 <= chunkIndex) break;
				byte[] compressedChunk = fileInput.ReadBytes((int) chunkSizes[chunkIndex]);
				byte[] uncompressedChunk;
				switch (b.header.compression)
				{
					case ButterFile.CompressionFormat.none:
						uncompressedChunk = compressedChunk;
						break;
					case ButterFile.CompressionFormat.gzip:
						uncompressedChunk = ButterFile.UnzipBytes(compressedChunk);
						break;
					case ButterFile.CompressionFormat.zstd_3:
					case ButterFile.CompressionFormat.zstd_7:
					case ButterFile.CompressionFormat.zstd_15:
					case ButterFile.CompressionFormat.zstd_22:
					case ButterFile.CompressionFormat.zstd_7_dict:
#if ZSTD
						uncompressedChunk = decompressor.Unwrap(compressedChunk);
						break;
#else
						throw new Exception("Zstd not available.");
#endif
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
						game_clock = input.ReadSingle() + (isKeyframe ? 0 : lastFrame.game_clock)
					};

					f.game_clock_display = f.game_clock.ToGameClockDisplay();
					List<bool> inclusionBitmask = input.ReadByte().GetBitmaskValues();

					f.game_status = inclusionBitmask[0]
						? ButterFrame.ByteToGameStatus(input.ReadByte())
						: lastFrame?.game_status; // lastFrame could be null if this is Combat

					if (inclusionBitmask[1])
					{
						f.blue_points = input.ReadByte();
						f.orange_points = input.ReadByte();
					}
					else
					{
						f.blue_points = lastFrame?.blue_points ?? 0;
						f.orange_points = lastFrame?.orange_points ?? 0;
					}

					// Pause and restarts
					if (inclusionBitmask[2])
					{
						byte pauses = input.ReadByte();
						f.blue_team_restart_request = (pauses & 0b1) > 0;
						f.orange_team_restart_request = (pauses & 0b10) > 0;
						f.pause = new Pause
						{
							paused_requested_team = ButterFrame.TeamIndexToTeam((byte) ((pauses & 0b1100) >> 2)),
							unpaused_team = ButterFrame.TeamIndexToTeam((byte) ((pauses & 0b110000) >> 4)),
							paused_state = ButterFrame.ByteToPausedState((byte) ((pauses & 0b11000000) >> 6)),
							paused_timer = input.ReadSystemHalf(),
							unpaused_timer = input.ReadSystemHalf(),
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
							team = ButterFrame.TeamIndexToTeam((byte) (lastScoreByte & 0b11)),
							point_amount = (lastScoreByte & 0b100) > 0 ? 3 : 2,
							goal_type = ((ButterFile.GoalType) ((lastScoreByte & 0b11111000) >> 3)).ToString()
								.Replace("_", " "),
							person_scored = b.header.GetPlayerName(input.ReadByte()),
							assist_scored = b.header.GetPlayerName(input.ReadByte()),
							disc_speed = input.ReadSystemHalf(),
							distance_thrown = input.ReadSystemHalf(),
						};
					}
					else
					{
						f.last_score = lastFrame?.last_score;
					}

					// Last Throw
					if (inclusionBitmask[5])
					{
						f.last_throw = new LastThrow
						{
							arm_speed = input.ReadSystemHalf(),
							total_speed = input.ReadSystemHalf(),
							off_axis_spin_deg = input.ReadSystemHalf(),
							wrist_throw_penalty = input.ReadSystemHalf(),
							rot_per_sec = input.ReadSystemHalf(),
							pot_speed_from_rot = input.ReadSystemHalf(),
							speed_from_arm = input.ReadSystemHalf(),
							speed_from_movement = input.ReadSystemHalf(),
							speed_from_wrist = input.ReadSystemHalf(),
							wrist_align_to_throw_deg = input.ReadSystemHalf(),
							throw_align_to_movement_deg = input.ReadSystemHalf(),
							off_axis_penalty = input.ReadSystemHalf(),
							throw_move_penalty = input.ReadSystemHalf()
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
						p += lastFrame?.player.Position ?? UniversalUnityExtensions.UniversalVector3Zero();
						f.player = new VRPlayer
						{
							vr_position = p.ToFloatList(),
							vr_forward = q.Forward().ToFloatList(),
							vr_left = q.Left().ToFloatList(),
							vr_up = q.Up().ToFloatList(),
						};
					}
					else
					{
						f.player = lastFrame.player;
					}

					// Disc
					if (inclusionBitmask[7])
					{
						(Vector3 p, Quaternion q) = input.ReadPose();

						p += lastFrame?.disc.position.ToVector3() ??
#if UNITY
						     Vector3.zero;
#else
						     Vector3.Zero;
#endif


						f.disc = new Disc
						{
							position = p.ToFloatArray().ToList(),
							forward = q.Forward().ToFloatArray().ToList(),
							left = q.Left().ToFloatArray().ToList(),
							up = q.Up().ToFloatArray().ToList(),
							velocity = new List<float>()
							{
								input.ReadSystemHalf() + (lastFrame?.disc.velocity[0] ?? 0),
								input.ReadSystemHalf() + (lastFrame?.disc.velocity[1] ?? 0),
								input.ReadSystemHalf() + (lastFrame?.disc.velocity[2] ?? 0),
							}
						};
					}
					else
					{
						f.disc = lastFrame?.disc;
					}

					byte teamDataBitmask = input.ReadByte();
					List<bool> teamDataBools = teamDataBitmask.GetBitmaskValues();
					f.teams = new List<Team>()
					{
						new Team(),
						new Team(),
						new Team(),
					};
					f.teams[0].possession = teamDataBools[0];
					f.teams[1].possession = teamDataBools[1];

					// add team data
					for (int i = 0; i < 3; i++)
					{
						if (teamDataBools[i + 2])
						{
							f.teams[i].stats = input.ReadStats();
							// TODO diff
						}
						else
						{
							// these could just be 0 to start with, so we create a new Stats obj
							f.teams[i].stats = lastFrame?.teams[i].stats ?? new Stats();
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
								p.stats = lastFrame?.GetPlayer(p.userid)?.stats;
							}

							if (playerStateBitmask[5])
							{
								p.ping = input.ReadInt16() + (lastFrame?.GetPlayer(p.userid)?.ping ?? 0);
								p.packetlossratio = input.ReadSystemHalf() +
								                    (lastFrame?.GetPlayer(p.userid)?.packetlossratio ?? 0);
							}
							else
							{
								p.ping = lastFrame.GetPlayer(p.userid).ping;
								p.packetlossratio = lastFrame.GetPlayer(p.userid).packetlossratio;
							}

							if (playerStateBitmask[6])
							{
								if (!f.InCombat)
								{
									p.holding_left = b.header.ByteToHolding(input.ReadByte());
									p.holding_right = b.header.ByteToHolding(input.ReadByte());
								}
							}
							else
							{
								if (!f.InCombat)
								{
									p.holding_left = lastFrame.GetPlayer(p.userid).holding_left;
									p.holding_right = lastFrame.GetPlayer(p.userid).holding_right;
								}
							}

							if (playerStateBitmask[7])
							{
								p.velocity =
									(input.ReadVector3Half() + (lastFrame?.GetPlayer(p.userid)?.velocity?.ToVector3() ??
									                            UniversalUnityExtensions.UniversalVector3Zero()))
									.ToFloatArray().ToList();
							}
							else
							{
								p.velocity = lastFrame?.GetPlayer(p.userid)?.velocity ?? new List<float>() {0, 0, 0};
							}

							List<bool> playerPoseBitmask = input.ReadByte().GetBitmaskValues();

							p.head = new Transform();
							p.body = new Transform();
							p.lhand = new Transform();
							p.rhand = new Transform();

							if (playerPoseBitmask[0])
							{
								p.head.position =
									(input.ReadVector3Half() + (lastFrame?.GetPlayer(p.userid)?.head.Position ??
									                            UniversalUnityExtensions.UniversalVector3Zero()))
									.ToFloatList(); // welcome to weird formatting land
							}
							else
							{
								p.head.position = lastFrame.GetPlayer(p.userid).head.position;
							}

							p.head.Rotation = playerPoseBitmask[1]
								? input.ReadSmallestThree()
								: lastFrame?.GetPlayer(p.userid).head.Rotation ??
								  UniversalUnityExtensions.UniversalQuaternionIdentity();

							if (playerPoseBitmask[2])
							{
								p.body.position =
									(input.ReadVector3Half() +
									 (lastFrame?.GetPlayer(p.userid)?.body.Position ??
									  UniversalUnityExtensions.UniversalVector3Zero())).ToFloatList();
							}
							else
							{
								p.body.position = lastFrame?.GetPlayer(p.userid).body.position
								                  ?? UniversalUnityExtensions.UniversalVector3Zero().ToFloatList();
							}


							p.body.Rotation = playerPoseBitmask[3]
								? input.ReadSmallestThree()
								: lastFrame?.GetPlayer(p.userid).body.Rotation ??
								  UniversalUnityExtensions.UniversalQuaternionIdentity();


							if (playerPoseBitmask[4])
							{
								p.lhand.pos =
									(input.ReadVector3Half() +
									 (lastFrame?.GetPlayer(p.userid)?.lhand.Position
									  ?? UniversalUnityExtensions.UniversalVector3Zero())).ToFloatList();
							}
							else
							{
								p.lhand.pos = lastFrame?.GetPlayer(p.userid).lhand.pos
								              ?? UniversalUnityExtensions.UniversalVector3Zero().ToFloatList();
							}

							p.lhand.Rotation = playerPoseBitmask[5]
								? input.ReadSmallestThree()
								: lastFrame?.GetPlayer(p.userid).lhand.Rotation ??
								  UniversalUnityExtensions.UniversalQuaternionIdentity();

							if (playerPoseBitmask[6])
							{
								p.rhand.pos =
									(input.ReadVector3Half() +
									 (lastFrame?.GetPlayer(p.userid)?.rhand.Position ??
									  UniversalUnityExtensions.UniversalVector3Zero())).ToFloatList();
							}
							else
							{
								p.rhand.pos = lastFrame?.GetPlayer(p.userid).rhand.pos
								              ?? UniversalUnityExtensions.UniversalVector3Zero().ToFloatList();
							}

							p.rhand.Rotation = playerPoseBitmask[7]
								? input.ReadSmallestThree()
								: lastFrame?.GetPlayer(p.userid).rhand.Rotation ??
								  UniversalUnityExtensions.UniversalQuaternionIdentity();


							// Combat loadout
							if (f.InCombat)
							{
								byte loadoutByte = input.ReadByte();
								p.Weapon = ((ButterFile.Weapon) ((loadoutByte & (0b11 << 0)) >> 0)).ToString();
								p.Ordnance = ((ButterFile.Ordnance) ((loadoutByte & (0b11 << 2)) >> 2)).ToString();
								p.TacMod = ((ButterFile.TacMod) ((loadoutByte & (0b11 << 4)) >> 4)).ToString();
								p.Arm = ((ButterFile.Arm) ((loadoutByte & (0b1 << 6)) >> 6)).ToString();
							}

							f.teams[i].players.Add(p);
						}
					}

					// bone data
					byte boneHeaderByte = input.ReadByte();
					bool bonesIncluded = (boneHeaderByte & 1) == 1;
					ushort numBonePlayers = (ushort) (boneHeaderByte >> 1);
					if (bonesIncluded)
					{
						f.bones = new Bones
						{
							user_bones = new BonePlayer[numBonePlayers]
						};
						BonePlayer[] lastUserBones = lastFrame?.bones?.user_bones;

						// for each player
						for (int i = 0; i < numBonePlayers; i++)
						{
							f.bones.user_bones[i] = new BonePlayer
							{
								bone_o = new float[92],
								bone_t = new float[69]
							};

							List<bool> posInclusion = input.ReadBytes(3).GetBitmaskValues();
							List<bool> rotInclusion = input.ReadBytes(3).GetBitmaskValues();

							// for each bone in player
							for (int j = 0; j < 23; j++)
							{
								Vector3 lastPos = UniversalUnityExtensions.UniversalVector3Zero();
								if (lastUserBones?[i] != null)
								{
									lastPos = lastUserBones[i].GetPosition(j);
								}

								if (posInclusion[j])
								{
									f.bones.user_bones[i].SetPosition(j,
										lastPos + input.ReadVector3FixedPrecision(-2, 2, 14));
								}
								else
								{
									f.bones.user_bones[i].SetPosition(j, lastPos);
								}
							}

							for (int j = 0; j < 23; j++)
							{
								Quaternion lastRot = UniversalUnityExtensions.UniversalQuaternionIdentity();
								if (lastUserBones?[i] != null)
								{
									lastRot = lastUserBones[i].GetRotation(j);
								}

								if (rotInclusion[j])
								{
									Quaternion newRot = input.ReadSmallestThree();
									f.bones.user_bones[i].SetRotation(j, newRot * lastRot);
								}
								else
								{
									f.bones.user_bones[i].SetRotation(j, lastRot);
								}
							}
						}
					}

					if (isKeyframe) lastKeyframe = f;
					lastFrame = f;
					l.Add(f);
				}

				readProgress = (float) chunkIndex / numChunks;
			}

			return l;
		}
	}
}