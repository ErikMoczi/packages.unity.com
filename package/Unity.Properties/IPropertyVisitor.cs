#if (NET_4_6 || NET_STANDARD_2_0)

using System.Collections.Generic;

namespace Unity.Properties
{
    /// <summary>
    /// Generic visitable class
    /// </summary>
    public interface IVisitableClass
    {
        void Visit<TContainer>(TContainer container, IPropertyVisitor visitor)
            where TContainer : class, IPropertyContainer;
    }

    /// <summary>
    /// Generic visitable struct class
    /// </summary>
    public interface IVisitableStruct
    {
        void Visit<TContainer>(ref TContainer container, IPropertyVisitor visitor)
            where TContainer : struct, IPropertyContainer;
    }
    
    /// <summary>
    /// Typed visitable class
    /// </summary>
    /// <typeparam name="TContainer"></typeparam>
    public interface IVisitableClass<in TContainer>
        where TContainer : IPropertyContainer
    {
        void Visit(TContainer container, IPropertyVisitor visitor);
    }
    
    /// <summary>
    /// Typed visitable struct
    /// </summary>
    /// <typeparam name="TContainer"></typeparam>
    public interface IVisitableStruct<TContainer>
        where TContainer : struct, IPropertyContainer
    {
        void Visit(ref TContainer container, IPropertyVisitor visitor);
    }
    
    public struct VisitContext<TValue>
    {
        public IProperty Property;
        public TValue Value;
        public int Index;
    }

    public interface IPropertyVisitor
    {
        bool ExcludeVisit<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
            where TContainer : class, IPropertyContainer;
        
        bool ExcludeVisit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : struct, IPropertyContainer;
        
        bool CustomVisit<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
            where TContainer : class, IPropertyContainer;
        
        bool CustomVisit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : struct, IPropertyContainer;
        
        void Visit<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
            where TContainer : class, IPropertyContainer;
        
        void Visit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : struct, IPropertyContainer;
        
        bool BeginContainer<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
            where TContainer : class, IPropertyContainer
            where TValue : IPropertyContainer;
        
        bool BeginContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : struct, IPropertyContainer
            where TValue : IPropertyContainer;

        void EndContainer<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
            where TContainer : class, IPropertyContainer
            where TValue : IPropertyContainer;
        
        void EndContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : struct, IPropertyContainer
            where TValue : IPropertyContainer;

        bool BeginCollection<TContainer, TValue>(TContainer container, VisitContext<IList<TValue>> context)
            where TContainer : class, IPropertyContainer;
        
        bool BeginCollection<TContainer, TValue>(ref TContainer container, VisitContext<IList<TValue>> context)
            where TContainer : struct, IPropertyContainer;

        void EndCollection<TContainer, TValue>(TContainer container, VisitContext<IList<TValue>> context)
            where TContainer : class, IPropertyContainer;
        
        void EndCollection<TContainer, TValue>(ref TContainer container, VisitContext<IList<TValue>> context)
            where TContainer : struct, IPropertyContainer;
    }
    
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
    
    /// <inheritdoc cref="ICustomVisit"/>
    /// <summary>
    /// ICustomVisit declarations for all .NET primitive types.
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

    public static class PropertyVisitorExtensions
    {
        public static bool ExcludeOrCustomVisit<TContainer, TValue>(this IPropertyVisitor visitor, TContainer container, VisitContext<TValue> context)
            where TContainer : class, IPropertyContainer
        {
            return visitor.ExcludeVisit(container, context) || visitor.CustomVisit(container, context);
        }
        
        public static bool ExcludeOrCustomVisit<TContainer, TValue>(this IPropertyVisitor visitor, ref TContainer container, VisitContext<TValue> context)
            where TContainer : struct, IPropertyContainer
        {
            return visitor.ExcludeVisit(ref container, context) || visitor.CustomVisit(ref container, context);
        }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)