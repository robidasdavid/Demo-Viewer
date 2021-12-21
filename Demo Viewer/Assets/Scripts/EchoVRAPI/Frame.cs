using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace EchoVRAPI
{
	// ReSharper disable InconsistentNaming
	// ReSharper disable UnusedAutoPropertyAccessor.Global
	// ReSharper disable UnusedMember.Global
	// ReSharper disable MemberCanBePrivate.Global
	// ReSharper disable PropertyCanBeMadeInitOnly.Global
	// ReSharper disable UnassignedField.Global
	// ReSharper disable ClassNeverInstantiated.Global

	/// <summary>
	/// A recreation of the JSON object given by EchoVR
	/// https://github.com/Ajedi32/echovr_api_docs
	/// </summary>
	public class Frame
	{
		/// <summary>
		/// This isn't in the api, just useful for recorded data.
		/// The time this frame was fetched from the API.
		/// </summary>
		[JsonIgnore]
		public DateTime recorded_time { get; set; }

		/// <summary>
		/// Disc object at the given instance.
		/// </summary>
		public Disc disc { get; set; }

		public LastThrow last_throw { get; set; }
		public string sessionid { get; set; }
		public bool orange_team_restart_request { get; set; }
		public string sessionip { get; set; }

		/// <summary>
		/// The current state of the match
		/// { pre_match, round_start, playing, score, round_over, pre_sudden_death, sudden_death, post_sudden_death, post_match }
		/// </summary>
		public string game_status { get; set; }

		/// <summary>
		/// Game time as displayed in game.
		/// </summary>
		public string game_clock_display { get; set; }

		/// <summary>
		/// Time of remaining in match (in seconds)
		/// </summary>
		public float game_clock { get; set; }

		[JsonIgnore] public bool inLobby => map_name == "mpl_lobby_b2";

		public string match_type { get; set; }
		public string map_name { get; set; }
		public bool private_match { get; set; }
		public int orange_points { get; set; }
		public int total_round_count { get; set; }
		public int blue_round_score { get; set; }
		public int orange_round_score { get; set; }
		public VRPlayer player { get; set; }
		public Pause pause { get; set; }

		/// <summary>
		/// List of integers to determine who currently has possession.
		/// [ team, player ]
		/// </summary>
		public List<int> possession { get; set; }

		public bool tournament_match { get; set; }
		public bool left_shoulder_pressed { get; set; }
		public bool right_shoulder_pressed { get; set; }
		public bool left_shoulder_pressed2 { get; set; }
		public bool right_shoulder_pressed2 { get; set; }
		public bool blue_team_restart_request { get; set; }

		/// <summary>
		/// Name of the oculus username recording.
		/// </summary>
		public string client_name { get; set; }

		public int blue_points { get; set; }

		/// <summary>
		/// Object containing data from the last goal made.
		/// </summary>
		public LastScore last_score { get; set; }

		public List<Team> teams { get; set; }

		[JsonIgnore]
		public List<Team> playerTeams =>
			new List<Team>
			{
				teams[0], teams[1]
			};

		/// <summary>
		/// Gets all the g_Player objects from both teams
		/// </summary>
		public List<Player> GetAllPlayers(bool includeSpectators = false)
		{
			List<Player> list = new List<Player>();
			list.AddRange(teams[0].players);
			list.AddRange(teams[1].players);
			if (includeSpectators)
			{
				list.AddRange(teams[2].players);
			}

			return list;
		}

		/// <summary>
		/// Get a player from all players their name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public Player GetPlayer(string name)
		{
			foreach (Team t in teams)
			{
				foreach (Player p in t.players)
				{
					if (p.name == name) return p;
				}
			}

			return null;
		}

		/// <summary>
		/// Get a player from all players their userid.
		/// </summary>
		/// <param name="userid"></param>
		/// <returns></returns>
		public Player GetPlayer(long userid)
		{
			foreach (Team t in teams)
			{
				foreach (Player p in t.players)
				{
					if (p.userid == userid) return p;
				}
			}

			return null;
		}

		public Team GetTeam(string player_name)
		{
			foreach (Team t in teams)
			{
				foreach (Player p in t.players)
				{
					if (p.name == player_name) return t;
				}
			}

			return null;
		}

		public Team GetTeam(long userid)
		{
			foreach (Team t in teams)
			{
				foreach (Player p in t.players)
				{
					if (p.userid == userid) return t;
				}
			}

			return null;
		}

		public Team.TeamColor GetTeamColor(long userid)
		{
			foreach (Team t in teams)
			{
				foreach (Player p in t.players)
				{
					if (p.userid == userid) return t.color;
				}
			}

			return Team.TeamColor.spectator;
		}
		
		

		public (Vector3, Quaternion) GetCameraTransform()
		{
			return (
				player.vr_position.ToVector3(),
				Math2.QuaternionLookRotation(player.vr_forward.ToVector3(), player.vr_up.ToVector3()) 
			);
		}

		/// <summary>
		/// ↔ Mixes the two frames with a linear interpolation based on t
		/// For binary or int values, the "from" frame is preferred.
		/// </summary>
		/// <param name="from">The start frame</param>
		/// <param name="to">The next frame</param>
		/// <param name="t">The DateTime of the playhead</param>
		/// <returns>A mix of the two frames</returns>
		public static Frame Lerp(Frame from, Frame to, DateTime t)
		{
			if (from.recorded_time == to.recorded_time) return from;
			if (from.recorded_time > to.recorded_time)
			{
				Console.WriteLine("From frame is after To frame");
				return null;
			}

			if (from.recorded_time > t) return from;
			if (to.recorded_time < t) return to;

			// the ratio between the frames
			float lerpValue =
				(float) ((t - from.recorded_time).TotalSeconds /
				         (to.recorded_time - from.recorded_time).TotalSeconds);

			Frame newFrame = new Frame()
			{
				recorded_time = t,

				disc = Disc.Lerp(from.disc, to.disc, lerpValue),
				sessionid = from.sessionid,
				orange_points = from.orange_points,
				private_match = from.private_match,
				client_name = from.client_name,
				game_clock_display = from.game_clock_display, // TODO this could be interpolated
				player = VRPlayer.Lerp(from.player, to.player, lerpValue),
				game_status = from.game_status,
				game_clock = Math2.Lerp(from.game_clock, to.game_clock, lerpValue),
				match_type = from.match_type,

				map_name = from.map_name,
				possession = from.possession,
				tournament_match = from.tournament_match,
				blue_points = from.blue_points,
				last_score = from.last_score
			};

			int numTeams = Math.Max(from.teams.Count, to.teams.Count);

			newFrame.teams = new List<Team>(numTeams);

			for (int i = 0; i < numTeams; i++)
			{
				if (from.teams.Count <= i &&
				    to.teams.Count > i)
				{
					newFrame.teams[i] = to.teams[i];
				}
				else if (to.teams.Count <= i && from.teams.Count > i)
				{
					newFrame.teams[i] = from.teams[i];
				}
				else if (from.teams.Count > i &&
				         to.teams.Count > i)
				{
					// actually lerp the team
					newFrame.teams.Add(Team.Lerp(from.teams[i], to.teams[i], lerpValue));
				}
			}

			return newFrame;
		}
		
		public static Frame FromEchoReplayString(string line)
		{
			if (!string.IsNullOrEmpty(line))
			{
				string[] splitJSON = line.Split('\t');
				string onlyJSON, onlyTime;
				if (splitJSON.Length == 2)
				{
					onlyJSON = splitJSON[1];
					onlyTime = splitJSON[0];
				}
				else
				{
					Debug.LogError("Row doesn't include both a time and API JSON");
					return null;
				}

				if (onlyTime.Length == 23 && onlyTime[13] == '.')
				{
					StringBuilder sb = new StringBuilder(onlyTime)
					{
						[13] = ':',
						[16] = ':'
					};
					onlyTime = sb.ToString();
				}
			
				if (!DateTime.TryParse(onlyTime, out DateTime frameTime))
				{
					Debug.LogError($"Can't parse date: {onlyTime}");
					return null;
				}

				// if this is actually valid arena data
				if (onlyJSON.Length > 800)
				{
					return FromJSON(frameTime, onlyJSON);
				}
				else
				{
					Debug.LogError("Row is not arena data.");
					return null;
				}
			}
			else
			{
				Debug.LogError("String is empty");
				return null;
			}
		}
		
		/// <summary>
		/// Creates a frame from json and a timestamp
		/// </summary>
		/// <param name="time">The time the frame was recorded</param>
		/// <param name="json">The json for the frame</param>
		/// <returns>A Frame object</returns>
		public static Frame FromJSON(DateTime time, string json)
		{
			if (string.IsNullOrEmpty(json)) return null;

			try
			{
				Frame frame = JsonConvert.DeserializeObject<Frame>(json);
				frame.recorded_time = time;
				return frame;
			} catch (JsonReaderException ex)
			{
				Console.WriteLine(ex);
				return null;
			}
		}
	}

	public static class Math2
	{
		public static float Lerp(float from, float to, float t)
		{
			// TODO verify
			float diff = to - from;
			t *= diff;
			t += from;
			return t;
		}

		public static float Clamp01(float f)
		{
			if (f > 1) return 1;
			if (f < 0) return 0;
			return f;
		}

		public static Quaternion QuaternionLookRotation(Vector3 forward, Vector3 up)
		{
			forward /= forward.magnitude;

			Vector3 vector = Vector3.Normalize(forward);
			Vector3 vector2 = Vector3.Normalize(Vector3.Cross(up, vector));
			Vector3 vector3 = Vector3.Cross(vector, vector2);
			var m00 = vector2.x;
			var m01 = vector2.y;
			var m02 = vector2.z;
			var m10 = vector3.x;
			var m11 = vector3.y;
			var m12 = vector3.z;
			var m20 = vector.x;
			var m21 = vector.y;
			var m22 = vector.z;


			float num8 = (m00 + m11) + m22;
			var quaternion = new Quaternion();
			if (num8 > 0f)
			{
				var num = (float) Math.Sqrt(num8 + 1f);
				quaternion.w = num * 0.5f;
				num = 0.5f / num;
				quaternion.x = (m12 - m21) * num;
				quaternion.y = (m20 - m02) * num;
				quaternion.z = (m01 - m10) * num;
				return quaternion;
			}

			if ((m00 >= m11) && (m00 >= m22))
			{
				var num7 = (float) Math.Sqrt(((1f + m00) - m11) - m22);
				var num4 = 0.5f / num7;
				quaternion.x = 0.5f * num7;
				quaternion.y = (m01 + m10) * num4;
				quaternion.z = (m02 + m20) * num4;
				quaternion.w = (m12 - m21) * num4;
				return quaternion;
			}

			if (m11 > m22)
			{
				var num6 = (float) Math.Sqrt(((1f + m11) - m00) - m22);
				var num3 = 0.5f / num6;
				quaternion.x = (m10 + m01) * num3;
				quaternion.y = 0.5f * num6;
				quaternion.z = (m21 + m12) * num3;
				quaternion.w = (m20 - m02) * num3;
				return quaternion;
			}

			var num5 = (float) Math.Sqrt(((1f + m22) - m00) - m11);
			var num2 = 0.5f / num5;
			quaternion.x = (m20 + m02) * num2;
			quaternion.y = (m21 + m12) * num2;
			quaternion.z = 0.5f * num5;
			quaternion.w = (m01 - m10) * num2;
			return quaternion;
		}
	}


	/// <summary>
	/// Custom Vector3 class used to keep track of 3D coordinates.
	/// Works more like the Vector3 included with Unity now.
	/// </summary>
	public static class Vector3Extensions
	{
		public static Vector3 ToVector3(this List<float> input)
		{
			if (input.Count != 3)
			{
				throw new Exception("Can't convert List to Vector3. There must be 3 elements.");
			}

			return new Vector3(input[2], input[1], input[0]);
		}

		public static Vector3 ToVector3(this float[] input)
		{
			if (input.Length != 3)
			{
				throw new Exception("Can't convert array to Vector3. There must be 3 elements.");
			}

			return new Vector3(input[2], input[1], input[0]);
		}

		public static Vector3 ToVector3Backwards(this float[] input)
		{
			if (input.Length != 3)
			{
				throw new Exception("Can't convert array to Vector3");
			}

			return new Vector3(input[0], input[1], input[2]);
		}

		public static Vector3 ToVector3Backwards(this List<float> input)
		{
			if (input.Count != 3)
			{
				throw new Exception("Can't convert array to Vector3");
			}

			return new Vector3(input[0], input[1], input[2]);
		}

		public static float[] ToFloatArray(this Vector3 vector3)
		{
			return new float[]
			{
				vector3.z,
				vector3.y,
				vector3.x
			};
		}

		public static List<float> ToFloatList(this Vector3 vector3)
		{
			return new List<float>
			{
				vector3.z,
				vector3.y,
				vector3.x
			};
		}

		public static float DistanceTo(this Vector3 v1, Vector3 v2)
		{
			return (float) Math.Sqrt(Math.Pow(v1.x - v2.x, 2) + Math.Pow(v1.y - v2.y, 2) + Math.Pow(v1.z - v2.z, 2));
		}

		public static Vector3 Normalized(this Vector3 v1)
		{
			return v1 / v1.magnitude;
		}


		/// <summary>
		/// converts this quaternion to its forward vector
		/// </summary>
		public static Vector3 Forward(this Quaternion q)
		{
			return new Vector3(
				2 * (q.x * q.z + q.w * q.y), 
				2 * (q.y * q.z - q.w * q.x),
				1 - 2 * (q.x * q.x + q.y * q.y));
		}

		/// <summary>
		/// converts this quaternion to its forward vector
		/// </summary>
		public static Vector3 ForwardBackwards(this Quaternion q)
		{
			return new Vector3(
				1 - 2 * (q.x * q.x + q.y * q.y), 
				2 * (q.y * q.z - q.w * q.x),
				2 * (q.x * q.z + q.w * q.y));
		}

		/// <summary>
		/// converts this quaternion to its left vector
		/// </summary>
		public static Vector3 Left(this Quaternion q)
		{
			return Vector3.Cross(q.Up(), q.Forward());
		}

		/// <summary>
		/// converts this quaternion to its up vector
		/// </summary>
		public static Vector3 Up(this Quaternion q)
		{
			return new Vector3(
				2 * (q.x * q.y - q.w * q.z), 
				1 - 2 * (q.x * q.x + q.z * q.z),
				2 * (q.y * q.z + q.w * q.x));
		}

		/// <summary>
		/// converts this quaternion to its up vector
		/// </summary>
		public static Vector3 UpBackwards(this Quaternion q)
		{
			return new Vector3(
				2 * (q.y * q.z + q.w * q.x),
				1 - 2 * (q.x * q.x + q.z * q.z),
				2 * (q.x * q.y - q.w * q.z));
		}
	}
}
