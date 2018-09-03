using System;
using System.Collections.Generic;

namespace Unity.Properties
{
    public interface IListProperty : IProperty
    {
        Type ItemType { get; }
        int Count(IPropertyContainer container);
        object GetObjectValueAtIndex(IPropertyContainer container, int index);
    }

    public interface ITypedContainerListProperty<in TContainer> : IListProperty
        where TContainer : class, IPropertyContainer
    {
        int Count(TContainer container);
        void Add(TContainer container);
        
        void AddObject(TContainer container, object item);

        void Clear(TContainer container);
        void RemoveAt(TContainer container, int index);
        
        object GetObjectValueAtIndex(TContainer container, int index);
        void SetObjectValueAtIndex(TContainer container, int index, object value);
    }

    public interface IListProperty<in TContainer, TItem> : ITypedContainerListProperty<TContainer>
        where TContainer : class, IPropertyContainer
    {
        TItem GetValueAtIndex(TContainer container, int index);
        void SetValueAtIndex(TContainer container, int index, TItem value);

        IEnumerator<TItem> GetEnumerator(TContainer container);
        void Add(TContainer container, TItem item);
        bool Contains(TContainer container, TItem item);
        bool Remove(TContainer container, TItem item);
        int IndexOf(TContainer container, TItem value);
        void Insert(TContainer container, int index, TItem value);
    }
    
    public interface ITypedContainerStructListProperty<TContainer> : IListProperty
        where TContainer : struct, IPropertyContainer
    {
        int Count(ref TContainer container);
        void Add(ref TContainer container);
        
        object GetObjectValueAtIndex(ref TContainer container, int index);
        void SetObjectValueAtIndex(ref TContainer container, int index, object value);
    }
    
    public interface IStructListProperty<TContainer, TItem> : ITypedContainerStructListProperty<TContainer>
        where TContainer : struct, IPropertyContainer
    {
        TItem GetValueAtIndex(ref TContainer container, int index);
        void SetValueAtIndex(ref TContainer container, int index, TItem value);

        IEnumerator<TItem> GetEnumerator(ref TContainer container);
        void Add(ref TContainer container, TItem item);
        bool Contains(ref TContainer container, TItem item);
        bool Remove(ref TContainer container, TItem item);
        int IndexOf(ref TContainer container, TItem value);
        void Insert(ref TContainer container, int index, TItem value);
    }
}
