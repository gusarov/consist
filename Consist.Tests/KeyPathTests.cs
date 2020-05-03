using Consist.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consist.Tests
{
	[TestClass]
	public class KeyPathTests
	{
		[TestMethod]
		public void Should_find_proper_parent()
		{
			Assert.AreEqual(@"\abc\", KeyPath.GetParent(@"\abc\file.txt"));
			Assert.AreEqual(@"\abc\def\", KeyPath.GetParent(@"\abc\def\file.txt"));
			Assert.AreEqual(@"\abc\", KeyPath.GetParent(@"\abc\def"));
			Assert.AreEqual(@"\abc\", KeyPath.GetParent(@"\abc\def\"));
			Assert.AreEqual(@"\", KeyPath.GetParent(@"\abc"));
			Assert.AreEqual(@"\", KeyPath.GetParent(@"\abc\"));
			Assert.AreEqual(null, KeyPath.GetParent(@"\"));
		}

		[TestMethod]
		public void Should_find_proper_parent_span()
		{
			Assert.AreEqual(@"\abc\",     new string(KeyPath.GetParent(@"\abc\file.txt".ToCharArray()).ToArray()));
			Assert.AreEqual(@"\abc\def\", new string(KeyPath.GetParent(@"\abc\def\file.txt".ToCharArray()).ToArray()));
			Assert.AreEqual(@"\abc\",     new string(KeyPath.GetParent(@"\abc\def".ToCharArray()).ToArray()));
			Assert.AreEqual(@"\abc\",     new string(KeyPath.GetParent(@"\abc\def\".ToCharArray()).ToArray()));
			Assert.AreEqual(@"\",         new string(KeyPath.GetParent(@"\abc".ToCharArray()).ToArray()));
			Assert.AreEqual(@"\",         new string(KeyPath.GetParent(@"\abc\".ToCharArray()).ToArray()));
			Assert.AreEqual("",           new string(KeyPath.GetParent(@"\".ToCharArray()).ToArray()));
		}

		[TestMethod]
		public void Should_find_proper_parent_full()
		{
			Assert.AreEqual(@"C:\abc\", KeyPath.GetParent(@"C:\abc\file.txt"));
			Assert.AreEqual(@"C:\abc\def\", KeyPath.GetParent(@"C:\abc\def\file.txt"));
			Assert.AreEqual(@"C:\abc\", KeyPath.GetParent(@"C:\abc\def"));
			Assert.AreEqual(@"C:\abc\", KeyPath.GetParent(@"C:\abc\def\"));
			Assert.AreEqual(@"C:\", KeyPath.GetParent(@"C:\abc"));
			Assert.AreEqual(@"C:\", KeyPath.GetParent(@"C:\abc\"));
			Assert.AreEqual(null, KeyPath.GetParent(@"C:\"));
		}

		[TestMethod]
		public void Should_find_parent_fast()
		{
			Assert.AreEqual(@"\", KeyPath.GetParent(@"\abc\"));

			var sw = Stopwatch.StartNew();
			int cnt = 0;
			do
			{
				const int batch = 100;
				for (int i = 0; i < batch; i++)
				{
					KeyPath.GetParent(@"\abc\file.txt");
					KeyPath.GetParent(@"\abc\def\file.txt");
					KeyPath.GetParent(@"\abc\def");
					KeyPath.GetParent(@"\abc\def\");
					KeyPath.GetParent(@"\abc");
					KeyPath.GetParent(@"\abc\");
					KeyPath.GetParent(@"\");
				}
				cnt += batch;
			} while (sw.ElapsedMilliseconds < 1000);

			sw.Stop();

			var perf = 1.0 * cnt / sw.ElapsedMilliseconds;

			Console.WriteLine(perf);

			Assert.IsTrue(perf > 2_000);
		}
	}
}
