using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Consist.Implementation;

namespace Consist.Tests
{
	[TestClass]
	public abstract class FilesTests
	{
		private static string __origRoot;

		private static string OrigRoot
		{
			get
			{
				if (__origRoot == null)
				{
					__origRoot = Directory.GetCurrentDirectory();
				}

				return __origRoot;
			}
		}

		[ClassInitialize]
		public static void CInit(TestContext ctx)
		{
			Directory.SetCurrentDirectory(OrigRoot);
			Directory.Delete("Test", true);
		}

		[ClassCleanup]
		public static void CClean()
		{
			Directory.SetCurrentDirectory(OrigRoot);
		}

		[TestInitialize]
		public void Init()
		{
			Directory.SetCurrentDirectory(OrigRoot);
			Directory.CreateDirectory("Test");
			var test = "Test\\" + Guid.NewGuid().ToString("N");
			var dir = Directory.CreateDirectory(test);
			Directory.SetCurrentDirectory(dir.FullName);
			Console.WriteLine(dir.FullName);
		}
	}

	[TestClass]
	public class ScannerTests : FilesTests
	{

		[TestInitialize]
		public void ScannerTestsInit()
		{
			Analyzer = new Analyzer(Directory.GetCurrentDirectory());
		}

		public Analyzer Analyzer;

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

		void Scan()
		{
			Analyzer.Scan(new AnalyzerContext
			{
				ScanSubfolders = true,
				CalculateHashSum = true,
			});
		}

		[TestMethod]
		public void Should_10_scan_file()
		{
			File.WriteAllText("test.txt", "test data");
			Scan();

			var rec = Analyzer.Container.Get("\\test.txt");

			// md5('test data')
			Assert.AreEqual("EB733A00C0C9D336E65691A37AB54293", rec.Hash.ToString());
		}

		[TestMethod]
		public void Should_10_scan_hidden_system_file()
		{
			File.WriteAllText("test_hidden.txt", "test data");
			File.SetAttributes("test_hidden.txt",
				FileAttributes.Normal | FileAttributes.Hidden | FileAttributes.System);
			Scan();

			var rec = Analyzer.Container.Get("\\test_hidden.txt");

			// md5('test data')
			Assert.AreEqual("EB733A00C0C9D336E65691A37AB54293", rec.Hash.ToString());
		}

		[TestMethod]
		public void Should_20_scan_folder()
		{
			Directory.CreateDirectory("abc");
			File.WriteAllText("abc\\test1.txt", "test data1");
			File.WriteAllText("abc\\test2.txt", "test data2");
			Scan();

			var rec1 = Analyzer.Container.Get("\\abc\\test1.txt");
			Assert.AreEqual("634C3BFF7870EB5430A3CB355B88EE1A", rec1.Hash.ToString());

			var rec2 = Analyzer.Container.Get("\\abc\\test2.txt");
			Assert.AreEqual("76E17179BC18263FBFBB16B35C228B88", rec2.Hash.ToString());

		}

		[TestMethod]
		public void Should_25_generate_virtual_folders()
		{
			Directory.CreateDirectory("abc");
			File.WriteAllText("abc\\test1.txt", "test data1");
			File.WriteAllText("abc\\test2.txt", "test data2");
			Scan();

			var root = Analyzer.Container.Get("\\");
			Assert.IsNotNull(root);
			Assert.AreEqual("\\", root.Name);
			Assert.AreEqual("\\", root.KeyPath);
			Assert.IsNotNull(root.SubRecords);
			Assert.AreEqual(1, root.SubRecords.Count);
			var abc = root.SubRecords[0];
			Assert.IsNotNull(abc);

			var abcGet = Analyzer.Container.Get("\\abc\\");
			Assert.AreSame(abc, abcGet);
			Assert.AreEqual("abc", abc.Name);
			Assert.AreEqual("\\abc\\", abc.KeyPath);

			Assert.IsNotNull(abc.SubRecords);
			Assert.AreEqual(2, abc.SubRecords.Count);
			var test1 = abc.SubRecords[0];
			Assert.IsNotNull(test1);

			var test1Get = Analyzer.Container.Get("\\abc\\test1.txt");

			Assert.AreSame(test1, test1Get);
			Assert.AreEqual("test1.txt", test1.Name);
			Assert.AreEqual("\\abc\\test1.txt", test1.KeyPath);
			Assert.AreEqual("634C3BFF7870EB5430A3CB355B88EE1A", test1.Hash.ToString());

		}
	}
}
