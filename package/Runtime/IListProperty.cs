using System;
using System.Collections.Generic;

namespace Unity.Properties
{
    public interface IListProperty : IProperty
    {
        Type ItemType { get; }
    }

    public interface IListProperty<in TContainer, TItem> : IListProperty
        where TContainer : class, IPropertyContainer
    {
        TItem GetValueAtIndex(TContainer container, int index);
        void SetValueAtIndex(TContainer container, int index, TItem value);

        IEnumerator<TItem> GetEnumerator(TContainer container);
        int Count(TContainer container);
        void Add(TContainer container);
        void Add(TContainer container, TItem item);
        bool Contains(TContainer container, TItem item);
        bool Remove(TContainer container, TItem item);
        int IndexOf(TContainer container, TItem value);
        void Insert(TContainer container, int index, TItem value);
    }
    
    public interface IStructListProperty<TContainer, TItem> : IListProperty
        where TContainer : struct, IPropertyContainer
    {
        TItem GetValueAtIndex(ref TContainer container, int index);
        void SetValueAtIndex(ref TContainer container, int index, TItem value);

        IEnumerator<TItem> GetEnumerator(ref TContainer container);
        int Count(ref TContainer container);
        void Add(ref TContainer container);
        void Add(ref TContainer container, TItem item);
        bool Contains(ref TContainer container, TItem item);
        bool Remove(ref TContainer container, TItem item);
        int IndexOf(ref TContainer container, TItem value);
        void Insert(ref TContainer container, int index, TItem value);
    }
}
