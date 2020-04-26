using Consist.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Consist.Utils;
using System.Diagnostics;

namespace Consist.ViewModel
{
	class RecordViewModel : ViewModel
	{
		private readonly Record _record;
		private readonly string _customName;
		private readonly FileSystemInfo _info;

		public RecordViewModel(FileSystemInfo info, string customName = null)
		{
			_customName = customName;
			_info = info;
		}

		public string Name
		{
			get { return _customName ?? _info.Name; }
		}

		public ImageSource Icon
		{
			get { return ShellManager.GetImageSource(_info.FullName, false); }
		}

		public int Size
		{
			get { return 0; }
		}

		public int Items { get; set; }

		private bool? _isExpanded;

		public bool IsExpanded
		{
			get
			{
				if (Name == "D:\\")
				{
					return _isExpanded ?? true;
				}
				return _isExpanded ?? false;
			}
			set
			{
				_isExpanded = value;
				OnPropertyChanged();
			}
		}

		private string _error;

		public string Error
		{
			get => _error;
			set
			{
				_error = value;
				OnPropertyChanged();
			}
		}

		public ObservableCollection<RecordViewModel> _children;

		public IEnumerable<RecordViewModel> Children
		{
			get
			{
				if (_children == null)
				{
					var children = new ObservableCollection<RecordViewModel>();
					_children = children;

					Task.Run(RescanChildren);
				}

				return _children;
			}
		}

		[DebuggerNonUserCode]
		[DebuggerStepThrough]
		void RescanChildren()
		{
			MainThread.AssertNotUiThread();
			if (_info is DirectoryInfo di)
			{
				try
				{
					foreach (var sub in di.EnumerateFileSystemInfos())
					{
						MainThread.Invoke(delegate { _children.Add(new RecordViewModel(sub)); });
					}
				}
				catch (Exception ex)
				{
					Error = ex.Message;
				}
			}
		}
	}
}
