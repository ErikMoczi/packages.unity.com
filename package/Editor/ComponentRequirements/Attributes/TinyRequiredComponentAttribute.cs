

using System;

namespace Unity.Tiny
{
    internal class TinyRequiredComponentAttribute : TinyAttribute
    {
        public readonly TinyId Id;

        public TinyRequiredComponentAttribute(string typeId)
            : base(10)
        {
            Id = new TinyId(typeId);
        }
    }
}

