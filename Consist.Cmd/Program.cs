using Consist.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consist.Cmd
{
	class Program
	{
		static void Main(string[] args)
		{
			var scan = new Collector(args[0]);
			scan.Scan();
			Console.WriteLine(scan.Container.GetSize());
		}
	}
}
