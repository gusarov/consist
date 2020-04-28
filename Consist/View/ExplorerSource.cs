using System.Collections.Generic;
using Consist.Model;

namespace Consist.View
{
	public class ExplorerSource
	{
		public ExplorerSource()
		{
			/*
			var path = Environment.GetCommandLineArgs().Skip(1).Take(1).FirstOrDefault();

			if (path != null)
			{
				_container = Container.LoadFrom(path);
				_records = _container.Get("\\").SubRecords;
			}
			*/

		}

		private MetadataContainer _container;

		private IEnumerable<Record> _records;
		public IEnumerable<Record> Records => _records;
	}
}