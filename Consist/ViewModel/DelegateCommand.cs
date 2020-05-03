using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Consist.ViewModel
{
	public class DelegateCommand : ICommand
	{
		private readonly Action<object> _act;
		private readonly Func<object, bool> _can;

		public DelegateCommand(Action<object> act, Func<object, bool> can = null)
		{
			_act = act;
			_can = can;
		}

		public DelegateCommand(Action act, Func<bool> can = null)
		{
			_act = _ => act();
			_can = _ => can == null || can();
		}

		public event EventHandler CanExecuteChanged {
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}

		public bool CanExecute(object parameter)
		{
			return _can == null || _can(parameter);
		}

		public void Execute(object parameter)
		{
			_act(parameter);
		}
	}
}
