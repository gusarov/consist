using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Consist.ViewModel
{
	public class BooleanToVisibilityConverter : IValueConverter
	{
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is bool input)
			{
				var falseVis = Visibility.Hidden;
				if (parameter is string par)
				{
					if (par.Contains("I"))
					{
						input = !input;
					}

					if (par.Contains("C"))
					{
						falseVis = Visibility.Collapsed;
					}
				}

				return input
					? Visibility.Visible
					: falseVis;
			}

			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
