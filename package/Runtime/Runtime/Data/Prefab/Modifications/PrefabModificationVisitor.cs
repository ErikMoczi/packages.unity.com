using System;
using System.Collections.Generic;
using System.Text;
using Unity.Properties;
using Unity.Tiny.Attributes;
using UnityEngine;

namespace Unity.Tiny
{
    internal class PrefabModificationVisitor : PropertyVisitor,
        ICustomVisit<TinyEnum.Reference>,
        ICustomVisit<TinyEntity.Reference>
    {
        private readonly Stack<IPropertyContainer> m_Containers = new Stack<IPropertyContainer>();
        private readonly PropertyPath m_Path = new PropertyPath();
        private TinyEntityInstance m_Instance;
        private TinyType.Reference m_Type;
        
        public PrefabModificationVisitor(TinyEntityInstance instance, TinyType.Reference type)
        {
            m_Instance = instance;
            m_Type = type;
        }

        public void PushContainer(IPropertyContainer container)
        {
            m_Containers.Push(container);
        }

        public void Clear()
        {
            m_Containers.Clear();
        }
        
        public override bool BeginContainer<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
        {
            PushContainer(ref container, context);
            return base.BeginContainer(container, context);
        }

        public override void EndContainer<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
        {
            PopContainer(ref container, context);
            base.EndContainer(container, context);
        }

        public override bool BeginContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            PushContainer(ref container, context);
            return base.BeginContainer(ref container, context);
        }

        public override void EndContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            PopContainer(ref container, context);
            base.EndContainer(ref container, context);
        }

        private void PushContainer<TContainer, TValue>(ref TContainer source, VisitContext<TValue> context)
            where TContainer : IPropertyContainer
        {
            m_Path.Push(context.Property.Name, context.Index);
            
            var target = m_Containers.Peek();
            var property = target?.PropertyBag.FindProperty(context.Property.Name);

            if (null == property)
            {
                m_Containers.Push(null);
                return;
            }
            
            IPropertyContainer value = null;
            
            // Non list elements
            if (context.Index < 0)
            {
                switch (property)
                {
                    case IValueClassProperty valueClassProperty:
                        value = valueClassProperty.GetObjectValue(target) as IPropertyContainer;
                        break;
                    case IValueStructProperty valueStructProperty:
                        value = valueStructProperty.GetObjectValue(target) as IPropertyContainer;
                        break;
                }
            }
            // List elements
            else
            {
                switch (property)
                {
                    case IListClassProperty listClassProperty:
                        if (listClassProperty.Count(target) > context.Index)
                        {
                            value = listClassProperty.GetObjectAt(target, context.Index) as IPropertyContainer;
                        }
                        break;
                    case IListStructProperty listStructProperty:
                        if (listStructProperty.Count(target) > context.Index)
                        {
                            value = listStructProperty.GetObjectAt(ref target, context.Index) as IPropertyContainer;
                        }
                        break;
                }
            }

            m_Containers.Push(value);
        }

        private void PopContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : IPropertyContainer
        {
            m_Path.Pop();
            m_Containers.Pop();
        }

        protected override void Visit<TValue>(TValue value)
        {
            RecordModification(value);
        }

        public void CustomVisit(TinyEnum.Reference value)
        {
            RecordModification(value);
        }
        
        public void CustomVisit(TinyEntity.Reference value)
        {
            RecordModification(value);
        }

        private void RecordModification<TValue>(TValue sourceValue)
        {
            var targetContainer = m_Containers.Peek();
            var targetProperty = targetContainer?.PropertyBag.FindProperty(Property.Name);

            if (targetProperty == null)
            {
                return;
            }

            object targetValue = null;
            
            switch (targetProperty)
            {
                case IValueProperty valueProperty:
                    targetValue = valueProperty.GetObjectValue(targetContainer);
                    break;
                case IListClassProperty listClassProperty:
                    targetValue = listClassProperty.Count(targetContainer) > ListIndex ? listClassProperty.GetObjectAt(targetContainer, ListIndex) : null;
                    break;
                case IListStructProperty listStructProperty:
                    targetValue = listStructProperty.Count(targetContainer) > ListIndex ? listStructProperty.GetObjectAt(ref targetContainer, ListIndex) : null;
                    break;
            }

            var name = Property.Name;

            if (Property.HasAttribute<CustomFieldAttribute>())
            {
                name = Property.GetAttribute<CustomFieldAttribute>().Prefix + name;
            }
            
            m_Path.Push(name, ListIndex);
            
            if (!ValuesAreEqual(targetValue, sourceValue))
            {
                TypeConversion.TryConvert<TValue>(targetValue, out var value);
                m_Instance.SetModification(m_Type, PrefabManager.CompressPropertyPath(m_Path), value);
            }
            
            m_Path.Pop();
        }
        
        private static bool ValuesAreEqual(object a, object b)
        {
            return a?.Equals(b) ?? b == null;
        }
    }
}