using System;
using System.Linq;
using System.Net.Http.Headers;

namespace Consist.Model
{
	public class Hash : ISpace
	{
		public Hash(byte[] value)
		{
			Value = value;
		}

		public byte[] Value { get; }
		public int GetSize() => Value.Length
		                        + IntPtr.Size // byte array pointer
		                        + IntPtr.Size // class overhead
		;

		public override string ToString() => string.Join("", Value.Select(x => x.ToString("X2")));

		
	}
}