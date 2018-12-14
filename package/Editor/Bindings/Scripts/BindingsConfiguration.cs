

using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Tiny
{
    internal class BindingConfiguration : IEquatable<BindingConfiguration>
    {
        private readonly BindingProfile[] m_Bindings;

        public BindingConfiguration()
            :this(new BindingProfile[0])
        {
        }

        public BindingConfiguration(BindingProfile[] bindings)
        {
            m_Bindings = bindings;
        }

        public IEnumerable<BindingProfile> Bindings => m_Bindings;
        public IEnumerable<BindingProfile> ReversedOrderBindings => m_Bindings.Reverse();

        public bool Equals(BindingConfiguration other)
        {
            return null != other && m_Bindings.SequenceEqual(other.m_Bindings);
        }
    }
}

