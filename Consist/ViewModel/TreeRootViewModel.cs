using Consist.Model;
using Consist.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consist.ViewModel
{
	class TreeRootViewModel : ObservableCollection<RecordViewModel>
	{
		public TreeRootViewModel()
		{
			Task.Run(() => RefreshAsync().Wait());
			// RefreshAsync().ConfigureAwait(false);
		}

		async Task RefreshAsync()
		{
			await Task.Delay(1); // switch
			MainThread.AssertNotUiThread();
			foreach (var drive in DriveInfo.GetDrives())
			{
				MainThread.Invoke(delegate
				{
					Add(new RecordViewModel(drive.RootDirectory, drive.Name)
					{

					});
				});
			}
		}
	}

	class RootDataContext
	{
		public static RootDataContext Instance = new RootDataContext();

		RootDataContext()
		{
			
		}

		public TreeRootViewModel TreeRoot { get; } = new TreeRootViewModel();

		public WorldMetadata World { get; } = new WorldMetadata();

	}

	/// <summary>
	/// This stores currently discovered metadata as well as snapshot metadata
	/// </summary>
	class WorldMetadata
	{
		public ObservableCollection<SnapshotViewModel> Snapshots { get; } = new ObservableCollection<SnapshotViewModel>();


	}
}
