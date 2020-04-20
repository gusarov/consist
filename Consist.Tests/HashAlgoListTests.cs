using Consist.HashAlgo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Consist.Tests
{
	[TestClass]
	public class HashAlgoListTests
	{

		[TestMethod]
		public void Should_0_print_algos()
		{
			foreach (var item in HashAlgoList.List)
			{
				Console.WriteLine(item);
			}
		}

		[TestMethod]
		public void Should_10_contain_basic_algos()
		{
			Assert.IsTrue(HashAlgoList.List.Any(x => x == "SHA1"));
		}


		[TestMethod]
		public void Should_90_instantiate_all_algos()
		{
			foreach (var type in HashAlgoList.List)
			{
				HashAlgorithm.Create(type);
			}
		}

		[TestMethod]
		[Ignore]
		public void Should_15_contain_legacy_algos()
		{
			Assert.IsTrue(HashAlgoList.List.Any(x => x == "MD5"));
			Assert.IsTrue(HashAlgoList.List.Any(x => x == "CRC32"));
			Assert.IsTrue(HashAlgoList.List.Any(x => x == "CRC64"));
		}

		[TestMethod]
		public void Should_20_contain_net_common_algos()
		{
			Assert.IsTrue(HashAlgoList.List.Any(x => x == "SHA1"));
			Assert.IsTrue(HashAlgoList.List.Any(x => x == "SHA256"));
			Assert.IsTrue(HashAlgoList.List.Any(x => x == "SHA384"));
			Assert.IsTrue(HashAlgoList.List.Any(x => x == "SHA512"));
		}

		[TestMethod]
		public void Should_20_contain_net_48_algos()
		{
			Assert.IsTrue(HashAlgoList.List.Any(x => x == "SHA1"));
			Assert.IsTrue(HashAlgoList.List.Any(x => x == "SHA256"));
			Assert.IsTrue(HashAlgoList.List.Any(x => x == "SHA384"));
			Assert.IsTrue(HashAlgoList.List.Any(x => x == "SHA512"));
		}

		[TestMethod]
		public void Should_20_contain_net_core_31_algos()
		{
			Assert.Inconclusive();
		}

		[TestMethod]
		public void Should_20_contain_net_5_algos()
		{
			Assert.Inconclusive();
		}
	}
}
