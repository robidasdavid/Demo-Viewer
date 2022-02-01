using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
#if UNITY
using UnityEngine;
using Debug = UnityEngine.Debug;
using Transform = EchoVRAPI.Transform;
#else
using System.Numerics;
#endif
using EchoVRAPI;
using Half = SystemHalf.Half;

namespace ButterReplays
{
	public class ButterFrame
	{
		private readonly Frame frame;

		/// <summary>
		/// The previous frame shouldn't be null except for the first frame.
		/// </summary>
		public readonly ButterFrame lastFrame;

		/// <summary>
		/// The previous frame in this chunk. Should be null for keyframes.
		/// </summary>
		private ButterFrame lastFrameInChunk;

		private readonly int frameIndex;
		public bool IsKeyframe => frameIndex % butterHeader.keyframeInterval == 0 || lastFrame == null;
		private readonly ButterHeader butterHeader;

		private byte[] _pauseAndRestartsBytes;
		private byte[] _inputBytes;
		private byte[] _lastScoreBytes;
		private byte[] _lastThrowBytes;
		private byte[] _vrPlayerBytes;
		private byte[] _discBytes;

		/// <summary>
		/// Creates a new Butter frame from a decompressed frame class
		/// </summary>
		/// <param name="frame">The original data</param>
		/// <param name="frameIndex">The index of the current frame in this file</param>
		/// <param name="lastFrame">Previous butter frame to do diffs with. Pass in null to avoid diffs</param>
		/// <param name="butterHeader">The butter header, which contains player dictionaries</param>
		public ButterFrame(Frame frame, int frameIndex, ButterFrame lastFrame, ButterHeader butterHeader)
		{
			this.frame = frame;
			this.frameIndex = frameIndex;
			this.lastFrame = lastFrame;
			this.butterHeader = butterHeader;
		}

