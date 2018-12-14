

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.Assertions;

namespace Unity.Tiny.Serialization
{
    internal static class TransientState
    {
        public static IPropertyContainer Persist(IPropertyContainer container)
        {
            var visitor = new TransientStateVisitor();
            container.Visit(visitor);
            return new ObjectContainer(visitor.Result);
        }

        private class TransientStateVisitor : PropertyVisitor
        {
            private readonly PropertyPath m_Path = new PropertyPath();
            private readonly Dictionary<string, object> m_Container = new Dictionary<string, object>();
            private int m_TransientContainerStack;

            public IDictionary<string, object> Result => m_Container;
            
            protected override bool BeginContainer()
            {
                m_Path.Push(Property.Name, ListIndex);

                if (Property.HasAttribute<TransientAttribute>())
                {
                    m_TransientContainerStack++;
                }

                return base.BeginContainer();
            }

            protected override void EndContainer()
            {
                m_Path.Pop();
                
                if (Property.HasAttribute<TransientAttribute>())
                {
                    m_TransientContainerStack--;
                }
                
                base.EndContainer();
            }
            
            protected override void Visit<TValue>(TValue value)
            {
                if (!Property.HasAttribute<TransientAttribute>() && m_TransientContainerStack <= 0)
                {
                    return;
                }
                
                m_Path.Push(Property.Name, ListIndex);
                try
                {
                    RecordProperty(m_Path, value);
                }
                finally
                {
                    m_Path.Pop();
                }
            }

            private void RecordProperty(PropertyPath path, object value)
            {
                IDictionary<string, object> container = m_Container;
                
                var count = path.PartsCount;
                
                for (var i = 0; i < count; i++)
                {
                    var part = path[i];
                    
                    Assert.IsNotNull(container);

                    if (part.IsListItem)
                    {
                        object obj;
                        if (!container.TryGetValue(part.propertyName, out obj))
                        {
                            // Create a new list and register it
                            obj = new List<object>();
                            container.Add(part.propertyName, obj);
                        }

                        var list = obj as IList<object>;
                        
                        Assert.IsNotNull(list);
                        
                        // Ensure capacity
                        while (list.Count <= part.listIndex)
                        {
                            list.Add(null);
                        }
                        
                        if (i < count - 1)
                        {
                            // List element which is NOT our final value
                            var element = list[part.listIndex];

                            // This can ONLY be a nested container
                            if (null == element)
                            {
                                var c = new Dictionary<string, object>();
                                list[part.listIndex] = c;
                                container = c;
                            }
                            else
                            {
                                container = element as IDictionary<string, object>;
                            }
                        }
                        else
                        {
                            // Insert the final value
                            list[part.listIndex] = value;
                        }
                    }
                    else
                    {
                        if (i < count - 1)
                        {
                            object obj;
                            if (!container.TryGetValue(part.propertyName, out obj))
                            {
                                // Create a new nested container and register it
                                var c = new Dictionary<string, object>();
                                container.Add(part.propertyName, c);
                                container = c;
                            }
                            else
                            {
                                // Pick up existing nested container
                                container = obj as IDictionary<string, object>;
                            }
                        }
                        else
                        {
                            // Insert the final value
                            container.Add(part.propertyName, value);
                        }
                    }
                }
            }
        }
    }
}

