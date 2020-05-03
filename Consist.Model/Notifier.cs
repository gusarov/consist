using System;
using System.Diagnostics;

namespace Consist.Model
{
	public class Notifier
	{
		public static Notifier Instance = new Notifier();

		internal Notifier()
		{
			
		}

		public event EventHandler<ItemScannedEventArgs> ItemScanned;

		public void NotifyItemScanned(ItemScannedEventArgs e)
		{
			// Debug.WriteLine(e.Record + " " + (e.ChildrenAdded != null ? "(c)" : ""));
			ItemScanned?.Invoke(this, e);
		}

		public event EventHandler<EventArgs> StatsChanged;

		public void NotifyStatsChanged(EventArgs e)
		{
			// Debug.WriteLine(e.Record + " " + (e.ChildrenAdded != null ? "(c)" : ""));
			StatsChanged?.Invoke(this, e);
		}
	}

	public class ItemScannedEventArgs : EventArgs
	{
		public override string ToString() => $"EA: P {Parent} I {Item}";

		public MetadataContainer Container;
		public Record Parent;
		public Record Item;
		/*
		public Record NewChildren;
		public Record Record;
		public Record ChildrenAdded;
		*/
	}
	/*
		public class ItemScannedEventArgs : EventArgs
		{
			public MetadataContainer Container;
			public Record Parent;
			public Record NewChildren;
			/ *
			public Record Record;
			public Record ChildrenAdded;
			* /
		}
	*/
}
