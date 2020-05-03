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

		public string LocalRootPath => Metadata
			.LastOrDefault(x => x.MetadataRecordType == MetadataRecordType.OriginalPath)?.Value;
		// last is the most recent container value when opened

		public Record TryGet(string key)
		{
			_records.TryGetValue(key, out var record);
			return record;
		}

		public Record GetOrCreate(string key, Record parent = null)
		{
			if (!_records.TryGetValue(key, out var record))
			{
				if (parent != null && !parent.IsFolder)
				{
					throw new Exception("parent must be a folder");
				}

				if (parent == null)
				{
					parent = GetOrCreateParent(key);
				}
				else
				{
#if DEBUG
					if (!ReferenceEquals(parent, GetOrCreateParent(key)))
					{
						throw new Exception("parent is not corresponding to the key");
					}
#endif
				}
				if (parent != null && !parent.IsFolder)
				{
					throw new Exception("parent must be a folder");
				}
				_records[key] = record = new Record(this, parent, key);

				// UpdateParentFolder(record, parent); // Record ctor will add to parent
			}
			return record;
		}

		public IEnumerable<Record> GetAll()
		{
			return _records.Values/*.Concat(_recordsVirtual.Values)*/;
		}

		public Record Add(Record parent, string keyPath)
		{
			var rec = new Record(this, parent, keyPath);
			Add(rec);
			return rec;
		}

		public void Add(Record rec)
		{
			if (rec.Container != this)
			{
				throw new Exception("Can only add records that are bind to this container");
			}
			if (rec.SubRecords != null)
			{
				throw new Exception("Injecting a record with subfolders! Don't do this, I'm managing it myself");
			}

			// _records.TryGetValue(rec.KeyPath, out var old);
			_records.Add(rec.KeyPath, rec);
			// if (old != null) { rec.SubRecords = old.SubRecords; }

			/*
			if (rec.KeyPath.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
			{
				_recordsVirtual.Add(rec.KeyPath, rec);
			}
			else
			{
				_records.Add(rec.KeyPath, rec);
			}


			*/

			if (rec.Parent != null)
			{
				UpdateParentFolder(rec, rec.Parent);
			}

			/*
			Notifier.Instance.NotifyItemScanned(new ItemScannedEventArgs
			{
				Container = this,
				NewRecord = rec,
			});
			*/
			// Console.WriteLine($"{rec.KeyPath} {rec.Hash}");
		}

		internal void SetMetadata(MetadataRecordType type, IEnumerable<string> values)
		{
			// var types = new HashSet<MetadataRecordType>(records.Select(x => x.MetadataRecordType));

			foreach (var record in _metadata.Where(x => type==x.MetadataRecordType).ToArray())
			{
				_metadata.Remove(record);
			}

			foreach (var val in values)
			{
				_metadata.Add(new MetadataRecord(type, val));
			}
		}

		Record GetOrCreateParent(string itemKey)
		{
			/*
			var fileSystemEntryName = itemKey.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

			if (fileSystemEntryName.Length == 0)
			{
				return null;
			}

			var parentKey = Path.GetDirectoryName(fileSystemEntryName);
			if (!parentKey.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				parentKey += Path.DirectorySeparatorChar;
			}
			*/

			var parentKey = KeyPath.GetParent(itemKey);
			if (parentKey == null)
			{
				return null;
			}
			return GetOrCreate(parentKey);
		}

		void UpdateParentFolder(Record rec, Record parent)
		{
			if (parent == null)
			{
				throw new Exception("Currently is mandatory in Record constructor");
				parent = TryGet(KeyPath.GetParent(rec.KeyPath));
			}
			if (parent == null)
			{
				throw new Exception("Must have parent in container before adding this item!");
				/*
				item = new Record(parent)
				{
				};
				// _recordsVirtual.Add(parent, item);
				// if (parent != Path.DirectorySeparatorChar.ToString() &&  parent != Path.AltDirectorySeparatorChar.ToString())
				{
					UpdateParentFolder(item, null);
				}
				*/
			}

			if (parent.SubRecords == null)
			{
				parent.SubRecords = new List<Record>();
			}

			/*
			if (!item.SubRecords.Remove(old))
			{
				old = null;
			}
			*/

			lock (parent.SubRecords)
			{
#if DEBUG
				if (parent.SubRecords.Any(x => x.KeyPath == rec.KeyPath))
				{
					throw new Exception("Key path already exists in parent folder");
				}
#endif
				parent.SubRecords.Add(rec);
			}

			/*

			Notifier.Instance.NotifyItemScanned(new ItemScannedEventArgs
			{
				Container = this,
				Parent = item,
				NewChildren = rec,
			});

			if (old == null) // notify new elements. Updates does not need to be notified
			{
			}

			*/
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
			if (Path.IsPathRooted(metadataFile))
			{
				Directory.CreateDirectory(Path.GetDirectoryName(metadataFile));
			}

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

					var ctx = new StorageContext(this, 1, 16);

					if (Metadata.Count >= 64) throw new Exception();
					bw.Write((byte)Metadata.Count); // meta len
					foreach (var meta in Metadata)
					{
						meta.Save(bw, ctx);
					}

					foreach (var rec in _records.OrderBy(x => x.Key))
					{
						rec.Value.Save(bw, ctx);
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

					var ctx = new StorageContext(this, ver, hashLen)
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
						Record.Load(br, ctx);
						// _records.Add(rec.KeyPath, rec);
						// Add(rec);
					}
				}
			}
		}
	}
}
