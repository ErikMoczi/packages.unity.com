#if (NET_4_6 || NET_STANDARD_2_0)
    
using System.Collections.Generic;

namespace Unity.Properties.Editor.Experimental
{
    public enum AccessModifier
    {
        None,
        Public,
        Private,
        Internal
    }

    public enum PropertyType
    {
        /// <summary>
        /// Value property (primitive or custom handled types)
        /// </summary>
        Value,
        
        /// <summary>
        /// A nested class `IPropertyContainer`
        /// </summary>
        ClassValue,
        
        /// <summary>
        /// A nested struct `IPropertyContainer`
        /// </summary>
        StructValue,
        
        /// <summary>
        /// A list of values (primitive or custom handled types)
        /// </summary>
        ValueList,
        
        /// <summary>
        /// A list of class `IPropertyContainer`
        /// </summary>
        ClassList,
        
        /// <summary>
        /// A list of struct `IPropertyContainer`
        /// </summary>
        StructList
    }
    
    /// <summary>
    /// Schema is a set of `IPropertyContainer` type definitions represented in the same file.
    /// </summary>
    public class Schema : IPropertyContainer
    {
        public static class Property
        {
            public static readonly Property<Schema, int> Version = new Property<Schema, int>(
                "Version",
                c => c.m_Version,
                (c, v) => c.m_Version = v
            );

            public static readonly ListProperty<Schema, IList<string>, string> UsingAssemblies = new ListProperty<Schema, IList<string>, string>(
                "UsingAssemblies",
                c => c.m_UsingAssemblies,
                null
            );

            public static readonly ContainerListProperty<Schema, IList<TypeNode>, TypeNode> Types = new ContainerListProperty<Schema, IList<TypeNode>, TypeNode>(
                "Types",
                c => c.m_Types,
                null,
                c => new TypeNode()
            );

            public static readonly PropertyBag PropertyBag = new PropertyBag(
                Version,
                UsingAssemblies,
                Types
            );
        }

        public IPropertyBag PropertyBag => Property.PropertyBag;
        public IVersionStorage VersionStorage => null;

        private int m_Version;
        private readonly List<string> m_UsingAssemblies = new List<string>();
        private readonly List<TypeNode> m_Types = new List<TypeNode>();
        
        /// <summary>
        /// API version for this schema
        /// </summary>
        public int Version
        {
            get { return Property.Version.GetValue(this); }
            set { Property.Version.SetValue(this, value); }
        }

        /// <summary>
        /// List of assembly names that should be included in the generated code
        /// </summary>
        public IList<string> UsingAssemblies => m_UsingAssemblies.AsReadOnly();

        /// <summary>
        /// TypeNode definitions for this schema
        /// </summary>
        public IReadOnlyList<TypeNode> Types => m_Types.AsReadOnly();
    }

    /// <summary>
    /// TypeNode describes an `IPropertyContainer` type definition
    /// </summary>
    public class TypeNode : IPropertyContainer
    {
        public static class Property
        {
            public static readonly Property<TypeNode, string> Namespace = new Property<TypeNode, string>(
                "Namespace",
                c => c.m_Namespace,
                (c, v) => c.m_Namespace = v
            );
            
            public static readonly Property<TypeNode, string> Name = new Property<TypeNode, string>(
                "Name",
                c => c.m_Name,
                (c, v) => c.m_Name = v
            );
            
            public static readonly Property<TypeNode, AccessModifier> AccessModifier = new Property<TypeNode, AccessModifier>(
                "AccessModifier",
                c => c.m_AccessModifier,
                (c, v) => c.m_AccessModifier = v
            );
            
            public static readonly Property<TypeNode, bool> IsAbstract = new Property<TypeNode, bool>(
                "IsAbstract",
                c => c.m_IsAbstract,
                (c, v) => c.m_IsAbstract = v
            );
            
            public static readonly Property<TypeNode, bool> IsStruct = new Property<TypeNode, bool>(
                "IsStruct",
                c => c.m_IsStruct,
                (c, v) => c.m_IsStruct = v
            );
            
            public static readonly Property<TypeNode, bool> IncludeStaticConstructor = new Property<TypeNode, bool>(
                "IncludeStaticConstructor", 
                c => c.m_IncludeStaticConstructor,
                (c, v) => c.m_IncludeStaticConstructor = v
            );
            
            public static readonly Property<TypeNode, bool> IncludePropertyBagAccessor = new Property<TypeNode, bool>(
                "IncludePropertyBagAccessor", 
                c => c.m_IncludePropertyBagAccessor,
                (c, v) => c.m_IncludePropertyBagAccessor = v
            );
            
            public static readonly Property<TypeNode, bool> IncludeVersionStorageAccessor = new Property<TypeNode, bool>(
                "IncludeVersionStorageAccessor", 
                c => c.m_IncludeVersionStorageAccessor,
                (c, v) => c.m_IncludeVersionStorageAccessor = v
            );

