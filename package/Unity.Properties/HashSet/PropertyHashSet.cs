#if (NET_4_6 || NET_STANDARD_2_0)

using System.Collections;
using System.Collections.Generic;

namespace Unity.Properties
{
    public struct PropertyHashSet<TContainer, TValue> : IEnumerable<TValue>
        where TContainer : class, IPropertyContainer
    {
        private readonly IHashSetClassProperty<TContainer, TValue> m_Property;
        private readonly TContainer m_Container;

        public int Count => m_Property.Count(m_Container);

        public PropertyHashSet(IHashSetClassProperty<TContainer, TValue> property, TContainer container)
        {
            m_Property = property;
            m_Container = container;
        }

        public void Add(TValue item)
        {
            m_Property.Add(m_Container, item);
        }

        public bool Remove(TValue item)
        {
            return m_Property.Remove(m_Container, item);
        }

        public bool Contains(TValue item)
        {
            return m_Property.Contains(m_Container, item);
        }

        public void Clear()
        {
            m_Property.Clear(m_Container);
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            return m_Property.GetEnumerator(m_Container);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)