using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Consist.Model;
using Consist.Utils;

namespace Consist.ViewModel
{
	class StatsViewModel : ViewModel
	{
		public static StatsViewModel Instance = new StatsViewModel();

		StatsViewModel()
		{
			Notifier.Instance.StatsChanged += Instance_StatsChanged;

			Test = new DelegateCommand((Action) delegate
			{
				MainThread.Invoke("Test1", delegate { Debug.WriteLine("Test1"); }, true);
				MainThread.Invoke("Test2", delegate { Debug.WriteLine("Test2 A"); }, true);
				MainThread.Invoke("Test2", delegate { Debug.WriteLine("Test2 B"); }, true);
				MainThread.Invoke("Test3", delegate { Debug.WriteLine("Test3"); }, true);
			});
		}

		private void Instance_StatsChanged(object sender, EventArgs e)
		{
			MainThread.Invoke("Stats Changed", delegate
			{
				OnPropertyChanged(null);
			});
		}

		public Stats Stats { get; } = Stats.Instance;

		public ICommand Test { get; }
	}
}
