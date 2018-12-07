

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEngine.Assertions;

namespace Unity.Tiny
{
    internal enum TinyTypeCode
    {
        Unknown = 0,

        // IEEE Types; low level types with well defined storage
        Int8 = 1,
        Int16 = 2,
        Int32 = 3,
        Int64 = 4,
        UInt8 = 5,
        UInt16 = 6,
        UInt32 = 7,
        UInt64 = 8,
        Float32 = 9,
        Float64 = 10,

        // Mid level platform types; implementations may vary across platforms
        Boolean = 11,
        String = 13,

        // Internal types
        Component = 14,
        Struct = 15,
        Enum = 16,
        Configuration = 17,

        // High level dynamic types; these are handled in a special way by Tiny code
        EntityReference = 18,

        // Unity object types
        UnityObject = 19
    }

    /// <inheritdoc cref="TinyRegistryObjectBase"/>
    /// <summary>
    /// Represents a single type in the system. This can be used to define custom components, structs, enums etc.
    /// It is also used to define built in types (Int32, Float32 etc.)
    /// </summary>
    internal sealed partial class TinyType : TinyRegistryObjectBase, IAttributable, IPersistentObject
    {
        private struct DefaultValueBackingField
        {
            public int ObjectVersion;
            public int TypeCodeVersion;
            public int FieldsVersion;
            public object Value;
            public TinyObject Object;
        }

        static partial void InitializeCustomProperties()
        {
            // [IMPORTANT FOR ADDING OR REMVING PROPERTIES]
            // IF you add or remove properties be SURE to update the `m_PropertyBag` variable below
            // the property bag for types is dynamic since we want to ADD and REMOVE the default value properyt dynamically
            
            TypeIdProperty = new ValueClassProperty<TinyType, TinyTypeId>("$TypeId",
                    c => TinyTypeId.Type,
                    null
                )
                .WithAttribute(InspectorAttributes.HideInInspector)
                .WithAttribute(InspectorAttributes.Readonly);
            
            PersistenceIdProperty
                .WithAttribute(SerializationAttributes.Transient)
                .WithAttribute(SerializationAttributes.NonSerializedForPersistence);
            
            NameProperty
                .WithAttribute(InspectorAttributes.HideInInspector)
                .WithAttribute(InspectorAttributes.Readonly)
                .WithAttribute(SerializationAttributes.Transient);
            
            SerializedVersionProperty
                .WithAttribute(SerializationAttributes.Transient);
            
            FieldsProperty = new ClassListClassProperty<TinyType, TinyField>("Fields",
                c => c.m_Fields,
                c => c.NewField(TinyId.New(), "NewField", (Reference) Int32)
            );
        }
        
        private readonly PropertyBag m_PropertyBag = new PropertyBag(
            TypeIdProperty,
            // inherited
            PersistenceIdProperty,
            IdProperty,
            NameProperty,
            ExportFlagsProperty,
            UnlistedProperty,
            DocumentationProperty,
            // end - inherited
            TypeCodeProperty,
            BaseTypeProperty,
            FieldsProperty
        );

        private DefaultValueBackingField m_DefaultValue;
        private IValueClassProperty m_DefaultValueProperty;

        private int m_TypeCodeVersion;
        private int m_FieldsVersion;
        
        public override string Name
        {
            get { return NameProperty.GetValue(this); }
            set { NameProperty.SetValue(this, value); }
        }

        public Reference Ref => (Reference)this;

        public bool IsPrimitive
        {
            get
            {
                var typeCode = TypeCode;
                return typeCode != TinyTypeCode.Struct &&
                       typeCode != TinyTypeCode.Component &&
                       typeCode != TinyTypeCode.Enum &&
                       typeCode != TinyTypeCode.Configuration;
            }
        }

        public bool IsComponent => TypeCode == TinyTypeCode.Component;
        public bool IsStruct => TypeCode == TinyTypeCode.Struct;
        public bool IsEnum => TypeCode == TinyTypeCode.Enum;
        public bool IsConfiguration => TypeCode == TinyTypeCode.Configuration;

        public object DefaultValue
        {
            get { return GetDefaultValue(); }
            set
            {
                Refresh();
                m_DefaultValueProperty.SetObjectValue(this, value);
            }
        }

        public override IPropertyBag PropertyBag => m_PropertyBag;
        
