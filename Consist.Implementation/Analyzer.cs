using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Consist.Model;

namespace Consist.Implementation
{
	public class Analyzer
	{
		private readonly string _pathToScan;
		private readonly string _pathContainerRoot;
		private readonly PersistedMetadataProvider _persistedMetadataProvider;

		public Analyzer(string path, PersistedMetadataProvider persistedMetadataProvider = null, MetadataContainer container = null)
		{
			if (!Path.IsPathRooted(path))
			{
				throw new Exception("Must have rooted path");
			}

			var isFolder = (new FileInfo(path).Attributes & FileAttributes.Directory) > 0;

			if (isFolder)
			{
				_pathToScan = path.EnsureEndsByDirectorySeparator();
			}
			else
			{
				throw new NotImplementedException("Scan file self not completely supported");
				_pathToScan = path;
			}

			_persistedMetadataProvider = persistedMetadataProvider ?? PersistedMetadataProvider.Instance;
			Container = container ?? _persistedMetadataProvider.GetContainer(path);

			var cr = Container.LocalRootPath;
			if (cr == null)
			{
				throw new Exception("Container root is invalid");
			}

			_pathContainerRoot = cr.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
			// _path = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
		}

		public MetadataContainer Container { get; }

		public void Scan(AnalyzerContext ctx)
		{
			Debug.WriteLine($"Scan: {_pathToScan} Self:{ctx.ScanNodeItself} Kids:{ctx.ScanChildren}");

			/*
			foreach (var meta in Container.Metadata
				.Where(x => x.MetadataRecordType == MetadataRecordType.OriginalPath).ToArray())
			{
				Container.Metadata.Remove(meta);
			}
			*/

			if (!ctx.IsValid())
			{
				throw new Exception("Context is not valid - nothing to scan");
			}

			var lrp = Container.LocalRootPath.TrimEnd(Path.AltDirectorySeparatorChar);
			// _pathToScan.EnsureStartsFromDirectorySeparator();
			// EnsureIsLocalFolder // if starts / ends on \

			var rel = _pathToScan.Substring(lrp.Length).EnsureStartsFromDirectorySeparator();
			var record = Container.GetOrCreate(rel);

			ScanDir(record.Parent, ctx, new DirectoryInfo(_pathToScan), new StructContext(ctx.ScanNodeItself, ctx.ScanChildren));

			if (ctx.Save != false)
			{
				// Container.Save();
			}

			Debug.WriteLine($"Scan Done");
		}

		struct StructContext
		{
			
			public bool ScanNode;
			public bool ScanChildren;

			public StructContext(bool scanNode, bool scanChildren)
			{
				ScanNode = scanNode;
				ScanChildren = scanChildren;
			}
		}

		void ScanDir(Record parent, AnalyzerContext ctx, DirectoryInfo dir, StructContext sCtx)
		{
			// CURRENT NODE
			var relPath = dir.FullName.Substring(_pathContainerRoot.Length - 1).EnsureEndsByDirectorySeparator();
			var record = Container.GetOrCreate(relPath, parent);

			if (sCtx.ScanNode)
			{
				Debug.WriteLine("Scan Dir Self" + dir);
				try
				{
					Fill(ctx, dir, record);
				}
				catch (Exception ex)
				{
					record.Error = ex.GetType().Name + ": " + ex.Message;
				}

#if DEBUG
				if (parent == record)
				{
					throw new Exception();
				}
#endif
				Notifier.Instance.NotifyItemScanned(new ItemScannedEventArgs
				{
					Container = Container,
					Parent = parent,
					Item = record,
				});
			}

			// CHILDREN
			if (sCtx.ScanChildren)
			{
				Debug.WriteLine($"!!Scan Children Of {dir}");
				try
				{
					foreach (var file in dir.EnumerateFiles())
					{
						ScanFile(record, ctx, file);
					}

					foreach (var subdir in dir.EnumerateDirectories())
					{
						ScanDir(record, ctx, subdir, new StructContext(true, ctx.ScanRecursively));
					}
				}
				catch (Exception ex)
				{
					record.Error = ex.GetType().Name + ": " + ex.Message;
				}
			}
		}

		void Fill(AnalyzerContext ctx, FileSystemInfo info, Record rec)
		{
			if (ctx.ReadAttributes)
			{
				rec.FileAttributes = info.Attributes;
			}

			if (ctx.ReadModificationDate)
			{
				rec.LastModificationUtc = info.LastWriteTimeUtc;
			}
		}

		private readonly HashAlgorithm _hashAlgorithm = HashAlgorithm.Create("MD5");

		void ScanFile(Record parent, AnalyzerContext ctx, FileInfo file)
		{
			var relPath = file.FullName.Substring(_pathContainerRoot.Length - 1);
			var record = Container.GetOrCreate(relPath, parent);
			// var record = new Record(Container, parent, relPath);

			Fill(ctx, file, record);
			record.FileSize = file.Length;

			if (ctx.CalculateHashSum)
			{
				byte[] hash;
				using (var stream = file.OpenRead())
				{
					hash = _hashAlgorithm.ComputeHash(stream);
				}
				record.Hash = new Hash(hash);
			}

			Notifier.Instance.NotifyItemScanned(new ItemScannedEventArgs
			{
				Container = Container,
				Parent = parent,
				Item = record,
			});
		}

	}
}
