
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;

namespace Unity.Tiny
{
    internal struct UIVisitContext<TValue>
    {
        private VisitContext<TValue> m_Context;

        public IProperty Property => m_Context.Property;
        public GUIVisitor Visitor { get; }
        public List<IPropertyContainer> Targets { get; }

        public TValue Value
        {
            get => m_Context.Value;
            set => m_Context.Value = value;
        }

        public bool IsListProperty => Property is IListProperty && Index < 0;
        public bool IsListItem => Property is IListProperty && Index >= 0;
        public int Index => m_Context.Index;
        public string Label
        {
            get
            {
                var self = this;
                return Visitor.NameResolver.Resolve(ref self);
            }
        }

        public UIVisitContext(VisitContext<TValue> context, GUIVisitor visitor, List<IPropertyContainer> targets)
        {
            m_Context = context;
            Visitor = visitor;
            Targets = targets;
        }

        public T MainTarget<T>()
            where T : IPropertyContainer
            => Targets.OfType<T>().FirstOrDefault();
    }
}