            public static readonly ContainerListProperty<TypeNode, IList<PropertyNode>, PropertyNode> Properties = new ContainerListProperty<TypeNode, IList<PropertyNode>, PropertyNode>(
                "Properties",
                c => c.m_Properties,
                null,
                c => new PropertyNode()
            );
            
            public static readonly ContainerListProperty<TypeNode, IList<TypeNode>,  TypeNode> NestedTypes = new ContainerListProperty<TypeNode, IList<TypeNode>, TypeNode>(
                "NestedTypes",
                c => c.m_NestedTypes,
                null,
                c => new TypeNode()
            );

            public static readonly PropertyBag PropertyBag = new PropertyBag(
                Namespace,
                Name,
                AccessModifier,
                IsAbstract,
                IsStruct,
                IncludeStaticConstructor,
                IncludePropertyBagAccessor,
                IncludeVersionStorageAccessor,
                Properties,
                NestedTypes
            );
        }

        public IPropertyBag PropertyBag => Property.PropertyBag;
        public IVersionStorage VersionStorage => null;

        private string m_Name;
        private string m_Namespace;
        private AccessModifier m_AccessModifier = AccessModifier.Public;
        private bool m_IsAbstract;
        private bool m_IsStruct;
        private bool m_IncludeStaticConstructor = true;
        private bool m_IncludePropertyBagAccessor = true;
        private bool m_IncludeVersionStorageAccessor = true;
        private readonly List<PropertyNode> m_Properties = new List<PropertyNode>();
        private readonly List<TypeNode> m_NestedTypes = new List<TypeNode>();

        /// <summary>
        /// TypeName for this type
        /// </summary>
        public string Name
        {
            get { return Property.Name.GetValue(this); }
            set { Property.Name.SetValue(this, value); }
        }

        /// <summary>
        /// Namespace for this type
        /// </summary>
        public string Namespace
        {
            get { return Property.Namespace.GetValue(this); }
            set { Property.Namespace.SetValue(this, value); }
        }
        
        /// <summary>
        /// .NET access modifier for this type <see cref="AccessModifier"/>
        /// </summary>
        public AccessModifier AccessModifier
        {
            get { return Property.AccessModifier.GetValue(this); }
            set { Property.AccessModifier.SetValue(this, value); }
        }

        /// <summary>
        /// Should this type be generated as abstract
        /// </summary>
        public bool IsAbstract
        {
            get { return Property.IsAbstract.GetValue(this); }
            set { Property.IsAbstract.SetValue(this, value); }
        }

        /// <summary>
        /// Should this type be generated as a ValueType
        /// </summary>
        public bool IsStruct
        {
            get { return Property.IsStruct.GetValue(this); }
            set { Property.IsStruct.SetValue(this, value); }
        }
        
        /// <summary>
        /// Should the static constructor be generated for this type
        ///
        /// if false; the implementor is responsible for calling `InitializeProperties()` and `InitializePropertyBag()` at the appropriate time
        ///
        /// defaults to `true`
        /// </summary>
        public bool IncludeStaticConstructor
        {
            get { return Property.IncludeStaticConstructor.GetValue(this); }
            set { Property.IncludeStaticConstructor.SetValue(this, value); }
        }
        
        /// <summary>
        /// Should the PropertyBag accessor be generated for this type
        ///
        /// if false; the implementor is responsible for implmenting the `PropertyBag` interface accessor
        ///
        /// defaults to `true`
        /// </summary>
        public bool IncludePropertyBagAccessor
        {
            get { return Property.IncludePropertyBagAccessor.GetValue(this); }
            set { Property.IncludePropertyBagAccessor.SetValue(this, value); }
        }
        
        /// <summary>
        /// Should the VersionStorage accessor be generated for this type
        ///
        /// if false; the implementor is responsible for implmenting the `VersionStorage` interface accessor
        ///
        /// defaults to `true`
        /// </summary>
        public bool IncludeVersionStorageAccessor
        {
            get { return Property.IncludeVersionStorageAccessor.GetValue(this); }
            set { Property.IncludeVersionStorageAccessor.SetValue(this, value); }
        }

        public IReadOnlyList<PropertyNode> Properties => m_Properties.AsReadOnly();
        public IReadOnlyList<TypeNode> NestedTypes => m_NestedTypes.AsReadOnly();
    }
    
    /// <summary>
    /// PropertyNode describes a single property of a type
    /// </summary>
    public class PropertyNode : IPropertyContainer
    {
        public static class Property
        {
            public static readonly Property<PropertyNode, string> Name = new Property<PropertyNode, string>(
                "Name",
                c => c.m_Name,
                (c, v) => c.m_Name = v
            );

            public static readonly Property<PropertyNode, string> ValueType = new Property<PropertyNode, string>(
                "ValueType",
                c => c.m_ValueType,
                (c, v) => c.m_ValueType = v
            );
            
            public static readonly Property<PropertyNode, PropertyType> PropertyType = new Property<PropertyNode, PropertyType>(
                "PropertyType",
                c => c.m_PropertyType,
                (c, v) => c.m_PropertyType = v
            );
            
