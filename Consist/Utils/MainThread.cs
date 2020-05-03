using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Consist.Model;

namespace Consist.Utils
{
	public static class MainThread
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

			public bool IgnoreDuplicates { get; }

			protected ManagedItem(string name, bool ignoreDuplicates)
			{
				_name = name;
				IgnoreDuplicates = ignoreDuplicates;
			}

			public abstract void Execute();

			public override string ToString() => _name;

			protected bool Equals(ManagedItem other)
			{
				if (IgnoreDuplicates)
				{
					return _name == other._name;
				}

				return ReferenceEquals(this, other);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != this.GetType()) return false;
				return Equals((ManagedItem) obj);
			}

			public override int GetHashCode()
			{
				if (IgnoreDuplicates)
				{
					return (_name != null ? _name.GetHashCode() : 0);
				}

				return base.GetHashCode();
			}
		}

		class ActionManagedItem : ManagedItem
		{
			private readonly Action _act;

			public ActionManagedItem(string name, bool ignoreDuplicates, Action act) : base(name, ignoreDuplicates)
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

			public ActionManagedItem(string name, bool ignoreDuplicates, Action<T> act, T arg) : base(name, ignoreDuplicates)
			{
				_act = act;
				_arg = arg;
			}

			public override void Execute()
			{
				_act(_arg);
			}

			public override string ToString() => base.ToString() + " " +_arg;

			protected bool Equals(ActionManagedItem<T> other)
			{
				if (IgnoreDuplicates)
				{
					return base.Equals(other) && EqualityComparer<T>.Default.Equals(_arg, other._arg);
				}

				return ReferenceEquals(this, other);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != this.GetType()) return false;
				return Equals((ActionManagedItem<T>) obj);
			}

			public override int GetHashCode()
			{
				if (!IgnoreDuplicates)
				{
					return base.GetHashCode();
				}

				unchecked
				{
					return (base.GetHashCode() * 397) ^ EqualityComparer<T>.Default.GetHashCode(_arg);
				}
			}
		}

		static readonly Queue<ManagedItem> _queue = new Queue<ManagedItem>();
		static readonly Dictionary<ManagedItem, int> _set = new Dictionary<ManagedItem, int>();

		private static bool _dequeuerScheduled;

		public static void Invoke(string name, Action act, bool ignoreDuplicate = false)
		{
			lock (_queue)
			{
				var item = new ActionManagedItem(name, ignoreDuplicate, act);
				_queue.Enqueue(item);
				_set.TryGetValue(item, out var countInSet);
				_set[item] = countInSet + 1;
				EnsureScheduled();
			}
		}

		public static void Invoke<T>(string name, Action<T> act, T arg, bool ignoreDuplicate = false)
		{
			lock (_queue)
			{
				var item = new ActionManagedItem<T>(name, ignoreDuplicate, act, arg);
				_queue.Enqueue(item);
				_set.TryGetValue(item, out var countInSet);
				_set[item] = countInSet + 1;
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
				Task.Run(async delegate
				{
					// await Task.Delay(100); // to accumulate similar items
					await _dispatcher.BeginInvoke((Action) Dequeuer, DispatcherPriority.Background);
					// _dispatcher.BeginInvoke((Action) Dequeuer, DispatcherPriority.ContextIdle);
				});
			}
		}

		static void Dequeuer()
		{
			// Debug.WriteLine("Dequeuer started...");

			int lastQueueCount = -1;

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
						lastQueueCount = _queue.Count;
						if (lastQueueCount > 0)
						{
							item = _queue.Dequeue();
							var countInSet = _set[item];
							if (countInSet == 1)
							{
								_set.Remove(item);
							}
							else
							{
								_set[item] = countInSet - 1;
							}
						}
						else
						{
							item = null;
						}
					}

					if (item == null)
					{
#if DEBUG
						if (_set.Count != 0 && Debugger.IsAttached)
						{
							Debugger.Break();
						}
#endif
						break;
					}

					cnt++;

					if (item.IgnoreDuplicates)
					{
						lock (_queue)
						{
							if (_set.ContainsKey(item))
							{
								Debug.WriteLine("Dequeuer skipped an item with identifier that exists in the subsequent queue: " +item);
								continue;
							}
						}
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
				Debug.WriteLine($"Dequeuer processed {cnt} items at once during {time}ms:\r\n {string.Join("\r\n", processed)}");
#endif
			}
			finally
			{
				// if (lastQueueCount != -1)
				{
					Stats.Instance.MessageLoop = lastQueueCount;
				}
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
