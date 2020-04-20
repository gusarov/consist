using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Consist.Model
{
	public class StorageContext
	{
		public StorageContext(int hashLen)
		{
			HashLen = hashLen;
		}
		public int HashLen { get; }
		public string CurrentFolder { get; set; }
	}

	public class Container : ISpace
	{
		public string Path { get; set; }

		private readonly Dictionary<string, Record> _records = new Dictionary<string, Record>();

		public Record Get(string key)
		{
			return _records[key];
		}

		public void Put(Record rec)
		{
			_records.Add(rec.KeyPath, rec);
			// Console.WriteLine($"{rec.KeyPath} {rec.Hash}");
		}

		public int GetSize() =>
			_records.Select(x => x.Value.GetSize()).Sum()
			;

		public void Save(string metadataFile)
		{
			using (var file = File.OpenWrite(metadataFile))
			{
				using (var bw = new BinaryWriter(file, new UTF8Encoding(false, false), true))
				{
					bw.Write("Consist Metadata"); // marker
					bw.Write((byte)1); // ver
					bw.Write((byte)16); // md5 len

					var ctx = new StorageContext(16);
					foreach (var rec in _records.OrderBy(x => x.Key))
					{
						var folder = System.IO.Path.GetDirectoryName(rec.Key);
						if (ctx.CurrentFolder == folder)
						{
							rec.Value.Save(bw, true);
						}
						else
						{
							rec.Value.Save(bw, false);
						}
						ctx.CurrentFolder = folder;
					}

				}
				file.SetLength(file.Position);
			}
		}
		public void Load(string metadataFile)
		{
			using (var file = File.OpenRead(metadataFile))
			{
				using (var br = new BinaryReader(file))
				{
					var marker = br.ReadString();
					if (marker != "Consist Metadata")
					{
						throw new Exception("The file is not Consist Metadata file");
					}

					var ver = br.ReadByte();
					if (ver != 1)
					{
						throw new Exception("Version not supported");
					}

					var hashLen = br.ReadByte();

					var ctx = new StorageContext(hashLen)
					{
						
					};
					while (file.Position != file.Length)
					{
						var rec = Record.Load(br, ctx);
						_records.Add(rec.KeyPath, rec);
					}
				}
			}
		}
	}
}
