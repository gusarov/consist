using Consist.Implementation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consist.Tests
{
	[TestClass]
	public class PersistedMetadataProviderTests
	{
		[TestMethod]
		public void Should_0_read_serial_of_volumes()
		{
			var sut = new PersistedMetadataProvider();
			foreach (var drive in DriveInfo.GetDrives())
			{
				Console.WriteLine($"{drive.RootDirectory.FullName} {sut.GetVolumeSerial(drive):X4} {drive.Name}");
			}
		}
	}
}
