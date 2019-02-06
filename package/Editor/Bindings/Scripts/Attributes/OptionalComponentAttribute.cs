

using System.Linq;

namespace Unity.Tiny
{
    internal class OptionalComponentAttribute : TinyAttribute
    {
        public readonly TinyId[] TypeIds;

        public OptionalComponentAttribute(params string[] ids)
            :base (0)
        {
            TypeIds = ids.Select(id => new TinyId(id)).ToArray();
        }
    }
}

