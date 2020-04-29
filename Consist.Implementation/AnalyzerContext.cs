using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consist.Implementation
{
	public class AnalyzerContext
	{
		public bool ScanSubfolders { get; set; } = true;
		public bool CalculateHashSum { get; set; }

		public bool ReadAttributes => true;
		public bool ReadModificationDate => true;
	}
}
