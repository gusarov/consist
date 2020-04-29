using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace Consist.Implementation.HashAlgo
{
	public static class HashAlgoList
	{
		private static HashSet<string> _list = new HashSet<string>
		{
			"SHA1", // SHA1CryptoServiceProvider
			"MD5", // MD5CryptoServiceProvider
			"SHA256", // SHA256Managed
			"SHA384", // SHA384Managed
			"SHA512", // SHA512Managed
		};

		public static IEnumerable<string> List => _list;

		static HashAlgoList()
		{
			//try
			{
				// init CryptoConfig
				HashAlgorithm.Create().Dispose();

				var names = new HashSet<string>();

				// discover dictionaries
				var fis = typeof(CryptoConfig).GetFields(BindingFlags.NonPublic | BindingFlags.Static);
				foreach (var fieldInfo in fis)
				{
					if (typeof(IDictionary).IsAssignableFrom(fieldInfo.FieldType))
					{
						var dic = (IDictionary) fieldInfo.GetValue(null);
						if (dic != null)
						{
							foreach (DictionaryEntry kvp in dic)
							{
								if (kvp.Key is string key)
								{
									names.Add(key);
								}
							}
						}
					}
				}

				// resolve types
				var instances = names.Select(x =>
					{
						try
						{
							return new {name = x, inst = HashAlgorithm.Create(x)};
						}
						catch
						{
							return null;
						}
					})
					.ToList(); // never iterate factory twice!

				foreach (var name in typeof(HashAlgorithmName)
					.GetFields(BindingFlags.Static | BindingFlags.Public))
				{
					try
					{
						instances.Add(new {name = name.Name, inst = HashAlgorithm.Create(name.Name) });
					}
					catch { }
				}

				var types = instances
					.Where(x => x?.inst != null)
					.Select(x => new {x.name, x.inst.GetType().FullName})
					.ToArray(); // break from previous collection that I'm going to destroy

				foreach (var instance in instances)
				{
					instance?.inst?.Dispose();
				}

				var best = types
						.GroupBy(g => g.FullName)
						.Select(g => g.OrderBy(x => x.name.Length).First().name)
					;
				foreach (var name in best)
				{
					_list.Add(name);
				}
			}
			//catch { }
		}
	}
}
