#if (NET_4_6 || NET_STANDARD_2_0)

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Properties
{
    public static partial class PropertyContainer
    {
        public static void Transfer<TSource, TDestination>(TSource source, TDestination destination)
            where TSource : class, IPropertyContainer
            where TDestination : class, IPropertyContainer
        {
            source.Visit(new TransferVisitor(destination));
        }
        
        public static TDestination Transfer<TSource, TDestination>(TSource source)
            where TSource : class, IPropertyContainer
            where TDestination : struct, IStructPropertyContainer<TDestination>
        {
            var visitor = new TransferVisitor(new TDestination());
            source.Visit(visitor);
            return (TDestination) visitor.Pop();
        }
        
        public static void Transfer<TSource, TDestination>(ref TSource source, TDestination destination)
            where TSource : struct, IPropertyContainer
            where TDestination : class, IPropertyContainer
        {
            Visit(ref source, new TransferVisitor(destination));
        }
        
        public static TDestination Transfer<TSource, TDestination>(ref TSource source)
            where TSource : struct, IPropertyContainer
            where TDestination : struct, IStructPropertyContainer<TDestination>
        {
            var visitor = new TransferVisitor(new TDestination());
            Visit(ref source, visitor);
            return (TDestination) visitor.Pop();
        }
        
        private class TransferVisitor : IPropertyVisitor
        {
            private readonly Stack<IPropertyContainer> m_PropertyContainers = new Stack<IPropertyContainer>();

            public TransferVisitor(IPropertyContainer container)
            {
                m_PropertyContainers.Push(container);
            }
            
            public IPropertyContainer Pop()
            {
                return m_PropertyContainers.Pop();
            }
            
            public bool ExcludeVisit<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
                where TContainer : class, IPropertyContainer
            {
                return false;
            }

            public bool ExcludeVisit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context) 
                where TContainer : struct, IPropertyContainer
            {
                return false;
            }

            public void Visit<TContainer, TValue>(TContainer container, VisitContext<TValue> context) 
                where TContainer : class, IPropertyContainer
            {
                SetPropertyValue(context.Property.Name, context.Value, context.Index);
            }

            public void Visit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
                where TContainer : struct, IPropertyContainer
            {
                SetPropertyValue(context.Property.Name, context.Value, context.Index);
            }

            public bool BeginContainer<TContainer, TValue>(TContainer container, VisitContext<TValue> context) 
                where TContainer : class, IPropertyContainer where TValue : IPropertyContainer
            {
                return PushContainer(context.Property.Name, context.Index);
            }

            public bool BeginContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context) where TContainer : struct, IPropertyContainer where TValue : IPropertyContainer
            {
                return PushContainer(context.Property.Name, context.Index);
            }
            
            public void EndContainer<TContainer, TValue>(TContainer container, VisitContext<TValue> context) where TContainer : class, IPropertyContainer where TValue : IPropertyContainer
            {
                PopContainer(context.Property.Name,  context.Value, context.Index);
            }

            public void EndContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context) where TContainer : struct, IPropertyContainer where TValue : IPropertyContainer
            {
                PopContainer(context.Property.Name, context.Value, context.Index);
            }
            
            public bool BeginCollection<TContainer, TValue>(TContainer container, VisitContext<TValue> context) where TContainer : class, IPropertyContainer
            {
                BeginCollection(context.Property.Name);
                return true;
            }

            public bool BeginCollection<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context) where TContainer : struct, IPropertyContainer
            {
                BeginCollection(context.Property.Name);
                return true;
            }

            public void EndCollection<TContainer, TValue>(TContainer container, VisitContext<TValue> context) where TContainer : class, IPropertyContainer
            {
                
            }

            public void EndCollection<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context) where TContainer : struct, IPropertyContainer
            {
            }

            private void BeginCollection(string name)
            {
                var container = m_PropertyContainers.Peek();
                var property = container.PropertyBag.FindProperty(name);
                
                // class properties
                (property as IListClassProperty)?.Clear(container);
                (property as IHashSetClassProperty)?.Clear(container);
                
                // struct properties
                (property as IListStructProperty)?.Clear(ref container);
                (property as IHashSetStructProperty)?.Clear(ref container);
            }
            
            private bool PushContainer(string name, int index)
            {
                var container = m_PropertyContainers.Peek();
                var property = container?.PropertyBag.FindProperty(name);

                if (null == property)
                {
                    m_PropertyContainers.Push(null);
                    return false;
                }

                // @NOTE When dealing with struct containers we will incurs boxing allocations here
                //       Theres no reasonable way around this.
                IPropertyContainer value = null;
                Type type;
                
                if (index < 0)
                {
                    type = (property as IValueProperty)?.ValueType;

                    value = value ?? (property as IValueClassProperty)?.GetObjectValue(container) as IPropertyContainer;
                    value = value ?? (property as IValueStructProperty)?.GetObjectValue(container) as IPropertyContainer;
                }
                else
                {
                    var listProperty = property as IListProperty;
                    type = listProperty?.ItemType;
                    
                    var listClassProperty = property as IListClassProperty;
                    if (null != listClassProperty)
                    {
                        // @TODO Fix up
                        listClassProperty.AddNew(container);
                        value = listClassProperty.GetObjectAt(container, index) as IPropertyContainer;
                        listClassProperty.RemoveAt(container, index);
                    }

                    var listStructProperty = property as IListStructProperty;
                    if (null != listStructProperty)
                    {
                        // @TODO as above, missing API calls
                        throw new NotImplementedException(); 
                    }
                }

                m_PropertyContainers.Push(value);
                var result = typeof(IPropertyContainer).IsAssignableFrom(type);
                return result;
            }

            private void PopContainer<TValue>(string name, TValue value, int index)
            {
                var container = m_PropertyContainers.Pop();

                if (null == container)
                {
                    SetPropertyValue(name, value, index);
                    return;
                }
                
                SetPropertyValue(name, container, index);
            }

            private void SetPropertyValue<TValue>(string name, TValue value, int index)
            {
                var target = m_PropertyContainers.Peek();
                var property = target?.PropertyBag.FindProperty(name);
                
                if (null == property)
                {
                    return;
                }

                if ((property as IValueProperty)?.IsReadOnly ?? false)
                {
                    return;
                }
                
                // class properties
                if (property is IClassProperty)
                {
                    // try to use a typed interface to avoid boxing
                    var valueTypedValueProperty = property as IValueTypedValueClassProperty<TValue>;
                    if (valueTypedValueProperty != null)
                    {
                        valueTypedValueProperty.SetValue(target, value);
                        return;
                    }
                
                    var listTypedItemProperty = property as IListTypedItemClassProperty<TValue>;
                    if (listTypedItemProperty != null)
                    {
                        listTypedItemProperty.Add(target, value);
                        return;
                    }
                
                    var hashSetTypedItemProperty = property as IHashSetTypedItemClassProperty<TValue>;
                    if (hashSetTypedItemProperty != null)
                    {
                        hashSetTypedItemProperty.Add(target, value);
                        return;
                    }
                
                    // fallback to object interface
                    (property as IValueClassProperty)?.SetObjectValue(target, value);
                    (property as IListClassProperty)?.AddObject(target, value);
                    (property as IHashSetClassProperty)?.AddObject(target, value);
                }
                else if (property is IStructProperty)
                {
                    // try to use a typed interface to avoid boxing
                    var valueTypedValueProperty = property as IValueTypedValueStructProperty<TValue>;
                    if (valueTypedValueProperty != null)
                    {
                        valueTypedValueProperty.SetValue(ref target, value);
                        return;
                    }
                
                    var listTypedItemProperty = property as IListTypedItemStructProperty<TValue>;
                    if (listTypedItemProperty != null)
                    {
                        listTypedItemProperty.Add(ref target, value);
                        return;
                    }
                
                    var hashSetTypedItemProperty = property as IHashSetTypedItemStructProperty<TValue>;
                    if (hashSetTypedItemProperty != null)
                    {
                        hashSetTypedItemProperty.Add(ref target, value);
                        return;
                    }
                    
                    // fallback to object interface
                    (property as IValueStructProperty)?.SetObjectValue(ref target, value);
                    (property as IListStructProperty)?.AddObject(ref target, value);
                    (property as IHashSetStructProperty)?.AddObject(ref target, value);
                }
                
                if (property is IStructProperty)
                {
                    m_PropertyContainers.Pop();
                    m_PropertyContainers.Push(target);
                }
            }
        }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)