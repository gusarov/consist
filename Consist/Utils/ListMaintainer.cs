using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consist.Utils
{
	static class ListMaintainer
	{
		public static void ViewMaintenance<T>(this IList<T> view, IEnumerable<T> data)
		{
#if DEBUG
			if (view == null)
			{
				throw new ArgumentNullException(nameof(view));
			}

			if (data == null)
			{
				throw new ArgumentNullException(nameof(data));
			}
#endif

			// var toAdd = new List<T>();
			var newState = new HashSet<T>();
			foreach (var item in data)
			{
				newState.Add(item);
				if (!view.Contains(item))
				{
					view.Add(item);
				}
			}

			var toRemove = new List<T>();
			foreach (var item in view)
			{
				if (!newState.Contains(item))
				{
					toRemove.Add(item);
				}
			}

			foreach (var item in toRemove)
			{
				view.Remove(item);
			}
		}
	}
}
