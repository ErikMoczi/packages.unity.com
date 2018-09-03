using System;
using System.Collections.Generic;

namespace Unity.Properties
{
    /// <summary>
    /// Specialized property visit.
    /// See <see cref="PropertyVisitor"/> for usage.
    /// </summary>
    /// <typeparam name="TValue">Property value type to visit.</typeparam>
    public interface ICustomVisit<TValue>
    {
        /// <summary>
        /// Performs the visitor logic on this property value type.
        /// </summary>
        /// <param name="value">Reference to the current property value.</param>
        void CustomVisit(TValue value);
    }
    
    /// <summary>
    /// ICustomVisit declarations for all .Net primitive types.
    /// </summary>
    public interface ICustomVisitPrimitives :
        ICustomVisit<bool>
        , ICustomVisit<byte>
        , ICustomVisit<sbyte>
        , ICustomVisit<ushort>
        , ICustomVisit<short>
        , ICustomVisit<uint>
        , ICustomVisit<int>
        , ICustomVisit<ulong>
        , ICustomVisit<long>
        , ICustomVisit<float>
        , ICustomVisit<double>
        , ICustomVisit<char>
        , ICustomVisit<string>
    {}

    /// <summary>
    /// Specialized property visit exclusion.
    /// See <see cref="PropertyVisitor"/> for usage.
    /// </summary>
    /// <typeparam name="TValue">Property value type to exclude from the visit (contravariant).</typeparam>
    public interface IExcludeVisit<in TValue>
    {
        /// <summary>
        /// Validates whether or not a property of type TValue should be visited.
        /// </summary>
        /// <param name="value">The current property value.</param>
        /// <returns>True if the property should be visited, False otherwise.</returns>
        bool ExcludeVisit(TValue value);
    }

    /// <summary>
    /// Adapter for the <see cref="IPropertyVisitor"/> interface.
    /// Only primitive Visit methods are required to override. VisitEnum default implementation forward the call to
    /// their Visit counterparts.
    /// </summary>
    public abstract class PropertyVisitorAdapter : IPropertyVisitor
    {
        public virtual bool ExcludeVisit<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
            where TContainer : class, IPropertyContainer
        {
            return false;
        }

        public virtual bool ExcludeVisit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : struct, IPropertyContainer
        {
            return false;
        }

        public abstract void Visit<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
            where TContainer : class, IPropertyContainer;

        public abstract void Visit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : struct, IPropertyContainer;

        public virtual void VisitEnum<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
            where TContainer : class, IPropertyContainer where TValue : struct
        {
            Visit(container, context);
        }

        public virtual void VisitEnum<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : struct, IPropertyContainer where TValue : struct
        {
            Visit(ref container, context);
        }

        public virtual bool BeginContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : IPropertyContainer where TValue : IPropertyContainer
        {
            return true;
        }

        public virtual void EndContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : IPropertyContainer where TValue : IPropertyContainer
        {
        }

        public virtual bool BeginList<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : IPropertyContainer
        {
            return true;
        }

        public virtual void EndList<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : IPropertyContainer
        {
        }
    }
    
    /// <inheritdoc />
    /// <summary>
    /// Default implementation of the <see cref="T:Unity.Properties.IPropertyVisitor" /> interface.
    /// 
    /// Use this adapter to simplify read-only use cases. For read-write scenarios (e.g. IMGUI modifying the
    /// property values inline), consider using <see cref="T:Unity.Properties.PropertyVisitorAdapter" /> directly.
    /// You can define the visitor behavior by adding <see cref="T:Unity.Properties.ICustomVisit" /> and <see cref="T:Unity.Properties.IExcludeVisit" /> mixins.
    /// </summary>
    public abstract class PropertyVisitor : PropertyVisitorAdapter
    {
        /// <summary>
        /// The property being visited.
        /// </summary>
        protected IProperty Property { get; set; }
        
        protected int ListIndex { get; set; }

        /// <summary>
        /// Whether or not the current property is part of a list.
        /// </summary>
        protected bool IsListItem => ListIndex >= 0;

        protected void VisitSetup<TValue>(ref VisitContext<TValue> context)
        {
            Property = context.Property;
            ListIndex = context.Index;
        }

        protected virtual bool CustomVisit<TValue>(TValue value)
        {
            var validationHandler = this as IExcludeVisit<TValue>;
            if ((validationHandler != null && validationHandler.ExcludeVisit(value)) ||
                ExcludeVisit(value))
            {
                return true;
            }

            var handler = this as ICustomVisit<TValue>;
            if (handler != null)
            {
                handler.CustomVisit(value);
                return true;
            }

            return false;
        }
        
        public override bool ExcludeVisit<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
        {
            VisitSetup(ref context);
            return CustomVisit(context.Value);
        }
        
        public override bool ExcludeVisit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            VisitSetup(ref context);
            return CustomVisit(context.Value);
        }

        protected virtual bool ExcludeVisit<TValue>(TValue value)
        {
            return false;
        }

        /// <summary>
        /// Override this method to visit generic property types (including Enum values).
        /// </summary>
        /// <param name="value">The current property value.</param>
        /// <typeparam name="TValue">The current property value type.</typeparam>
        protected abstract void Visit<TValue>(TValue value);
        
        public override void Visit<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
        {
            VisitSetup(ref context);
            Visit(context.Value);
        }

        public override void Visit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            VisitSetup(ref context);
            Visit(context.Value);
        }
        
        public override void VisitEnum<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
        {
            VisitSetup(ref context);
            Visit(context.Value);
        }
        
        public override void VisitEnum<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            VisitSetup(ref context);
            Visit(context.Value);
        }
        
        public override bool BeginContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            VisitSetup(ref context);
            return true;
        }
        
        public override void EndContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            VisitSetup(ref context);
        }
        
        public override bool BeginList<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            VisitSetup(ref context);
            return true;
        }

        public override void EndList<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            VisitSetup(ref context);
        }
    }
}