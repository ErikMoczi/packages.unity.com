using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Properties
{
    public class ListProperty<TContainer, TValue, TItem> : Property<TContainer, TValue>, IListProperty<TContainer, TItem>
        where TContainer : class, IPropertyContainer
        where TValue : class, IList<TItem>
    {
        public delegate TItem CreateInstanceMethod(TContainer c);

        private CreateInstanceMethod m_CreateInstanceMethod;
        
        public Type ItemType => typeof(TItem);
        
        public ListProperty(string name, GetValueMethod getValue, SetValueMethod setValue, CreateInstanceMethod createInstance = null) 
            : base(name, getValue, setValue)
        {
            m_CreateInstanceMethod = createInstance ?? DefaultCreateInstance;
        }

        private static TItem DefaultCreateInstance(TContainer unused)
        {
            Debug.Assert(typeof(TItem).IsValueType,
                $"List on container {typeof(TContainer)} of reference type {typeof(TItem)} should have their createInstanceMethod specified");
            return default(TItem);
        }

        public override void Accept(TContainer container, IPropertyVisitor visitor)
        {
            var value = GetValue(container);
            
            if (false == visitor.ExcludeVisit(container,
                new VisitContext<TValue> {Property = this, Value = value, Index = -1}))
            {
                var listContext =
                    new VisitContext<TValue> { Property = this, Value = value, Index = -1 };

                if (visitor.BeginList(container, listContext))
                {
                    var itemVisitContext = new VisitContext<TItem>
                    {
                        Property = this
                    };

                    var count = Count(container);
                    
                    for (var i = 0; i < count; i++)
                    {
                        var item = GetItemAt(container, i);
                        itemVisitContext.Value = item;
                        itemVisitContext.Index = i;

                        if (false == visitor.ExcludeVisit(container, itemVisitContext))
                        {
                            visitor.Visit(container, itemVisitContext);
                        }
                    }
                }
                visitor.EndList(container, listContext);
            }
        }

        public int Count(IPropertyContainer container)
        {
            return GetValue((TContainer) container).Count;
        }
        
        public IEnumerator<TItem> GetEnumerator(TContainer container)
        {
            return GetValue(container).GetEnumerator();
        }

        public TItem CreateNewItem(TContainer container)
        {
            return m_CreateInstanceMethod(container);
        }

        public void AddNewItem(IPropertyContainer container)
        {
            Add((TContainer)container, CreateNewItem((TContainer)container));
        }

        public void AddObject(IPropertyContainer container, object item)
        {
            Add((TContainer)container, TypeConversion.Convert<TItem>(item));
        }

        object IListProperty.GetObjectAt(IPropertyContainer container, int index)
        {
            return GetItemAt((TContainer) container, index);
        }
        
        void IListProperty.SetObjectAt(IPropertyContainer container, int index, object value)
        {
            SetItemAt((TContainer) container, index, TypeConversion.Convert<TItem>(value));
        }

        public virtual void Add(TContainer container, TItem item)
        {
            var list = GetValue(container);
            list.Add(item);
            container.VersionStorage?.IncrementVersion(this, container);
        }

        public void Clear(TContainer container)
        {
            var list = GetValue(container);
            list.Clear();
            container.VersionStorage?.IncrementVersion(this, container);
        }

        public bool Contains(TContainer container, TItem item)
        {            
            var list = GetValue(container);
            return list.Contains(item);
        }

        public bool Remove(TContainer container, TItem item)
        {
            var list = GetValue(container);
            var result = list.Remove(item);
            if (result)
            {
                container.VersionStorage?.IncrementVersion(this, container);
            }
            return result;
        }

        public int IndexOf(TContainer container, TItem item)
        {
            var list = GetValue(container);
            return list.IndexOf(item);
        }

        public void Insert(TContainer container, int index, TItem item)
        {
            var list = GetValue(container);
            list.Insert(index, item);
            container.VersionStorage?.IncrementVersion(this, container);
        }

        public void RemoveAt(IPropertyContainer container, int index)
        {
            var list = GetValue((TContainer)container);
            list.RemoveAt(index);
            container.VersionStorage?.IncrementVersion(this, container);
        }

        public void InsertObject(IPropertyContainer container, int index, object item)
        {
            var list = GetValue((TContainer)container);
            list.Insert(index, TypeConversion.Convert<TItem>(item));
        }

        public void Clear(IPropertyContainer container)
        {
            var list = GetValue((TContainer)container);
            list.Clear();
        }

        public virtual TItem GetItemAt(TContainer container, int index)
        {
            var list = GetValue(container);
            return list[index];
        }
        
        public void SetItemAt(TContainer container, int index, TItem value)
        {
            var list = GetValue(container);
            
            if (ItemEquals(list[index], value))
            {
                return;
            }
            
            list[index] = value;
            container.VersionStorage?.IncrementVersion(this, container);
        }
        
        private static bool ItemEquals(TItem a, TItem b)
        {
            if (null == a && null == b)
            {
                return true;
            }

            return null != a && a.Equals(b);
        }
    }
    
    public class StructListProperty<TContainer, TValue, TItem> : StructProperty<TContainer, TValue>, IStructListProperty<TContainer, TItem>
        where TContainer : struct, IPropertyContainer
        where TValue : class, IList<TItem>
    {
        public delegate TItem CreateInstanceMethod(ref TContainer c);

        private CreateInstanceMethod m_CreateInstanceMethod;
        
        public Type ItemType => typeof(TItem);
        
        public StructListProperty(string name, GetValueMethod getValue, SetValueMethod setValue, CreateInstanceMethod createInstance = null) 
            : base(name, getValue, setValue)
        {
            m_CreateInstanceMethod = createInstance ?? DefaultCreateInstance;
        }

        private static TItem DefaultCreateInstance(ref TContainer unused)
        {
            Debug.Assert(typeof(TItem).IsValueType, $"List on container {typeof(TContainer)} of reference type {typeof(TItem)} should have their createInstanceMethod specified");
            return default(TItem);
        }

        private TValue GetListValue(IPropertyContainer container)
        {
            var c = (TContainer) container;
            return GetValue(ref c);
        }

        public override void Accept(ref TContainer container, IPropertyVisitor visitor)
        {
            var value = GetValue(ref container);
            
            if (false == visitor.ExcludeVisit(ref container,
                new VisitContext<TValue> {Property = this, Value = value, Index = -1}))
            {
                var listContext =
                    new VisitContext<TValue> { Property = this, Value = value, Index = -1 };

                if (visitor.BeginList(ref container, listContext))
                {
                    var itemVisitContext = new VisitContext<TItem>
                    {
                        Property = this
                    };

                    var count = Count(ref container);
                    for (var i = 0; i < count; i++)
                    {
                        var item = GetItemAt(ref container, i);
                        itemVisitContext.Value = item;
                        itemVisitContext.Index = i;

                        if (false == visitor.ExcludeVisit(ref container, itemVisitContext))
                        {
                            visitor.Visit(ref container, itemVisitContext);
                        }
                    }
                }
                visitor.EndList(ref container, listContext);
            }
        }
        
        public int Count(IPropertyContainer container)
        {
            var list = GetListValue(container);
            return list.Count;
        }
        
        public int Count(ref TContainer container)
        {
            return GetValue(ref container).Count;
        }

        public void InsertObject(IPropertyContainer container, int index, object item)
        {
            var c = (TContainer) container;
            Insert(ref c, index, TypeConversion.Convert<TItem>(item));
        }

        public void Clear(IPropertyContainer container)
        {
            var list = GetListValue(container);
            list.Clear();
            container.VersionStorage?.IncrementVersion(this, container);
        }
        
        public IEnumerator<TItem> GetEnumerator(ref TContainer container)
        {
            return GetValue(ref container).GetEnumerator();
        }

        public TItem CreateNewItem(ref TContainer container)
        {
            return m_CreateInstanceMethod(ref container);
        }

        public void AddNewItem(IPropertyContainer container)
        {
            var c = (TContainer) container;
            Add(ref c, m_CreateInstanceMethod(ref c));
        }

        public void AddObject(IPropertyContainer container, object item)
        {
            var c = (TContainer) container;
            Add(ref c, TypeConversion.Convert<TItem>(item));
        }

        public void RemoveAt(IPropertyContainer container, int index)
        {
            var c = (TContainer) container;
            RemoveAt(ref c, index);
        }

        object IListProperty.GetObjectAt(IPropertyContainer container, int index)
        {
            var c = (TContainer) container;
            return GetItemAt(ref c, index);
        }
        
        void IListProperty.SetObjectAt(IPropertyContainer container, int index, object value)
        {
            var c = (TContainer) container;
            SetItemAt(ref c, index, TypeConversion.Convert<TItem>(value));
        }

        public virtual void Add(ref TContainer container, TItem item)
        {
            var list = GetValue(ref container);
            list.Add(item);
            container.VersionStorage?.IncrementVersion(this, container);
        }

        public bool Contains(ref TContainer container, TItem item)
        {            
            var list = GetValue(ref container);
            return list.Contains(item);
        }

        public bool Remove(ref TContainer container, TItem item)
        {
            var list = GetValue(ref  container);
            var result = list.Remove(item);
            container.VersionStorage?.IncrementVersion(this, container);
            return result;
        }

        public int IndexOf(ref TContainer container, TItem item)
        {
            var list = GetValue(ref container);
            return list.IndexOf(item);
        }

        public void Insert(ref TContainer container, int index, TItem item)
        {
            var list = GetValue(ref container);
            list.Insert(index, item);
            container.VersionStorage?.IncrementVersion(this, container);
        }

        public void RemoveAt(ref TContainer container, int index)
        {
            var list = GetValue(ref container);
            list.RemoveAt(index);
            container.VersionStorage?.IncrementVersion(this, container);
        }
        
        public TItem GetItemAt(ref TContainer container, int index)
        {
            var list = GetValue(ref container);
            return list[index];
        }
        
        public void SetItemAt(ref TContainer container, int index, TItem value)
        {
            var list = GetValue(ref container);
            
            if (ItemEquals(list[index], value))
            {
                return;
            }
            
            list[index] = value;
            container.VersionStorage?.IncrementVersion(this, container);
        }
        
        private static bool ItemEquals(TItem a, TItem b)
        {
            if (null == a && null == b)
            {
                return true;
            }

            return null != a && a.Equals(b);
        }
    }
    
    public class ContainerListProperty<TContainer, TValue, TItem> : ListProperty<TContainer, TValue, TItem>
        where TContainer : class, IPropertyContainer
        where TValue : class, IList<TItem>
        where TItem : class, IPropertyContainer
    {
        public ContainerListProperty(string name, GetValueMethod getValue, SetValueMethod setValue, CreateInstanceMethod createInstanceMethod = null) 
            : base(name, getValue, setValue, createInstanceMethod)
        {
        }

        public override void Accept(TContainer container, IPropertyVisitor visitor)
        {
            var value = GetValue(container);
            
            if (false == visitor.ExcludeVisit(container,
                new VisitContext<TValue> {Property = this, Value = value, Index = -1}))
            {
                var listContext =
                    new VisitContext<TValue> { Property = this, Value = value, Index = -1 };

                if (visitor.BeginList(container, listContext))
                {
                    var itemVisitContext = new VisitContext<TItem>
                    {
                        Property = this
                    };

                    var count = Count(container);
                    for (var i = 0; i < count; i++)
                    {
                        var item = GetItemAt(container, i);
                        itemVisitContext.Value = item;
                        itemVisitContext.Index = i;

                        if (false == visitor.ExcludeVisit(container, itemVisitContext))
                        {
                            if (visitor.BeginContainer(container, itemVisitContext))
                            {
                                item.Visit(visitor);
                            }
                            visitor.EndContainer(container, itemVisitContext);
                        }
                    }
                }
                visitor.EndList(container, listContext);
            }
        }
    }
    
    public class StructContainerListProperty<TContainer, TValue, TItem> : StructListProperty<TContainer, TValue, TItem>
        where TContainer : struct, IPropertyContainer
        where TValue : class, IList<TItem>
        where TItem : class, IPropertyContainer
    {
        public StructContainerListProperty(string name, GetValueMethod getValue, SetValueMethod setValue, CreateInstanceMethod createInstanceMethod = null) 
            : base(name, getValue, setValue, createInstanceMethod)
        {
        }

        public override void Accept(ref TContainer container, IPropertyVisitor visitor)
        {
            var value = GetValue(ref container);
            
            if (false == visitor.ExcludeVisit(ref container,
                new VisitContext<TValue> {Property = this, Value = value, Index = -1}))
            {
                var listContext =
                    new VisitContext<TValue> { Property = this, Value = value, Index = -1 };

                if (visitor.BeginList(ref container, listContext))
                {
                    var itemVisitContext = new VisitContext<TItem>
                    {
                        Property = this
                    };

                    var count = Count(ref container);
                    for (var i = 0; i < count; i++)
                    {
                        var item = GetItemAt(ref container, i);
                        itemVisitContext.Value = item;
                        itemVisitContext.Index = i;

                        if (false == visitor.ExcludeVisit(ref container, itemVisitContext))
                        {
                            if (visitor.BeginContainer(ref container, itemVisitContext))
                            {
                                item.Visit(visitor);
                            }
                            visitor.EndContainer(ref container, itemVisitContext);
                        }
                    }
                }
                visitor.EndList(ref container, listContext);
            }
        }
    }
    
    public class MutableContainerListProperty<TContainer, TValue, TItem> : ListProperty<TContainer, TValue, TItem>
        where TContainer : class, IPropertyContainer
        where TValue : class, IList<TItem>
        where TItem : struct, IPropertyContainer
    {
        public MutableContainerListProperty(string name, GetValueMethod getValue, SetValueMethod setValue, CreateInstanceMethod createInstanceMethod = null) 
            : base(name, getValue, setValue, createInstanceMethod)
        {
        }

        public override void Accept(TContainer container, IPropertyVisitor visitor)
        {
            var value = GetValue(container);
            
            if (false == visitor.ExcludeVisit(container,
                new VisitContext<TValue> {Property = this, Value = value, Index = -1}))
            {
                var listContext =
                    new VisitContext<TValue> { Property = this, Value = value, Index = -1 };

                if (visitor.BeginList(container, listContext))
                {
                    var itemVisitContext = new VisitContext<TItem>
                    {
                        Property = this
                    };

                    var count = Count(container);
                    for (var i = 0; i < count; i++)
                    {
                        var item = GetItemAt(container, i);
                        itemVisitContext.Value = item;
                        itemVisitContext.Index = i;

                        if (false == visitor.ExcludeVisit(container, itemVisitContext))
                        {
                            if (visitor.BeginContainer(container, itemVisitContext))
                            {
                                PropertyContainer.Visit(ref item, visitor);
                            }
                            visitor.EndContainer(container, itemVisitContext);
                        }
                    }
                }
                visitor.EndList(container, listContext);
            }
        }
    }
    
    public class StructMutableContainerListProperty<TContainer, TValue, TItem> : StructListProperty<TContainer, TValue, TItem>
        where TContainer : struct, IPropertyContainer
        where TValue : class, IList<TItem>
        where TItem : struct, IPropertyContainer
    {
        public StructMutableContainerListProperty(string name, GetValueMethod getValue, SetValueMethod setValue, CreateInstanceMethod createInstanceMethod = null) 
            : base(name, getValue, setValue, createInstanceMethod)
        {
        }

        public override void Accept(ref TContainer container, IPropertyVisitor visitor)
        {
            var value = GetValue(ref container);
            
            if (false == visitor.ExcludeVisit(ref container,
                new VisitContext<TValue> {Property = this, Value = value, Index = -1}))
            {
                var listContext =
                    new VisitContext<TValue> { Property = this, Value = value, Index = -1 };

                if (visitor.BeginList(ref container, listContext))
                {
                    var itemVisitContext = new VisitContext<TItem>
                    {
                        Property = this
                    };

                    var count = Count(ref container);
                    for (var i = 0; i < count; i++)
                    {
                        var item = GetItemAt(ref container, i);
                        itemVisitContext.Value = item;
                        itemVisitContext.Index = i;

                        if (false == visitor.ExcludeVisit(ref container, itemVisitContext))
                        {
                            if (visitor.BeginContainer(ref container, itemVisitContext))
                            {
                                PropertyContainer.Visit(ref item, visitor);
                            }
                            visitor.EndContainer(ref container, itemVisitContext);
                        }
                    }
                }
                visitor.EndList(ref container, listContext);
            }
        }
    }
}
