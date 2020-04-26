using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Consist.Utils
{
	static class MainThread
	{
		private static Thread _thread;
		private static Dispatcher _dispatcher;

		public static void Register()
		{
			_dispatcher = Application.Current.Dispatcher;
			if (_dispatcher != Dispatcher.CurrentDispatcher)
			{
				throw new Exception("Must be called from UI Thread");
			}

			_thread = Thread.CurrentThread;
		}

		public static void AssertNotUiThread()
		{
			if (_thread == null)
			{
				throw new Exception("ThreadAssert not initialized");
			}
			if (_dispatcher == Dispatcher.CurrentDispatcher || Thread.CurrentThread == _thread)
			{
				throw new Exception("Do not call this from UI Thread");
			}
		}


		public static void AssertUiThread()
		{
			if (_thread == null)
			{
				throw new Exception("ThreadAssert not initialized");
			}
			if (_dispatcher != Dispatcher.CurrentDispatcher || Thread.CurrentThread != _thread)
			{
				throw new Exception("Must be called from UI Thread");
			}
		}

		public static void Invoke(Action act)
		{
			_dispatcher.Invoke(act);
		}

	}
}
