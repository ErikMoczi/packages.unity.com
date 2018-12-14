

using System;

namespace Unity.Tiny
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal sealed class TinyCustomEditorAttribute : TinyAttribute, IIdentified<TinyId>
    {
        private readonly TinyId m_Id;

        public TinyId Id => m_Id;

        public TinyCustomEditorAttribute(string guid, int order = 0)
            : base(order)
        {
            m_Id = new TinyId(guid);
        }
    }
}

