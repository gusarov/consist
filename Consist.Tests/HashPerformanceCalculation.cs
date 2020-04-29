using System;
using System.Security.Cryptography;
using Consist.Implementation.HashAlgo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Consist.Tests
{
	[TestClass]
	[Ignore]
	public class HashPerformanceCalculation
	{
		[TestMethod]
		public void Should_10_measure_perf()
		{
			var profiler = new HashAlgoProfiler();
			var res = profiler.Measure("SHA1");
			Console.WriteLine($"{res / 1024.0 / 1024.0:0.0} MB/s");
		}

		[TestMethod]
		[Timeout(60_000)]
		public void Should_15_list_all_profiling_results()
		{
			var profiler = new HashAlgoProfiler();
			foreach (var algo in HashAlgoList.List)
			{
				var res = profiler.Measure(algo);
				Console.WriteLine($"{res / 1024.0 / 1024.0:0.0} MB/s\t{algo} ({HashAlgorithm.Create(algo).GetType().Name})");
			}
		}

		[TestMethod]
		[Timeout(60_000)]
		public void Should_20_list_all_profiling_results_by_profiler()
		{
			var profiler = new HashAlgoProfiler();
			foreach (var res in profiler.Measure())
			{
				Console.WriteLine($"{res.BytesPerSecond / 1024.0 / 1024.0:0.0} MB/s\t{res.Name} ({res.Type.Name})");
			}
		}
	}
}
