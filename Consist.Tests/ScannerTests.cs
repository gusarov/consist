using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Consist.Implementation;
using Consist.Model;

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

	public class FakePersistedMetadataProvider : PersistedMetadataProvider
	{

	}

	[TestClass]
	public class ScannerTests : FilesTests
	{

		[TestInitialize]
		public void ScannerTestsInit()
		{
			_container.Metadata.Add(new MetadataRecord(MetadataRecordType.OriginalPath,
				Directory.GetCurrentDirectory()));

		}

		private readonly MetadataContainer _container = new MetadataContainer();
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


		void Scan(string relPath = null, AnalyzerContext cxt = null)
		{
			Analyzer = new Analyzer(Path.Combine(Directory.GetCurrentDirectory(), relPath ?? ""),
				new FakePersistedMetadataProvider(), _container);

			Analyzer.Scan(cxt ?? new AnalyzerContext
			{
				ScanChildren = true,
				ScanNodeItself = true,
				ScanRecursively = true,
				CalculateHashSum = true,
				Save = false,
			});
		}

		[TestMethod]
		public void Should_10_scan_file()
		{
			File.WriteAllText("test.txt", "test data");
			Scan();

			var rec = Analyzer.Container.TryGet("\\test.txt");

			// md5('test data')
			Assert.AreEqual("EB733A00C0C9D336E65691A37AB54293", rec.Hash.ToString());
		}

		[TestMethod]
		public void Container_should_keep_original_record()
		{
			File.WriteAllText("test.txt", "test data");
			Scan();

			var rec = Analyzer.Container.TryGet("\\test.txt");

			// File.WriteAllText("test2.txt", "test data");
			Scan();

			var rec1 = Analyzer.Container.TryGet("\\test.txt");
			// var rec2 = Analyzer.Container.Get("\\test2.txt");

			Assert.AreEqual(rec, rec1);
		}

		[TestMethod]
		public void Should_10_scan_hidden_system_file()
		{
			File.WriteAllText("test_hidden.txt", "test data");
			File.SetAttributes("test_hidden.txt",
				FileAttributes.Normal | FileAttributes.Hidden | FileAttributes.System);
			Scan();

			var rec = Analyzer.Container.TryGet("\\test_hidden.txt");

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

			var rec1 = Analyzer.Container.TryGet("\\abc\\test1.txt");
			Assert.AreEqual("634C3BFF7870EB5430A3CB355B88EE1A", rec1.Hash.ToString());

			var rec2 = Analyzer.Container.TryGet("\\abc\\test2.txt");
			Assert.AreEqual("76E17179BC18263FBFBB16B35C228B88", rec2.Hash.ToString());

		}

		[TestMethod]
		public void Should_25_populate_folders()
		{
			Directory.CreateDirectory("abc");
			File.WriteAllText("abc\\test1.txt", "test data1");
			File.WriteAllText("abc\\test2.txt", "test data2");
			Scan();

			var root = Analyzer.Container.TryGet("\\");
			Assert.IsNotNull(root);
			Assert.AreEqual("\\", root.Name);
			Assert.AreEqual("\\", root.KeyPath);
			Assert.IsNotNull(root.SubRecords);
			Assert.AreEqual(1, root.SubRecords.Count);
			var abc = root.SubRecords[0];
			Assert.IsNotNull(abc);

			var abcGet = Analyzer.Container.TryGet("\\abc\\");
			Assert.AreSame(abc, abcGet);
			Assert.AreEqual("abc", abc.Name);
			Assert.AreEqual("\\abc\\", abc.KeyPath);

			Assert.IsNotNull(abc.SubRecords);
			Assert.AreEqual(2, abc.SubRecords.Count);
			var test1 = abc.SubRecords[0];
			Assert.IsNotNull(test1);

			var test1Get = Analyzer.Container.TryGet("\\abc\\test1.txt");

			Assert.AreSame(test1, test1Get);
			Assert.AreEqual("test1.txt", test1.Name);
			Assert.AreEqual("\\abc\\test1.txt", test1.KeyPath);
			Assert.AreEqual("634C3BFF7870EB5430A3CB355B88EE1A", test1.Hash.ToString());

		}

		[TestMethod]
		public void Should_30_scan_only_children()
		{
			Directory.CreateDirectory("abc");
			File.WriteAllText("abc\\test1.txt", "test data1");
			File.WriteAllText("abc\\test2.txt", "test data2");
			Directory.CreateDirectory("abc\\def");
			File.WriteAllText("abc\\def\\test3.txt", "test data1");
			Scan("abc", new AnalyzerContext
			{
				ScanChildren = true,
				ScanRecursively = false,
				CalculateHashSum = false,
				Save = false,
			});

			var rec1 = Analyzer.Container.TryGet("\\abc\\test1.txt");
			Assert.IsNotNull(rec1);
			Assert.AreEqual("test1.txt", rec1.Name);
			Assert.AreEqual(10, rec1.FileSize);

			var rec2 = Analyzer.Container.TryGet("\\abc\\test2.txt");
			Assert.IsNotNull(rec2);
			Assert.AreEqual("test2.txt", rec2.Name);

			Assert.IsNotNull(rec1 = Analyzer.Container.TryGet("\\"));
			Assert.IsNull(rec1.LastModificationUtc);

			Assert.IsNull(rec1 = Analyzer.Container.TryGet("\\abc")); // file
			Assert.IsNotNull(rec1 = Analyzer.Container.TryGet("\\abc\\")); // folder
			Assert.IsNull(rec1.LastModificationUtc);

			Assert.IsNull(Analyzer.Container.TryGet("\\abc\\def"));
			Assert.IsNotNull(rec1 = Analyzer.Container.TryGet("\\abc\\def\\")); // this is a children
			Assert.IsNotNull(rec1.LastModificationUtc);

			Assert.IsNull(Analyzer.Container.TryGet("\\abc\\def\\test3.txt"));

		}

		[TestMethod]
		public void Should_30_scan_without_subfolder()
		{
			Directory.CreateDirectory("abc");
			File.WriteAllText("abc\\test1.txt", "test data1");
			File.WriteAllText("abc\\test2.txt", "test data2");
			Directory.CreateDirectory("abc\\def");
			File.WriteAllText("abc\\def\\test3.txt", "test data1");

			Scan("abc", new AnalyzerContext
			{
				ScanNodeItself = true,
				ScanChildren = false, // !
				ScanRecursively = false, // !
				CalculateHashSum = false,
				Save = false,
			});

			var rec = Analyzer.Container.TryGet("\\abc\\");
			Assert.IsNotNull(rec);
			Assert.AreEqual("abc", rec.Name);

			Assert.IsTrue(rec.IsFolder);

			Assert.IsNotNull(Analyzer.Container.TryGet("\\"));
			Assert.IsNull(Analyzer.Container.TryGet("\\abc")); // it is a file
			Assert.IsNotNull(Analyzer.Container.TryGet("\\abc\\")); // it is a folder
			Assert.IsNull(Analyzer.Container.TryGet("\\abc\\test1.txt"));
			Assert.IsNull(Analyzer.Container.TryGet("\\abc\\test2.txt"));
			Assert.IsNull(Analyzer.Container.TryGet("\\abc\\def"));
			Assert.IsNull(Analyzer.Container.TryGet("\\abc\\def\\"));
			Assert.IsNull(Analyzer.Container.TryGet("\\abc\\def\\test3.txt"));

		}
	}
}