            public static readonly Property<PropertyNode, bool> IncludeDeclaration = new Property<PropertyNode, bool>(
                "IncludeDeclaration",
                c => c.m_IncludeDeclaration,
                (c, v) => c.m_IncludeDeclaration = v
            );
            
            public static readonly Property<PropertyNode, bool> IncludeInitializer = new Property<PropertyNode, bool>(
                "IncludeInitializer",
                c => c.m_IncludeInitializer,
                (c, v) => c.m_IncludeInitializer = v
            );
            
            public static readonly Property<PropertyNode, bool> IncludeBackingField = new Property<PropertyNode, bool>(
                "IncludeBackingField",
                c => c.m_IncludeBackingField,
                (c, v) => c.m_IncludeBackingField = v
            );

            public static readonly Property<PropertyNode, bool> IncludeAccessor = new Property<PropertyNode, bool>(
                "IncludeAccessor",
                c => c.m_IncludeAccessor,
                (c, v) => c.m_IncludeAccessor = v
            );
            
            public static readonly Property<PropertyNode, string> BackingField = new Property<PropertyNode, string>(
                "BackingField",
                c => c.m_BackingField,
                (c, v) => c.m_BackingField = v
            );
            
            public static readonly Property<PropertyNode, string> DefaultValue = new Property<PropertyNode, string>(
                "DefaultValue",
                c => c.m_DefaultValue,
                (c, v) => c.m_DefaultValue = v
            );
            
            public static readonly PropertyBag PropertyBag = new PropertyBag(
                Name,
                ValueType,
                PropertyType,
                IncludeDeclaration,
                IncludeInitializer,
                IncludeBackingField,
                IncludeAccessor,
                BackingField,
                DefaultValue
            );
        }

        public IPropertyBag PropertyBag => Property.PropertyBag;
        public IVersionStorage VersionStorage => null;

        private string m_Name;
        private string m_ValueType;
        private PropertyType m_PropertyType = PropertyType.Value;
        private bool m_IncludeDeclaration = true;
        private bool m_IncludeInitializer = true;
        private bool m_IncludeBackingField = true;
        private bool m_IncludeAccessor = true;
        private string m_BackingField;
        private string m_DefaultValue;

        /// <summary>
        /// Name of the property
        /// </summary>
        public string Name
        {
            get { return Property.Name.GetValue(this); }
            set { Property.Name.SetValue(this, value); }
        }

        /// <summary>
        /// The value type for this property (e.g. int, float, string)
        /// 
        /// @NOTE When dealing with collections this is the item type
        /// </summary>
        public string ValueType
        {
            get { return Property.ValueType.GetValue(this); }
            set { Property.ValueType.SetValue(this, value); }
        }

        /// <summary>
        /// The property type <see cref="PropertyType"/>
        ///
        /// defaults to `Value`
        /// </summary>
        public PropertyType PropertyType
        {
            get { return Property.PropertyType.GetValue(this); }
            set { Property.PropertyType.SetValue(this, value); }
        }
        
        /// <summary>
        /// Should the declaration be generated for this property
        ///
        /// defaults to `true`
        /// </summary>
        public bool IncludeDeclaration
        {
            get { return Property.IncludeDeclaration.GetValue(this); }
            set { Property.IncludeDeclaration.SetValue(this, value); }
        }
        
        /// <summary>
        /// Should the initializer be generated for this property; (initializers are added to the `InitializeProperties` method)
        ///
        /// defaults to `true`
        /// </summary>
        public bool IncludeInitializer
        {
            get { return Property.IncludeInitializer.GetValue(this); }
            set { Property.IncludeInitializer.SetValue(this, value); }
        }
        
        /// <summary>
        /// Should the backing field be generated for this property
        ///
        /// defaults to `true`
        /// </summary>
        public bool IncludeBackingField
        {
            get { return Property.IncludeBackingField.GetValue(this); }
            set { Property.IncludeBackingField.SetValue(this, value); }
        }

        /// <summary>
        /// Should the .NET property accessors be generated for this property
        ///
        /// defaults to `true`
        /// </summary>
        public bool IncludeAccessor
        {
            get { return Property.IncludeAccessor.GetValue(this); }
            set { Property.IncludeAccessor.SetValue(this, value); }
        }

        /// <summary>
        /// Backing field name;
        ///
        /// If none is provided `m_{Name}` will be used
        /// </summary>
        public string BackingField
        {
            get { return Property.BackingField.GetValue(this); }
            set { Property.BackingField.SetValue(this, value); }
        }
        
        /// <summary>
        /// Default value for the backing field
        ///
        /// @NOTE This is assigned in the field initializer
        /// </summary>
        public string DefaultValue
        {
            get { return Property.DefaultValue.GetValue(this); }
            set { Property.DefaultValue.SetValue(this, value); }
        }

        /// <summary>
        /// Should this property be generated as a list
        /// </summary>
        public bool IsList => m_PropertyType == PropertyType.ClassList || m_PropertyType == PropertyType.StructList;
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)
