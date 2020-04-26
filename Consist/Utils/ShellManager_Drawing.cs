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
		public static Icon GetIcon(string path, bool isFolder, FileInfoFlags extra = FileInfoFlags.SmallIcon)
		{
			var attributes = isFolder ? FileInfoAttribute.Directory : FileInfoAttribute.File;
			var flags = FileInfoFlags.Icon | FileInfoFlags.UseFileAttributes | extra;

			var result = Shell32.SHGetFileInfo(path, attributes, out var fileInfo, ShellFileInfo.Size, flags);

			if (result == IntPtr.Zero)
			{
				throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
			}

			try
			{
				return (Icon) Icon.FromHandle(fileInfo.hIcon).Clone();
			}
			finally
			{
				User32.DestroyIcon(fileInfo.hIcon);
			}
		}

	}
}
