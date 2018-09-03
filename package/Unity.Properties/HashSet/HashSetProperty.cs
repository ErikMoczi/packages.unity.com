#if (NET_4_6 || NET_STANDARD_2_0)

using System;
using System.Collections;
using System.Collections.Generic;

namespace Unity.Properties
{
    /*
     * CLASS PROPERTIES
     */
    
    public abstract class HashSetClassPropertyBase<TContainer, TItem> : Property<TContainer>, IHashSetClassProperty<TContainer, TItem>
        where TContainer : class, IPropertyContainer
    {
        public Type ItemType => typeof(TItem);

        protected HashSetClassPropertyBase(string name) : base(name)
        {
        }
        
        public int Count(IPropertyContainer container)
        {
            return Count((TContainer) container);
        }
        
        public void AddObject(IPropertyContainer container, object item)
        {
            Add((TContainer) container, TypeConversion.Convert<TItem>(item));
        }
        
        public bool RemoveObject(IPropertyContainer container, object item)
        {
            return Remove((TContainer) container, TypeConversion.Convert<TItem>(item));
        }

        public bool ContainsObject(IPropertyContainer container, object item)
        {
            return Contains((TContainer) container, TypeConversion.Convert<TItem>(item));
        }

        public void Add(IPropertyContainer container, TItem item)
        {
            Add((TContainer) container, item);
        }

        public bool Remove(IPropertyContainer container, TItem item)
        {
            return Remove((TContainer) container, item);
        }

        public bool Contains(IPropertyContainer container, TItem item)
        {
            return Contains((TContainer) container, item);
        }

        public void Clear(IPropertyContainer container)
        {
            Clear((TContainer) container);
        }

        public IEnumerator GetEnumerator(IPropertyContainer container)
        {
            return GetEnumerator((TContainer) container);
        }

        public void Accept(IPropertyContainer container, IPropertyVisitor visitor)
        {
            Accept((TContainer) container, visitor);
        }

        public abstract int Count(TContainer container);
        public abstract void Add(TContainer container, TItem item);
        public abstract bool Remove(TContainer container, TItem item);
        public abstract void Clear(TContainer container);
        public abstract bool Contains(TContainer container, TItem item);
        public abstract void Accept(TContainer container, IPropertyVisitor visitor);
        public abstract IEnumerator<TItem> GetEnumerator(TContainer container);
    }
    
    /*
     * STRUCT PROPERTIES
     */
    
    public abstract class HashSetStructPropertyBase<TContainer, TItem> : Property<TContainer>, IHashSetStructProperty<TContainer, TItem>
        where TContainer : struct, IPropertyContainer
    {
        public Type ItemType => typeof(TItem);

        protected HashSetStructPropertyBase(string name) : base(name)
        {
        }

        private struct Locals
        {
            public HashSetStructPropertyBase<TContainer, TItem> Property;
            public TItem Item;
            public IPropertyVisitor Visitor;
        }

        public int Count(IPropertyContainer container)
        {
            var typed = (TContainer) container;
            return Count(ref typed);
        }

        public void AddObject(ref IPropertyContainer container, object item)
        {
            (container as IStructPropertyContainer<TContainer>)?.MakeRef((ref TContainer c, Locals l) => { l.Property.Add(ref c, l.Item); },
                new Locals {Property = this, Item = TypeConversion.Convert<TItem>(item)});
        }
        
        public bool RemoveObject(ref IPropertyContainer container, object item)
        {
            return (container as IStructPropertyContainer<TContainer>).MakeRef((ref TContainer c, Locals l) => l.Property.Remove(ref c, l.Item),
                new Locals {Property = this, Item = TypeConversion.Convert<TItem>(item)});
        }

        public bool ContainsObject(ref IPropertyContainer container, object item)
        {
            return (container as IStructPropertyContainer<TContainer>).MakeRef((ref TContainer c, Locals l) => l.Property.Contains(ref c, l.Item),
                new Locals {Property = this, Item = TypeConversion.Convert<TItem>(item)});
        }

        public void Add(ref IPropertyContainer container, TItem item)
        {
            (container as IStructPropertyContainer<TContainer>)?.MakeRef((ref TContainer c, Locals l) => { l.Property.Add(ref c, l.Item); },
                new Locals {Property = this, Item = item});
        }

        public bool Remove(ref IPropertyContainer container, TItem item)
        {
            return (container as IStructPropertyContainer<TContainer>).MakeRef((ref TContainer c, Locals l) => l.Property.Remove(ref c, l.Item),
                new Locals {Property = this, Item = item});
        }

        public bool Contains(ref IPropertyContainer container, TItem item)
        {
            return (container as IStructPropertyContainer<TContainer>).MakeRef((ref TContainer c, Locals l) => l.Property.Contains(ref c, l.Item),
                new Locals {Property = this, Item = item});
        }


        public void Clear(ref IPropertyContainer container)
        {
            (container as IStructPropertyContainer<TContainer>)?.MakeRef((ref TContainer c, HashSetStructPropertyBase<TContainer, TItem> p) => { p.Clear(ref c); }, this);
        }

        IEnumerator IHashSetStructProperty.GetEnumerator(ref IPropertyContainer container)
        {
            return (container as IStructPropertyContainer<TContainer>).MakeRef((ref TContainer c, Locals l) => l.Property.GetEnumerator(ref c),
                new Locals {Property = this});
        }

        public void Accept(ref IPropertyContainer container, IPropertyVisitor visitor)
        {
            (container as IStructPropertyContainer<TContainer>)?.MakeRef((ref TContainer c, Locals l) => { l.Property.Accept(ref c, l.Visitor); },
                new Locals {Property = this, Visitor = visitor });
        }

        public abstract int Count(ref TContainer container);
        public abstract void Add(ref TContainer container, TItem item);
        public abstract bool Contains(ref TContainer container, TItem item);
        public abstract bool Remove(ref TContainer container, TItem item);
        public abstract void Clear(ref TContainer container);
        public abstract void Accept(ref TContainer container, IPropertyVisitor visitor);
        public abstract IEnumerator<TItem> GetEnumerator(ref TContainer container);
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)