		private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public byte[] GetBytes()
		{
			using MemoryStream memoryStream = new MemoryStream();
			using BinaryWriter writer = new BinaryWriter(memoryStream);

			writer.Write(IsKeyframe ? (ushort)0xFEFC : (ushort)0xFEFE);

			lastFrameInChunk = IsKeyframe ? null : lastFrame;

			Debug.Assert(lastFrameInChunk != null || IsKeyframe);

			if (IsKeyframe)
			{
				// TODO make sure this is switched to UTC
				writer.Write((long)(frame.recorded_time - UnixEpoch).TotalMilliseconds);
			}
			else
			{
				Debug.Assert(lastFrameInChunk != null, nameof(lastFrameInChunk) + " != null");
				writer.Write((ushort)((frame.recorded_time - lastFrameInChunk.frame.recorded_time)
					.TotalMilliseconds));
			}

			writer.Write((float)(frame.game_clock - (lastFrameInChunk?.frame.game_clock ?? 0)));

			List<bool> inclusionBits = new List<bool>()
			{
				frame.InArena && frame.game_status != lastFrameInChunk?.frame.game_status,
				frame.InArena && frame.blue_points != lastFrameInChunk?.frame.blue_points ||
				frame.InArena && frame.orange_points != lastFrameInChunk?.frame.orange_points,
				!PauseAndRestartsBytes.SameAs(lastFrameInChunk?.PauseAndRestartsBytes),
				!InputBytes.SameAs(lastFrameInChunk?.InputBytes),
				frame.InArena && !LastScoreBytes.SameAs(lastFrameInChunk?.LastScoreBytes),
				!LastThrowBytes.SameAs(lastFrameInChunk?.LastThrowBytes),
				!VrPlayerBytes.SameAs(lastFrameInChunk?.VrPlayerBytes),
				frame.InArena && !DiscBytes.SameAs(lastFrameInChunk?.DiscBytes)
			};
			writer.Write(inclusionBits.GetBitmasks()[0]);


			if (inclusionBits[0])
			{
				writer.Write(GameStatusToByte(frame.game_status));
			}

			if (inclusionBits[1])
			{
				writer.Write((byte)frame.blue_points);
				writer.Write((byte)frame.orange_points);
			}

			// Pause and restarts
			if (inclusionBits[2])
			{
				writer.Write(PauseAndRestartsBytes);
			}

			// Inputs
			if (inclusionBits[3])
			{
				writer.Write(InputBytes);
			}

			// Last Score
			if (inclusionBits[4])
			{
				writer.Write(LastScoreBytes);
			}

			// Last Throw
			if (inclusionBits[5])
			{
				writer.Write(LastThrowBytes);
			}

			// VR Player
			if (inclusionBits[6])
			{
				writer.Write(VrPlayerBytes);
			}

			// Disc
			if (inclusionBits[7])
			{
				writer.Write(DiscBytes);
			}

			List<bool> teamDataBools = new List<bool>
			{
				frame.teams[0].possession,
				frame.teams[1].possession,
				
				// TODO check team stats diff
				// Team stats included
				frame.teams[0]?.stats != null && !StatsBytes(frame.teams[0].stats)
					.SameAs(StatsBytes(lastFrameInChunk?.frame.teams[0].stats)),
				frame.teams[1]?.stats != null && !StatsBytes(frame.teams[1].stats)
					.SameAs(StatsBytes(lastFrameInChunk?.frame.teams[1].stats)),
				frame.teams[2]?.stats != null && !StatsBytes(frame.teams[2].stats)
					.SameAs(StatsBytes(lastFrameInChunk?.frame.teams[2].stats)),
			};
			writer.Write(teamDataBools.GetBitmasks()[0]);


			// add team data
			for (int i = 0; i < 3; i++)
			{
				if (teamDataBools[i+2])
				{
					writer.Write(StatsBytes(frame.teams[i].stats, lastFrameInChunk?.frame.teams[i].stats));
				}

				writer.Write((byte)(frame.teams[i]?.players?.Count ?? 0));
				if ((frame.teams[i]?.players?.Count ?? 0) > 0)
				{
					foreach (Player player in frame.teams[i].players)
					{
						writer.Write(butterHeader.GetPlayerIndex(player.name));
						writer.Write((byte)player.playerid);

						Player lastFramePlayer = lastFrameInChunk?.frame.GetPlayer(player.userid);
						Player lastLastFramePlayer = lastFrameInChunk?.lastFrameInChunk?.frame.GetPlayer(player.userid);

#if UNITY
						Vector3 vel = player.velocity.ToVector3() - (lastFramePlayer?.velocity.ToVector3() ?? Vector3.zero);
#else
						Vector3 vel = player.velocity.ToVector3() - (lastFramePlayer?.velocity.ToVector3() ?? Vector3.Zero);
#endif


						List<bool> playerStateBitmask = new List<bool>()
						{
							player.possession,
							player.blocking,
							player.stunned,
							player.invulnerable,
							!StatsBytes(player.stats).SameAs(StatsBytes(lastFramePlayer?.stats)),
							lastFramePlayer == null || !(player.ping == lastFramePlayer.ping &&
							                             Math.Abs(player.packetlossratio -
							                                      lastFramePlayer.packetlossratio) < float.Epsilon),
							lastFramePlayer == null || !(player.holding_left == lastFramePlayer.holding_left &&
							                             player.holding_right == lastFramePlayer.holding_right),
#if UNITY
							vel.sqrMagnitude > .0001f
#else
							vel.LengthSquared() > .0001f
#endif
						};
						writer.Write(playerStateBitmask.GetBitmasks()[0]);


						// player stats
						if (playerStateBitmask[4])
						{
							writer.Write(StatsBytes(player.stats, lastFramePlayer?.stats));
						}

						// ping/packetloss
						if (playerStateBitmask[5])
						{
							writer.Write((ushort)(player.ping - (lastFramePlayer?.ping ?? 0)));
							writer.WriteHalf((Half)(player.packetlossratio - (lastFramePlayer?.packetlossratio ?? 0)));
						}

						// holding
						if (playerStateBitmask[6])
						{
							if (!frame.InCombat)
							{
								writer.Write(butterHeader.HoldingToByte(player.holding_left));
								writer.Write(butterHeader.HoldingToByte(player.holding_right));
							}
						}

						// velocity
						if (playerStateBitmask[7])
						{
							writer.Write(vel.GetHalfBytes());
						}

						byte[] headBytes = PoseToBytes(player.head - lastFramePlayer?.head);
						byte[] bodyBytes = PoseToBytes(player.body - lastFramePlayer?.body);
						byte[] lHandBytes = PoseToBytes(player.lhand - lastFramePlayer?.lhand);
						byte[] rHandBytes = PoseToBytes(player.rhand - lastFramePlayer?.rhand);

						byte[] lastHeadBytes = PoseToBytes(lastFramePlayer?.head - lastLastFramePlayer?.head);
						byte[] lastBodyBytes = PoseToBytes(lastFramePlayer?.body - lastLastFramePlayer?.body);
						byte[] lastLHandBytes = PoseToBytes(lastFramePlayer?.lhand - lastLastFramePlayer?.lhand);
						byte[] lastRHandBytes = PoseToBytes(lastFramePlayer?.rhand - lastLastFramePlayer?.rhand);


						List<bool> playerPoseBitmask = new List<bool>()
						{
							// lastFramePlayer == null || Vector3.DistanceSquared(player.head.Position, lastFramePlayer.head.Position) > .0001f,
							!headBytes.SameAs(lastHeadBytes),
							!headBytes.Skip(6).ToArray().SameAs(lastHeadBytes?.Skip(6).ToArray()),

							// lastFramePlayer == null ||
							// Vector3.DistanceSquared(player.body.Position - player.head.Position,
							// 	lastFramePlayer.body.Position - lastFramePlayer.head.Position) > .0001f,
							!bodyBytes.SameAs(lastBodyBytes),
							!bodyBytes.Skip(6).ToArray().SameAs(lastBodyBytes?.Skip(6).ToArray()),

							// lastFramePlayer == null ||
							// Vector3.DistanceSquared(player.lhand.Position - player.head.Position,
							// 	lastFramePlayer.lhand.Position - lastFramePlayer.head.Position) > .0001f,
							!lHandBytes.SameAs(lastLHandBytes),
							!lHandBytes.Skip(6).ToArray().SameAs(lastLHandBytes?.Skip(6).ToArray()),

							// lastFramePlayer == null ||
							// Vector3.DistanceSquared(player.rhand.Position - player.head.Position,
							// 	lastFramePlayer.rhand.Position - lastFramePlayer.head.Position) > .0001f,
							!rHandBytes.SameAs(lastRHandBytes),
							!rHandBytes.Skip(6).ToArray().SameAs(lastRHandBytes?.Skip(6).ToArray()),
						};
						writer.Write(playerPoseBitmask.GetBitmasks()[0]);

						// write poses
						// head
						if (playerPoseBitmask[0]) writer.Write(headBytes.Take(6).ToArray());
						if (playerPoseBitmask[1]) writer.Write(headBytes.Skip(6).ToArray());
						// body
						if (playerPoseBitmask[2]) writer.Write(bodyBytes.Take(6).ToArray());
						if (playerPoseBitmask[3]) writer.Write(bodyBytes.Skip(6).ToArray());
						// lhand
						if (playerPoseBitmask[4]) writer.Write(lHandBytes.Take(6).ToArray());
						if (playerPoseBitmask[5]) writer.Write(lHandBytes.Skip(6).ToArray());
						// rhand
						if (playerPoseBitmask[6]) writer.Write(rHandBytes.Take(6).ToArray());
						if (playerPoseBitmask[7]) writer.Write(rHandBytes.Skip(6).ToArray());
						
						// Combat loadout
						if (frame.InCombat)
						{
							byte loadoutByte = (byte)Enum.Parse(typeof(ButterFile.Weapon), player.Weapon);
							loadoutByte |= (byte)((byte) Enum.Parse(typeof(ButterFile.Ordnance), player.Ordnance) << 2);
							loadoutByte |= (byte)((byte) Enum.Parse(typeof(ButterFile.TacMod), player.TacMod) << 4);
							loadoutByte |= (byte)((byte) Enum.Parse(typeof(ButterFile.Arm), player.Arm) << 6);
							writer.Write(loadoutByte);
						}
					}
				}
			}

			// add bone data
			bool bonesIncluded = frame.bones?.user_bones != null;
			byte boneInclusion = bonesIncluded ? (byte)1 : (byte)0;
			if (bonesIncluded)
			{
				// add the number of players
				boneInclusion |= (byte)((ushort)frame.bones.user_bones.Length << 1);
			}

			writer.Write(boneInclusion);
			if (bonesIncluded)
			{
				// bones are diffed with the matching index, which may not be the same player with joins/leaves
				// last frame
				Bones lastBones = lastFrame?.frame.bones;
				if (lastBones?.user_bones.Length != frame.bones.user_bones.Length)
				{
					lastBones = null;
				}

				// for each player
				for (int playerIndex = 0; playerIndex < frame.bones.user_bones.Length; playerIndex++)
				{
					BonePlayer player = frame.bones.user_bones[playerIndex];
					BonePlayer lastPlayer = lastBones?.user_bones[playerIndex];

					List<byte> posBytes = new List<byte>();
					List<byte> rotBytes = new List<byte>();
					List<bool> posInclusion = new List<bool>();
					List<bool> rotInclusion = new List<bool>();

					(Vector3, Quaternion)[] poses = player.GetPoses();
					(Vector3, Quaternion)[] lastPoses = lastPlayer?.GetPoses();

					// for each bone in player
					for (int poseIndex = 0; poseIndex < poses.Length; poseIndex++)
					{
						(Vector3 p, Quaternion q) = poses[poseIndex];
						(Vector3 lastP, Quaternion lastQ) = lastPoses?[poseIndex] ?? (UniversalUnityExtensions.UniversalVector3Zero(), UniversalUnityExtensions.UniversalQuaternionIdentity());

						Vector3 posDiff = p - lastP;
						Quaternion rotDiff = q * Quaternion.Inverse(lastQ);

						if (posDiff.UniversalLength() > .001f)
						{
							posInclusion.Add(true);
							posBytes.AddRange(posDiff.GetHalfBytes(-2, 2, 14));
						}
						else
						{
							posInclusion.Add(false);
						}

						if (UniversalUnityExtensions.QuaternionAngle(q, lastQ) > 1f)
						{
							rotInclusion.Add(true);
							rotBytes.AddRange(SmallestThree(rotDiff));
						}
						else
						{
							rotInclusion.Add(false);
						}
					}

					writer.Write(posInclusion.GetBitmasks());
					writer.Write(rotInclusion.GetBitmasks());
					writer.Write(posBytes.ToArray());
					writer.Write(rotBytes.ToArray());
				}
			}

			writer.Flush();
			return memoryStream.ToArray();
		}

