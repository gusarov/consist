using Consist.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Consist.Implementation
{


	public class PersistedMetadataProvider
	{
		public static PersistedMetadataProvider Instance = new PersistedMetadataProvider();

		internal PersistedMetadataProvider()
		{
			
		}

		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool GetVolumeInformation(
			string rootPathName,
			StringBuilder volumeNameBuffer,
			int volumeNameSize,
			out uint volumeSerialNumber,
			out uint maximumComponentLength,
			out FileSystemFeature fileSystemFlags,
			StringBuilder fileSystemNameBuffer,
			int nFileSystemNameSize);

		public uint GetVolumeSerial(DriveInfo drive)
		{
			return GetVolumeSerial(drive.RootDirectory.FullName);
		}

		public uint GetVolumeSerial(string root)
		{
			var volumeName = new StringBuilder(256);
			var fileSystemName = new StringBuilder(256);
			if (GetVolumeInformation(root
				, volumeName
				, volumeName.Capacity
				, out var serial
				, out var maxCompLen
				, out var fsFeatures
				, fileSystemName
				, fileSystemName.Capacity
			))
			{
				/*
				DateTime FromCtmToDateTime(uint dateTime)
				{
					var startTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
					return startTime.AddSeconds(Convert.ToDouble(dateTime));
				}

				var dd = FromCtmToDateTime(serial);
				*/
				return serial;
			}

			return default;
		}

		[Flags]
		public enum FileSystemFeature : uint
		{
			/// <summary>
			/// The file system preserves the case of file names when it places a name on disk.
			/// </summary>
			CasePreservedNames = 2,

			/// <summary>
			/// The file system supports case-sensitive file names.
			/// </summary>
			CaseSensitiveSearch = 1,

			/// <summary>
			/// The specified volume is a direct access (DAX) volume. This flag was introduced in Windows 10, version 1607.
			/// </summary>
			DaxVolume = 0x20000000,

			/// <summary>
			/// The file system supports file-based compression.
			/// </summary>
			FileCompression = 0x10,

			/// <summary>
			/// The file system supports named streams.
			/// </summary>
			NamedStreams = 0x40000,

			/// <summary>
			/// The file system preserves and enforces access control lists (ACL).
			/// </summary>
			PersistentACLS = 8,

			/// <summary>
			/// The specified volume is read-only.
			/// </summary>
			ReadOnlyVolume = 0x80000,

			/// <summary>
			/// The volume supports a single sequential write.
			/// </summary>
			SequentialWriteOnce = 0x100000,

			/// <summary>
			/// The file system supports the Encrypted File System (EFS).
			/// </summary>
			SupportsEncryption = 0x20000,

			/// <summary>
			/// The specified volume supports extended attributes. An extended attribute is a piece of
			/// application-specific metadata that an application can associate with a file and is not part
			/// of the file's data.
			/// </summary>
			SupportsExtendedAttributes = 0x00800000,

			/// <summary>
			/// The specified volume supports hard links. For more information, see Hard Links and Junctions.
			/// </summary>
			SupportsHardLinks = 0x00400000,

			/// <summary>
			/// The file system supports object identifiers.
			/// </summary>
			SupportsObjectIDs = 0x10000,

			/// <summary>
			/// The file system supports open by FileID. For more information, see FILE_ID_BOTH_DIR_INFO.
			/// </summary>
			SupportsOpenByFileId = 0x01000000,

			/// <summary>
			/// The file system supports re-parse points.
			/// </summary>
			SupportsReparsePoints = 0x80,

			/// <summary>
			/// The file system supports sparse files.
			/// </summary>
			SupportsSparseFiles = 0x40,

			/// <summary>
			/// The volume supports transactions.
			/// </summary>
			SupportsTransactions = 0x200000,

			/// <summary>
			/// The specified volume supports update sequence number (USN) journals. For more information,
			/// see Change Journal Records.
			/// </summary>
			SupportsUsnJournal = 0x02000000,

			/// <summary>
			/// The file system supports Unicode in file names as they appear on disk.
			/// </summary>
			UnicodeOnDisk = 4,

			/// <summary>
			/// The specified volume is a compressed volume, for example, a DoubleSpace volume.
			/// </summary>
			VolumeIsCompressed = 0x8000,

			/// <summary>
			/// The file system supports disk quotas.
			/// </summary>
			VolumeQuotas = 0x20
		}

		string GetDefaultMetadataPath()
		{
			var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			var app = "Consist";
			return Path.Combine(appData, app, "Metadata");
		}

		private readonly Dictionary<uint, MetadataContainer> _containers = new Dictionary<uint, MetadataContainer>();

		MetadataContainer _gs;
		public MetadataContainer GetContainerGlobalSettings()
		{
			if (_gs == null)
			{
				var container = new MetadataContainer();
				var path = Path.Combine(GetDefaultMetadataPath(), $"GlobalSettings.consist");
				container.Load(path); // if file not here - just remember where it should be
				_gs = container;
			}

			return _gs;
		}

		public MetadataContainer GetContainer(string path)
		{
			foreach (var drive in DriveInfo.GetDrives())
			{
				var root = drive.RootDirectory.FullName;
				var rootSlashed = root.EndsWith(Path.DirectorySeparatorChar.ToString()) ||
				                  root.EndsWith(Path.AltDirectorySeparatorChar.ToString())
					? root
					: root + Path.DirectorySeparatorChar;

				if (path == root || path == rootSlashed || path.StartsWith(rootSlashed))
				{
					var serial = GetVolumeSerial(drive);
					if (serial == default)
					{
						var fake = new MetadataContainer();
						fake.Metadata.Add(new MetadataRecord(MetadataRecordType.OriginalPath, path));
						return fake;
						// throw new Exception($"Unable to prepare container - no serial on the volume '{drive.RootDirectory.Name}'");
					}

					if (!_containers.TryGetValue(serial, out var container))
					{
						_containers[serial] = container = OpenContainer(serial, drive);
					}

					return container;
				}
			}

			throw new Exception($"Unable to prepare container - no drive for '{path}'");
		}

		public MetadataContainer OpenContainer(uint serial, DriveInfo drive)
		{
			// todo show user warning when there is multiple volumes have same serial
			var container = new MetadataContainer();
			var path = Path.Combine(GetDefaultMetadataPath(), $"{serial:X4}.consist");
			container.Load(path); // if file not here - just remember where it should be

			var origPath = drive.RootDirectory.FullName;
			if (!container.Metadata.Any(x => x.MetadataRecordType == MetadataRecordType.OriginalPath && x.Value == origPath))
			{
				container.Metadata.Add(new MetadataRecord(MetadataRecordType.OriginalPath, origPath));
			}
			return container;
		}

		public Record TryGet(string path)
		{
			// find container
			var container = GetContainer(path);

			var root = Path.GetPathRoot(path);
			var rel = path.Substring(root.Length).EnsureStartsFromDirectorySeparator();
			return container.TryGet(rel);
		}

		public void Set(string path, Record rec)
		{
			// find container
			var container = GetContainer(path);

			// var root = Path.GetPathRoot(path);
			// var rel = path.Substring(root.Length).EnsureStartsFromDirectorySeparator();

			container.Add(rec);
		}
	}
}
