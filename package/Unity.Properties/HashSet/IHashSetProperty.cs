#if (NET_4_6 || NET_STANDARD_2_0)

using System;
using System.Collections;
using System.Collections.Generic;

namespace Unity.Properties
{
    public interface IHashSetProperty
    {
        Type ItemType { get; }
        
        int Count(IPropertyContainer container);
    }
    
    /*
     * CLASS PROPERTIES
     */
    
    public interface IHashSetClassProperty : IListProperty
    {
        void AddObject(IPropertyContainer container, object item);
        bool RemoveObject(IPropertyContainer container, object item);
        bool ContainsObject(IPropertyContainer container, object item);
        void Clear(IPropertyContainer container);
        
        IEnumerator GetEnumerator(IPropertyContainer container);
    }

    public interface IHashSetTypedItemClassProperty<TItem> : IHashSetClassProperty
    {
        void Add(IPropertyContainer container, TItem item);
        bool Remove(IPropertyContainer container, TItem item);
        bool Contains(IPropertyContainer container, TItem item);
    }

    public interface IHashSetClassProperty<in TContainer> : IHashSetClassProperty, IClassProperty<TContainer>
        where TContainer : class, IPropertyContainer
    {
        int Count(TContainer container);
    }

    public interface IHashSetClassProperty<in TContainer, TItem> : IHashSetClassProperty<TContainer>, IHashSetTypedItemClassProperty<TItem>
        where TContainer : class, IPropertyContainer
    {
        void Add(TContainer container, TItem item);
        bool Remove(TContainer container, TItem item);
        void Clear(TContainer container);
        bool Contains(TContainer container, TItem item);
        
        IEnumerator<TItem> GetEnumerator(TContainer container);
    }

    /*
     * STRUCT PROPERTIES
     */

    public interface IHashSetStructProperty : IListProperty
    {
        void AddObject(ref IPropertyContainer container, object item);
        bool RemoveObject(ref IPropertyContainer container, object item);
        bool ContainsObject(ref IPropertyContainer container, object item);
        void Clear(ref IPropertyContainer container);
        
        IEnumerator GetEnumerator(ref IPropertyContainer container);
    }
    
    public interface IHashSetTypedItemStructProperty<TItem> : IHashSetStructProperty
    {
        void Add(ref IPropertyContainer container, TItem item);
        bool Remove(ref IPropertyContainer container, TItem item);
        bool Contains(ref IPropertyContainer container, TItem item);
    }

    public interface IHashSetStructProperty<TContainer> : IHashSetStructProperty, IStructProperty<TContainer>
        where TContainer : struct, IPropertyContainer
    {
        int Count(ref TContainer container);
    }

    public interface IHashSetStructProperty<TContainer, TItem> : IHashSetStructProperty<TContainer>, IHashSetTypedItemStructProperty<TItem>
        where TContainer : struct, IPropertyContainer
    {
        void Add(ref TContainer container, TItem item);
        bool Remove(ref TContainer container, TItem item);
        void Clear(ref TContainer container);
        bool Contains(ref TContainer container, TItem item);
        
        IEnumerator<TItem> GetEnumerator(ref TContainer container);
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)