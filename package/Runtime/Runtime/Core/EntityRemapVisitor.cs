using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    internal class EntityRemapVisitor : PropertyVisitor,
        ICustomVisit<TinyEntity.Reference>
    {
        private readonly Stack<IPropertyContainer> m_Containers = new Stack<IPropertyContainer>();
        private readonly IDictionary<TinyEntity.Reference, TinyEntity.Reference> m_Remap;

        public EntityRemapVisitor(IDictionary<TinyEntity.Reference, TinyEntity.Reference> remap)
        {
            m_Remap = remap;
        }

        public void PushContainer(IPropertyContainer container)
        {
            m_Containers.Push(container);
        }

        public void PopContainer()
        {
            m_Containers.Pop();
        }

        protected override void Visit<TValue>(TValue value)
        {
        }

        public override bool BeginContainer<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
        {
            PushContainer(context.Value);
            return base.BeginContainer(container, context);
        }

        public override void EndContainer<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
        {
            PopContainer();
            base.EndContainer(container, context);
        }

        public override bool BeginContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            PushContainer(context.Value);
            return base.BeginContainer(ref container, context);
        }

        public override void EndContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            PopContainer();
            base.EndContainer(ref container, context);
        }

        public void CustomVisit(TinyEntity.Reference entity)
        {
            if (!m_Remap.ContainsKey(entity))
            {
                return;
            }

            var container = m_Containers.Peek();

            var value = m_Remap[entity];

            if (IsListItem)
            {
                (Property as IListClassProperty)?.SetObjectAt(container, ListIndex, value);
                (Property as IListStructProperty)?.SetObjectAt(ref container, ListIndex, value);
            }
            else
            {
                (Property as IValueClassProperty)?.SetObjectValue(container, value);
                (Property as IValueStructProperty)?.SetObjectValue(ref container, value);
            }

            if (Property is IStructProperty)
            {
                m_Containers.Pop();
                m_Containers.Push(container);
            }
        }
    }
}