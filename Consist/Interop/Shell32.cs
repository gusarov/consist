using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Consist.Interop
{
	static class User32
	{
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DestroyIcon(IntPtr pointer);
	}

	static class Shell32
	{
		[DllImport("shell32", CharSet = CharSet.Unicode)]
		public static extern IntPtr SHGetFileInfo(string path,
			FileInfoAttribute attributes,
			out ShellFileInfo fileInfo,
			uint size,
			FileInfoFlags flags
			);
	}

	public enum FileInfoAttribute : uint
	{
		Directory = 16,
		File = 256
	}

	[Flags]
	public enum FileInfoFlags : uint
	{
		LargeIcon = 0,              // 0x000000000
		SmallIcon = 1,              // 0x000000001
		OpenIcon = 2,               // 0x000000002
		ShellIconSize = 4,          // 0x000000004
		Pidl = 8,                   // 0x000000008
		UseFileAttributes = 16,     // 0x000000010
		AddOverlays = 32,           // 0x000000020
		OverlayIndex = 64,          // 0x000000040
		Others = 128,               // Not defined, really?
		Icon = 256,                 // 0x000000100  
		DisplayName = 512,          // 0x000000200
		TypeName = 1024,            // 0x000000400
		Attributes = 2048,          // 0x000000800
		IconLocation = 4096,        // 0x000001000
		ExeType = 8192,             // 0x000002000
		SystemIconIndex = 16384,    // 0x000004000
		LinkOverlay = 32768,        // 0x000008000 
		Selected = 65536,           // 0x000010000
		AttributeSpecified = 131072 // 0x000020000
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	public struct ShellFileInfo
	{
		private static uint _size;
		public static uint Size
		{
			get
			{
				if (_size == 0)
				{
					var fileInfo = new ShellFileInfo();
					_size = (uint)Marshal.SizeOf(fileInfo);
				}

				return _size;
			}
		}


		public IntPtr hIcon;

		public int iIcon;

		public uint dwAttributes;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public string szDisplayName;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
		public string szTypeName;
	}
}
