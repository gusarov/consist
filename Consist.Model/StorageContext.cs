namespace Consist.Model
{
	public class StorageContext
	{
		public StorageContext(MetadataContainer container, int ver, int hashLen)
		{
			Container = container;
			Ver = ver;
			HashLen = hashLen;
		}
		public MetadataContainer Container { get; }
		public int Ver { get; }
		public int HashLen { get; }
		public string CurrentFolder { get; set; }
		public Record CurrentParent { get; set; }
	}
}