        public IEnumerable<IPropertyContainer> EnumerateContainers()
        {
            yield return this;
        }

        public TinyType(IRegistry registry, IVersionStorage versionStorage) : base(registry, versionStorage)
        {
            m_Fields = new List<TinyField>();
            m_DefaultValue = new DefaultValueBackingField();
        }

        public object GetDefaultValue(bool skipTypeCheck = false)
        {
            if (!skipTypeCheck)
            {
                Refresh();
            }
            return m_DefaultValueProperty?.GetObjectValue(this);
        }

        public override void Refresh()
        {
            // Primitives are immutable
            if (IsPrimitive)
            {
                return;
            }

            // Update each field, this will make sure its type is still up to date
            var fields = Fields;
            for (var i = 0; i < fields.Count; i++)
            {
                fields[i].Refresh(Registry);
            }
            
            if (m_DefaultValue.TypeCodeVersion != m_TypeCodeVersion)
            {
                // Rebuild the default value property
                if (null != m_DefaultValueProperty)
                {
                    m_PropertyBag.RemoveProperty(m_DefaultValueProperty);
                }

                m_DefaultValueProperty = CreateDefaultValueProperty(TypeCode);

                if (null == m_DefaultValue.Object)
                {
                    // @TODO Overload constructor
                    m_DefaultValue.Object = new TinyObject(Registry, (Reference) this, this, false)
                    {
                        IsDefaultValue = true
                    };
                }

                m_DefaultValue.Object.Refresh();

                if (null != m_DefaultValueProperty)
                {
                    m_PropertyBag.AddProperty(m_DefaultValueProperty);
                }
            }
            else if (!IsPrimitive && (m_DefaultValue.FieldsVersion != m_FieldsVersion || m_DefaultValue.ObjectVersion != m_DefaultValue.Object?.Version))
            {
                m_DefaultValue.Object?.Refresh();
            }
            else
            {
                return;
            }

            m_DefaultValue.ObjectVersion = m_DefaultValue.Object?.Version ?? -0;
            m_DefaultValue.TypeCodeVersion = m_TypeCodeVersion;
            m_DefaultValue.FieldsVersion = m_FieldsVersion;
        }

        public TinyField CreateField(string name, Reference type, bool array = false)
        {
            var module = Registry?.CacheManager.GetModuleOf(Ref);
            var id = $"{module?.Name}.{Name}.{name}";
            return CreateField(TinyId.Generate(id), name, type, array);
        }

        public TinyField CreateField(TinyId id, string name, Reference type, bool array = false)
        {
            var field = NewField(id, name, type, array);
            FieldsProperty.Add(this, field);
            return field;
        }
        
        private TinyField NewField(TinyId id, string name, Reference type, bool array = false)
        {
            Assert.IsTrue(m_TypeCode == TinyTypeCode.Component || 
                          m_TypeCode == TinyTypeCode.Struct || 
                          m_TypeCode == TinyTypeCode.Enum ||
                          m_TypeCode == TinyTypeCode.Configuration);

            var field = new TinyField(this)
            {
                Id = id,
                Name = name,
                FieldType = type,
                Array = array,
                DeclaringType = this
            };

            return field;
        }

        /// <summary>
        /// Removes the given field from this type
        /// </summary>
        /// <param name="field">The field to remove</param>
        public void RemoveField(TinyField field)
        {
            Assert.IsTrue(FieldsProperty.Contains(this, field));
            Assert.IsTrue(field.DeclaringType == this);
            field.DeclaringType = null;
            FieldsProperty.Remove(this, field);
        }

        public void InsertField(int index, TinyField field)
        {
            Assert.IsFalse(FieldsProperty.Contains(this, field));
            Assert.IsTrue(field.DeclaringType == null);
            field.DeclaringType = this;
            FieldsProperty.Insert(this, index, field);
        }

        public TinyField FindFieldById(TinyId id)
        {
            return m_Fields.FirstOrDefault(field => Equals(field.Id, id));
        }

        public TinyField FindFieldByName(string name)
        {
            return m_Fields.FirstOrDefault(field => string.Equals(field.Name, name));
        }