		private static byte[] StatsBytes(Stats stats, Stats lastStats = null)
		{
			if (stats == null) return Array.Empty<byte>();
			List<byte> bytes = new List<byte>();

			if (stats.stuns > ushort.MaxValue) throw new Exception("Too many stuns to fit.");

			bytes.Add((byte)(stats.assists - (lastStats?.assists ?? 0)));
			bytes.Add((byte)(stats.blocks - (lastStats?.blocks ?? 0)));
			bytes.Add((byte)(stats.catches - (lastStats?.catches ?? 0)));
			bytes.Add((byte)(stats.goals - (lastStats?.goals ?? 0)));
			bytes.Add((byte)(stats.interceptions - (lastStats?.interceptions ?? 0)));
			bytes.Add((byte)(stats.passes - (lastStats?.passes ?? 0)));
			bytes.Add((byte)(stats.points - (lastStats?.points ?? 0)));
			bytes.Add((byte)(stats.saves - (lastStats?.saves ?? 0)));
			bytes.Add((byte)(stats.steals - (lastStats?.steals ?? 0)));
			bytes.Add((byte)(stats.shots_taken - (lastStats?.shots_taken ?? 0)));

			bytes.AddRange(ButterFile.GetHalfBytes((Half)(stats.possession_time - (lastStats?.possession_time ?? 0))));
#if UNITY
			bytes.AddRange(BitConverter.GetBytes((ushort)Mathf.Clamp(stats.stuns - (lastStats?.stuns ?? 0), 0, ushort.MaxValue)));
#else
			bytes.AddRange(BitConverter.GetBytes((ushort)Math.Clamp(stats.stuns - (lastStats?.stuns ?? 0), 0, ushort.MaxValue)));
#endif
			return bytes.ToArray();
		}

