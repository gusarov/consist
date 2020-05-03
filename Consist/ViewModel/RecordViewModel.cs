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
using System.Windows.Input;
using System.ComponentModel;
using Consist.Implementation;

namespace Consist.ViewModel
{
	class RecordViewModel : ViewModel, ITreeListNode
	{
		public override string ToString() => $"[VM: {_record}]";

		private readonly Record _record;
		// public Record Record => _record;

		private readonly MetadataContainer _container;
		private readonly string _customName;
		private readonly bool _isFolder;

		/*
		public RecordViewModel(Record record, string customName = null, bool isPinnedToRoot = false)
			:this(recordTuple.Container, recordTuple.Record, customName, isPinnedToRoot)
		{
		}
		*/

		public RecordViewModel(Record record, string customName = null, bool isPinnedToRoot = false)
		{
			_container = record.Container;
			_record = record;
			_customName = customName;
			IsPinnedToRoot = isPinnedToRoot;
			_isFolder = _record.IsFolder;

			// Debug.WriteLine(LocalPath);
#if DEBUG
			var stack = new StackTrace();
			if (Debugger.IsAttached)
			{
				Task.Run(async delegate
				{
					await Task.Delay(200);
					// should be cached if not by now then immediately after construction
					if (RecordViewModelFactory.Instance.Get(record, false) != this)
					{
						Debug.WriteLine(stack);
						Debugger.Break();
					}
				});
			}
#endif
		}

		public string ToolTip
		{
			get
			{
				if (IsPinnedToRoot)
				{
					return LocalPath;
				}

				return null;
			}
		}


		public string LocalPath
		{
			get
			{
				var lrp = _container.LocalRootPath;
				if (string.IsNullOrEmpty(lrp))
				{
					throw new Exception("Container local path is unknown");
				}
				return Path.Combine(lrp, _record.KeyPath.TrimStart(Path.DirectorySeparatorChar));
			}
		}

		public string Name
		{
			get
			{
				return _customName ?? _record.Name;
				// return _customName ?? _info.Name;
			}
		}

		private bool _iconRequested;

		private static readonly Dictionary<string, ImageSource> _iconByExt = new Dictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);
		private Icon _icon;

		private static readonly Lazy<ImageSource> _imageSourceDefaultFolder = new Lazy<ImageSource>(() =>
		{
			MainThread.AssertUiThread();
			return ShellManager.GetImageSource(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				false, true);
		});

		private static ImageSource ImageSourceDefaultFolder => _imageSourceDefaultFolder.Value;

		// private static ImageSource _imageSourceDefaultFile = ShellManager.GetImageSource(".ttt", false, false);

		private static readonly HashSet<string> _iconNeeded = new HashSet<string>()
		{
			".exe",
			".lnk",
		};

		bool NeedIcon()
		{
			if (!_isFolder)
			{
				return _iconNeeded.Contains(Path.GetExtension(_record.KeyPath).ToLowerInvariant());
			}

			return true; // folders and drives have decorators
		}

