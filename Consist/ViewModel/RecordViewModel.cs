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
using System.Globalization;
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
		private readonly DirectoryInfo _infoDir;
		private readonly FileInfo _infoFile;
		private readonly bool _isFolder;

		public RecordViewModel(FileSystemInfo info, string customName = null)
		{
			_customName = customName;
			_info = info;
			_infoDir = info as DirectoryInfo;
			_infoFile = info as FileInfo;
			_isFolder = _infoDir != null;
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
						MainThread.Invoke("get_ImageSource deferred part", delegate
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

		private long? _size;
		public long? Size
		{
			get
			{
				if (_size is null)
				{
					Task.Run(delegate
					{
						if (_isFolder)
						{
							_size = default;
						}
						else
						{
							_size = _infoFile.Length;
							MainThread.Invoke("get_Size deferred part", delegate
							{
								OnPropertyChanged(nameof(Size));
							});
						}
					});
				}
				return _size;
			}
		}
		DateTime? _lastChange;
		public DateTime? LastChange
		{
			get
			{
				if (_lastChange is null)
				{
					Task.Run(delegate
					{
						_lastChange = _info.LastWriteTimeUtc;
						MainThread.Invoke("get_LastChange deferred part",
							() => OnPropertyChanged(nameof(LastChange)));
					});
				}
				return _lastChange;
			}
		}

		public int? Items
		{
			get
			{
				if (_isFolder)
				{
					return default;
				}
				else
				{
					return 1;
				}
			}
		}

		private string _attributes;
		public string Attributes
		{
			get
			{
				if (_attributes == null)
				{
					Task.Run(delegate
					{
						var r = "";
						var a = _info.Attributes;
						var orig = a;

						if ((a & FileAttributes.ReadOnly) > 0)
						{
							r += "R";
							a &= ~FileAttributes.ReadOnly;
						}
						if ((a & FileAttributes.Hidden) > 0)
						{
							r += "H";
							a &= ~FileAttributes.Hidden;
						}
						if ((a & FileAttributes.System) > 0)
						{
							r += "S";
							a &= ~FileAttributes.System;
						}
						if ((a & FileAttributes.Archive) > 0)
						{
							r += "A";
							a &= ~FileAttributes.Archive;
						}
						if ((a & FileAttributes.Encrypted) > 0)
						{
							r += "E";
							a &= ~FileAttributes.Encrypted;
						}
						if ((a & FileAttributes.Compressed) > 0)
						{
							r += "C";
							a &= ~FileAttributes.Compressed;
						}

						if (_isFolder)
						{
							a &= ~FileAttributes.Directory;
						}

						if (a == 0)
						{
							_attributes = r;
						}
						else if ((int) orig == -1)
						{
							_attributes = "(0x" + (((int) orig).ToString("X8")) + ")";
						}
						else
						{
							_attributes = r + " " + a + " (0x" + (((int) orig).ToString("X8")) + ")";
						}

						MainThread.Invoke("get_Attributes deferred part", delegate
						{
							OnPropertyChanged(nameof(Attributes));
						});
					});
				}

				return _attributes;
			}

		}

		private bool? _isExpanded;

		public bool IsExpanded
		{
			get
			{
				/*
				if (Name == "D:\\")
				{
					return _isExpanded ?? true;
				}
				*/
				return _isExpanded ?? false;
			}
			set
			{
				_isExpanded = value;
				OnPropertyChanged();
			}
		}

		public double? Percentage
		{
			get { return default; }
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
				PopulateChildren(false);
				return _children;
			}
		}

		void PopulateChildren(bool any)
		{
			// call to this method is thread safe
			if (_isFolder)
			{
				if (_children == null)
				{
					_children = new ObservableCollection<RecordViewModel>();
					Task.Run(() => RescanChildren(any));
				}
			}
		}

		public bool HasItems
		{
			get
			{
				if (_isFolder)
				{
					return Children.Any();
					// PopulateChildren(true);
					// return _children.Any();
				}

				return false;
			}
		}

		// [DebuggerNonUserCode]
		// [DebuggerStepThrough]
		async void RescanChildren(bool any)
		{
			// call to this method is NOT thread safe
#if DELAY
			await Task.Delay(1000);
#endif
			MainThread.AssertNotUiThread();
			lock (_children)
			{
				if (_info is DirectoryInfo di)
				{
					try
					{
						var firstPush = false;

						void Push(IEnumerable<RecordViewModel> items)
						{
							MainThread.Invoke($"Children rescan push batch {_info.FullName}... ", lst =>
							{
								if (!firstPush)
								{
									_children.Clear();
								}

								foreach (var item in lst)
								{
									_children.Add(item);
								}

								if (!firstPush)
								{
									firstPush = true;
									OnPropertyChanged(nameof(HasItems));
								}
							}, items);
						}

						int c = 0;
						var list = new List<RecordViewModel>();
						foreach (var sub in di.EnumerateFileSystemInfos())
						{
							var model = new RecordViewModel(sub);
							list.Add(model);
							if (any)
							{
								break; // this is enough for now
							}
							if (++c % 100 == 0)
							{
								Push(list);
								list = new List<RecordViewModel>();
							}
						}

						if (list.Any())
						{
							Push(list);
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
}
