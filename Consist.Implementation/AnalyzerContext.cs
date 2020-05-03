using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consist.Implementation
{
	public class AnalyzerContext
	{
		public bool IsValid()
		{
			if (ScanRecursively && !ScanChildren)
			{
				return false;
			}
			return ScanNodeItself || ScanChildren;
		}

		public bool ScanNodeItself { get; set; }
		public bool ScanChildren { get; set; }

		public bool ScanRecursively { get; set; }

		public bool CalculateHashSum { get; set; }

		public bool ReadAttributes => true;
		public bool ReadModificationDate => true;

		public bool? Save { get; set; }

	}
}
