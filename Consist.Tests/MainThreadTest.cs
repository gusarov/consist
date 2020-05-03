using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Consist;
using Consist.Utils;

namespace Consist.Tests
{
	[TestClass]
	public class MainThreadTest
	{
		[TestMethod]
		public void Should_have_proper_signature_action_no_id()
		{
			MainThread.Invoke("Log Name", () => _ = 1);
		}

		[TestMethod]
		public void Should_have_proper_signature_action0_id()
		{
			MainThread.Invoke("Log Name", () => _ = 1);
			MainThread.Invoke("Log Name", () => _ = 1, true);
			MainThread.Invoke("Log Name", () => _ = 1, false);
		}

		[TestMethod]
		public void Should_have_proper_signature_action1_arg()
		{
			MainThread.Invoke("Log Name", x => _ = 1, "some");
			MainThread.Invoke("Log Name", _ => _ = 1, new object());
			MainThread.Invoke("Log Name", x => _ = 1, "some", true);
			MainThread.Invoke("Log Name", _ => _ = 1, new object(), false);
		}
	}
}
