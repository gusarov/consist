using System;
using System.Runtime.Serialization;

namespace Consist.ViewModel
{
	internal class WeakReference<T> : WeakReference
	{
		public WeakReference(object target) : base(target)
		{
		}

		public WeakReference(object target, bool trackResurrection) : base(target, trackResurrection)
		{
		}

		protected WeakReference(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

		public new T Target => (T)base.Target;
	}
}
