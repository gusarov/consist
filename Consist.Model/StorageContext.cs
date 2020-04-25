namespace Consist.Model
{
	public class StorageContext
	{
		public StorageContext(int ver, int hashLen)
		{
			Ver = ver;
			HashLen = hashLen;
		}
		public int Ver { get; }
		public int HashLen { get; }
		public string CurrentFolder { get; set; }
	}
}