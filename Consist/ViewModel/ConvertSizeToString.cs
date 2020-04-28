using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Consist.ViewModel
{
	class ConvertSizeToStringOptions
	{
		public static ConvertSizeToStringOptions Instance = new ConvertSizeToStringOptions();

		public ConvertSizeToStringMode Mode { get; set; }

		public bool Decimal => ((int)Mode & 1) > 0;
		public bool Binary => !Decimal;
		public bool IecPrefix => ((int)Mode & 2) > 0;
	}

	[Flags]
	public enum ConvertSizeToStringMode
	{
		SiForBin = 0x0, // 1KB means 1024 bytes (Windows)
		SiForDec = 0x1, // 1KB means 1000 bytes (Apple)
		IecForBin = 0x2, // 1KiB means 1024 bytes (IEC)
		IecForDec = 0x3, // 1KiB means 1000 bytes
	}

	class ConvertSizeToString : IValueConverter
	{
		public static string ToSizeString(long size, ConvertSizeToStringOptions options)
		{
			var factor = options.Binary ? 1024 : 1000;

			double s = size;
			int scale = 0;

			while (s > factor)
			{
				s /= factor;
				scale++;
			}

			return string.Format(PrefixPattern(scale), s, options.IecPrefix ? "i" : "");
		}

		public static string PrefixPattern(int scale)
		{
			switch (scale)
			{
				case 0:
					return "{0} bytes";
				case 1:
					return "{0:#.0} K{1}B";
				case 2:
					return "{0:#.0} M{1}B";
				case 3:
					return "{0:#.0} G{1}B";
				case 4:
					return "{0:#.0} T{1}B";

				case 5:
					return "{0:#.0} P{1}B";
				case 6:
					return "{0:#.0} E{1}B";
				case 7:
					return "{0:#.0} Z{1}B";
				case 8:
					return "{0:#.0} Y{1}B";
				default:
					return $"{{0:#.0}} e{scale}{{1}}B";
			}
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var options = (ConvertSizeToStringOptions) parameter;
			if (options == null)
			{
				options = ConvertSizeToStringOptions.Instance;
				// return "parameter is not specified";
			}
			if (value is long size)
			{
				return ToSizeString(size, options);
			}

			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
	}
}
