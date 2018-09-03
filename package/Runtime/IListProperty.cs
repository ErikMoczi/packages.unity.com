#if (NET_4_6 || NET_STANDARD_2_0)

using System;
using System.Collections;
using System.Collections.Generic;

namespace Unity.Properties
{
    public interface IListProperty : IProperty
    {
        Type ItemType { get; }
        
        int Count(IPropertyContainer container);
        
        object GetObjectAt(IPropertyContainer container, int index);
        void SetObjectAt(IPropertyContainer container, int index, object item);
        
        void AddNewItem(IPropertyContainer container);
        void AddObject(IPropertyContainer container, object item);
        
        void RemoveAt(IPropertyContainer container, int index);
        void InsertObject(IPropertyContainer container, int index, object item);
        void Clear(IPropertyContainer container);
    }

    public interface IListProperty<in TContainer, TItem> : IListProperty
        where TContainer : class, IPropertyContainer
    {
        TItem GetItemAt(TContainer container, int index);
        void SetItemAt(TContainer container, int index, TItem value);

        IEnumerator<TItem> GetEnumerator(TContainer container);

        TItem CreateNewItem(TContainer container);

        void Add(TContainer container, TItem item);
        bool Contains(TContainer container, TItem item);
        bool Remove(TContainer container, TItem item);
        int IndexOf(TContainer container, TItem value);
        void Insert(TContainer container, int index, TItem value);
    }

    public interface IStructListProperty<TContainer, TItem> : IListProperty
        where TContainer : struct, IPropertyContainer
    {
        int Count(ref TContainer container);
        
        TItem GetItemAt(ref TContainer container, int index);
        void SetItemAt(ref TContainer container, int index, TItem value);

        IEnumerator<TItem> GetEnumerator(ref TContainer container);
        
        TItem CreateNewItem(ref TContainer container);
        
        void Add(ref TContainer container, TItem item);
        bool Contains(ref TContainer container, TItem item);
        bool Remove(ref TContainer container, TItem item);
        int IndexOf(ref TContainer container, TItem value);
        void Insert(ref TContainer container, int index, TItem value);
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)