		private byte[] DiscBytes
		{
			get
			{
				if (frame.InCombat) return Array.Empty<byte>();
				if (_discBytes == null)
				{
					List<byte> bytes = new List<byte>();

					bytes.AddRange(PoseToBytes(
#if UNITY
						frame.disc.Position - (lastFrameInChunk?.frame.disc.Position ?? Vector3.zero),
#else
						frame.disc.Position - (lastFrameInChunk?.frame.disc.Position ?? Vector3.Zero),
#endif
						frame.disc.Rotation
					));

					bytes.AddRange((frame.disc.velocity.ToVector3() -
#if UNITY
					                (lastFrameInChunk?.frame.disc.velocity.ToVector3() ?? Vector3.zero))
#else
					                (lastFrameInChunk?.frame.disc.velocity.ToVector3() ?? Vector3.Zero))
#endif
						.GetHalfBytes());

					_discBytes = bytes.ToArray();
				}

				return _discBytes;
			}
		}

		private byte[] VrPlayerBytes
		{
			get
			{
				if (_vrPlayerBytes == null)
				{
					List<byte> bytes = new List<byte>();

					bytes.AddRange(PoseToBytes((frame.player - lastFrameInChunk?.frame.player).ToTransform()));

					_vrPlayerBytes = bytes.ToArray();
				}

				return _vrPlayerBytes;
			}
		}

