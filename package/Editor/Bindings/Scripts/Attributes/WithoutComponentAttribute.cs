

using System;
using System.Linq;

namespace Unity.Tiny
{
    internal class WithoutComponentAttribute : TinyAttribute
    {
        public readonly TinyId[] TypeIds;

        public WithoutComponentAttribute(params string[] ids)
            :base (0)
        {
            TypeIds = ids.Select(id => new TinyId(id)).ToArray();
        }
    }
}

