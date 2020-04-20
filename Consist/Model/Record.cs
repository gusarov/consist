using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Windows.Media.Converters;

namespace Consist.Model
{
	public class Record : ISpace
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

		// public IEnumerable<Record> SubRecords { get; set; }

		public int GetSize() =>
			Hash.GetSize()
			+ KeyPath.Length * sizeof(char)
			+ IntPtr.Size // KeyPath pointer
			+ IntPtr.Size // Hash pointer
			+ IntPtr.Size // class overhead
		;

		public void Save(BinaryWriter bw, bool nameOnly)
		{
			bw.Write(nameOnly ? Path.GetFileName(KeyPath) : KeyPath);
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

			var hash = new byte[ctx.HashLen];
			var r = br.Read(hash, 0, ctx.HashLen);
			if (r != ctx.HashLen)
			{
				throw new Exception("Not enough bytes read for next value");
			}

			return new Record(key)
			{
				Hash = new Hash(hash),
			};
		}
	}
}