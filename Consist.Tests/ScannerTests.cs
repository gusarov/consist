using Consist.Logic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consist.Tests
{
	[TestClass]
	public class FilesTests
	{
		[ClassInitialize]
		public static void CInit(TestContext ctx)
		{
			Directory.Delete("Test", true);
		}

		[TestInitialize]
		public void Init()
		{
			var test = "Test\\" + Guid.NewGuid().ToString("N");
			var dir = Directory.CreateDirectory(test);
			Directory.SetCurrentDirectory(dir.FullName);
		}
	}

	[TestClass]
	public class ScannerTests : FilesTests
	{

		[TestInitialize]
		public void ScannerTestsInit()
		{
			Collector = new Collector(Directory.GetCurrentDirectory());
		}

		public Collector Collector;

		[TestMethod]
		public void Should_0_isolate_tests()
		{
			Console.WriteLine(Directory.GetCurrentDirectory());
			if (
				Directory.GetFiles(Directory.GetCurrentDirectory(), "*.exe").Any()
				|| Directory.GetFiles(Directory.GetCurrentDirectory(), "*.dll").Any()
				)
			{
				Assert.Fail("Folder should not contain assemblies");
			}
		}

		[TestMethod]
		public void Should_10_scan_file()
		{
			File.WriteAllText("test.txt", "test data");
			Collector.Scan();

			var rec = Collector.Container.Get("\\test.txt");
			// md5('test data')
			Assert.AreEqual("EB733A00C0C9D336E65691A37AB54293", rec.Hash.ToString());
		}

		[TestMethod]
		public void Should_20_scan_folder()
		{
			Directory.CreateDirectory("abc");
			File.WriteAllText("abc\\test1.txt", "test data1");
			File.WriteAllText("abc\\test2.txt", "test data2");
			Collector.Scan();

			var rec1 = Collector.Container.Get("\\abc\\test1.txt");
			Assert.AreEqual("634C3BFF7870EB5430A3CB355B88EE1A", rec1.Hash.ToString());

			var rec2 = Collector.Container.Get("\\abc\\test2.txt");
			Assert.AreEqual("76E17179BC18263FBFBB16B35C228B88", rec2.Hash.ToString());

		}
	}
}
