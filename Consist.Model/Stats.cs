using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Consist.Model
{
	public class Stats : INotifyPropertyChanged
	{
		public static Stats Instance = new Stats();

		internal Stats()
		{
			
		}

		private int _messageLoop;

		public int MessageLoop
		{
			get => _messageLoop;
			set
			{
				_messageLoop = value;
				OnPropertyChanged();
			}
		}

		private int _analyzeQueue;

		public int AnalyzeQueue
		{
			get => _analyzeQueue;
			set
			{
				_analyzeQueue = value;
				// OnPropertyChanged();
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
