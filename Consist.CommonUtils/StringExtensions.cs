using System.IO;

namespace Consist
{
	public static class StringExtensions
	{
		public static string EnsureStartsFromDirectorySeparator(this string path)
		{
			if (string.IsNullOrEmpty(path)
			    || path[0] != Path.DirectorySeparatorChar
			    && path[0] != Path.AltDirectorySeparatorChar)
			{
				return Path.DirectorySeparatorChar + path;
			}

			return path;
		}
		public static string EnsureEndsByDirectorySeparator(this string path)
		{
			if (string.IsNullOrEmpty(path)
			    || path[path.Length-1] != Path.DirectorySeparatorChar
			    && path[path.Length-1] != Path.AltDirectorySeparatorChar)
			{
				return path + Path.DirectorySeparatorChar;
			}

			return path;
		}

		/*
		public static void EnsureIsLocalFolder(this string path)
		{
			new FileInfo
			if (path.Length == 0
			    || path[0] != Path.DirectorySeparatorChar
			    && path[0] != Path.AltDirectorySeparatorChar)
			{
				return Path.DirectorySeparatorChar + path;
			}

			return path;
		}
		*/

	}
}
