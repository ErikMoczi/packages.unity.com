using System;
using UnityEngine;

namespace Unity.Properties
{
    public class Property<TContainer, TValue> : IProperty<TContainer, TValue>
        where TContainer : class, IPropertyContainer
    {
        public delegate void SetValueMethod(TContainer container, TValue value);
        public delegate TValue GetValueMethod(TContainer container);
        
        private readonly GetValueMethod m_GetValue;
        private readonly SetValueMethod m_SetValue;

        public string Name { get; }
        public Type ValueType => typeof(TValue);
        public Type ContainerType => typeof(TContainer);
        public virtual bool IsReadOnly => m_SetValue == null;
        
        private Property(string name)
        {
            Name = name;
        }
        
        public Property(string name, GetValueMethod getValue, SetValueMethod setValue) 
            : this(name)
        {
            m_GetValue = getValue;
            m_SetValue = setValue;
        }
        
        public virtual object GetObjectValue(IPropertyContainer container)
        {
            return GetValue((TContainer) container);
        }

        public virtual void SetObjectValue(IPropertyContainer container, object value)
        {
            // TODO: value type coercion
            SetValue((TContainer)container, (TValue)value);
        }

        public virtual TValue GetValue(TContainer container)
        {
            if (m_GetValue == null)
            {
                return default(TValue);
            }
            return m_GetValue(container);
        }

        public virtual void SetValue(TContainer container, TValue value)
        {
            if (Equals(container, value))
            {
                return;
            }
            
            m_SetValue(container, value);
            container.VersionStorage?.IncrementVersion(this, container);
        }

        private bool Equals(TContainer container, TValue value)
        {
            if (m_GetValue == null)
            {
                return false;
            }
            var v = m_GetValue(container);

            if (null == v && null == value)
            {
                return true;
            }

            return null != v && v.Equals(value);
        }

        public virtual void Accept(TContainer container, IPropertyVisitor visitor)
        {
            var context = new VisitContext<TValue> { Property = this, Value = GetValue(container), Index = -1 };
            if (false == visitor.ExcludeVisit(container, context))
            {
                visitor.Visit(container, context);
            }
        }
    }
    
    public class StructProperty<TContainer, TValue> : IStructProperty<TContainer, TValue>
        where TContainer : struct, IPropertyContainer
    {
        public delegate void SetValueMethod(ref TContainer container, TValue value);
        public delegate TValue GetValueMethod(ref TContainer container);

        private readonly GetValueMethod m_GetValue;
        private readonly SetValueMethod m_SetValue;

        public string Name { get; }
        public Type ValueType => typeof(TValue);
        public Type ContainerType => typeof(TContainer);
        public virtual bool IsReadOnly => m_SetValue == null;

        private StructProperty(string name)
        {
            Name = name;
        }
        
        public StructProperty(string name, GetValueMethod getValue, SetValueMethod setValue) 
            : this(name)
        {
            m_GetValue = getValue;
            m_SetValue = setValue;
        }
        
        public virtual object GetObjectValue(IPropertyContainer container)
        {
            var temp = (TContainer)container;
            return GetValue(ref temp);
        }

        public void SetObjectValue(IPropertyContainer container, object value)
        {
            // here we have to copy the given container to a temp variable, and then assign the property value...
            // which makes no sense for a struct container
            
            // not throwing here would open the door to hard-to-find bugs
            throw new NotSupportedException("SetObjectValue cannot be called on a StructProperty. Use SetValue instead.");
        }

        public virtual TValue GetValue(ref TContainer container)
        {
            if (m_GetValue == null)
            {
                return default(TValue);
            }
            return m_GetValue(ref container);
        }

        public virtual void SetValue(ref TContainer container, TValue value)
        {
            if (Equals(container, value))
            {
                return;
            }
            
            m_SetValue(ref container, value);
            container.VersionStorage?.IncrementVersion(this, container);
        }

        private bool Equals(TContainer container, TValue value)
        {
            if (m_GetValue == null)
            {
                return false;
            }
            var v = m_GetValue(ref container);

            if (null == v && null == value)
            {
                return true;
            }

            return null != v && v.Equals(value);
        }

        public virtual void Accept(ref TContainer container, IPropertyVisitor visitor)
        {
            var context = new VisitContext<TValue> { Property = this, Value = GetValue(ref container), Index = -1 };
            if (false == visitor.ExcludeVisit(ref container, context))
            {
                visitor.Visit(ref container, context);
            }
        }
    }

