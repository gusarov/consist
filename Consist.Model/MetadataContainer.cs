using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Consist.Model
{
	public class MetadataContainer : ISpace
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

		internal void SetMetadata(IEnumerable<MetadataRecord> records)
		{
			var types = new HashSet<MetadataRecordType>(records.Select(x => x.MetadataRecordType));

			foreach (var record in _metadata.Where(x => types.Contains(x.MetadataRecordType)).ToArray())
			{
				_metadata.Remove(record);
			}
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

		private readonly List<MetadataRecord> _metadata = new List<MetadataRecord>();

		public IList<MetadataRecord> Metadata => _metadata;


		public void Save()
		{
			if (_metadataFile == null)
			{
				throw new Exception("You should specify file name to SaveAs, or Load from file first");
			}
			Save(_metadataFile);
		}

		public void Save(string metadataFile)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(metadataFile));
			using (var file = File.OpenWrite(metadataFile))
			{
				_metadataFile = metadataFile;
				long flagPos;
				using (var bw = new BinaryWriter(file, new UTF8Encoding(false, false), true))
				{
					bw.Write("Consist Metadata"); // marker
					bw.Flush(); // to read position correctly
					flagPos = bw.BaseStream.Position;
					bw.Write((byte)0); // transaction flag. 0 - transaction in progress. 1 - transaction completed
					bw.Write((byte)1); // ver
					bw.Write((byte)16); // md5 len

					var ctx = new StorageContext(1, 16);

					if (Metadata.Count >= 64) throw new Exception();
					bw.Write((byte)Metadata.Count); // meta len
					foreach (var meta in Metadata)
					{
						meta.Save(bw, ctx);
					}

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
				file.Flush(); // flush first as a separate stage
				file.Position = flagPos;
				file.WriteByte(1); // set transaction flag to completed in the final stage
			}
		}

		public static MetadataContainer LoadFrom(string metadataFile)
		{
			var cont = new MetadataContainer();
			cont.Load(metadataFile);
			return cont;
		}

		string _metadataFile;

		public void Load(string metadataFile)
		{
			_metadataFile = metadataFile;
			if (!File.Exists(metadataFile))
			{
				return;
			}

			using (var file = File.OpenRead(metadataFile))
			{
				using (var br = new BinaryReader(file))
				{
					var marker = br.ReadString();
					if (marker != "Consist Metadata")
					{
						throw new Exception("The file is not Consist Metadata file");
					}

					var tranFlag = br.ReadByte();
					if ((tranFlag & 1) == 0)
					{
						throw new Exception("File corrupted, previous transaction aborted");
					}
					var ver = br.ReadByte();
					if (ver != 1)
					{
						throw new Exception("Version not supported");
					}

					var hashLen = br.ReadByte();

					var ctx = new StorageContext(ver, hashLen)
					{
					};

					var metaLen = br.ReadByte();
					if (metaLen >= 64) throw new Exception();
					for (var i = 0; i < metaLen; i++)
					{
						Metadata.Add(MetadataRecord.Read(br, ctx));
					}

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
