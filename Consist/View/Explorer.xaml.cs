using Consist.Model;
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

namespace Consist.View
{
	/// <summary>
	/// Interaction logic for Explorer.xaml
	/// </summary>
	public partial class Explorer : UserControl
	{
		public Explorer()
		{
			InitializeComponent();
			DataContext = new ExplorerSource();
		}

	}

	public class ExplorerSource
	{
		public ExplorerSource()
		{
			var path = Environment.GetCommandLineArgs().Skip(1).Take(1).FirstOrDefault();

			if (path != null)
			{
				_container = Container.LoadFrom(path);
				_records = _container.Get("\\").SubRecords;
			}
		}

		private Container _container;

		private IEnumerable<Record> _records;
		public IEnumerable<Record> Records => _records;
	}
}
