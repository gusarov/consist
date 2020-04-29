using Consist.Implementation;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Consist.Cmd
{
	class Program
	{
		static int Main(string[] args)
		{
			for (var i = 0; i < args.Length; i++)
			{
				var arg = args[i];
				switch (arg.TrimStart('/', '-').ToLowerInvariant())
				{
					case "snapshot":
						var dir = args[++i];
						var metadata = args[++i];

						var scan = new Analyzer(dir);
						scan.Scan(new AnalyzerContext {
							CalculateHashSum = true,
						});
						Console.WriteLine(scan.Container.GetSize());
						scan.Container.Save(metadata);

						return 1;
					default:
						Console.WriteLine("Unknown Option: " + arg);
						Console.Error.WriteLine("Unknown Option: " + arg);
						return 1;
				}
			}

			return 1;
		}
	}
}
