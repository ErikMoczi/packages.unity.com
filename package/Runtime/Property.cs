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

        public virtual TValue GetValue(TContainer container)
        {
            return m_GetValue(container);
        }

        public virtual void SetValue(TContainer container, TValue value)
        {
            if (Equals(container, value))
            {
                return;
            }
            
            m_SetValue(container, value);
            container.VersionStorage?.IncrementVersion(this, ref container);
        }

        private bool Equals(TContainer container, TValue value)
        {
            var v = m_GetValue(container);

            if (null == v && null == value)
            {
                return true;
            }

            return null != v && v.Equals(value);
        }

        public virtual void Accept(TContainer container, IPropertyVisitor visitor)
        {
            // Have a single GetValue call per property visit
            // Get value is a user driven operation. 
            // We don't know performance impact or side effects
            var value = GetValue(container);

            // Delegate the Visit implementaton to the user
            if (TryUserAccept(ref container, visitor, value))
            {
                // User has handled the visit; early exit
                return;
            }

            // Default visit implementation
            visitor.Visit(ref container, new VisitContext<TValue> {Property = this, Value = value, Index = -1});
        }

        /// <summary>
        /// Checks for user implemntations of IPropertyVisitor and IPropertyVisitorValidaton and returns true if the visit was handled
        /// </summary>
        /// <param name="container"></param>
        /// <param name="visitor"></param>
        /// <returns>True if the default Accept method should be skipped</returns>
        protected bool TryUserAccept(ref TContainer container, IPropertyVisitor visitor, TValue value)
        {
            var typedValidation = visitor as IPropertyVisitorValidation<TValue>;

            if (null != typedValidation && !typedValidation.ShouldVisit(ref container, new VisitContext<TValue> {Property = this, Value = value, Index = -1}))
            {
                return true;
            }

            var validation = visitor as IPropertyVisitorValidation;
            
            if (null != validation && !validation.ShouldVisit(ref container, new VisitContext<TValue> {Property = this, Value = value, Index = -1}))
            {
                return true;
            }

            var typedVisitor = visitor as IPropertyVisitor<TValue>;

            if (null == typedVisitor)
            {
                return false;
            }
            
            typedVisitor.Visit(ref container, new VisitContext<TValue> {Property = this, Value = value, Index = -1});
            return true;
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

        public virtual TValue GetValue(ref TContainer container)
        {
            return m_GetValue(ref container);
        }

        public virtual void SetValue(ref TContainer container, TValue value)
        {
            if (Equals(container, value))
            {
                return;
            }
            
            m_SetValue(ref container, value);
            container.VersionStorage?.IncrementVersion(this, ref container);
        }

        private bool Equals(TContainer container, TValue value)
        {
            var v = m_GetValue(ref container);

            if (null == v && null == value)
            {
                return true;
            }

            return null != v && v.Equals(value);
        }

        public virtual void Accept(ref TContainer container, IPropertyVisitor visitor)
        {
            // Have a single GetValue call per property visit
            // Get value is a user driven operation. 
            // We don't know performance impact or side effects
            var value = GetValue(ref container);
            
            // Delegate the Visit implementaton to the user
            if (TryUserAccept(ref container, visitor, value))
            {
                // User has handled the visit; early exit
                return;
            }
            
            // Default visit implementation
            visitor.Visit(ref container, new VisitContext<TValue> {Property = this, Value = value, Index = -1});
        }

        /// <summary>
        /// Checks for user implemntations of IPropertyVisitor and IPropertyVisitorValidaton and returns true if the visit was handled
        /// </summary>
        /// <param name="container"></param>
        /// <param name="visitor"></param>
        /// <returns>True if the default Accept method should be skipped</returns>
        protected bool TryUserAccept(ref TContainer container, IPropertyVisitor visitor, TValue value)
        {
            var typedValidation = visitor as IPropertyVisitorValidation<TValue>;

            if (null != typedValidation && !typedValidation.ShouldVisit(ref container, new VisitContext<TValue> {Property = this, Value = value, Index = -1}))
            {
                return true;
            }

            var validation = visitor as IPropertyVisitorValidation;
            
            if (null != validation && !validation.ShouldVisit(ref container, new VisitContext<TValue> {Property = this, Value = value, Index = -1}))
            {
                return true;
            }

            var typedVisitor = visitor as IPropertyVisitor<TValue>;
            if (null == typedVisitor)
            {
                return false;
            }

            typedVisitor.Visit(ref container, new VisitContext<TValue> {Property = this, Value = value, Index = -1});
            return true;
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

            var typedVisitor = visitor as IPropertyVisitor<TValue>;

            if (null != typedVisitor)
            {
                typedVisitor.Visit(ref container, new VisitContext<TValue> { Property = this, Value = value, Index = -1 });
            }
            else
            {
                var subtreeContext = new SubtreeContext<TValue> {Property = this, Index = -1};
                if (visitor.BeginContainer(ref container, subtreeContext))
                {
                    value.PropertyBag.Visit(value, visitor);
                }
                visitor.EndContainer(ref container, subtreeContext);
            }
        }
    }
    
    public class MutableContainerProperty<TContainer, TValue> : Property<TContainer, TValue>
        where TContainer : class, IPropertyContainer
        where TValue : struct, IPropertyContainer
    {
        private RefAccessMethod m_RefAccessMethod;
        
        public delegate void RefVisitMethod(ref TValue value, IPropertyVisitor visitor);
        public delegate void RefAccessMethod(TContainer container, RefVisitMethod a, IPropertyVisitor visitor);

        public MutableContainerProperty(string name, GetValueMethod getValue, SetValueMethod setValue, RefAccessMethod refAccess) : base(name, getValue, setValue)
        {
            m_RefAccessMethod = refAccess;
        }
        
        private static void RefVisit(ref TValue value, IPropertyVisitor visitor)
        {
            value.PropertyBag.VisitStruct(ref value, visitor);
        }
        
        public override void Accept(TContainer container, IPropertyVisitor visitor)
        {
            var typedVisitor = visitor as IPropertyVisitor<TValue>;

            if (null != typedVisitor)
            {
                typedVisitor.Visit(ref container, new VisitContext<TValue> { Property = this, Value = GetValue(container), Index = -1 });
            }
            else
            {
                var subtreeContext = new SubtreeContext<TValue> { Property = this, Index = -1 };
                if (visitor.BeginContainer(ref container, subtreeContext))
                {
                    // the compiler generates a static RefVisitMethod delegate from on RefVisit
                    // no allocation here
                    m_RefAccessMethod(container, RefVisit, visitor);
                }
                visitor.EndContainer(ref container, subtreeContext);
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

            var typedVisitor = visitor as IPropertyVisitor<TValue>;

            if (null != typedVisitor)
            {
                typedVisitor.Visit(ref container, new VisitContext<TValue> {Property = this, Value = value, Index = -1});
            }
            else
            {
                var subtreeContext = new SubtreeContext<TValue> {Property = this, Index = -1};
                if (visitor.BeginContainer(ref container, subtreeContext))
                {
                    value.PropertyBag.Visit(value, visitor);
                }
                visitor.EndContainer(ref container, subtreeContext);
            }
        }
    }

    public class StructMutableContainerProperty<TContainer, TValue> : StructProperty<TContainer, TValue>
        where TContainer : struct, IPropertyContainer
        where TValue : struct, IPropertyContainer
    {
        private RefAccessMethod m_RefAccessMethod;
        
        public delegate void RefVisitMethod(ref TValue value, IPropertyVisitor visitor);
        public delegate void RefAccessMethod(ref TContainer container, RefVisitMethod a, IPropertyVisitor visitor);

        public StructMutableContainerProperty(string name, GetValueMethod getValue, SetValueMethod setValue, RefAccessMethod refAccess) : base(name, getValue, setValue)
        {
            m_RefAccessMethod = refAccess;
        }
        
        private static void RefVisit(ref TValue value, IPropertyVisitor visitor)
        {
            value.PropertyBag.VisitStruct(ref value, visitor);
        }
        
        public override void Accept(ref TContainer container, IPropertyVisitor visitor)
        {
            var typedVisitor = visitor as IPropertyVisitor<TValue>;

            if (null != typedVisitor)
            {
                typedVisitor.Visit(ref container, new VisitContext<TValue> { Property = this, Value = GetValue(ref container), Index = -1 });
            }
            else
            {
                var subtreeContext = new SubtreeContext<TValue> { Property = this, Index = -1 };
                if (visitor.BeginContainer(ref container, subtreeContext))
                {
                    // the compiler generates a static RefVisitMethod delegate from on RefVisit
                    // no allocation here
                    m_RefAccessMethod(ref container, RefVisit, visitor);
                }
                visitor.EndContainer(ref container, subtreeContext);
            }
        }
    }
}