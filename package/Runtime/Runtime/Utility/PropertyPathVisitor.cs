

using System.Collections.Generic;
using System.Linq;
using Unity.Properties;

namespace Unity.Tiny
{
    internal struct PropertyPathContext
    {
        public IPropertyContainer Container;
        public IProperty Property;
        public int ListIndex;
    }

    internal class PropertyPathVisitor : PropertyVisitorAdapter
    {
        List<PropertyPathContext> m_Properties;
        List<string> m_paths;
        PropertyPathBuilder m_Builder;

        public List<PropertyPath> GetPropertyPaths()
        {
            return m_paths.Select(path => new PropertyPath(path)).ToList();
        }

        public PropertyPathVisitor(IEnumerable<PropertyPathContext> properties)
        {
            m_Properties = new List<PropertyPathContext>(properties);
            if (null == m_Properties)
            { }
            m_Builder = new PropertyPathBuilder();
            m_paths = new List<string>();
        }

        public PropertyPathVisitor(PropertyPathContext property)
            : this(new[] { property })
        {
        }

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
                if (m_Properties.Any(p => (p.Container == c) && p.Property == context.Property))
                {
                    m_paths.Add(m_Builder.ToString());
                    m_Properties.RemoveAll(p => p.Container == c && p.Property == context.Property);
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
                var i = m_Properties.FindIndex(p => Equals(p.Container, c) && p.Property == context.Property);
                if (i > -1)
                {
                    m_Builder.PushProperty(context.Property);
                    
                    if (m_Properties[i].ListIndex > -1)
                    {
                        m_Builder.PushListItem(m_Properties[i].ListIndex);
                    }
                    
                    m_paths.Add(m_Builder.ToString());
                    m_Builder.Pop();
                    m_Properties.RemoveAll(p => p.Container == c && p.Property == context.Property);
                }
            }
        }
    }
}

