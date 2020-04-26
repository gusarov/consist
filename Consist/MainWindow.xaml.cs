using Consist.Utils;
using Consist.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Consist
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			DataContext = RootDataContext.Instance;
			Task.Run(async delegate
			{
				await Task.Delay(100);
				MainThread.Invoke(delegate
				{
					// Height = 1200;
					WindowState = WindowState.Maximized;
				});
			});
		}
	}
}
