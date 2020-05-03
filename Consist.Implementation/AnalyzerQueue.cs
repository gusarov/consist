using Consist.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Consist.Implementation
{
	public class AnalyzerQueue
	{
		public static AnalyzerQueue Instance = new AnalyzerQueue();

		private readonly ConcurrentQueue<(Analyzer Analyzer, AnalyzerContext Context)> _analyzers
			= new ConcurrentQueue<(Analyzer, AnalyzerContext)>();

		private readonly AutoResetEvent _event = new AutoResetEvent(false);

		internal AnalyzerQueue()
		{
			Task.Run(delegate
			{
				try
				{
					while (true)
					{
						_event.WaitOne();
						while (_analyzers.TryDequeue(out var item))
						{
							try
							{
								item.Analyzer.Scan(item.Context);
							}
							catch (Exception ex)
							{
								Debug.WriteLine(ex);
							}
						}

						Stats.Instance.AnalyzeQueue = _analyzers.Count; // wrong thread
						Notifier.Instance.NotifyStatsChanged(null);
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex);
				}
			});
		}

		public void Scan(string localPath, AnalyzerContext ctx)
		{
			Debug.WriteLine($"Queue: {localPath} Self:{ctx.ScanNodeItself} Kids:{ctx.ScanChildren}");

			var item = (new Analyzer(localPath), ctx);
			int cnt;
			lock (_analyzers)
			{
				_analyzers.Enqueue(item);
				cnt = _analyzers.Count;
				_event.Set();
			}

			Stats.Instance.AnalyzeQueue = cnt;
			Notifier.Instance.NotifyStatsChanged(null);
		}
			
	}
}
