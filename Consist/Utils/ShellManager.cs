using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Consist.Interop;
using System.Drawing;

namespace Consist.Utils
{
	public partial class ShellManager
	{
		private static readonly object _lock = new object();

		public static Icon GetIcon(string path, bool isFolder, FileInfoFlags extra = FileInfoFlags.SmallIcon)
		{
			/*
			if (!isFolder)
			{
				try
				{
					return Icon.ExtractAssociatedIcon(path);
				}
				catch
				{
					return null;
				}
			}
			*/

			var attributes = isFolder ? FileInfoAttribute.Directory : FileInfoAttribute.File;
			var flags = FileInfoFlags.Icon | FileInfoFlags.UseFileAttributes | extra;

			IntPtr result;
			ShellFileInfo fileInfo;
			lock (_lock) // SHGetFileInfo can be invoked from any thread but must be thread safe
			{
				result = Shell32.SHGetFileInfo(path, attributes, out fileInfo, ShellFileInfo.Size, flags);
			}

			if (result == IntPtr.Zero)
			{
				throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
			}

			try
			{
				return Icon.FromHandle(fileInfo.hIcon);
			}
			catch
			{
				return null;
			}
			finally
			{
				// User32.DestroyIcon(fileInfo.hIcon);
			}
		}

	}
}
