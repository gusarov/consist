using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Consist.View
{
	public class IndentConverter : IValueConverter
	{
		public const double Indentation = 20;

		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
			{
				return null;
			}

			// Convert the item to a double
			if (targetType == typeof(double) && value is DependencyObject element)
			{
				// Create a level counter with value set to -1
				var level = -1;

				// Move up the visual tree and count the number of TreeViewItem's.
				for (; element != null; element = VisualTreeHelper.GetParent(element))
				{
					if (element is TreeViewItem)
					{
						level++;
					}
				}

				//Return the indentation as a double
				return Indentation * level;
			}

			//Type conversion is not supported
			throw new NotSupportedException($"Cannot convert from <{value.GetType()}> to <{targetType}> using <{nameof(IndentConverter)}>.");
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException("This method is not supported.");
		}

		#endregion
	}
}