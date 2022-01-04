using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using EchoVRAPI;
using Newtonsoft.Json;

namespace ButterReplays
{
	public static class EchoReplay
	{
		public static void SaveReplay(string fileName, List<Frame> frames)
		{
			// write the frames directly into a zip
			using MemoryStream memoryStream = new MemoryStream();
			using (ZipArchive archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
			{
				ZipArchiveEntry zipContents = archive.CreateEntry(Path.GetFileName(fileName));

				using (Stream entryStream = zipContents.Open())
				{
					using (StreamWriter streamWriter = new StreamWriter(entryStream))
					{
						foreach (Frame f in frames)
						{
							string s = JsonConvert.SerializeObject(f);
							streamWriter.WriteLine(f.recorded_time.ToString("yyyy/MM/dd HH:mm:ss.fff") + "\t" + s);
						}
					}
				}
			}

			using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
			{
				memoryStream.Seek(0, SeekOrigin.Begin);
				memoryStream.CopyTo(fileStream);
			}
		}


		public static List<Frame> ReadReplayFile(StreamReader fileReader)
		{
			bool fileFinishedReading = false;
			List<Frame> readFrames = new List<Frame>();

			using (fileReader = OpenOrExtract(fileReader))
			{
				while (!fileFinishedReading)
				{
					if (fileReader == null) continue;

					string rawJson = fileReader.ReadLine();
					if (rawJson == null)
					{
						fileFinishedReading = true;
						fileReader.Close();
					}
					else
					{
						string[] splitJson = rawJson.Split('\t');
						string onlyBones = null, onlyJson, onlyTime;
						switch (splitJson.Length)
						{
							case 3:
								onlyBones = splitJson[2];
								onlyJson = splitJson[1];
								onlyTime = splitJson[0];
								break;
							case 2:
								onlyJson = splitJson[1];
								onlyTime = splitJson[0];
								break;
							default:
								Console.WriteLine("Row doesn't include both a time and API JSON");
								continue;
						}

						DateTime frameTime = DateTime.Parse(onlyTime);

						// if this is actually valid arena data
						if (onlyJson.Length <= 300) continue;

						try
						{
							Frame foundFrame = JsonConvert.DeserializeObject<Frame>(onlyJson);
							if (onlyBones != null)
							{
								Bones bones = JsonConvert.DeserializeObject<Bones>(onlyBones);
								foundFrame.bones = bones;
							}

							if (foundFrame != null)
							{
								foundFrame.recorded_time = frameTime;
								readFrames.Add(foundFrame);
							}
						}
						catch (Exception)
						{
							Console.WriteLine("Couldn't read frame. File is corrupted.");
						}
					}
				}
			}

			return readFrames;
		}

		public static StreamReader OpenOrExtract(StreamReader reader)
		{
			char[] buffer = new char[2];
			reader.Read(buffer, 0, buffer.Length);
			reader.DiscardBufferedData();
			reader.BaseStream.Seek(0, SeekOrigin.Begin);
			if (buffer[0] != 'P' || buffer[1] != 'K') return reader;
			ZipArchive archive = new ZipArchive(reader.BaseStream);
			StreamReader ret = new StreamReader(archive.Entries[0].Open());
			return ret;
		}
	}
}