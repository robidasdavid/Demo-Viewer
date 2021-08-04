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
	[ScriptedImporter(1, "echocsv")]
	class EchoCSVImporterEditor : ScriptedImporter
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

		private class DataBody
		{
			public readonly List<Vector3> vertices;
			public readonly List<Color32> colors;
			public readonly List<Vector3> normals;

			public DataBody()
			{
				vertices = new List<Vector3>();
				colors = new List<Color32>();
				normals = new List<Vector3>();
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
				Debug.LogError($"Failed importing {path}.\n{e}");
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

		List<string> ReadReplayFile(StreamReader fileReader)
		{
			using (fileReader = DemoStart.OpenOrExtract(fileReader))
			{
				List<string> allLines = new List<string>();
				do
				{
					allLines.Add(fileReader.ReadLine());
				} while (!fileReader.EndOfStream && allLines.Count < 10000000);

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

		private DataBody ReadDataBody(List<string> lines)
		{
			DataBody data = new DataBody();

			foreach (string line in lines)
			{
				// this converts the frame from raw json data to a deserialized object
				if (!string.IsNullOrEmpty(line))
				{
					List<string> values = line.Split(',').ToList();
					if (string.IsNullOrEmpty(values.Last()))
					{
						values.RemoveAt(values.Count - 1);
					}

					// if this is actually valid arena data
					switch (values.Count)
					{
						case 3:
							data.vertices.Add(new Vector3(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2])));
							data.colors.Add(new Color32(255, 255, 255, 255));
							data.normals.Add(new Vector3(0, 0, 0));
							break;
						case 13:
						case 14:
							int index = int.Parse(values[6]);
							string map = values[7];
							string game_status = values[8];
							
							// 10,11,12 - n,b,o
							float timeSinceJoust = float.Parse(values[11]);
							Vector3 velocity = new Vector3(float.Parse(values[3]), float.Parse(values[4]), float.Parse(values[5]));
							if (timeSinceJoust > 5 || timeSinceJoust < 0) continue;
							if (index == 2) continue;	// don't add spectators
							if (velocity.z == 0) continue;	// don't add if vel is 0
							
							data.vertices.Add(new Vector3(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2])));
							switch (index)
							{
								case -1:
									data.colors.Add(new Color32(255, 255, 255, (byte)(timeSinceJoust/5*255)));
									break;
								case 0:
									data.colors.Add(new Color32(255, 136, 0, (byte)(timeSinceJoust/5*255)));
									break;
								case 1:
									data.colors.Add(new Color32(0, 123, 255, (byte)(timeSinceJoust/5*255)));
									break;
								case 2:
									data.colors.Add(new Color32(255, 255, 255, (byte)(timeSinceJoust/5*255)));
									break;
							}

							data.normals.Add(velocity);

							break;
						default:
							Debug.LogError("Row is not arena data.");
							break;
						// return null;
					}
				}
			}

			return data;
		}
	}

	#endregion
}