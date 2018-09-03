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

                if (visitor.BeginList(ref container, listContext))
                {
                    var itemVisitContext = new VisitContext<TItem>
                    {
                        Property = this
                    };

                    var count = Count(container);
                    
                    for (var i = 0; i < count; i++)
                    {
                        var item = GetValueAtIndex(container, i);
                        itemVisitContext.Value = item;
                        itemVisitContext.Index = i;

                        if (false == visitor.ExcludeVisit(container, itemVisitContext))
                        {
                            visitor.Visit(container, itemVisitContext);
                        }
                    }
                }
                visitor.EndList(ref container, listContext);
            }
        }

        public int Count(TContainer container)
        {
            var list = GetValue(container);
            return list.Count;
        }
        
        int IListProperty.Count(IPropertyContainer container)
        {
            return Count((TContainer) container);
        }
        
        public IEnumerator<TItem> GetEnumerator(TContainer container)
        {
            return GetValue(container).GetEnumerator();
        }

        public void Add(TContainer container)
        {
            Add(container, m_CreateInstanceMethod(container));
        }

        public void AddObject(TContainer container, object item)
        {
            Add(container, (TItem)item);
        }

        public object GetObjectValueAtIndex(TContainer container, int index)
        {
            return GetValueAtIndex(container, index);
        }
        
        object IListProperty.GetObjectValueAtIndex(IPropertyContainer container, int index)
        {
            return GetObjectValueAtIndex((TContainer) container, index);
        }

        public void SetObjectValueAtIndex(TContainer container, int index, object value)
        {
            SetValueAtIndex(container, index, (TItem)value);
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
            container.VersionStorage?.IncrementVersion(this, container);
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

        public void RemoveAt(TContainer container, int index)
        {
            var list = GetValue(container);
            list.RemoveAt(index);
            container.VersionStorage?.IncrementVersion(this, container);
        }
        
        public virtual TItem GetValueAtIndex(TContainer container, int index)
        {
            var list = GetValue(container);
            return list[index];
        }
        
        public void SetValueAtIndex(TContainer container, int index, TItem value)
        {
            var list = GetValue(container);
            
            if (Equals(list[index], value))
            {
                return;
            }
            
            list[index] = value;
            container.VersionStorage?.IncrementVersion(this, container);
        }
        
        private static bool Equals(TItem a, TItem b)
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
                        var item = GetValueAtIndex(ref container, i);
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
        
        public int Count(ref TContainer container)
        {
            var list = GetValue(ref container);
            return list.Count;
        }
        
        int IListProperty.Count(IPropertyContainer container)
        {
            var c = (TContainer)container;
            return Count(ref c);
        }

        public void Clear(ref TContainer container)
        {
            var list = GetValue(ref container);
            list.Clear();
            container.VersionStorage?.IncrementVersion(this, container);
        }
        
        public IEnumerator<TItem> GetEnumerator(ref TContainer container)
        {
            return GetValue(ref container).GetEnumerator();
        }

        public void Add(ref TContainer container)
        {
            Add(ref container, m_CreateInstanceMethod(ref container));
        }

        public object GetObjectValueAtIndex(ref TContainer container, int index)
        {
            return GetValueAtIndex(ref container, index);
        }
        
        object IListProperty.GetObjectValueAtIndex(IPropertyContainer container, int index)
        {
            var c = (TContainer) container;
            return GetObjectValueAtIndex(ref c, index);
        }

        public void SetObjectValueAtIndex(ref TContainer container, int index, object value)
        {
            SetValueAtIndex(ref container, index, (TItem)value);
        }

        public virtual void Add(ref TContainer container, TItem item)
        {
            var list = GetValue(ref container);
            list.Add(item);
            container.VersionStorage?.IncrementVersion(this, container);
        }

        public void Clear(TContainer container)
        {
            var list = GetValue(ref container);
            list.Clear();
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
        
        public TItem GetValueAtIndex(ref TContainer container, int index)
        {
            var list = GetValue(ref container);
            return list[index];
        }
        
        public void SetValueAtIndex(ref TContainer container, int index, TItem value)
        {
            var list = GetValue(ref container);
            
            if (Equals(list[index], value))
            {
                return;
            }
            
            list[index] = value;
            container.VersionStorage?.IncrementVersion(this, container);
        }
        
        private static bool Equals(TItem a, TItem b)
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

                if (visitor.BeginList(ref container, listContext))
                {
                    var itemVisitContext = new VisitContext<TItem>
                    {
                        Property = this
                    };

                    var count = Count(container);
                    for (var i = 0; i < count; i++)
                    {
                        var item = GetValueAtIndex(container, i);
                        itemVisitContext.Value = item;
                        itemVisitContext.Index = i;

                        if (false == visitor.ExcludeVisit(container, itemVisitContext))
                        {
                            if (visitor.BeginContainer(ref container, itemVisitContext))
                            {
                                item.PropertyBag.Visit(item, visitor);
                            }
                            visitor.EndContainer(ref container, itemVisitContext);
                        }
                    }
                }
                visitor.EndList(ref container, listContext);
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
                        var item = GetValueAtIndex(ref container, i);
                        itemVisitContext.Value = item;
                        itemVisitContext.Index = i;

                        if (false == visitor.ExcludeVisit(ref container, itemVisitContext))
                        {
                            if (visitor.BeginContainer(ref container, itemVisitContext))
                            {
                                item.PropertyBag.Visit(item, visitor);
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

                if (visitor.BeginList(ref container, listContext))
                {
                    var itemVisitContext = new VisitContext<TItem>
                    {
                        Property = this
                    };

                    var count = Count(container);
                    for (var i = 0; i < count; i++)
                    {
                        var item = GetValueAtIndex(container, i);
                        itemVisitContext.Value = item;
                        itemVisitContext.Index = i;

                        if (false == visitor.ExcludeVisit(container, itemVisitContext))
                        {
                            if (visitor.BeginContainer(ref container, itemVisitContext))
                            {
                                item.PropertyBag.Visit(ref item, visitor);
                            }
                            visitor.EndContainer(ref container, itemVisitContext);
                        }
                    }
                }
                visitor.EndList(ref container, listContext);
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
                        var item = GetValueAtIndex(ref container, i);
                        itemVisitContext.Value = item;
                        itemVisitContext.Index = i;

                        if (false == visitor.ExcludeVisit(ref container, itemVisitContext))
                        {
                            if (visitor.BeginContainer(ref container, itemVisitContext))
                            {
                                item.PropertyBag.Visit(ref item, visitor);
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
