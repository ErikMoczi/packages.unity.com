using System;

namespace Unity.Properties
{
    /// <summary>
    /// Common nongeneric interface required for all property types.
    /// See <see cref="Property{TContainer,TValue}"/> for an implementation example.
    /// </summary>
    public interface IProperty
    {
        /// <summary>
        /// Property name.
        /// Names must be unique within an <see cref="IPropertyBag"/>.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// <see cref="System.Type"/> of this property value.
        /// </summary>
        Type ValueType { get; }
        
        Type ContainerType { get; }
        
        /// <summary>
        /// When true, calling <c>SetObjectValue</c> will throw. 
        /// </summary>
        bool IsReadOnly { get; }

        object GetObjectValue(IPropertyContainer container);

        void SetObjectValue(IPropertyContainer container, object value);
        
        void Accept(IPropertyContainer container, IPropertyVisitor visitor);
    }

    public interface ITypedValueProperty<TValue> : IProperty
    {
        TValue GetValue(IPropertyContainer container);
        void SetValue(IPropertyContainer container, TValue value);
    }

    public interface ITypedContainerProperty<in TContainer> : IProperty
        where TContainer : class, IPropertyContainer
    {
        void Accept(TContainer container, IPropertyVisitor visitor);
    }
    
    public interface IStructTypedContainerProperty<TContainer> : IProperty
        where TContainer : struct, IPropertyContainer
    {
        void Accept(ref TContainer container, IPropertyVisitor visitor);
    }
    
    public interface IProperty<in TContainer, TValue> : ITypedContainerProperty<TContainer>, ITypedValueProperty<TValue>
        where TContainer : class, IPropertyContainer
    {
        TValue GetValue(TContainer container);
        void SetValue(TContainer container, TValue value);
    }

    public interface IStructProperty<TContainer, TValue> : IStructTypedContainerProperty<TContainer>, ITypedValueProperty<TValue>
        where TContainer : struct, IPropertyContainer
    {
        TValue GetValue(ref TContainer container);
        void SetValue(ref TContainer container, TValue value);
    }
}