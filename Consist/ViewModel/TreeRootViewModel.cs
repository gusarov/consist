using Consist.Implementation;
using Consist.Model;
using Consist.Utils;
using System;
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
		public TreeRootViewModel(RootDataContext root, PersistedMetadataProvider persistedMetadataProvider = null)
		{
			RunRefresh();
			Root = root;
			_persistedMetadataProvider = persistedMetadataProvider ?? PersistedMetadataProvider.Instance;
			// RefreshAsync().ConfigureAwait(false);
		}
		public RootDataContext Root { get; }

		private Dictionary<uint, RecordViewModel> _viewModelsByDriveSerial = new Dictionary<uint, RecordViewModel>();
		private Dictionary<string, RecordViewModel> _viewModelsByPinnedLocal = new Dictionary<string, RecordViewModel>();
		private readonly PersistedMetadataProvider _persistedMetadataProvider;

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
				if (!_viewModelsByPinnedLocal.TryGetValue(pin.Value, out var vm))
				{
					_viewModelsByPinnedLocal[pin.Value] = vm = new RecordViewModel(new DirectoryInfo(pin.Value), isPinnedToRoot: true);
				}
				viewModels.Add(vm);
			}

			foreach (var drive in DriveInfo.GetDrives().Select(x=>new
			{
				Drive = x,
				Serial = _persistedMetadataProvider.GetVolumeSerial(x),
			}))
			{
				if (!_viewModelsByDriveSerial.TryGetValue(drive.Serial, out var vm))
				{
					_viewModelsByDriveSerial[drive.Serial] = vm = new RecordViewModel(drive.Drive.RootDirectory, drive.Drive.Name);
				}
				viewModels.Add(vm);
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

		RootDataContext(PersistedMetadataProvider persistedMetadataProvider = null)
		{
			// ScanCommand = new DelegateCommand(x => Scan((RecordViewModel)x));
			TreeRoot = new TreeRootViewModel(this);
			_persistedMetadataProvider = persistedMetadataProvider ?? PersistedMetadataProvider.Instance;
		}

		public TreeRootViewModel TreeRoot { get; }

		public WorldMetadata World { get; } = new WorldMetadata();

		private readonly Queue<Analyzer> _analyzers = new Queue<Analyzer>();
		private readonly PersistedMetadataProvider _persistedMetadataProvider;

		// public ICommand ScanCommand { get; }

		public void Scan(RecordViewModel record, AnalyzerContext ctx)
		{
			Analyzer analyzer;
			lock (_analyzers)
			{
				_analyzers.Enqueue(analyzer = new Analyzer(record.LocalPath));
			}

			Task.Run(() =>
			{
				analyzer.Scan(ctx);
			});
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
