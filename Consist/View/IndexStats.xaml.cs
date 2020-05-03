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
using Consist.Model;

namespace Consist.View
{
	/// <summary>
	/// Interaction logic for IndexStats.xaml
	/// </summary>
	public partial class IndexStats : UserControl
	{
		public IndexStats()
		{
			InitializeComponent();
			DataContext = StatsViewModel.Instance;
		}
	}
}
