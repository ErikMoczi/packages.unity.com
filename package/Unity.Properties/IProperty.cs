#if (NET_4_6 || NET_STANDARD_2_0)

using System;

namespace Unity.Properties
{
    /// <summary>
    /// Common nongeneric interface required for all property types
    /// </summary>
    public interface IProperty 
    {
        /// <summary>
        /// The name of this property
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Container type that hosts the data for this property
        /// </summary>
        Type ContainerType { get; }
    }
    
    /*
     * CLASS PROPERTIES
     */

    public interface IClassProperty : IProperty
    {
        void Accept(IPropertyContainer container, IPropertyVisitor visitor);
    }
    
    public interface IClassProperty<in TContainer> : IClassProperty
        where TContainer : class, IPropertyContainer
    {
        void Accept(TContainer container, IPropertyVisitor visitor);
    }
    
    /*
     * STRUCT PROPERTIES
     */
    
    public interface IStructProperty : IProperty
    {
        void Accept(ref IPropertyContainer container, IPropertyVisitor visitor);
    }

    public interface IStructProperty<TContainer> : IStructProperty
        where TContainer : struct, IPropertyContainer
    {
        void Accept(ref TContainer container, IPropertyVisitor visitor);
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)