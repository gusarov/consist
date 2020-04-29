using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
		private static SynchronizationContext _context;

		public static void Register()
		{
			if (_thread != null)
			{
				if (_thread != Thread.CurrentThread)
				{
					throw new Exception("Cannot initialize multiple times with different thread");
				}
			}
			else
			{
				_thread = Thread.CurrentThread;
				_context = SynchronizationContext.Current;
				_dispatcher = Dispatcher.CurrentDispatcher;
				if (_dispatcher != Application.Current.Dispatcher)
				{
					throw new Exception("Must initialize from main UI thread");
				}
				Debug.WriteLine("Set Main Thread: " + _thread.ManagedThreadId);
			}
		}

		// [DebuggerStepThrough]
		public static void AssertNotUiThread()
		{
			if (_thread == null || _dispatcher == Dispatcher.CurrentDispatcher || Thread.CurrentThread == _thread)
			{
				if (Debugger.IsAttached)
				{
					Debugger.Break();
				}
				if (_thread == null)
				{
					throw new Exception("ThreadAssert not initialized");
				}
				throw new Exception("Do not call this from UI Thread");
			}
		}


		public static void AssertUiThread()
		{
			if (_thread == null || _dispatcher != Dispatcher.CurrentDispatcher || Thread.CurrentThread != _thread)
			{
				if (_thread == null)
				{
					throw new Exception("ThreadAssert not initialized");
				}
				throw new Exception("Must be called from UI Thread");
			}
		}

		abstract class ManagedItem
		{
			private readonly string _name;

			protected ManagedItem(string name)
			{
				_name = name;
			}

			public abstract void Execute();

			public override string ToString() => _name;
		}

		class ActionManagedItem : ManagedItem
		{
			private readonly Action _act;

			public ActionManagedItem(string name, Action act) : base(name)
			{
				_act = act;
			}

			public override void Execute()
			{
				_act();
			}
		}

		class ActionManagedItem<T> : ManagedItem
		{
			private readonly Action<T> _act;
			private readonly T _arg;

			public ActionManagedItem(string name, Action<T> act, T arg) : base(name)
			{
				_act = act;
				_arg = arg;
			}

			public override void Execute()
			{
				_act(_arg);
			}

			public override string ToString() => base.ToString() + " " +_arg;
		}

		static readonly Queue<ManagedItem> _queue = new Queue<ManagedItem>();
		private static bool _dequeuerScheduled;

		public static void Invoke(string name, Action act, DispatcherPriority priority = DispatcherPriority.Background)
		{
			lock (_queue)
			{
				_queue.Enqueue(new ActionManagedItem(name, act));
				EnsureScheduled();
			}
		}

		public static void Invoke<T>(string name, Action<T> act, T arg, DispatcherPriority priority = DispatcherPriority.Background)
		{
			lock (_queue)
			{
				_queue.Enqueue(new ActionManagedItem<T>(name, act, arg));
				EnsureScheduled();
			}
		}

		static void EnsureScheduled()
		{
			// this is also locked over _queue
			if (!_dequeuerScheduled)
			{
				// Debug.WriteLine("Scheduling Dequeuer...");
				_dequeuerScheduled = true;
				_dispatcher.BeginInvoke((Action) Dequeuer, DispatcherPriority.ContextIdle);
			}
		}

		static void Dequeuer()
		{
			// Debug.WriteLine("Dequeuer started...");

			try
			{
				AssertUiThread();

				// invoke a batch of items that is quick enough to make it not noticeable for user
				var started = Stopwatch.StartNew();
				int cnt = 0;
				long time = 0;
#if DEBUG
				var processed = new List<ManagedItem>();
#endif

				do
				{
					ManagedItem item;
					lock (_queue)
					{
						item = _queue.Count > 0 ? _queue.Dequeue() : null;
					}

					cnt++;
					if (item == null)
					{
						break;
					}

					try
					{
						item.Execute();
					}
					catch (Exception ex)
					{
						Trace.TraceError("Invoke Failed: {0}", ex);
					}
#if DEBUG
					processed.Add(item);
#endif
					time = started.ElapsedMilliseconds;
				} while (time < 60);

#if DEBUG
				// Debug.WriteLine($"Dequeuer processed {cnt} items at once during {time}ms:\r\n {string.Join("\r\n", processed)}");
#endif
			}
			finally
			{
				lock (_queue)
				{
					_dequeuerScheduled = false; // planning to quit
					if (_queue.Count > 0)
					{
						// put this back as a last and low priority element in a message loop
						EnsureScheduled(); // might set it back
					}
				}
			}
		}

	}
}
