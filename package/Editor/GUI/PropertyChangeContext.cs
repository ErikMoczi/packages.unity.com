
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;

namespace Unity.Tiny
{
    internal struct PropertyChangeContext : IEquatable<PropertyChangeContext>
    {
        public List<IPropertyContainer> Resolvers;
        public IPropertyContainer Container;
        public IProperty Property;

        public bool Equals(PropertyChangeContext other)
        {
            return Equals(Resolvers, other.Resolvers) && Equals(Container, other.Container) && Equals(Property, other.Property);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is PropertyChangeContext other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Resolvers != null ? Resolvers.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Container != null ? Container.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Property != null ? Property.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    internal class PropertyChangeVisitor : PropertyVisitorAdapter
    {
        List<PropertyChangeContext> m_properties;
        List<string> m_paths;
        PropertyPathBuilder m_Builder;

        public List<PropertyPath> GetPropertyPaths()
        {
            return m_paths.Select(path => new PropertyPath(path)).ToList();
        }

        public PropertyChangeVisitor(IEnumerable<PropertyChangeContext> properties)
        {
            m_properties = new List<PropertyChangeContext>(properties);
            if (null == m_properties) { }

            m_Builder = new PropertyPathBuilder();
            m_paths = new List<string>();
        }

        public PropertyChangeVisitor(PropertyChangeContext property)
            : this(new[] { property }) { }

        public override bool BeginContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            return BeginContainerInternal(ref container, context);
        }

        public override bool BeginContainer<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
        {
            return BeginContainerInternal(ref container, context);
        }

        private bool BeginContainerInternal<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : IPropertyContainer
        {
            if (null != context.Property)
            {
                m_Builder.PushProperty(context.Property);

                var c = container as IPropertyContainer;
                if (m_properties.Any(p => (p.Container == c) && p.Property == context.Property))
                {
                    m_paths.Add(m_Builder.ToString());
                    m_properties.RemoveAll(p => p.Container == c && p.Property == context.Property);
                }

                if (context.Index != -1)
                {
                    m_Builder.PushListItem(context.Index);
                }
            }

            return true;
        }

        public override void EndContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            EndContainerInternal(ref container, context);
        }

        public override void EndContainer<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
        {
            EndContainerInternal(ref container, context);
        }

        private void EndContainerInternal<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : IPropertyContainer
        {
            if (null != context.Property)
            {
                m_Builder.Pop();
                if (context.Index != -1)
                {
                    m_Builder.Pop();
                }
            }
        }

        public override void Visit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            VisitImpl(ref container, context);
        }

        public override void Visit<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
        {
            VisitImpl(ref container, context);
        }

        private void VisitImpl<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : IPropertyContainer
        {
            if (null != context.Property)
            {
                var c = container as IPropertyContainer;
                if (m_properties.Any(p => (p.Container == c) && p.Property == context.Property))
                {
                    m_Builder.PushProperty(context.Property);
                    m_paths.Add(m_Builder.ToString());
                    m_Builder.Pop();
                    m_properties.RemoveAll(p => p.Container == c && p.Property == context.Property);
                }
            }
        }
    }
}
