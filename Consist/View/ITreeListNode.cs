using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consist.View
{
	interface ITreeListNode
	{
		bool HasItems { get; }

		bool IsExpanded { get; }
	}
}