		private byte[] LastThrowBytes
		{
			get
			{
				if (_lastThrowBytes == null)
				{
					List<byte> bytes = new List<byte>();
					bytes.AddRange(ButterFile.GetHalfBytes((Half)frame.last_throw.arm_speed));
					bytes.AddRange(ButterFile.GetHalfBytes((Half)frame.last_throw.total_speed));
					bytes.AddRange(ButterFile.GetHalfBytes((Half)frame.last_throw.off_axis_spin_deg));
					bytes.AddRange(ButterFile.GetHalfBytes((Half)frame.last_throw.wrist_throw_penalty));
					bytes.AddRange(ButterFile.GetHalfBytes((Half)frame.last_throw.rot_per_sec));
					bytes.AddRange(ButterFile.GetHalfBytes((Half)frame.last_throw.pot_speed_from_rot));
					bytes.AddRange(ButterFile.GetHalfBytes((Half)frame.last_throw.speed_from_arm));
					bytes.AddRange(ButterFile.GetHalfBytes((Half)frame.last_throw.speed_from_movement));
					bytes.AddRange(ButterFile.GetHalfBytes((Half)frame.last_throw.speed_from_wrist));
					bytes.AddRange(ButterFile.GetHalfBytes((Half)frame.last_throw.wrist_align_to_throw_deg));
					bytes.AddRange(ButterFile.GetHalfBytes((Half)frame.last_throw.throw_align_to_movement_deg));
					bytes.AddRange(ButterFile.GetHalfBytes((Half)frame.last_throw.off_axis_penalty));
					bytes.AddRange(ButterFile.GetHalfBytes((Half)frame.last_throw.throw_move_penalty));
					_lastThrowBytes = bytes.ToArray();
				}

				return _lastThrowBytes;
			}
		}

		private byte[] LastScoreBytes
		{
			get
			{
				if (frame.InCombat) return Array.Empty<byte>();
				if (_lastScoreBytes == null)
				{
					List<byte> bytes = new List<byte>();
					byte b = TeamToTeamIndex(frame.last_score.team);
					b |= (byte)((frame.last_score.point_amount == 2 ? 0 : 1) << 2);
					if (!Enum.TryParse(frame.last_score.goal_type.Replace(" ", "_"), out ButterFile.GoalType type))
					{
						type = ButterFile.GoalType.unknown;
					}

					b |= (byte)((byte)type << 3);
					bytes.Add(b);

					bytes.Add(butterHeader.GetPlayerIndex(frame.last_score.person_scored));
					bytes.Add(butterHeader.GetPlayerIndex(frame.last_score.assist_scored));
					bytes.AddRange(ButterFile.GetHalfBytes((Half)frame.last_score.disc_speed));
					bytes.AddRange(ButterFile.GetHalfBytes((Half)frame.last_score.distance_thrown));
					_lastScoreBytes = bytes.ToArray();
				}

				return _lastScoreBytes;
			}
		}

		private byte[] InputBytes
		{
			get
			{
				if (_inputBytes == null)
				{
					List<byte> bytes = new List<byte>();
					List<bool> bools = new List<bool>()
					{
						frame.left_shoulder_pressed,
						frame.right_shoulder_pressed,
						frame.left_shoulder_pressed2,
						frame.right_shoulder_pressed2,
					};
					bytes.AddRange(bools.GetBitmasks());
					_inputBytes = bytes.ToArray();
				}

				return _inputBytes;
			}
		}

		private byte[] PauseAndRestartsBytes
		{
			get
			{
				if (_pauseAndRestartsBytes == null)
				{
					List<byte> bytes = new List<byte>();
					byte pauses = 0;
					pauses |= (byte)((frame.blue_team_restart_request ? 1 : 0) << 0);
					pauses |= (byte)((frame.orange_team_restart_request ? 1 : 0) << 1);
					pauses |= (byte)(TeamToTeamIndex(frame.pause.paused_requested_team) << 2);
					pauses |= (byte)(TeamToTeamIndex(frame.pause.unpaused_team) << 4);
					pauses |= (byte)(PausedStateToByte(frame.pause.paused_state) << 6);
					bytes.Add(pauses);

					bytes.AddRange(ButterFile.GetHalfBytes((Half)(frame.pause.paused_timer - lastFrameInChunk?.frame.pause.paused_timer ?? 0)));
					bytes.AddRange(ButterFile.GetHalfBytes((Half)(frame.pause.unpaused_timer - lastFrameInChunk?.frame.pause.unpaused_timer ?? 0)));
					_pauseAndRestartsBytes = bytes.ToArray();
				}

				return _pauseAndRestartsBytes;
			}
		}


