

using System;

namespace Unity.Tiny
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	internal sealed class TinyCustomDrawerAttribute : TinyAttribute, IIdentified<TinyId>
	{
		private readonly TinyId m_Id;

		public TinyId Id => m_Id;

		public TinyCustomDrawerAttribute(string guid, int order = 0)
			: base(order)
		{
			m_Id = new TinyId(guid);
		}
	}
}

