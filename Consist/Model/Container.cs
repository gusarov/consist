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
		private readonly Dictionary<string, Record> _records = new Dictionary<string, Record>();
		private readonly Dictionary<string, Record> _recordsVirtual = new Dictionary<string, Record>();

		public Record Get(string key)
		{
			if (_recordsVirtual.TryGetValue(key, out var record))
			{
				return record;
			}
			_records.TryGetValue(key, out record);
			return record;
		}
		public IEnumerable<Record> GetAll()
		{
			return _records.Values.Concat(_recordsVirtual.Values);
		}

		public void Put(Record rec)
		{
			if (rec.KeyPath.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
			{
				_recordsVirtual.Add(rec.KeyPath, rec);
			}
			else
			{
				_records.Add(rec.KeyPath, rec);
			}

			UpdateParentFolder(rec);
			// Console.WriteLine($"{rec.KeyPath} {rec.Hash}");
		}

		void UpdateParentFolder(Record rec)
		{
			var parent = Path.GetDirectoryName(rec.KeyPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
			if (!parent.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				parent += Path.DirectorySeparatorChar;
			}
			var item = Get(parent);
			if (item == null)
			{
				item = new Record(parent)
				{
					SubRecords = new List<Record>(),
				};
				_recordsVirtual.Add(parent, item);
				if (parent != Path.DirectorySeparatorChar.ToString() &&
				    parent != Path.AltDirectorySeparatorChar.ToString())
				{
					UpdateParentFolder(item);
				}
			}

			item.SubRecords.Add(rec);
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
						rec.Value.Save(bw, ctx);
						/*
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
						*/
					}

				}
				file.SetLength(file.Position);
			}
		}

		public static Container LoadFrom(string metadataFile)
		{
			var cont = new Container();
			cont.Load(metadataFile);
			return cont;
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
						// _records.Add(rec.KeyPath, rec);
						Put(rec);
					}
				}
			}
		}
	}
}