    public class ContainerProperty<TContainer, TValue> : Property<TContainer, TValue>
        where TContainer : class, IPropertyContainer
        where TValue : class, IPropertyContainer
    {
        public ContainerProperty(string name, GetValueMethod getValue, SetValueMethod setValue) : base(name, getValue, setValue)
        {
        }
        
        public override void Accept(TContainer container, IPropertyVisitor visitor)
        {
            var value = GetValue(container);
            var context = new VisitContext<TValue> { Property = this, Value = value, Index = -1 };
            if (false == visitor.ExcludeVisit(container, context))
            {
                if (visitor.BeginContainer(ref container, context))
                {
                    value.PropertyBag.Visit(value, visitor);
                }
                visitor.EndContainer(ref container, context);
            }
        }
    }
    
    public class MutableContainerProperty<TContainer, TValue> : Property<TContainer, TValue>
        where TContainer : class, IPropertyContainer
        where TValue : struct, IPropertyContainer
    {
        protected RefAccessMethod RefAccess { get; set; }
        
        public delegate void RefVisitMethod(ref TValue value, IPropertyVisitor visitor);
        public delegate void RefAccessMethod(TContainer container, RefVisitMethod a, IPropertyVisitor visitor);

        public MutableContainerProperty(string name, GetValueMethod getValue, SetValueMethod setValue, RefAccessMethod refAccess) : base(name, getValue, setValue)
        {
            RefAccess = refAccess;
        }
        
        private static void RefVisit(ref TValue value, IPropertyVisitor visitor)
        {
            value.PropertyBag.Visit(ref value, visitor);
        }
        
        public override void Accept(TContainer container, IPropertyVisitor visitor)
        {
            var value = GetValue(container);
            var context = new VisitContext<TValue> { Property = this, Value = value, Index = -1 };
            if (false == visitor.ExcludeVisit(container, context))
            {
                if (visitor.BeginContainer(ref container, context))
                {
                    RefAccess(container, RefVisit, visitor);
                }
                visitor.EndContainer(ref container, context);
            }
        }
    }
    
    public class StructContainerProperty<TContainer, TValue> : StructProperty<TContainer, TValue>
        where TContainer : struct, IPropertyContainer
        where TValue : class, IPropertyContainer
    {
        public StructContainerProperty(string name, GetValueMethod getValue, SetValueMethod setValue) : base(name, getValue, setValue)
        {
        }

        public override void Accept(ref TContainer container, IPropertyVisitor visitor)
        {
            var value = GetValue(ref container);
            var context = new VisitContext<TValue> { Property = this, Value = value, Index = -1 };
            if (false == visitor.ExcludeVisit(ref container, context))
            {
                if (visitor.BeginContainer(ref container, context))
                {
                    value.PropertyBag.Visit(ref container, visitor);
                }
                visitor.EndContainer(ref container, context);
            }
        }
    }

    public class StructMutableContainerProperty<TContainer, TValue> : StructProperty<TContainer, TValue>
        where TContainer : struct, IPropertyContainer
        where TValue : struct, IPropertyContainer
    {
        protected RefAccessMethod RefAccess { get; set; }
        
        public delegate void RefVisitMethod(ref TValue value, IPropertyVisitor visitor);
        public delegate void RefAccessMethod(ref TContainer container, RefVisitMethod a, IPropertyVisitor visitor);

        public StructMutableContainerProperty(string name, GetValueMethod getValue, SetValueMethod setValue, RefAccessMethod refAccess) : base(name, getValue, setValue)
        {
            RefAccess = refAccess;
        }
        
        private static void RefVisit(ref TValue value, IPropertyVisitor visitor)
        {
            value.PropertyBag.Visit(ref value, visitor);
        }
        
        public override void Accept(ref TContainer container, IPropertyVisitor visitor)
        {
            var value = GetValue(ref container);
            var context = new VisitContext<TValue> { Property = this, Value = value, Index = -1 };
            if (false == visitor.ExcludeVisit(ref container, context))
            {
                if (visitor.BeginContainer(ref container, context))
                {
                    RefAccess(ref container, RefVisit, visitor);
                }
                visitor.EndContainer(ref container, context);
            }
        }
    }
}