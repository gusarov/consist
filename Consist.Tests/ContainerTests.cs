using System;
using System.IO;
using Consist.Implementation;
using Consist.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Consist.Tests
{
	[TestClass]
	public class ContainerTests : FilesTests
	{

		[TestInitialize]
		public void ContainerTestsInit()
		{
			Collector = new Analyzer(Directory.GetCurrentDirectory() + "\\data");
		}

		public Analyzer Collector;

		[TestMethod]
		public void Should_10_save_metadata()
		{
			// var col = new Collector(Directory.GetCurrentDirectory() + "\\data");

			Directory.CreateDirectory("data\\abc");
			File.WriteAllText("data\\abc\\test1.txt", "test data1");
			File.WriteAllText("data\\abc\\test2.txt", "test data2");
			Collector.Scan();

			Collector.Container.Save(".consist.metadata");

			var container = new MetadataContainer();
			container.Load(".consist.metadata");

			var rec1 = container.Get("\\abc\\test1.txt");
			Assert.AreEqual("634C3BFF7870EB5430A3CB355B88EE1A", rec1.Hash.ToString());

			var rec2 = container.Get("\\abc\\test2.txt");
			Assert.AreEqual("76E17179BC18263FBFBB16B35C228B88", rec2.Hash.ToString());

		}

		[TestMethod]
		public void Should_15_compress_folder_names()
		{
			const string folder =
				"abcabcabcabcabcabcabcabcabcabcabcabcabcabcabcabcabcabcabcabcabcabcabcabcabcabcabcabcabcabcabcabcabcabcabcabcabcabc2abcabcabca";
			Directory.CreateDirectory("data");
			Directory.CreateDirectory($"data\\{folder}");
			for (var i = 0; i < 1000; i++)
			{
				File.WriteAllText($"data\\{folder}\\test{i}.txt", "test data");
			}
			Collector.Scan();
			Collector.Container.Save(".consist.metadata");

			var len = new FileInfo(".consist.metadata").Length;

			Console.WriteLine(len);

			Assert.IsTrue(len < 40_000, len.ToString());

			var container = new MetadataContainer();
			container.Load(".consist.metadata");

			for (var i = 0; i < 1000; i++)
			{
				var rec = container.Get($"\\{folder}\\test{i}.txt");
				Assert.AreEqual("EB733A00C0C9D336E65691A37AB54293", rec.Hash.ToString());
			}

		}
	}
}
