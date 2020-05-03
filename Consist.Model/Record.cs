using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using JetBrains.Annotations;

namespace Consist.Model
{
	public static class KeyPath
	{
		public static string GetParentOld(string keyPath)
		{
			var fileSystemEntryName = keyPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

			if (fileSystemEntryName.Length == 0)
			{
				return null;
			}

			var parentKey = Path.GetDirectoryName(fileSystemEntryName);
			if (!parentKey.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				parentKey += Path.DirectorySeparatorChar;
			}

			return parentKey;
		}

		public static string GetParent(string keyPath)
		{
			if (keyPath.Length == 1)
			{
				return null;
			}
			var slashIndex = keyPath.LastIndexOf('\\', keyPath.Length - 2);

			if (slashIndex < 0)
			{
				return null;
			}

			return keyPath.Substring(0, slashIndex + 1);
		}

		public static ReadOnlySpan<char> GetParent(ReadOnlySpan<char> keyPath)
		{
			if (keyPath.Length == 1)
			{
				return null;
			}

			var slashIndex = -1;
			for (int i = keyPath.Length-2; i >= 0; i--)
			{
				if (keyPath[i] == '\\')
				{
					slashIndex = i;
					break;
				}
			}
			// var slashIndex = keyPath.LastIndexOf('\\', keyPath.Length - 2);

			if (slashIndex < 0)
			{
				return null;
			}

			return keyPath.Slice(0, slashIndex + 1);
		}
	}

	public class Record : ISpace
	{
		public override string ToString() => $"[R: {Container.LocalRootPath}{KeyPath}]";

		private long? _fileSize;

		public MetadataContainer Container { get; }
		public Record Parent { get; }
		public List<Record> SubRecords { get; set; }

		public Record(MetadataContainer container, Record parent, string keyPath)
		{
			Container = container ?? throw new ArgumentNullException(nameof(container));
			Parent = parent;
			if (parent == null)
			{
				if (keyPath != "\\")
				{
					throw new Exception("Key must be \\ for root Record");
				}
			}
			if (keyPath[0] != '\\')
			{
				throw new Exception("Key must start from \\");
			}
			KeyPath = keyPath;

			Container.Add(this);
		}

		public string KeyPath { get; }

		public Hash Hash { get; set; }

		public long? FileSize
		{
			get
			{
				if (IsFolder && SubRecords != null)
				{
					lock (SubRecords)
					{
						return SubRecords.Sum(x => x.FileSize);
					}
				}
				else
				{
					return _fileSize;
				}
			}
			set
			{
				if (IsFolder)
				{

				}
				else
				{
					_fileSize = value;
				}
			}
		}

		public DateTime? LastModificationUtc { get; set; }

		public FileAttributes? FileAttributes { get; set; }

		public int GetSize() =>
			Hash.GetSize()

			+ KeyPath.Length * sizeof(char)
			+ IntPtr.Size // String Overhead

			+ sizeof(long) // FileSize

			+ IntPtr.Size // KeyPath pointer

			+ IntPtr.Size // Hash pointer

			+ IntPtr.Size // NPC

			+ 12 // DateTime?

			+ 4 // FileAttributes?

			+ IntPtr.Size // class overhead
		;

		public void Save(BinaryWriter bw, StorageContext ctx)
		{
			// Size is mandatory
			if (FileSize.HasValue)
			{
				var sectionAttributes = FileAttributes.HasValue;
				var sectionModification = LastModificationUtc.HasValue;
				var sectionHash = Hash != default;

				byte header = 0;
				if (sectionAttributes)
				{
					header |= 0b_0001;
				}
				if (sectionModification)
				{
					header |= 0b_0010;
				}
				if (sectionHash)
				{
					header |= 0b_0100;
				}

				bw.Write(header); // 1 byte (can easily be extended with overflow flag for var int)

				/*
				var parent = KeyPath.TrimEnd(Path.DirectorySeparatorChar);
				if (parent == "")
				{ 

				}
				*/
				var parentKey = Model.KeyPath.GetParent(KeyPath);

				if (ctx.CurrentFolder != null && ctx.CurrentFolder == parentKey)
				{
					var p = Path.GetFileName(KeyPath);
					bw.Write(p); // file.txt
				}
				else
				{
					bw.Write(KeyPath); // \folder\folder\file.txt
				}

				ctx.CurrentFolder = parentKey;

				bw.Write(FileSize.Value); // Mandatory

				if (sectionAttributes)
				{
					bw.Write((int)FileAttributes.Value); // 4 bytes
				}
				if (sectionModification)
				{
					bw.Write(LastModificationUtc.Value.Ticks); // 8 bytes
				}
				if (sectionHash)
				{
					bw.Write(Hash.Value); // 16 bytes MD5
				}
			}
		}

		public static Record Load(BinaryReader br, StorageContext ctx)
		{
			var head = br.ReadByte();

			var key = br.ReadString();
			if (key[0] != '\\')
			{
				key = ctx.CurrentFolder + key;
			}
			else if (key == "\\")
			{
				ctx.CurrentFolder = null;
			}
			else
			{
				ctx.CurrentFolder = Model.KeyPath.GetParent(key);
				// todo optimize loading by sorting all entries
				// var rel = ctx.CurrentFolder.EnsureEndsByDirectorySeparator().Substring(ctx.Container.LocalRootPath.Length);
				ctx.CurrentParent = ctx.Container.GetOrCreate(ctx.CurrentFolder);
			}

			var rec = new Record(ctx.Container, ctx.CurrentParent, key);

			rec.FileSize = br.ReadInt64(); // size is mandatory

			if ((head & 0b_001) > 0) // have attributes
			{
				rec.FileAttributes = (FileAttributes)br.ReadInt32();
			}

			if ((head & 0b_010) > 0) // have modification
			{
				rec.LastModificationUtc = new DateTime(br.ReadInt64(), DateTimeKind.Utc);
			}

			if ((head & 0b_100) > 0) // have hash
			{
				var hash = new byte[ctx.HashLen];
				var r = br.Read(hash, 0, ctx.HashLen);
				if (r != ctx.HashLen)
				{
					throw new Exception("Not enough bytes received for hash value");
				}
				rec.Hash = new Hash(hash);
			}

			return rec;
		}

		public bool IsFolder => KeyPath[KeyPath.Length - 1] == Path.DirectorySeparatorChar;

		public string Error { get; set; }

		#region ViewModel

		public string Name
		{
			get
			{
				if (KeyPath.Length == 1) // Only @"\"
				{
					return KeyPath; // root have a name @"\"
				}
				return Path.GetFileName(KeyPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
			}
		}

		#endregion
	}
}