		/// <summary>
		/// This is only 2 bits
		/// </summary>
		public static byte TeamToTeamIndex(string team)
		{
			return team switch
			{
				"blue" => 0,
				"orange" => 1,
				"spectator" => 2,
				"none" => 3,
				_ => throw new Exception("Team index failed to parse")
			};
		}

		/// <summary>
		/// This is only 2 bits
		/// </summary>
		public static string TeamIndexToTeam(byte b)
		{
			return b switch
			{
				0 => "blue",
				1 => "orange",
				2 => "spectator",
				3 => "none",
				_ => throw new Exception("Team index failed to parse")
			};
		}

		/// <summary>
		/// This is only 2 bits
		/// </summary>
		public static byte PausedStateToByte(string team)
		{
			return team switch
			{
				"unpaused" => 0,
				"paused" => 1,
				"unpausing" => 2,
				"pausing" => 3, // TODO exists?
				_ => 3
			};
		}


		/// <summary>
		/// This is only 2 bits
		/// </summary>
		public static string ByteToPausedState(byte b)
		{
			return b switch
			{
				0 => "unpaused",
				1 => "paused",
				2 => "unpausing",
				3 => "pausing", // TODO exists?
				_ => "NOT FOUND"
			};
		}

		public static byte GameStatusToByte(string gameStatus)
		{
			return gameStatus switch
			{
				"" => 1,
				"pre_match" => 2,
				"round_start" => 3,
				"playing" => 4,
				"score" => 5,
				"round_over" => 6,
				"post_match" => 7,
				"pre_sudden_death" => 8,
				"sudden_death" => 9,
				"post_sudden_death" => 10,
				_ => 0
			};
		}

		public static string ByteToGameStatus(byte gameStatusByte)
		{
			return gameStatusByte switch
			{
				1 => "",
				2 => "pre_match",
				3 => "round_start",
				4 => "playing",
				5 => "score",
				6 => "round_over",
				7 => "post_match",
				8 => "pre_sudden_death",
				9 => "sudden_death",
				10 => "post_sudden_death",
				_ => "NOT FOUND"
			};
		}

		public static byte[] PoseToBytes(Transform transform)
		{
			if (transform == null) return null;
			return PoseToBytes(
				transform.Position,
				transform.Rotation
			);
		}

		/// <summary>
		/// 10 bytes
		/// </summary>
		public static byte[] PoseToBytes(Vector3 pos, Quaternion rot)
		{
			List<byte> bytes = new List<byte>();
			bytes.AddRange(pos.GetHalfBytes());
			bytes.AddRange(SmallestThree(rot));

			return bytes.ToArray();
		}

		public static byte[] SmallestThree(Quaternion q)
		{
#if UNITY
			q = Quaternion.LookRotation(q.ForwardBackwards(), q.UpBackwards());
#endif
			// make an array of the components
			float[] components =
			{
#if UNITY
				q.x, q.y, q.z, q.w
#else
				q.X, q.Y, q.Z, q.W
#endif
			};

			// find component with largest absolute value
			float max = 0;
			int maxIndex = 0;
			int sign = 1;
#if UNITY
			float[] absComponents = components.Select(Mathf.Abs).ToArray();
#else
			float[] absComponents = components.Select(MathF.Abs).ToArray();
#endif
			for (int i = 0; i < 4; i++)
			{
				if (absComponents[i] > max)
				{
					max = absComponents[i];
					maxIndex = i;
					sign = components[i] < 0 ? -1 : 1;
				}
			}

			// This is 32 bits, which is used to store the rotation
			uint data = 0;

			// store the index as 2 bits
			data |= (byte)maxIndex;

			// store the other three components as 10-bit numbers
			int j = 0;
			for (int i = 0; i < 4; i++)
			{
				if (i != maxIndex)
				{
					ushort val = (ushort)((components[i] * sign + 0.70710678) / 1.41421356 * 1023);
					data |= (uint)(val << ((j++ * 10) + 2));
				}
			}

			return BitConverter.GetBytes(data);
		}
	}
}