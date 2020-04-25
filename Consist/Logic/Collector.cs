using Consist.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Consist.Logic
{
	public class Collector
	{
		private readonly string _path;

		public Collector(string path)
		{
			if (!Path.IsPathRooted(path))
			{
				throw new Exception("Must have rooted path");
			}

			_path = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}

		public Container Container { get; set; } = new Container();

		public void Scan()
		{
			foreach (var meta in Container.Metadata
				.Where(x => x.MetadataRecordType == MetadataRecordType.OriginalPath))
			{
				Container.Metadata.Remove(meta);
			}
			ScanDir(new DirectoryInfo(_path));
		}

		void ScanDir(DirectoryInfo dir)
		{
			Console.WriteLine("Scan " + dir);
			foreach (var file in dir.EnumerateFiles())
			{
				ScanFile(file);
			}
			foreach (var subdir in dir.EnumerateDirectories())
			{
				ScanDir(subdir);
			}
		}

		private HashAlgorithm _hashAlgorithm = HashAlgorithm.Create("MD5");

		void ScanFile(FileInfo file)
		{
			byte[] hash;
			using (var stream = file.OpenRead())
			{
				hash = _hashAlgorithm.ComputeHash(stream);
			}

			var relPath = file.FullName.Substring(_path.Length);

			Container.Put(new Record(relPath)
			{
				Hash = new Hash(hash),
				FileSize = file.Length,
			});
		}


	}
}
