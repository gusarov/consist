using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Consist.Commands
{
	public static class CCommands
	{
		public static RoutedCommand Scan { get; } = new RoutedCommand();
		public static RoutedCommand ScanHash { get; } = new RoutedCommand();
		public static RoutedCommand Pin { get; } = new RoutedCommand();
		public static RoutedCommand Unpin { get; } = new RoutedCommand();
	}
}
