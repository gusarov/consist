using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Consist.Interop;

using Icon = System.Drawing.Icon;

namespace Consist.Utils
{
	public partial class ShellManager
	{
		public static ImageSource GetImageSource(string path, bool isOpen, bool isFolder = true)
		{
			return GetImageSource(path, new Size(16, 16), isOpen, isFolder);
		}

		public static ImageSource GetImageSource(Icon icon)
		{
			if (icon == null)
			{
				return null;
			}

			if (icon.Width == 0 || icon.Handle == default)
			{
				return null;
			}
			return Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(icon.Width, icon.Height));
		}

		public static ImageSource GetImageSource(string path, Size size, bool isOpen, bool isFolder = true)
		{
			var extra = FileInfoFlags.SmallIcon;
			if (isOpen)
			{
				extra |= FileInfoFlags.OpenIcon;
			}

			using (var icon = ShellManager.GetIcon(path, isFolder, extra))
			{
				return GetImageSource(icon);
			}
		}

	}
}
