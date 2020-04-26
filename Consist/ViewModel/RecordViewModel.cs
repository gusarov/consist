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
using System.Threading;

using Icon = System.Drawing.Icon;
using Consist.Interop;
using Consist.View;

namespace Consist.ViewModel
{
	class RecordViewModel : ViewModel, ITreeListNode
	{
		private readonly Record _record;
		private readonly string _customName;
		private readonly FileSystemInfo _info;
		private readonly bool _isFolder;

		public RecordViewModel(FileSystemInfo info, string customName = null)
		{
			_customName = customName;
			_info = info;
			_isFolder = info is DirectoryInfo;
		}

		public string Name
		{
			get { return _customName ?? _info.Name; }
		}

		private bool _iconRequested;

		private Icon _icon;

		private static ImageSource _imageSourceDefaultFolder = ShellManager.GetImageSource(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), false, true);

		private static ImageSource _imageSourceDefaultFile = ShellManager.GetImageSource(".ttt", false, false);

		private ImageSource _imageSource;
		public ImageSource ImageSource
		{
			get
			{
				if (!_iconRequested)
				{
					_iconRequested = true;
					Task.Run(async delegate
					{
#if DELAY
						await Task.Delay(500);
#endif
						if (_info is DirectoryInfo di)
						{
							_icon = ShellManager.GetIcon(_info.FullName, true);
						}
						else if (_info is FileInfo fi)
						{
							_icon = ShellManager.GetIcon(_info.FullName, false);
						}
						MainThread.Invoke(delegate
						{
							OnPropertyChanged(nameof(ImageSource));
						});
					});
				}

				if (_imageSource == null)
				{
					if (_icon != null)
					{
						_imageSource = ShellManager.GetImageSource(_icon);
					}
					else
					{
						return _isFolder
							? _imageSourceDefaultFolder
							: _imageSourceDefaultFile;
					}
				}
				return _imageSource;
			}
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

		public bool HasItems => Children.Any();

		[DebuggerNonUserCode]
		[DebuggerStepThrough]
		async void RescanChildren()
		{
#if DELAY
			await Task.Delay(1000);
#endif
			MainThread.AssertNotUiThread();
			if (_info is DirectoryInfo di)
			{
				try
				{
					var list = new List<RecordViewModel>();
					foreach (var sub in di.EnumerateFileSystemInfos())
					{
						list.Add(new RecordViewModel(sub));
					}

					if (list.Any())
					{
						MainThread.Invoke(delegate
						{
							for (int i = 0; i < list.Count; i++)
							{
								_children.Add(list[i]);
							}

							OnPropertyChanged(nameof(HasItems));
						});
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
