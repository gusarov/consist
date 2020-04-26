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

namespace Consist.Utils
{
	public partial class ShellManager
	{
		public static ImageSource GetImageSource(string path, bool isOpen)
		{
			return GetImageSource(path, new Size(16, 16), isOpen);
		}

		public static ImageSource GetImageSource(string path, Size size, bool isOpen)
		{
			var extra = FileInfoFlags.SmallIcon;
			if (isOpen)
			{
				extra |= FileInfoFlags.OpenIcon;
			}

			using (var icon = ShellManager.GetIcon(path, true, extra))
			{
				return Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty,
					BitmapSizeOptions.FromWidthAndHeight((int)size.Width, (int)size.Height));
			}
		}

	}
}
