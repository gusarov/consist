using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;

namespace Consist.Implementation.HashAlgo
{
	public class ProfilingResult
	{
		public double BytesPerSecond;
		public string Name;
		public Type Type;
	}

	public class HashAlgoProfiler
	{
		private byte[] _data = new byte[1024 * 1024];
		private Random _rnd = new Random();

		public HashAlgoProfiler()
		{
			_rnd.NextBytes(_data);
		}

		public IEnumerable<ProfilingResult> Measure()
		{
			var result = HashAlgoList.List.Select(x => new ProfilingResult
			{
				BytesPerSecond = Measure(x),
				Type = HashAlgorithm.Create(x).GetType(),
				Name = x,
			});
			return result.OrderByDescending(x => x.BytesPerSecond).ToArray();
		}

		public double Measure(string name)
		{
			// warm up
			using (var algo = HashAlgorithm.Create(name))
			{
				algo.ComputeHash(_data);

				var sw = Stopwatch.StartNew();
				long c = 0;
				while (sw.ElapsedMilliseconds < 300)
				{
					algo.ComputeHash(_data);
					c++;
				}

				sw.Stop();

				c *= _data.Length;
				var bytesPerSecond = c / sw.Elapsed.TotalSeconds;

				return bytesPerSecond;
			}
		}
	}
}
