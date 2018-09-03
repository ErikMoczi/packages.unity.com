#if (NET_4_6 || NET_STANDARD_2_0)

using System;
using System.Collections.Generic;
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
            where TDestination : struct, IPropertyContainer
        {
            throw new Exception("PropertyContainer Transfering to struct types is not supported");
        }
        
        public static void Transfer<TSource, TDestination>(ref TSource source, TDestination destination)
            where TSource : struct, IPropertyContainer
            where TDestination : class, IPropertyContainer
        {
            Visit(ref source, new TransferVisitor(destination));
        }
        
        public static TDestination Transfer<TSource, TDestination>(ref TSource source)
            where TSource : struct, IPropertyContainer
            where TDestination : struct, IPropertyContainer
        {
            throw new Exception("PropertyContainer Transfering to struct types is not supported");
        }

        /// <summary>
        /// Transfers property wise from one tree to another
        /// </summary>
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
            
            public void VisitEnum<TContainer, TValue>(TContainer container, VisitContext<TValue> context) 
                where TContainer : class, IPropertyContainer where TValue : struct
            {
            }

            public void VisitEnum<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context) 
                where TContainer : struct, IPropertyContainer where TValue : struct
            {
            }

            public bool BeginContainer<TContainer, TValue>(TContainer container, VisitContext<TValue> context) 
                where TContainer : class, IPropertyContainer where TValue : IPropertyContainer
            {
                PushContainer(context.Property.Name, context.Index);
                return true;
            }

            public bool BeginContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context) where TContainer : struct, IPropertyContainer where TValue : IPropertyContainer
            {
                PushContainer(context.Property.Name, context.Index);
                return true;
            }
            
            public void EndContainer<TContainer, TValue>(TContainer container, VisitContext<TValue> context) where TContainer : class, IPropertyContainer where TValue : IPropertyContainer
            {
                PopContainer(context.Property.Name, context.Index);
            }

            public void EndContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context) where TContainer : struct, IPropertyContainer where TValue : IPropertyContainer
            {
                PopContainer(context.Property.Name, context.Index);
            }
            
            public bool BeginList<TContainer, TValue>(TContainer container, VisitContext<TValue> context) where TContainer : class, IPropertyContainer
            {
                BeginCollection(context.Property.Name);
                return true;
            }

            public bool BeginList<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context) where TContainer : struct, IPropertyContainer
            {
                BeginCollection(context.Property.Name);
                return true;
            }

            public void EndList<TContainer, TValue>(TContainer container, VisitContext<TValue> context) where TContainer : class, IPropertyContainer
            {
                
            }

            public void EndList<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context) where TContainer : struct, IPropertyContainer
            {
            }

            private void BeginCollection(string name)
            {
                var container = m_PropertyContainers.Peek();
                var property = container?.PropertyBag.FindProperty(name);
                (property as IListProperty)?.Clear(container);
            }
            
            private void PushContainer(string name, int index)
            {
                var container = m_PropertyContainers.Peek();
                var property = container?.PropertyBag.FindProperty(name);

                if (null == property)
                {
                    // This is a valid case when the container structures do not match, we still need to maintain the stack
                    m_PropertyContainers.Push(null);
                    return;
                }

                // @NOTE When dealing with struct containers we will incurs boxing allocations here
                //       Theres no reasonable way around this.
                IPropertyContainer value = null;
                
                if (index < 0)
                {
                    // @TODO This should be a user specified construction method instead of relying on `Activator`
                    value = Activator.CreateInstance(property.ValueType) as IPropertyContainer;
                }
                else
                {
                    var listProperty = property as IListProperty;
                    if (null != listProperty)
                    {
                        // @TODO This should be a user specified construction method instead of relying on `Activator`
                        value = Activator.CreateInstance(listProperty.ItemType) as IPropertyContainer;   
                    }
                }
                
                m_PropertyContainers.Push(value);
            }

            private void PopContainer(string name, int index)
            {
                SetPropertyValue(name, m_PropertyContainers.Pop(), index);
            }

            private void SetPropertyValue<TValue>(string name, TValue value, int index)
            {
                // The container we are 'transfering' to
                var target = m_PropertyContainers.Peek();

                if (null == target)
                {
                    // No container was found, this means our structure does not match the source container fully
                    // Silently bail out
                    return;
                }

                if (target.GetType().IsValueType)
                {
                    // @TODO We cannot handle struct types since we have no way of downcasting to the concrete type (i.e. This means we cannot modify the struct in place)
                    throw new Exception("PropertyContainer Transfering to struct types is not supported");
                }
                
                // The property we are 'transfering' to
                var property = target.PropertyBag.FindProperty(name);
                
                if (null == property)
                {
                    // No matching property was found. Silently bail out
                    return;
                }
                
                // try to use a typed interface to avoid boxing
                var valueTypedValueProperty = property as ITypedValueProperty<TValue>;
                if (valueTypedValueProperty != null)
                {
                    valueTypedValueProperty.SetValue(target, value);
                    return;
                }

                var listPropery = property as IListProperty;
                if (listPropery != null)
                {
                    listPropery.AddObject(target, value);
                    return;
                }
                
                // fallback to the base interface
                property.SetObjectValue(target, value);
            }
        }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)
