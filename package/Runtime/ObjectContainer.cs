#if (NET_4_6 || NET_STANDARD_2_0)

using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace Unity.Properties
{
    /// <summary>
    /// ObjectContainer acts as a proxy container for an IDictionary`string, object
    /// </summary>
    public class ObjectContainer : IPropertyContainer
    {
        /// <summary>
        /// Underylying data storage
        /// </summary>
        private readonly IDictionary<string, object> m_Value;

        /// <summary>
        /// Cache for inner containers
        /// </summary>
        private IDictionary<string, ObjectContainer> m_Containers;

        /// <summary>
        /// Cache for inner container lists
        /// </summary>
        private IDictionary<string, IList<ObjectContainer>> m_ContainerLists;
        
        /// <summary>
        /// Dynamic property bag for this container
        /// </summary>
        private readonly PropertyBag m_PropertyBag;
        
        public IPropertyBag PropertyBag => m_PropertyBag;
        public IVersionStorage VersionStorage => null;
        
        /// <summary>
        /// Initializes the proxy container and generates all top level properties
        /// </summary>
        /// <param name="value"></param>
        public ObjectContainer(IDictionary<string, object> value)
        {
            Assert.IsNotNull(value);

            m_Value = value;
            m_PropertyBag = new PropertyBag();

            // Initialize the the property bag based on the given dictionary
            // @NOTE Only the first level of properties are created, nested container proxies and thier properties are created on demand
            foreach (var kvp in m_Value)
            {
                m_PropertyBag.AddProperty(Properties.GetOrCreateProperty(kvp.Key, kvp.Value));
            }
        }
        
        /// <summary>
        /// Gets a nested object wrapped with an `ObjectContainer` proxy
        /// </summary>
        private ObjectContainer GetContainer(string name)
        {
            if (null == m_Containers)
            {
                m_Containers = new Dictionary<string, ObjectContainer>();
            }

            ObjectContainer container;
            if (m_Containers.TryGetValue(name, out container))
            {
                return container;
            }

            object value;
            m_Value.TryGetValue(name, out value);

            container = new ObjectContainer(value as IDictionary<string, object>);
            m_Containers.Add(name, container);

            return container;
        }

        /// <summary>
        /// Gets a nested list of containers; each element is wrapped with an `ObjectContainer` proxy
        /// </summary>
        private IList<ObjectContainer> GetContainerList(string name)
        {
            if (null == m_ContainerLists)
            {
                m_ContainerLists = new Dictionary<string, IList<ObjectContainer>>();
            }

            IList<ObjectContainer> list;
            if (m_ContainerLists.TryGetValue(name, out list))
            {
                return list;
            }

            object value;
            m_Value.TryGetValue(name, out value);

            var data = value as IList<object>;
            Assert.IsNotNull(data);

            list = data.Select(item => new ObjectContainer(item as IDictionary<string, object>)).ToList();
            m_ContainerLists.Add(name, list);

            return list;
        }
        
        /// <summary>
        /// Helper class to cache dynamically generated properties
        ///
        /// Since object container properties always have untyped values. We can shared properties accross multiple objects
        /// </summary>
        private static class Properties
        {
            private static readonly Dictionary<string, ITypedContainerProperty<ObjectContainer>> s_ValueProperties = new Dictionary<string, ITypedContainerProperty<ObjectContainer>>();
            private static readonly Dictionary<string, ITypedContainerProperty<ObjectContainer>> s_ClassValueProperties = new Dictionary<string, ITypedContainerProperty<ObjectContainer>>();
            private static readonly Dictionary<string, ITypedContainerProperty<ObjectContainer>> s_ValueListProperties = new Dictionary<string, ITypedContainerProperty<ObjectContainer>>();
            private static readonly Dictionary<string, ITypedContainerProperty<ObjectContainer>> s_ClassListProperties = new Dictionary<string, ITypedContainerProperty<ObjectContainer>>();
            
            public static ITypedContainerProperty<ObjectContainer> GetOrCreateProperty(string name, object value)
            {
                ITypedContainerProperty<ObjectContainer> property = null;

                if (value is IDictionary<string, object>)
                {
                    if (s_ClassValueProperties.TryGetValue(name, out property)) return property;
                    property = new ClassValueProperty(name);
                    s_ClassValueProperties.Add(name, property);
                    return property;
                }

                if (value is IList<object>)
                {
                    var first = ((IList<object>) value).FirstOrDefault();
                    if (first is IDictionary<string, object>)
                    {
                        if (s_ClassListProperties.TryGetValue(name, out property)) return property;
                        property = new ClassListProperty(name);
                        s_ClassListProperties.Add(name, property);
                        return property;
                    }

                    if (s_ValueListProperties.TryGetValue(name, out property)) return property;
                    property = new ValueListProperty(name);
                    s_ValueListProperties.Add(name, property);
                    return property;
                }

                if (s_ValueProperties.TryGetValue(name, out property)) return property;
                property = new ValueProperty(name);
                s_ValueProperties.Add(name, property);
                
                return property;
            }
            
            /// <summary>
            /// Primitive value property
            /// </summary>
            private class ValueProperty : Property<ObjectContainer, object>
            {
                public override bool IsReadOnly => false;

                public ValueProperty(string name) : base(name, null, null)
                {
                }

                public override object GetValue(ObjectContainer container)
                {
                    if (null == container.m_Value)
                    {
                        return null;
                    }

                    object value;
                    return container.m_Value.TryGetValue(Name, out value) ? value : null;
                }

                public override void SetValue(ObjectContainer container, object value)
                {
                    if (null == container.m_Value)
                    {
                        return;
                    }

                    if (!container.m_Value.ContainsKey(Name))
                    {
                        return;
                    }

                    container.m_Value[Name] = value;
                }

                public override void Accept(ObjectContainer container, IPropertyVisitor visitor)
                {
                    var context = new VisitContext<object>
                    {
                        Property = this,
                        Value = GetValue(container),
                        Index = -1
                    };

                    if (!visitor.ExcludeVisit(container, context))
                    {
                        visitor.Visit(container, context);
                    }
                }
            }
            
            /// <summary>
            /// Container value property
            /// </summary>
            private class ClassValueProperty : ContainerProperty<ObjectContainer, ObjectContainer>
            {
                public override bool IsReadOnly => false;

                public ClassValueProperty(string name) : base(name, null, null)
                {
                }

                public override ObjectContainer GetValue(ObjectContainer container)
                {
                    return container.GetContainer(Name);
                }

                public override void SetValue(ObjectContainer container, ObjectContainer value)
                {
                    throw new System.NotImplementedException();
                }

                public override void Accept(ObjectContainer container, IPropertyVisitor visitor)
                {
                    var value = GetValue(container);

                    var context = new VisitContext<ObjectContainer>
                    {
                        Property = this,
                        Value = value,
                        Index = -1
                    };

                    if (visitor.ExcludeVisit(container, context))
                    {
                        return;
                    }

                    if (visitor.BeginContainer(container, context))
                    {
                        value.Visit(visitor);
                    }

                    visitor.EndContainer(container, context);
                }
            }
            
            /// <inheritdoc />
            /// <summary>
            /// Primitive list property
            /// </summary>
            private class ValueListProperty : ListProperty<ObjectContainer, IList<object>, object>
            {
                public ValueListProperty(string name) : base(name, null, null)
                {
                }

                public override IList<object> GetValue(ObjectContainer container)
                {
                    if (null == container.m_Value)
                    {
                        return null;
                    }

                    object value;
                    return container.m_Value.TryGetValue(Name, out value) ? value as IList<object> : null;
                }

                public override void Accept(ObjectContainer container, IPropertyVisitor visitor)
                {
                    var list = GetValue(container);

                    var listContext = new VisitContext<IList<object>>
                    {
                        Property = this,
                        Value = list,
                        Index = -1
                    };

                    if (visitor.ExcludeVisit(container, listContext))
                    {
                        return;
                    }

                    if (visitor.BeginList(container, listContext))
                    {
                        var itemVisitContext = new VisitContext<object>
                        {
                            Property = this
                        };

                        for (var i = 0; i < Count(container); i++)
                        {
                            var item = list?[i];

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

            /// <inheritdoc />
            /// <summary>
            /// Container list property
            /// </summary>
            private class ClassListProperty : ContainerListProperty<ObjectContainer, IList<ObjectContainer>, ObjectContainer>
            {
                public ClassListProperty(string name) : base(name, null, null)
                {
                }

                public override IList<ObjectContainer> GetValue(ObjectContainer container)
                {
                    return container.GetContainerList(Name);
                }

                public override void Accept(ObjectContainer container, IPropertyVisitor visitor)
                {
                    var list = GetValue(container);

                    var listContext = new VisitContext<IList<ObjectContainer>>
                    {
                        Property = this,
                        Value = list,
                        Index = -1
                    };

                    if (visitor.ExcludeVisit(container, listContext))
                    {
                        return;
                    }

                    if (visitor.BeginList(container, listContext))
                    {
                        var itemVisitContext = new VisitContext<ObjectContainer>
                        {
                            Property = this
                        };

                        for (var i = 0; i < Count(container); i++)
                        {
                            var item = list?[i];

                            itemVisitContext.Value = item;
                            itemVisitContext.Index = i;

                            if (visitor.BeginContainer(container, itemVisitContext))
                            {
                                item.Visit(visitor);
                            }

                            visitor.EndContainer(container, itemVisitContext);
                        }
                    }

                    visitor.EndList(container, listContext);
                }
            }
        }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)