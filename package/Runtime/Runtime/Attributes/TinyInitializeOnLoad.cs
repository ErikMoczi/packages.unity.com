
using System;

namespace Unity.Tiny
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class TinyInitializeOnLoad : TinyAttribute
    {
        public TinyInitializeOnLoad(int order = 0)
            : base(order) { }
    }

}
