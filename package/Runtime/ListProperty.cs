using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Properties
{
    public class ListProperty<TContainer, TValue, TItem> : Property<TContainer, TValue>, IListProperty<TContainer, TItem>
        where TContainer : class, IPropertyContainer
        where TValue : IList<TItem>
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
            Debug.Assert(typeof(TItem).IsValueType, $"List on container {typeof(TContainer)} of reference type {typeof(TItem)} should have their createInstanceMethod specified");
            return default(TItem);
        }

        public override void Accept(TContainer container, IPropertyVisitor visitor)
        {
            var value = GetValue(container);
            
            // Delegate the Visit implementation to the user
            if (TryUserAccept(ref container, visitor, value))
            {
                // User has handled the visit; early exit
                return;
            }

            var itemTypeVisitor = visitor as IPropertyVisitor<TItem>;
            var listContext = new ListContext<TValue> {Property = this, Value = value, Index = -1, Count = value.Count};

            if (visitor.BeginList(ref container, listContext))
            {
                var itemVisitContext = new VisitContext<TItem>
                {
                    // TODO: we have no properties for list items
                    Property = null
                };

                for (var i = 0; i < Count(container); i++)
                {
                    var item = GetValueAtIndex(container, i);
                    itemVisitContext.Value = item;
                    itemVisitContext.Index = i;

                    if (null != itemTypeVisitor)
                    {
                        itemTypeVisitor.Visit(ref container, itemVisitContext);
                    }
                    else
                    {
                        visitor.Visit(ref container, itemVisitContext);
                    }
                }
            }
            visitor.EndList(ref container, listContext);
        }

        public int Count(TContainer container)
        {
            var list = GetValue(container);
            return list.Count;
        }
        
        public IEnumerator<TItem> GetEnumerator(TContainer container)
        {
            return GetValue(container).GetEnumerator();
        }

        public void Add(TContainer container)
        {
            Add(container, m_CreateInstanceMethod(container));
        }
        
        public virtual void Add(TContainer container, TItem item)
        {
            var list = GetValue(container);
            list.Add(item);
            container.VersionStorage?.IncrementVersion(this, ref container);
        }

        public void Clear(TContainer container)
        {
            var list = GetValue(container);
            list.Clear();
            container.VersionStorage?.IncrementVersion(this, ref container);
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
            container.VersionStorage?.IncrementVersion(this, ref container);
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
            container.VersionStorage?.IncrementVersion(this, ref container);
        }

        public void RemoveAt(TContainer container, int index)
        {
            var list = GetValue(container);
            list.RemoveAt(index);
            container.VersionStorage?.IncrementVersion(this, ref container);
        }
        
        public TItem GetValueAtIndex(TContainer container, int index)
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
            container.VersionStorage?.IncrementVersion(this, ref container);
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
        where TValue : IList<TItem>
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
            
            // Delegate the Visit implementation to the user
            if (TryUserAccept(ref container, visitor, value))
            {
                // User has handled the visit; early exit
                return;
            }

            var itemTypeVisitor = visitor as IPropertyVisitor<TItem>;
            var listContext = new ListContext<TValue> {Property = this, Value = value, Index = -1, Count = value.Count};

            if (visitor.BeginList(ref container, listContext))
            {
                var itemVisitContext = new VisitContext<TItem>
                {
                    // TODO: we have no properties for list items
                    Property = null
                };

                for (var i = 0; i < Count(ref container); i++)
                {
                    var item = GetValueAtIndex(ref container, i);
                    itemVisitContext.Value = item;
                    itemVisitContext.Index = i;

                    if (null != itemTypeVisitor)
                    {
                        itemTypeVisitor.Visit(ref container, itemVisitContext);
                    }
                    else
                    {
                        visitor.Visit(ref container, itemVisitContext);
                    }
                }
            }
            visitor.EndList(ref container, listContext);
        }
        
        public int Count(ref TContainer container)
        {
            var list = GetValue(ref container);
            return list.Count;
        }

        public void Clear(ref TContainer container)
        {
            var list = GetValue(ref container);
            list.Clear();
            container.VersionStorage?.IncrementVersion(this, ref container);
        }
        
        public IEnumerator<TItem> GetEnumerator(ref TContainer container)
        {
            return GetValue(ref container).GetEnumerator();
        }

        public void Add(ref TContainer container)
        {
            Add(ref container, m_CreateInstanceMethod(ref container));
        }
        
        public virtual void Add(ref TContainer container, TItem item)
        {
            var list = GetValue(ref container);
            list.Add(item);
            container.VersionStorage?.IncrementVersion(this, ref container);
        }

        public void Clear(TContainer container)
        {
            var list = GetValue(ref container);
            list.Clear();
            container.VersionStorage?.IncrementVersion(this, ref container);
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
            container.VersionStorage?.IncrementVersion(this, ref container);
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
            container.VersionStorage?.IncrementVersion(this, ref container);
        }

        public void RemoveAt(ref TContainer container, int index)
        {
            var list = GetValue(ref container);
            list.RemoveAt(index);
            container.VersionStorage?.IncrementVersion(this, ref container);
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
            container.VersionStorage?.IncrementVersion(this, ref container);
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
        where TValue : IList<TItem>
        where TItem : class, IPropertyContainer
    {
        public ContainerListProperty(string name, GetValueMethod getValue, SetValueMethod setValue, CreateInstanceMethod createInstanceMethod = null) 
            : base(name, getValue, setValue, createInstanceMethod)
        {
        }

        public override void Accept(TContainer container, IPropertyVisitor visitor)
        {
            var value = GetValue(container);
            
            // Delegate the Visit implementation to the user
            if (TryUserAccept(ref container, visitor, value))
            {
                // User has handled the visit; early exit
                return;
            }
           
            var listContext = new ListContext<TValue> {Property = this, Value = value, Index = -1, Count = value.Count};

            if (visitor.BeginList(ref container, listContext))
            {
                var typedItemVisitor = visitor as IPropertyVisitor<TItem>;

                if (null != typedItemVisitor)
                {
                    for (var i=0; i<Count(container); i++)
                    {
                        var item = GetValueAtIndex(container, i);
                        
                        var context = new VisitContext<TItem>
                        {
                            // TODO: we have no property for items
                            Property = null,
                            Value = item,
                            Index = i
                        };

                        typedItemVisitor.Visit(ref container, context);
                    }
                }
                else
                {
                    var count = Count(container);
                    for (var i=0; i<count; i++)
                    {
                        var item = GetValueAtIndex(container, i);
                        var context = new SubtreeContext<TItem>
                        {
                            Property = this,
                            Value = item,
                            Index = i
                        };
                    
                        if (visitor.BeginContainer(ref container, context))
                        {
                            item.PropertyBag.Visit(item, visitor);
                        }
                        visitor.EndContainer(ref container, context);
                    }
                }
            }
            visitor.EndList(ref container, listContext);
        }
    }
    
    public class StructContainerListProperty<TContainer, TValue, TItem> : StructListProperty<TContainer, TValue, TItem>
        where TContainer : struct, IPropertyContainer
        where TValue : IList<TItem>
        where TItem : class, IPropertyContainer
    {
        public StructContainerListProperty(string name, GetValueMethod getValue, SetValueMethod setValue, CreateInstanceMethod createInstanceMethod = null) 
            : base(name, getValue, setValue, createInstanceMethod)
        {
        }

        public override void Accept(ref TContainer container, IPropertyVisitor visitor)
        {
            var value = GetValue(ref container);
            
            // Delegate the Visit implementation to the user
            if (TryUserAccept(ref container, visitor, value))
            {
                // User has handled the visit; early exit
                return;
            }
           
            var listContext = new ListContext<TValue> { Property = this, Value = value, Index = -1, Count = value.Count };

            if (visitor.BeginList(ref container, listContext))
            {
                var typedItemVisitor = visitor as IPropertyVisitor<TItem>;

                if (null != typedItemVisitor)
                {
                    for (var i = 0; i < Count(ref container); i++)
                    {
                        var item = GetValueAtIndex(ref container, i);
                        
                        var context = new VisitContext<TItem>
                        {
                            // TODO: we have no property for items
                            Property = null,
                            Value = item,
                            Index = i
                        };

                        typedItemVisitor.Visit(ref container, context);
                    }
                }
                else
                {
                    var count = Count(ref container);
                    for (var i=0; i<count; i++)
                    {
                        var item = GetValueAtIndex(ref container, i);
                        var context = new SubtreeContext<TItem>
                        {
                            Property = this,
                            Value = item,
                            Index = i
                        };
                    
                        if (visitor.BeginContainer(ref container, context))
                        {
                            item.PropertyBag.Visit(item, visitor);
                        }
                        visitor.EndContainer(ref container, context);
                    }
                }
            }
            visitor.EndList(ref container, listContext);
        }
    }
    
    public class MutableContainerListProperty<TContainer, TValue, TItem> : ListProperty<TContainer, TValue, TItem>
        where TContainer : class, IPropertyContainer
        where TValue : IList<TItem>
        where TItem : struct, IPropertyContainer
    {
        public MutableContainerListProperty(string name, GetValueMethod getValue, SetValueMethod setValue, CreateInstanceMethod createInstanceMethod = null) 
            : base(name, getValue, setValue, createInstanceMethod)
        {
        }

        public override void Accept(TContainer container, IPropertyVisitor visitor)
        {
            var value = GetValue(container);
            
            // Delegate the Visit implementation to the user
            if (TryUserAccept(ref container, visitor, value))
            {
                // User has handled the visit; early exit
                return;
            }
           
            var listContext = new ListContext<TValue> {Property = this, Value = value, Index = -1, Count = value.Count};

            if (visitor.BeginList(ref container, listContext))
            {
                var typedItemVisitor = visitor as IPropertyVisitor<TItem>;

                if (null != typedItemVisitor)
                {
                    for (var i=0; i<Count(container); i++)
                    {
                        var item = GetValueAtIndex(container, i);
                        
                        var context = new VisitContext<TItem>
                        {
                            // TODO: we have no property for items
                            Property = null,
                            Value = item,
                            Index = i
                        };

                        typedItemVisitor.Visit(ref container, context);
                        
                        // can't have a ref to a list item, so always assign it back
                        SetValueAtIndex(container, i, context.Value);
                    }
                }
                else
                {
                    var count = Count(container);
                    for (var i=0; i<count; i++)
                    {
                        var item = GetValueAtIndex(container, i);
                        var context = new SubtreeContext<TItem>
                        {
                            Property = this,
                            Value = item,
                            Index = i
                        };
                    
                        if (visitor.BeginContainer(ref container, context))
                        {
                            item.PropertyBag.VisitStruct(ref item, visitor);
                        }
                        visitor.EndContainer(ref container, context);
                        
                        // can't have a ref to a list item, so always assign it back
                        SetValueAtIndex(container, i, context.Value);
                    }
                }
            }
            visitor.EndList(ref container, listContext);
        }
    }
    
    public class StructMutableContainerListProperty<TContainer, TValue, TItem> : StructListProperty<TContainer, TValue, TItem>
        where TContainer : struct, IPropertyContainer
        where TValue : IList<TItem>
        where TItem : struct, IPropertyContainer
    {
        public StructMutableContainerListProperty(string name, GetValueMethod getValue, SetValueMethod setValue, CreateInstanceMethod createInstanceMethod = null) 
            : base(name, getValue, setValue, createInstanceMethod)
        {
        }

        public override void Accept(ref TContainer container, IPropertyVisitor visitor)
        {
            var value = GetValue(ref container);
            
            // Delegate the Visit implementation to the user
            if (TryUserAccept(ref container, visitor, value))
            {
                // User has handled the visit; early exit
                return;
            }
           
            var listContext = new ListContext<TValue> {Property = this, Value = value, Index = -1, Count = value.Count};

            if (visitor.BeginList(ref container, listContext))
            {
                var typedItemVisitor = visitor as IPropertyVisitor<TItem>;

                if (null != typedItemVisitor)
                {
                    for (var i=0; i<Count(ref container); i++)
                    {
                        var item = GetValueAtIndex(ref container, i);
                        
                        var context = new VisitContext<TItem>
                        {
                            // TODO: we have no property for items
                            Property = null,
                            Value = item,
                            Index = i
                        };

                        typedItemVisitor.Visit(ref container, context);
                        
                        // can't have a ref to a list item, so always assign it back
                        SetValueAtIndex(ref container, i, context.Value);
                    }
                }
                else
                {
                    var count = Count(ref container);
                    for (var i=0; i<count; i++)
                    {
                        var item = GetValueAtIndex(ref container, i);
                        var context = new SubtreeContext<TItem>
                        {
                            Property = this,
                            Value = item,
                            Index = i
                        };
                    
                        if (visitor.BeginContainer(ref container, context))
                        {
                            item.PropertyBag.VisitStruct(ref item, visitor);
                        }
                        visitor.EndContainer(ref container, context);
                        
                        // can't have a ref to a list item, so always assign it back
                        SetValueAtIndex(ref container, i, context.Value);
                    }
                }
            }
            visitor.EndList(ref container, listContext);
        }
    }
}
