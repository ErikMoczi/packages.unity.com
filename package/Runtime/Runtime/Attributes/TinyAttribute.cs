
using System;

namespace Unity.Tiny
{
    /// <summary>
    /// Base class for all Tiny attributes.
    /// </summary>
    public abstract class TinyAttribute : Attribute
    {
        private readonly int m_Order;

        internal int Order => m_Order;

        protected internal TinyAttribute(int order)
        {
            m_Order = order;
        }
    }
}
