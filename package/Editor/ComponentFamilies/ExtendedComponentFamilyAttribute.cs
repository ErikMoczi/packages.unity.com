
using System;

namespace Unity.Tiny
{
    internal class ExtendedComponentFamilyAttribute : TinyAttribute
    {
        private readonly Type m_Extends;

        public Type Extends => m_Extends;

        public ExtendedComponentFamilyAttribute(Type extends)
            : base(0)
        {
            m_Extends = extends;
        }
    }
}