        public override void IncrementVersion<TContainer>(IProperty property, TContainer container)
        {
            Version++;

            if (property == TypeCodeProperty)
            {
                m_TypeCodeVersion++;
            }

            if (property == FieldsProperty)
            {
                m_FieldsVersion++;
            }
            
            if (container is TinyField)
            {
                m_FieldsVersion++;
                SharedVersionStorage?.IncrementVersion(FieldsProperty, this);
                
            }
            else if (!ReferenceEquals(container, this))
            {
                SharedVersionStorage?.IncrementVersion(m_DefaultValueProperty, this);
            }
            else
            {
                SharedVersionStorage?.IncrementVersion(property, container);
            }
        }

        private static IValueClassProperty CreateDefaultValueProperty(TinyTypeCode typeCode)
        {
            switch (typeCode)
            {
                case TinyTypeCode.Unknown:
                    return null;
                case TinyTypeCode.Int8:
                    return CreateSimpleDefaultValueProperty<sbyte>();
                case TinyTypeCode.Int16:
                    return CreateSimpleDefaultValueProperty<short>();
                case TinyTypeCode.Int32:
                    return CreateSimpleDefaultValueProperty<int>();
                case TinyTypeCode.Int64:
                    return CreateSimpleDefaultValueProperty<long>();
                case TinyTypeCode.UInt8:
                    return CreateSimpleDefaultValueProperty<byte>();
                case TinyTypeCode.UInt16:
                    return CreateSimpleDefaultValueProperty<ushort>();
                case TinyTypeCode.UInt32:
                    return CreateSimpleDefaultValueProperty<uint>();
                case TinyTypeCode.UInt64:
                    return CreateSimpleDefaultValueProperty<ulong>();
                case TinyTypeCode.Float32:
                    return CreateSimpleDefaultValueProperty<float>();
                case TinyTypeCode.Float64:
                    return CreateSimpleDefaultValueProperty<double>();
                case TinyTypeCode.Boolean:
                    return CreateSimpleDefaultValueProperty<bool>();
                case TinyTypeCode.String:
                    return CreateSimpleDefaultValueProperty<string>();
                case TinyTypeCode.Configuration:
                case TinyTypeCode.Component:
                case TinyTypeCode.Struct:
                case TinyTypeCode.Enum:
                    return new ClassValueClassProperty<TinyType, TinyObject>("DefaultValue",
                        c => c.m_DefaultValue.Object,
                        (c, v) =>
                        {
                            if (null == v)
                            {
                                throw new NullReferenceException();
                            }

                            var obj = c.m_DefaultValue.Object;
                            obj?.CopyFrom(v);
                        });
                case TinyTypeCode.EntityReference:
                    return CreateSimpleDefaultValueProperty<TinyEntity.Reference>();
                case TinyTypeCode.UnityObject:
                    return CreateSimpleDefaultValueProperty<UnityEngine.Object>();
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeCode), typeCode, null);
            }
        }

        private static IValueClassProperty CreateSimpleDefaultValueProperty<TValue>()
        {
            return new ValueClassProperty<TinyType, TValue>("DefaultValue",
                c => (TValue) c.m_DefaultValue.Value,
                (c, v) => c.m_DefaultValue.Value = v);
        }

        /// <inheritdoc cref="IPropertyContainer"/>
        /// <summary>
        /// Weak reference to a type
        /// </summary>
        public partial struct Reference : IReference<TinyType>, IEquatable<Reference>
        {
            // ReSharper disable once MemberHidesStaticFromOuterClass
            static partial void InitializeCustomProperties()
            {
                IdProperty
                    .WithAttribute(InspectorAttributes.HideInInspector)
                    .WithAttribute(InspectorAttributes.Readonly);
            }

            public static Reference None { get; } = new Reference(TinyId.Empty, string.Empty);
            
            public Reference(TinyId id, string name)
            {
                m_Id = id;
                m_Name = name;
            }

            public TinyType Dereference(IRegistry registry)
            {
                return registry.Dereference<Reference, TinyType>(this);
            }

            public static explicit operator Reference(TinyType @object)
            {
                return new Reference(@object.Id, @object.Name);
            }

            public override string ToString()
            {
                return "Reference " + Name;
            }

            public bool Equals(Reference other)
            {
                return m_Id.Equals(other.m_Id);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is Reference && Equals((Reference) obj);
            }

            public override int GetHashCode()
            {
                return m_Id.GetHashCode();
            }
        }
    }
}

