// Pcx - Point cloud importer & renderer for Unity
// https://github.com/keijiro/Pcx

using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using unityutilities;

namespace Pcx
{
	[ScriptedImporter(1, "echoreplay")]
	class EchoReplayImporterEditor : ScriptedImporter
	{
		#region ScriptedImporter implementation

		public enum ContainerType
		{
			Mesh,
			ComputeBuffer,
			Texture
		}

		[SerializeField] ContainerType _containerType = ContainerType.Mesh;

		public override void OnImportAsset(AssetImportContext context)
		{
			if (_containerType == ContainerType.Mesh)
			{
				// Mesh container
				// Create a prefab with MeshFilter/MeshRenderer.
				var gameObject = new GameObject();
				var mesh = ImportAsMesh(context.assetPath);

				var meshFilter = gameObject.AddComponent<MeshFilter>();
				meshFilter.sharedMesh = mesh;

				var meshRenderer = gameObject.AddComponent<MeshRenderer>();
				meshRenderer.sharedMaterial = GetDefaultMaterial();

				context.AddObjectToAsset("prefab", gameObject);
				if (mesh != null) context.AddObjectToAsset("mesh", mesh);

				context.SetMainObject(gameObject);
			}
			else if (_containerType == ContainerType.ComputeBuffer)
			{
				// ComputeBuffer container
				// Create a prefab with PointCloudRenderer.
				var gameObject = new GameObject();
				var data = ImportAsPointCloudData(context.assetPath);

				var renderer = gameObject.AddComponent<PointCloudRenderer>();
				renderer.sourceData = data;

				context.AddObjectToAsset("prefab", gameObject);
				if (data != null) context.AddObjectToAsset("data", data);

				context.SetMainObject(gameObject);
			}
			else // _containerType == ContainerType.Texture
			{
				// Texture container
				// No prefab is available for this type.
				var data = ImportAsBakedPointCloud(context.assetPath);
				if (data != null)
				{
					context.AddObjectToAsset("container", data);
					context.AddObjectToAsset("position", data.positionMap);
					context.AddObjectToAsset("color", data.colorMap);
					context.SetMainObject(data);
				}
			}
		}

		#endregion

		#region Internal utilities

		static Material GetDefaultMaterial()
		{
			// Via package manager
			var path_upm = "Packages/jp.keijiro.pcx/Editor/Default Point.mat";
			// Via project asset database
			var path_prj = "Assets/Pcx/Editor/Default Point.mat";
			return AssetDatabase.LoadAssetAtPath<Material>(path_upm) ??
			       AssetDatabase.LoadAssetAtPath<Material>(path_prj);
		}

		#endregion

		#region Internal data structure

		enum DataProperty
		{
			Invalid,
			R8,
			G8,
			B8,
			A8,
			R16,
			G16,
			B16,
			A16,
			SingleX,
			SingleY,
			SingleZ,
			DoubleX,
			DoubleY,
			DoubleZ,
			Data8,
			Data16,
			Data32,
			Data64
		}

		static int GetPropertySize(DataProperty p)
		{
			switch (p)
			{
				case DataProperty.R8: return 1;
				case DataProperty.G8: return 1;
				case DataProperty.B8: return 1;
				case DataProperty.A8: return 1;
				case DataProperty.R16: return 2;
				case DataProperty.G16: return 2;
				case DataProperty.B16: return 2;
				case DataProperty.A16: return 2;
				case DataProperty.SingleX: return 4;
				case DataProperty.SingleY: return 4;
				case DataProperty.SingleZ: return 4;
				case DataProperty.DoubleX: return 8;
				case DataProperty.DoubleY: return 8;
				case DataProperty.DoubleZ: return 8;
				case DataProperty.Data8: return 1;
				case DataProperty.Data16: return 2;
				case DataProperty.Data32: return 4;
				case DataProperty.Data64: return 8;
			}

			return 0;
		}

		class DataHeader
		{
			public List<DataProperty> properties = new List<DataProperty>();
			public int vertexCount = -1;
		}

		class DataBody
		{
			public List<Vector3> vertices;
			public List<Color32> colors;
			public List<Vector3> normals;

			public DataBody(int vertexCount)
			{
				vertices = new List<Vector3>(vertexCount);
				colors = new List<Color32>(vertexCount);
				normals = new List<Vector3>(vertexCount);
			}

			public void AddPoint(
				float x, float y, float z,
				byte r, byte g, byte b, byte a
			)
			{
				vertices.Add(new Vector3(x, y, z));
				colors.Add(new Color32(r, g, b, a));
			}

			public void AddPoint(Vector3 vertex,
				byte r, byte g, byte b, byte a
			)
			{
				vertices.Add(vertex);
				colors.Add(new Color32(r, g, b, a));
			}
		}

		#endregion

		#region Reader implementation

