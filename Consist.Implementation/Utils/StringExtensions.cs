using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consist.Implementation.Utils
{
	public static class StringExtensions
	{
		public static string EnsureStartsFromDirectorySeparator(this string path)
		{
			if (!string.IsNullOrEmpty(path)
			    && path[0] != Path.DirectorySeparatorChar
			    && path[0] != Path.AltDirectorySeparatorChar)
			{
				return Path.DirectorySeparatorChar + path;
			}

			return path;
		}
	}
}
