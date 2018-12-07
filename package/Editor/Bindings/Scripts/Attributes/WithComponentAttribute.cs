

using System;
using System.Linq;

namespace Unity.Tiny
{
    internal class WithComponentAttribute : TinyAttribute
    {
        public readonly TinyId[] TypeIds;

        public WithComponentAttribute(params string[] ids)
            :base (0)
        {
            TypeIds = ids.Select(id => new TinyId(id)).ToArray();
        }
    }
}

