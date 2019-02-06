

using System;

namespace Unity.Tiny
{
    internal class BindingDependencyAttribute : TinyAttribute
    {
        public readonly Type[] Types;

        public BindingDependencyAttribute(params Type[] types)
            :base(0)
        {
            Types = types;
        }
    }

}

