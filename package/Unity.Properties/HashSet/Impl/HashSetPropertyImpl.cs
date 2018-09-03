#if (NET_4_6 || NET_STANDARD_2_0)

using System.Collections.Generic;

namespace Unity.Properties
{
    /*
     * CLASS PROPERTIES
     */
    
    /// <summary>
    /// Generic implementation for HashSets backed by a .NET HashSet
    /// </summary>
    /// <typeparam name="TContainer"></typeparam>
    /// <typeparam name="TItem"></typeparam>
    public abstract class BackedHashSetClassPropertyBase<TContainer, TItem> : HashSetClassPropertyBase<TContainer, TItem> 
        where TContainer : class, IPropertyContainer
    {
        protected abstract HashSet<TItem> GetHashSet(TContainer container);

        protected BackedHashSetClassPropertyBase(string name) : base(name)
        {
        }

        public override int Count(TContainer container)
        {
            return GetHashSet(container).Count;
        }
        
        public override void Add(TContainer container, TItem item)
        {
            GetHashSet(container).Add(item);
            container.VersionStorage?.IncrementVersion(this, container);
        }

        public override bool Remove(TContainer container, TItem item)
        {
            var result = GetHashSet(container).Remove(item);
            container.VersionStorage?.IncrementVersion(this, container);
            return result;    
        }

        public override bool Contains(TContainer container, TItem item)
        {
            return GetHashSet(container).Contains(item);
        }

        public override void Clear(TContainer container)
        {
            GetHashSet(container).Clear();
            container.VersionStorage?.IncrementVersion(this, container);
        }

        public override IEnumerator<TItem> GetEnumerator(TContainer container)
        {
            return GetHashSet(container).GetEnumerator();
        }
    }

    public abstract class DelegateHashSetClassPropertyBase<TContainer, TItem> : BackedHashSetClassPropertyBase<TContainer, TItem>
        where TContainer : class, IPropertyContainer
    {
        public delegate HashSet<TItem> GetHashSetMethod(TContainer container);

        private readonly GetHashSetMethod m_GetHashSet;
        
        protected DelegateHashSetClassPropertyBase(string name, GetHashSetMethod getHashSet) : base(name)
        {
            m_GetHashSet = getHashSet;
        }

        protected override HashSet<TItem> GetHashSet(TContainer container)
        {
            return m_GetHashSet(container);
        }
    }

    public class ValueHashSetClassProperty<TContainer, TItem> : DelegateHashSetClassPropertyBase<TContainer, TItem>
        where TContainer : class, IPropertyContainer
    {
        public ValueHashSetClassProperty(string name, GetHashSetMethod getHashSet) : base(name, getHashSet)
        {
        }

        public override void Accept(TContainer container, IPropertyVisitor visitor)
        {
            var set = GetHashSet(container);

            if (visitor.ExcludeVisit(container, new VisitContext<HashSet<TItem>> {Property = this, Value = set, Index = -1}))
            {
                return;
            }
            
            var setContext = new VisitContext<HashSet<TItem>> { Property = this, Value = set, Index = -1 };

            if (visitor.BeginCollection(container, setContext))
            {
                var itemVisitContext = new VisitContext<TItem>
                {
                    Property = this
                };

                var index = -1;
                foreach (var item in set)
                {
                    itemVisitContext.Value = item;
                    itemVisitContext.Index = ++index;

                    if (false == visitor.ExcludeVisit(container, itemVisitContext))
                    {
                        visitor.Visit(container, itemVisitContext);
                    }
                }
            }
            visitor.EndCollection(container, setContext);
        }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)