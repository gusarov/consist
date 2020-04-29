using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Consist.Model.Annotations;

namespace Consist.Model
{
	public class Record : ISpace, INotifyPropertyChanged
	{
		public Record(string keyPath)
		{
			if (keyPath[0] != '\\')
			{
				throw new Exception("Key must start from \\");
			}
			KeyPath = keyPath;
		}

		public string KeyPath { get; }

		public Hash Hash { get; set; }

		public long FileSize { get; set; }

		public List<Record> SubRecords { get; set; }

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
			var folder = Path.GetDirectoryName(KeyPath);
			if (ctx.CurrentFolder == folder)
			{
				bw.Write(Path.GetFileName(KeyPath)); // file.txt
			}
			else
			{
				bw.Write(KeyPath); // \folder\folder\file.txt
			}
			ctx.CurrentFolder = folder;

			bw.Write(FileSize);
			bw.Write(Hash.Value);
		}

		public static Record Load(BinaryReader br, StorageContext ctx)
		{
			var key = br.ReadString();
			if (key[0] != '\\')
			{
				key = ctx.CurrentFolder + '\\' + key;
			}
			else
			{
				ctx.CurrentFolder = Path.GetDirectoryName(key);
			}

			var len = br.ReadInt64();

			var hash = new byte[ctx.HashLen];
			var r = br.Read(hash, 0, ctx.HashLen);
			if (r != ctx.HashLen)
			{
				throw new Exception("Not enough bytes read for next value");
			}

			return new Record(key)
			{
				FileSize = len,
				Hash = new Hash(hash),
			};
		}

		public bool IsFolder => KeyPath[KeyPath.Length - 1] == Path.DirectorySeparatorChar;

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

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}