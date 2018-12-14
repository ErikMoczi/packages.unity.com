
using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;

namespace Unity.Tiny
{
    internal class DefaultGUIChangeTracker : IGUIChangeTracker
    {
        private readonly HashSet<PropertyChangeContext> m_Changes = new HashSet<PropertyChangeContext>();
        private readonly List<List<IPropertyContainer>> m_Resolvers = new List<List<IPropertyContainer>>();

        public bool HasChanges => m_Changes.Count > 0;

        public IReadOnlyList<List<IPropertyContainer>> Resolvers => m_Resolvers.AsReadOnly();

        public void PushChange(IPropertyContainer container, IProperty property)
            => PushChange(m_Resolvers[Resolvers.Count - 1], container, property);

        private void PushChange(List<IPropertyContainer> resolvers, IPropertyContainer container, IProperty property)
        {
            m_Changes.Add(new PropertyChangeContext
            {
                Resolvers = resolvers,
                Container = container,
                Property = property
            });
        }

        public void PropagateChanges()
        {
            if (!HasChanges)
            {
                return;
            }

            var changes = ListPool<PropertyChangeContext>.Get();
            try
            {
                changes.AddRange(m_Changes);
                foreach (var change in changes)
                {
                    var resolvers = change.Resolvers;
                    if (resolvers.Count <= 1)
                    {
                        continue;
                    }
                    var visitor = new PropertyChangeVisitor(change);
                    var firstResolver = resolvers[0];
                    firstResolver.Visit(visitor);
                    var paths = visitor.GetPropertyPaths();
                    foreach (var path in paths)
                    {
                        // Get the value from the edited property.
                        var resolution = path.Resolve(firstResolver);
                        if (false == resolution.success)
                        {
                            continue;
                        }

                        var value = resolution.value;
                        var index = resolution.listIndex;

                        // And propagate to the other properties.
                        for (var i = 1; i < resolvers.Count; ++i)
                        {
                            var otherTarget = resolvers[i];
                            var other = path.Resolve(otherTarget);
                            if (false == other.success)
                            {
                                Debug.Log($"{TinyConstants.ApplicationName}: Could not resolve changed property path on target.");
                                continue;
                            }

                            switch (other.property)
                            {
                                case IValueClassProperty valueClass:
                                    valueClass.SetObjectValue(other.container, value);
                                    break;
                                case IValueStructProperty valueStruct:
                                    valueStruct.SetObjectValue(ref other.container, value);
                                    break;
                                case IListClassProperty listClass:
                                    listClass.SetObjectAt(other.container, index, value);
                                    break;
                                case IListStructProperty listStruct:
                                    listStruct.SetObjectAt(ref other.container, index, value);
                                    break;
                            }
                        }
                    }
                }
            }
            finally
            {
                ListPool<PropertyChangeContext>.Release(changes);
            }
        }

        public void ClearChanges()
        {
            m_Changes.Clear();
        }

        public bool HasMixedValues<TValue>(IPropertyContainer container, IProperty property)
        {
            var currentResolvers = m_Resolvers[Resolvers.Count - 1];
            if (currentResolvers.Count <= 1)
            {
                return false;
            }

            // Check the actual mixed value.
            var visitor = new PropertyChangeVisitor(new PropertyChangeContext() { Resolvers = currentResolvers, Container = container, Property = property });

            var firstResolver = currentResolvers[0];

            TValue targetValue;
            if (property is ITypedValueProperty<TValue> typedProp)
            {
                targetValue = typedProp.GetValue(container);
            }
            else
            {
                return false;
            }

            firstResolver.Visit(visitor);
            var paths = visitor.GetPropertyPaths();
            if (paths.Count == 0)
            {
                throw new Exception($"{TinyConstants.ApplicationName}: Trying to get a property path for a property that is not part of the targets");
            }

            for(var i = 1; i < currentResolvers.Count; ++i)
            {
                var t = currentResolvers[i];
                var resolution = paths[0].Resolve(t);
                if (resolution.success == false)
                {
                    continue;
                }

                var value = resolution.value;
                if (ValuesAreDifferent(value, targetValue))
                {
                    return true;
                }
            }

            return false;
        }

        public void ClearResolvers()
        {
            m_Resolvers.Clear();
        }

        public void PushResolvers(List<IPropertyContainer> resolvers)
        {
            m_Resolvers.Add(resolvers);
        }

        public void PopResolvers()
        {
            m_Resolvers.RemoveAt(m_Resolvers.Count - 1);
        }

        public bool ValuesAreDifferent<TValue>(TValue left, TValue right)
        {
            if (null == left && null == right)
            {
                return false;
            }
            if (null == left)
            {
                return true;
            }
            return !left.Equals(right);
        }
    }
}
