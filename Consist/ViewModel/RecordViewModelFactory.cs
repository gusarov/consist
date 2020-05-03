using Consist.Implementation;
using Consist.Model;
using Consist.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Consist.ViewModel
{
	class RecordViewModelFactory
	{
		public static RecordViewModelFactory Instance = new RecordViewModelFactory();

		private readonly Notifier _notifier;
		private readonly PersistedMetadataProvider _persistedMetadataProvider;
		private Dictionary<string, WeakReference<RecordViewModel>> _cache = new Dictionary<string, WeakReference<RecordViewModel>>();

		RecordViewModelFactory(Notifier notifier = null, PersistedMetadataProvider persistedMetadataProvider = null)
		{
			_notifier = notifier ?? Notifier.Instance;
			_persistedMetadataProvider = persistedMetadataProvider ?? PersistedMetadataProvider.Instance;
			_notifier.ItemScanned += ItemScanned;
		}

		private void ItemScanned(object sender, ItemScannedEventArgs e)
		{
			Debug.WriteLine($"Item Scanned: {e.Item?.KeyPath} Parent {e.Parent?.KeyPath}");

			var vm = Get(e.Item, false);
			if (vm != null)
			{
				if (vm.Record != e.Item)
				{
					throw new Exception("Something wrong here");
				}
				Debug.WriteLine($"Item HaveVM!");
				MainThread.Invoke("ItemScanned - update vm",
					args=> // fucking fast because of auto-batching & Dispatched re-schedule
					{
#if DEBUG
						if (args.VM.Record != args.E.Item)
						{
							throw new Exception("Something wrong here");
						}
#endif
						args.VM.NotifyRecord(e.Item);
					}, (VM: vm, E: e), true);
			}

			var vm2 = Get(e.Parent, false);
			if (vm2 != null)
			{
				Debug.WriteLine($"Parent HaveVM!");
				if (vm2.Record != e.Parent)
				{
					throw new Exception("Something wrong here");
				}

				if (vm2.ChildrenPromised)
				{
					MainThread.Invoke("ItemScanned - children added",
						x=>
						{
							x.VM.NotifyChildren(x.E.Item);
						}, (VM: vm2, E: e), true);
				}
			}
			else
			{
				// Debug.WriteLine($"Parent {e?.Parent?.KeyPath} DO NOT HaveVM!");
			}
		}

		/*
		string GetKey(string localPath)
		{
			// var contRoot = rt.Container.LocalRootPath;
			// var full = Path.Combine(rt.Container.LocalRootPath, rt.Record.KeyPath.Substring(1));
#if DEBUG
			if (!Path.IsPathRooted(localPath))
			{
				throw new Exception();
			}
#endif
			var root = Path.GetPathRoot(localPath);
			var ser = _persistedMetadataProvider.GetVolumeSerial(root);
			return $"?\\{ser}\\{localPath}"; // when drive letter will be changed - cache for UI will be invalidated
		}
		*/

		string GetKey(Record record)
		{
			if (record == null)
			{
				throw new Exception();
			}
			var container = record.Container;
			// var contRoot = rt.Container.LocalRootPath;
			var full = Path.Combine(container.LocalRootPath, record.KeyPath.Substring(1));
			// todo performance
			return $"?\\{full}";
			// var ser = _persistedMetadataProvider.GetVolumeSerial(container.LocalRootPath);
			// return $"?\\{ser}\\{full}"; // when drive letter will be changed - cache for UI will be invalidated
		}

		/*
		public RecordViewModel Get(string localPath, bool orCreate = true)
		{
			var key = GetKey(localPath);
			return GetInternal(key, orCreate, () =>
			{

			});
		}
		*/

		public RecordViewModel Get(Record record, bool orCreate = true)
		{
			if (record == null && !orCreate)
			{
				return null;
			}
			var key = GetKey(record);
			return GetInternal(key, orCreate, record);
		}

		RecordViewModel GetInternal(string key, bool orCreate, Record record)
		{

			lock (_cache)
			{
				_cache.TryGetValue(key, out var wr);

				var vm = wr?.Target;

				if (vm == null && orCreate)
				{
					vm = new RecordViewModel(record);
					_cache[key] = new WeakReference<RecordViewModel>(vm);
				}

				return vm;
			}
		}

		internal void Cache(Record record, RecordViewModel vm)
		{
			lock (_cache)
			{
				var key = GetKey(record);
				var existing = GetInternal(key, false, record);
				if (existing != null)
				{
					throw new Exception("Already have in cache");
				}

				_cache[key] = new WeakReference<RecordViewModel>(vm);
			}
		}
	}
}
