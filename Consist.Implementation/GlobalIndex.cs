using Consist.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consist.Implementation
{
	/// <summary>
	/// Cache all records & record containers globally. Uses drive serial to make sure that entries are different after another drive is inserted to the same later.
	/// </summary>
	public class GlobalIndex
	{
		public static GlobalIndex Instance = new GlobalIndex();

		private readonly PersistedMetadataProvider _persistedMetadataProvider;
		private readonly AnalyzerQueue _analyzerQueue;

		internal GlobalIndex(PersistedMetadataProvider persistedMetadataProvider = null, AnalyzerQueue analyzerQueue = null)
		{
			_persistedMetadataProvider = persistedMetadataProvider ?? PersistedMetadataProvider.Instance;
			_analyzerQueue = analyzerQueue ?? AnalyzerQueue.Instance;
		}

		private readonly Dictionary<string, Record> _index = new Dictionary<string, Record>(StringComparer.OrdinalIgnoreCase);

		public Record Request(string fullLocalPath)
		{
			/*
			var root = Path.GetPathRoot(fullLocalPath);
			if (!root.Equals(fullLocalPath, StringComparison.OrdinalIgnoreCase)
			    && fullLocalPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				fullLocalPath = fullLocalPath.Substring(0, fullLocalPath.Length - 1);
			}
			*/
			if (!_index.TryGetValue(fullLocalPath, out var rec))
			{
				_index[fullLocalPath] = rec = FindOrCreate(fullLocalPath);
			}
			return rec;
		}

		private Record FindOrCreate(string fullLocalPath)
		{
#if DEBUG
			try
			{
				if (Path.GetPathRoot(fullLocalPath) != fullLocalPath) // avoid exceptions on absent disks
				{
					if ((File.GetAttributes(fullLocalPath) & FileAttributes.Directory) > 0)
					{
						if (!fullLocalPath.EndsWith("\\"))
						{
							throw new Exception(
								$"{nameof(fullLocalPath)} '{fullLocalPath}' must ends on \\ if this is a folder");
						}
					}
				}
			}
			catch { }
#endif

			var cont = _persistedMetadataProvider.GetContainer(fullLocalPath);
			if (cont == null)
			{
				throw new Exception($"Unable to prepare container for {fullLocalPath}");
			}
			var contRootPath = cont.LocalRootPath;
			if (contRootPath == null || !Path.IsPathRooted(contRootPath))
			{
				throw new Exception("Bad local path of container");
			}
			var rel = fullLocalPath.Substring(contRootPath.EnsureEndsByDirectorySeparator().Length - 1).EnsureStartsFromDirectorySeparator();
			var item = cont.GetOrCreate(rel);
			return item;
		}
	}
}