		Mesh ImportAsMesh(string path)
		{
			try
			{
				FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
				List<string> lines = ReadReplayFile(new StreamReader(stream));
				DataBody body = ReadDataBody(lines);

				Mesh mesh = new Mesh
				{
					name = Path.GetFileNameWithoutExtension(path),
					indexFormat = body.vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
				};

				mesh.SetVertices(body.vertices);
				mesh.SetColors(body.colors);
				mesh.SetNormals(body.normals);

				mesh.SetIndices(
					Enumerable.Range(0, body.vertices.Count).ToArray(),
					MeshTopology.Points, 0
				);

				mesh.UploadMeshData(true);
				return mesh;
			}
			catch (Exception e)
			{
				Debug.LogError("Failed importing " + path + ". " + e.Message);
				return null;
			}
		}

		PointCloudData ImportAsPointCloudData(string path)
		{
			try
			{
				FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
				List<string> lines = ReadReplayFile(new StreamReader(stream));
				DataBody body = ReadDataBody(lines);
				PointCloudData data = ScriptableObject.CreateInstance<PointCloudData>();
				data.Initialize(body.vertices, body.colors);
				data.name = Path.GetFileNameWithoutExtension(path);
				return data;
			}
			catch (Exception e)
			{
				Debug.LogError("Failed importing " + path + ". " + e.Message);
				return null;
			}
		}

		BakedPointCloud ImportAsBakedPointCloud(string path)
		{
			try
			{
				FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
				List<string> lines = ReadReplayFile(new StreamReader(stream));
				DataBody body = ReadDataBody(lines);
				BakedPointCloud data = ScriptableObject.CreateInstance<BakedPointCloud>();
				data.Initialize(body.vertices, body.colors);
				data.name = Path.GetFileNameWithoutExtension(path);
				return data;
			}
			catch (Exception e)
			{
				Debug.LogError("Failed importing " + path + ". " + e.Message);
				return null;
			}
		}

		private static List<string> ReadReplayFile(StreamReader fileReader)
		{
			using (fileReader = Replay.OpenOrExtract(fileReader))
			{
				List<string> allLines = new List<string>();
				do
				{
					allLines.Add(fileReader.ReadLine());
				} while (!fileReader.EndOfStream);

				return allLines;
			}
		}

		[Serializable]
		class SimpleEchoVRAPIData
		{
			public Disc disc;
			public Team[] teams;

			[Serializable]
			public class Disc
			{
				public float[] position;
				public float[] forward;
				public float[] left;
				public float[] up;
				public float[] velocity;
				public int bounce_count;
			}

			[Serializable]
			public class Team
			{
				public Player[] players;
				public string team;

				[Serializable]
				public class Player
				{
					public EchoTransform head { get; set; }
					public float[] velocity;
					public string name;
				}
			}

			[Serializable]
			public class EchoTransform
			{
				public float[] pos;
				public float[] position;
				public float[] forward;
				public float[] left;
				public float[] up;


				[JsonIgnore]
				public Vector3 Position
				{
					get
					{
						if (pos != null) return new Vector3(pos[2], pos[1], pos[0]);
						else if (position != null) return new Vector3(position[2], position[1], position[0]);
						else
						{
							Debug.LogError("Neither pos nor position are set");
							throw new NullReferenceException("Neither pos nor position are set");
						}
					}
				}
			}
		}

		DataBody ReadDataBody(List<string> lines)
		{
			DataBody data = new DataBody(lines.Count);

			foreach (string line in lines)
			{
				// this converts the frame from raw json data to a deserialized object
				if (!string.IsNullOrEmpty(line))
				{
					string[] splitJSON = line.Split('\t');
					string onlyJSON = splitJSON[1];
					string onlyTime = splitJSON[0];

					// // check because of weird date formats
					// if (onlyTime.Length == 23 && onlyTime[13] == '.')
					// {
					// 	StringBuilder sb = new StringBuilder(onlyTime)
					// 	{
					// 		[13] = ':',
					// 		[16] = ':'
					// 	};
					// 	onlyTime = sb.ToString();
					// }
					//
					// if (!DateTime.TryParse(onlyTime, out DateTime frameTime))
					// {
					// 	Debug.LogError($"Can't parse date: {onlyTime}");
					// 	return null;
					// }

					// if this is actually valid arena data
					if (onlyJSON.Length > 800)
					{
						SimpleEchoVRAPIData frame = JsonConvert.DeserializeObject<SimpleEchoVRAPIData>(onlyJSON);
						for (int t = 0; t < 2; t++)
						{
							if (frame.teams[t].players == null) continue;
							foreach (SimpleEchoVRAPIData.Team.Player player in frame.teams[t].players)
							{
								Vector3 playerVel = new Vector3(player.velocity[2], player.velocity[1], player.velocity[0]);
								data.vertices.Add(player.head.Position);
								data.colors.Add(t == 1 ? new Color32(255, 136, 0, 255) : new Color32(0, 123, 255, 255));
								data.normals.Add(playerVel);
							}
						}

						if (frame.disc != null)
						{
							Vector3 discVel = new Vector3(frame.disc.velocity[2], frame.disc.velocity[1], frame.disc.velocity[0]);
							data.vertices.Add(new Vector3(frame.disc.position[2], frame.disc.position[1], frame.disc.position[0]));
							data.colors.Add(new Color32(255, 255, 255, 255));
							data.normals.Add(discVel);
						}
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

			return data;
		}
	}

	#endregion
}