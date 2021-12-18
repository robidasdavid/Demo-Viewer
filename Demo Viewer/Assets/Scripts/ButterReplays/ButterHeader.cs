using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EchoVRAPI;

namespace ButterReplays
{
		public class ButterHeader
		{
			public byte formatVersion = 3;
			public readonly ushort keyframeInterval = 300;

			public readonly ButterFile.CompressionFormat compression;


			public Frame firstFrame;

			public List<ushort> chunkSizes = new List<ushort>();
			public List<string> players;
			public List<int> numbers;
			public List<int> levels;
			public List<long> userids;


			public ButterHeader(ushort keyframeInterval, ButterFile.CompressionFormat compression)
			{
				chunkSizes = new List<ushort>();
				players = new List<string>();
				numbers = new List<int>();
				levels = new List<int>();
				userids = new List<long>();
				this.compression = compression;
				this.keyframeInterval = keyframeInterval;
			}

			public void ConsiderNewFrame(Frame frame)
			{
				firstFrame ??= frame;
				
				foreach (Team team in frame.teams)
				{
					if (team.players == null) continue;
					foreach (Player player in team.players)
					{
						if (!userids.Contains(player.userid))
						{
							players.Add(player.name);
							numbers.Add(player.number);
							levels.Add(player.level);
							userids.Add(player.userid);
						}
					}
				}
			}

			/// <summary>
			/// IPv4 Addresses can be stored as 4 bytes
			/// </summary>
			public static byte[] IpAddressToBytes(string ipAddress)
			{
				string[] parts = ipAddress.Split('.');
				byte[] bytes = new byte[4];

				if (parts.Length != 4)
				{
					throw new ArgumentException("IP Address doesn't have 4 parts.");
				}

				for (int i = 0; i < 4; i++)
				{
					bytes[i] = byte.Parse(parts[i]);
				}

				return bytes;
			}

			/// <summary>
			/// Converts a session id from a string into 16 bytes
			/// </summary>
			/// <returns></returns>
			public static byte[] SessionIdToBytes(string sessionId)
			{
				string str = sessionId.Replace("-", "");
				return StringToByteArrayFastest(str);
			}

			/// <summary>
			/// https://stackoverflow.com/a/9995303
			/// </summary>
			public static byte[] StringToByteArrayFastest(string hex)
			{
				if (hex.Length % 2 == 1)
					throw new Exception("The binary key cannot have an odd number of digits");

				byte[] arr = new byte[hex.Length >> 1];

				for (int i = 0; i < hex.Length >> 1; ++i)
				{
					arr[i] = (byte) ((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
				}

				return arr;
			}

			public static int GetHexVal(char hex)
			{
				int val = hex;
				return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
			}

			public static string ByteArrayToString(byte[] bytes)
			{
				return BitConverter.ToString(bytes).Replace("-", string.Empty);
			}

			public byte GetPlayerIndex(string playerName)
			{
				int index = players.IndexOf(playerName);
				return (byte) (index + 1);
			}


			public string GetPlayerName(byte playerIndex)
			{
				return playerIndex == 0 ? "INVALID PLAYER" : players[playerIndex - 1];
			}

			public int GetPlayerLevel(byte playerIndex)
			{
				return playerIndex == 0 ? -1 : levels[playerIndex - 1];
			}

			public int GetPlayerNumber(byte playerIndex)
			{
				return playerIndex == 0 ? -1 : numbers[playerIndex - 1];
			}

			public long GetUserId(byte playerIndex)
			{
				return playerIndex == 0 ? 0 : userids[playerIndex - 1];
			}

			public byte GetPlayerIndex(long userId)
			{
				int index = userids.IndexOf(userId);
				return (byte) (index + 1);
			}


			public byte HoldingToByte(string holding)
			{
				return holding switch
				{
					"none" => 255,
					"geo" => 254,
					"disc" => 253,
					_ => GetPlayerIndex(long.Parse(holding))
				};
			}

			public string ByteToHolding(byte holding)
			{
				return holding switch
				{
					255 => "none",
					254 => "geo",
					253 => "disc",
					_ => GetUserId(holding).ToString()
				};
			}

			public byte[] GetBytes()
			{
				using MemoryStream memoryStream = new MemoryStream();
				using BinaryWriter writer = new BinaryWriter(memoryStream);
				writer.Write(formatVersion);
				writer.Write(keyframeInterval);
				writer.Write((byte)compression);
				writer.Write(Encoding.ASCII.GetBytes(firstFrame.client_name));
				writer.Write((byte) 0);
				writer.Write(SessionIdToBytes(firstFrame.sessionid));
				writer.Write(IpAddressToBytes(firstFrame.sessionip));
				if (players.Count > 255) throw new Exception("Number of players doesn't fit in a byte.");
				writer.Write((byte) players.Count);
				foreach (string playerName in players)
				{
					writer.Write(Encoding.ASCII.GetBytes(playerName));
					writer.Write((byte) 0);
				}

				writer.Write(userids.GetBytes());
				writer.Write(numbers.GetByteBytes());
				writer.Write(levels.GetByteBytes());
				writer.Write((byte) firstFrame.total_round_count);
				if (firstFrame.blue_round_score > 127 ||
				    firstFrame.orange_round_score > 127)
				{
					throw new Exception("Round scores don't fit.");
				}

				byte roundScores = (byte) firstFrame.blue_round_score;
				roundScores += (byte) (firstFrame.orange_round_score << 4);
				writer.Write(roundScores);

				byte mapByte = firstFrame.private_match ? (byte) 1 : (byte) 0;
				mapByte += (byte) ((byte) Enum.Parse(typeof(ButterFile.MapName), firstFrame.map_name) << 1);
				writer.Write(mapByte);

				writer.Flush();
				return memoryStream.ToArray();
			}
		}

}