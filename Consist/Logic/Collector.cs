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
			_path = path;
		}

		public Container Container { get; set; } = new Container();

		public void Scan()
		{
			ScanDir(_path);


		}
		void ScanDir(string dir)
		{
			foreach (var file in Directory.EnumerateFiles(dir))
			{
				ScanFile(file);
			}
			foreach (var subdir in Directory.EnumerateDirectories(dir))
			{
				ScanDir(subdir);
			}
		}

		private HashAlgorithm _hashAlgorithm = HashAlgorithm.Create("MD5");

		void ScanFile(string file)
		{
			byte[] hash;
			using (var stream = File.OpenRead(file))
			{
				hash = _hashAlgorithm.ComputeHash(stream);
			}

			var relPath = file.Substring(_path.Length);

			Container.Put(new Record(relPath)
			{
				Hash = new Hash(hash),
			});
		}


	}
}
