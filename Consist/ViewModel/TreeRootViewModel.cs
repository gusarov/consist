using Consist.Implementation;
using Consist.Model;
using Consist.Utils;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Consist.ViewModel
{

	class TreeRootViewModel : ObservableCollection<RecordViewModel>
	{
		public TreeRootViewModel(RootDataContext root, PersistedMetadataProvider persistedMetadataProvider = null, GlobalIndex globalIndex = null, AnalyzerQueue analyzerQueue = null)
		{
			Root = root;
			_persistedMetadataProvider = persistedMetadataProvider ?? PersistedMetadataProvider.Instance;
			_globalIndex = globalIndex ?? GlobalIndex.Instance;
			_analyzerQueue = analyzerQueue ?? AnalyzerQueue.Instance;

			RunRefresh();
			// RefreshAsync().ConfigureAwait(false);
		}
		public RootDataContext Root { get; }

		private readonly Dictionary<uint, RecordViewModel> _viewModelsByDriveSerial = new Dictionary<uint, RecordViewModel>();

		// private Dictionary<string, RecordViewModel> _viewModelsByPinnedLocal = new Dictionary<string, RecordViewModel>();

		private readonly PersistedMetadataProvider _persistedMetadataProvider;
		private readonly GlobalIndex _globalIndex;
		private readonly AnalyzerQueue _analyzerQueue;

		public void RunRefresh()
		{
			Task.Run(() => RefreshAsync().Wait());
		}

		async Task RefreshAsync()
		{
			await Task.Delay(1); // switch
			MainThread.AssertNotUiThread();

			var gs = _persistedMetadataProvider.GetContainerGlobalSettings();
			var pinned = gs.Metadata.Where(x => x.MetadataRecordType == MetadataRecordType.PinnedLocalFolder);
			var viewModels = new List<RecordViewModel>();
			foreach (var pin in pinned)
			{
				var rt = _globalIndex.Request(pin.Value.EnsureEndsByDirectorySeparator());
				var vm = RecordViewModelFactory.Instance.Get(rt, false);
				if (vm == null)
				{
					vm = new RecordViewModel(rt, isPinnedToRoot: true);
					RecordViewModelFactory.Instance.Cache(rt, vm);
#if DEBUG
					var test = RecordViewModelFactory.Instance.Get(rt, false);
					if (test != vm)
					{
						throw new System.Exception();
					}
#endif
				}

				_analyzerQueue.Scan(vm.LocalPath, new AnalyzerContext
				{
					ScanNodeItself = true,
				});

				/*
				if (!_viewModelsByPinnedLocal.TryGetValue(pin.Value, out var vm))
				{
					_viewModelsByPinnedLocal[pin.Value] = 
					// _viewModelsByPinnedLocal[pin.Value] = vm = new RecordViewModel(new DirectoryInfo(pin.Value), isPinnedToRoot: true);
				}
				*/
				viewModels.Add(vm);
			}

			foreach (var drive in DriveInfo.GetDrives().Select(x=>new
			{
				Drive = x,
				Serial = _persistedMetadataProvider.GetVolumeSerial(x),
			}))
			{
				if (drive.Drive.IsReady)
				{
					if (!_viewModelsByDriveSerial.TryGetValue(drive.Serial, out var vm))
					{
						var rec = _globalIndex.Request(drive.Drive.RootDirectory.FullName);
						_viewModelsByDriveSerial[drive.Serial] = vm = new RecordViewModel(rec, drive.Drive.Name);
						RecordViewModelFactory.Instance.Cache(rec, vm);
#if DEBUG
						var test = RecordViewModelFactory.Instance.Get(rec, false);
						if (test != vm)
						{
							throw new System.Exception();
						}
#endif
					}

					_analyzerQueue.Scan(vm.LocalPath, new AnalyzerContext
					{
						ScanNodeItself = true,
					});
					viewModels.Add(vm);
				}
			}

			MainThread.Invoke("TreeRoot RefreshAsync", delegate
			{
				this.ViewMaintenance(viewModels);
			});

		}
	}

	class RootDataContext
	{
		public static RootDataContext Instance = new RootDataContext();

		RootDataContext(PersistedMetadataProvider persistedMetadataProvider = null, AnalyzerQueue analyzerQueue = null)
		{
			// ScanCommand = new DelegateCommand(x => Scan((RecordViewModel)x));
			TreeRoot = new TreeRootViewModel(this);
			_persistedMetadataProvider = persistedMetadataProvider ?? PersistedMetadataProvider.Instance;
			_analyzerQueue = analyzerQueue ?? AnalyzerQueue.Instance;
		}

		public Stats Stats { get; } = Stats.Instance;

		public TreeRootViewModel TreeRoot { get; }

		public WorldMetadata World { get; } = new WorldMetadata();

		private readonly PersistedMetadataProvider _persistedMetadataProvider;
		private readonly AnalyzerQueue _analyzerQueue;

		// public ICommand ScanCommand { get; }

		public void Scan(RecordViewModel record, AnalyzerContext ctx)
		{
			_analyzerQueue.Scan(record.LocalPath, ctx);
		}

		private MetadataContainer RemovePin(RecordViewModel parameter)
		{
			var set = _persistedMetadataProvider.GetContainerGlobalSettings();
			var existing = set.Metadata.Where(x =>
				x.MetadataRecordType == MetadataRecordType.PinnedLocalFolder &&
				x.Value.ToLowerInvariant() == parameter.LocalPath.ToLowerInvariant()).ToList();
			foreach (var item in existing)
			{
				set.Metadata.Remove(item);
			}

			return set;
		}

		internal void Unpin(RecordViewModel parameter)
		{
			var set = RemovePin(parameter);
			set.Save();
			TreeRoot.RunRefresh();
		}

		internal void Pin(RecordViewModel parameter)
		{
			var set = RemovePin(parameter);
			set.Metadata.Add(new MetadataRecord(MetadataRecordType.PinnedLocalFolder, parameter.LocalPath));
			set.Save();
			TreeRoot.RunRefresh();
		}
	}

	/// <summary>
	/// This stores currently discovered metadata as well as snapshot metadata
	/// </summary>
	class WorldMetadata
	{
		public ObservableCollection<SnapshotViewModel> Snapshots { get; } = new ObservableCollection<SnapshotViewModel>();


	}
}
