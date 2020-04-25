using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consist.Model
{
	public class MetadataRecord
	{
		public MetadataRecord(MetadataRecordType metadataRecordType, string value)
		{
			MetadataRecordType = metadataRecordType;
			Value = value ?? throw new ArgumentNullException(nameof(value));
		}

		public MetadataRecordType MetadataRecordType { get; }

		public string Value { get; }

		internal void Save(BinaryWriter bw, StorageContext ctx)
		{
			var type = (byte)MetadataRecordType;
			if (type >= 64)
			{
				throw new Exception();
			}

			bw.Write(type);
			bw.Write(Value);
		}

		internal static MetadataRecord Read(BinaryReader br, StorageContext ctx)
		{
			var type = br.ReadByte();
			if (type >= 64)
			{
				throw new Exception();
			}

			var value = br.ReadString();
			return new MetadataRecord((MetadataRecordType) type, value);
		}
	}
}
