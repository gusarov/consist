using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Consist.Model;

namespace Consist.Implementation
{
	public class Analyzer
	{
		private readonly string _path;
		private readonly PersistedMetadataProvider _persistedMetadataProvider;

		public Analyzer(string path, PersistedMetadataProvider persistedMetadataProvider = null, MetadataContainer container = null)
		{
			if (!Path.IsPathRooted(path))
			{
				throw new Exception("Must have rooted path");
			}

			_path = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
			_persistedMetadataProvider = persistedMetadataProvider ?? PersistedMetadataProvider.Instance;
			Container = container ?? _persistedMetadataProvider.GetContainer(path);
		}

		public MetadataContainer Container { get; }

		public void Scan(AnalyzerContext ctx)
		{
			foreach (var meta in Container.Metadata
				.Where(x => x.MetadataRecordType == MetadataRecordType.OriginalPath))
			{
				Container.Metadata.Remove(meta);
			}
			ScanDir(ctx, new DirectoryInfo(_path));
		}

		void ScanDir(AnalyzerContext ctx, DirectoryInfo dir)
		{
			Console.WriteLine("Scan " + dir);
			foreach (var file in dir.EnumerateFiles())
			{
				ScanFile(ctx, file);
			}
			foreach (var subdir in dir.EnumerateDirectories())
			{
				ScanDir(ctx, subdir);
			}
		}

		private readonly HashAlgorithm _hashAlgorithm = HashAlgorithm.Create("MD5");

		void ScanFile(AnalyzerContext ctx, FileInfo file)
		{
			var relPath = file.FullName.Substring(_path.Length - 1);
			var record = new Record(relPath);

			if (ctx.CalculateHashSum)
			{
				byte[] hash;
				using (var stream = file.OpenRead())
				{
					hash = _hashAlgorithm.ComputeHash(stream);
				}
				record.Hash = new Hash(hash);
			}

			if (ctx.ReadModificationDate)
			{
				record.LastModificationUtc = file.LastWriteTimeUtc;
			}

			if (ctx.ReadAttributes)
			{
				record.FileAttributes = file.Attributes;
			}

			record.FileSize = file.Length;

			Container.Put(record);
		}


	}
}