		private ImageSource _imageSource;
		public ImageSource ImageSource
		{
			get
			{
				MainThread.AssertUiThread();
				if (!_iconRequested)
				{
					_iconRequested = true;
					if (NeedIcon())
					{
						// MainThread.Invoke("ImageSource Icon Request",
						Task.Run(
						delegate
						{
#if DELAY
						await Task.Delay(500);
#endif
							if (_isFolder)
							{
								var icon = ShellManager.GetIcon(LocalPath, true);
								if (icon != null)
								{
									_icon = icon;
								}
							}
							else
							{
								var icon = ShellManager.GetIcon(LocalPath, false);
								if (icon != null)
								{
									_icon = icon;
								}
							}

							MainThread.Invoke("get_ImageSource deferred part",
								delegate { OnPropertyChanged(nameof(ImageSource)); });
						});
						
					}
				}

				if (_imageSource == null)
				{
					if (_icon != null)
					{
						_imageSource = ShellManager.GetImageSource(_icon);
					}
					else if (_isFolder)
					{
						return ImageSourceDefaultFolder;
					}
					else
					{
						var ext = Path.GetExtension(_record.KeyPath);
						if (!_iconByExt.TryGetValue(ext, out var extIcon))
						{
							_iconByExt[ext] = extIcon = ShellManager.GetImageSource(ext, false, false);
						}
						return extIcon;
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
				return _size ?? (_size = _record.FileSize);
				/*
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
				*/
			}
		}
		// DateTime? _lastChange;
		public DateTime? LastChange
		{
			get
			{
				return _record.LastModificationUtc;
				/*
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
				*/
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
				if (_attributes == null && _record.FileAttributes.HasValue)
				{

					// Task.Run(delegate
					{
						var r = "";
						var a = _record.FileAttributes.Value;
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
						if ((a & FileAttributes.NotContentIndexed) > 0)
						{
							r += "N";
							a &= ~FileAttributes.NotContentIndexed;
						}
						if ((a & FileAttributes.ReparsePoint) > 0)
						{
							r += "P";
							a &= ~FileAttributes.ReparsePoint;
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
						/*
						MainThread.Invoke("get_Attributes deferred part", delegate
						{
							OnPropertyChanged(nameof(Attributes));
						});
						*/
					}
					// );
				}

				return _attributes;
			}

		}

		private bool? _isExpanded;
		private bool _isBeenEverExpanded;

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
				if (!_isBeenEverExpanded)
				{
					_isBeenEverExpanded = true;
					Task.Run(delegate
					{
						AnalyzerQueue.Instance.Scan(LocalPath, new AnalyzerContext
						{
							ScanChildren = true,
						});
					});
				}
				OnPropertyChanged();
			}
		}

		public string AttributesString
		{
			get { return (Record.FileAttributes & ~FileAttributes.Directory).ToString(); }
		}

		public string DebugString
		{
			get
			{
				int? cnt = null;
				if (_children != null)
				{
					cnt = _children.Count;
				}

				return $"{cnt}";
			}
		}

		public double? Percentage
		{
			get { return default; }
		}

		public string Error => _record.Error;

		public string HashCode
		{
			get
			{
				if (_record.Hash == null)
				{
					return null;
				}
				return string.Join("",_record.Hash.Value.Select(x => x.ToString("X2")));
			}
		}

		public bool ChildrenPromised => _children != null;

		ObservableCollection<RecordViewModel> _children;

		public IEnumerable<RecordViewModel> Children
		{
			get
			{
				if (_isFolder)
				{
					if (_children == null)
					{
						_children = new ObservableCollection<RecordViewModel>();
						UpdateCollection();
						OnPropertyChanged(nameof(ChildrenIfAny));
						RootDataContext.Instance.Scan(this, new AnalyzerContext
						{
							ScanNodeItself = false,
							ScanChildren = true,
							ScanRecursively = false,
						});
					}
				}
				return _children;
			}
		}

		public ObservableCollection<RecordViewModel> ChildrenIfAny
		{
			get
			{
				return _children;
			}
		}

		void UpdateCollection()
		{
			if (_record.SubRecords != null)
			{
				Record[] clone;
				lock (_record.SubRecords)
				{
					clone = _record.SubRecords.ToArray();
				}
				var subs = clone.Select(x => RecordViewModelFactory.Instance.Get(x))
#if DEBUG
					.ToArray()
#endif
					;

				/*
				Debug.WriteLine("Expected List:");
				foreach (var item in subs)
				{
					Debug.WriteLine(item.Record);
				}
				Debug.WriteLine("==== end ===");
				*/
				_children.ViewMaintenance(subs);

				OnPropertyChanged(nameof(HasItems));
			}
		}

		// private bool? _hasItems;
		public bool HasItems
		{
			get
			{
				if (_isFolder)
				{
					if (_children == null)
					{
						return true; // we don't really know, scan might be pending
					}

					if (_children.Count == 0)
					{
						return false; // we know for sure
					}

					return true;

					/*
					if (_hasItems == null)
					{
						Task.Run(delegate
						{
							try
							{
								_hasItems = _infoDir.EnumerateFileSystemInfos().Any();
							}
							catch
							{
								_hasItems = false;
							}

							if (_hasItems == false) // already been returned true
							{
								MainThread.Invoke("HasItems deferred", () => OnPropertyChanged(nameof(HasItems)));
							}
						});
					}
					*/
					// return _hasItems ?? true;
				}
				return false; // file
			}
		}

		public bool IsPinnedToRoot { get; }

		async void RescanChildren()
		{
			/*
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
			*/
		}

		public Record Record => _record;

		public void NotifyRecord(Record record)
		{
			if (_record != record)
			{
				throw new Exception();
			}

			// Debug.WriteLine($"Notify VM: {Record.KeyPath}");
			// _record = record;
			OnPropertyChanged(null); // all for WPF
		}
		public void NotifyChildren(Record record)
		{
			// _record = record;
			// Debug.WriteLine($"Synchronize VM Children of: {Record.KeyPath}");
			UpdateCollection();
		}
	}
}
