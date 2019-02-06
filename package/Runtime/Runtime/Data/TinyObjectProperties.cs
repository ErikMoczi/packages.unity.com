

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace Unity.Tiny
{
    internal interface ITinyValueProperty
    {
        bool IsOverridden(IPropertyContainer container);
        bool IsDynamic { get; }
        bool IsEditorOnly { get; }
        bool IsVersioned { get; }
    }

    internal sealed partial class TinyObject
    {
        public class PropertiesContainer : IPropertyContainer, IVersionStorage
        {
            private static bool ValueEquals(object a, object b)
            {
                if (null == a && null == b)
                {
                    return true;
                }

                return null != a && a.Equals(b);
            }

            #region Dynamic Properties

            private static readonly Dictionary<Type, Dictionary<string, IProperty>> s_DynamicPropertyCache =
                new Dictionary<Type, Dictionary<string, IProperty>>();

            private class DynamicProperty<TValue> : ValueClassProperty<PropertiesContainer, TValue>, ITinyValueProperty
            {
                public override bool IsReadOnly => false;
                public bool IsDynamic => true;
                public bool IsEditorOnly => false;
                public bool IsVersioned => true;

                public DynamicProperty(string name) : base(name, null, null)
                {
                }

                public override TValue GetValue(PropertiesContainer container)
                {
                    return (TValue) container.m_DynamicValues[Name];
                }

                public override void SetValue(PropertiesContainer container, TValue value)
                {
                    container.VersionStorage?.IncrementVersion(this, container);
                    container.m_DynamicValues[Name] = value;
                }

                public bool IsOverridden(IPropertyContainer container)
                {
                    return true;
                }
            }

            private class ContainerDynamicProperty<TValue> : ClassValueClassProperty<PropertiesContainer, TValue>, ITinyValueProperty
                where TValue : class, IPropertyContainer
            {
                public override bool IsReadOnly => false;
                public bool IsDynamic => true;
                public bool IsEditorOnly => false;
                public bool IsVersioned => true;

                public ContainerDynamicProperty(string name) : base(name, null, null)
                {
                }

                public override TValue GetValue(PropertiesContainer container)
                {
                    return (TValue) container.m_DynamicValues[Name];
                }

                public override void SetValue(PropertiesContainer container, TValue value)
                {
                    container.VersionStorage?.IncrementVersion(this, container);
                    container.m_DynamicValues[Name] = value;
                }

                public bool IsOverridden(IPropertyContainer container)
                {
                    return true;
                }
            }
            
            private class MutableContainerDynamicProperty<TValue> : StructValueClassProperty<PropertiesContainer, TValue>, ITinyValueProperty
                where TValue : struct, IPropertyContainer
            {
                public override bool IsReadOnly => false;
                public bool IsDynamic => true;
                public bool IsEditorOnly => false;
                public bool IsVersioned => true;

                public MutableContainerDynamicProperty(string name) : base(name, null, null, null)
                {
                }

                public override TValue GetValue(PropertiesContainer container)
                {
                    return (TValue) container.m_DynamicValues[Name];
                }

                public override void SetValue(PropertiesContainer container, TValue value)
                {
                    container.VersionStorage?.IncrementVersion(this, container);
                    container.m_DynamicValues[Name] = value;
                }

                public bool IsOverridden(IPropertyContainer container)
                {
                    return true;
                }
            }
            
            private class ObjectDynamicProperty : ClassValueClassProperty<PropertiesContainer, TinyObject>, ITinyValueProperty
            {
                public override bool IsReadOnly => false;
                public bool IsDynamic => true;
                public bool IsEditorOnly => false;
                public bool IsVersioned => true;

                public ObjectDynamicProperty(string name) : base(name, null, null)
                {
                }

                public override TinyObject GetValue(PropertiesContainer container)
                {
                    return (TinyObject) container.m_DynamicValues[Name];
                }

                public override void SetValue(PropertiesContainer container, TinyObject value)
                {
                    container.VersionStorage?.IncrementVersion(this, container);
                    var obj = container.m_DynamicValues[Name] as TinyObject;
                    obj?.CopyFrom(value);
                }

                public bool IsOverridden(IPropertyContainer container)
                {
                    return true;
                }
            }
            
            private class ListDynamicProperty : ClassValueClassProperty<PropertiesContainer, TinyList>, ITinyValueProperty
            {
                public override bool IsReadOnly => false;
                public bool IsDynamic => true;
                public bool IsEditorOnly => false;
                public bool IsVersioned => true;

                public ListDynamicProperty(string name) : base(name, null, null)
                {
                }

                public override TinyList GetValue(PropertiesContainer container)
                {
                    return (TinyList) container.m_DynamicValues[Name];
                }

                public override void SetValue(PropertiesContainer container, TinyList value)
                {
                    var obj = container.m_DynamicValues[Name] as TinyList;
                    obj?.CopyFrom(value);
                    container.VersionStorage?.IncrementVersion(this, container);
                }

                public bool IsOverridden(IPropertyContainer container)
                {
                    return true;
                }
            }
            
            private static IProperty CreateDynamicProperty(string name, Type type)
            {
                Dictionary<string, IProperty> typedPropertyByName;
                if (!s_DynamicPropertyCache.TryGetValue(type, out typedPropertyByName))
                {
                    typedPropertyByName = new Dictionary<string, IProperty>();
                    s_DynamicPropertyCache.Add(type, typedPropertyByName);
                }

                IProperty property;
                if (typedPropertyByName.TryGetValue(name, out property))
                {
                    return property;
                }

                var typeCode = System.Type.GetTypeCode(type);

                switch (typeCode)
                {
                    case TypeCode.Byte:
                        property = new DynamicProperty<byte>(name);
                        break;
                    case TypeCode.UInt16:
                        property = new DynamicProperty<ushort>(name);
                        break;
                    case TypeCode.UInt32:
                        property = new DynamicProperty<uint>(name);
                        break;
                    case TypeCode.UInt64:
                        property = new DynamicProperty<ulong>(name);
                        break;
                    case TypeCode.SByte:
                        property = new DynamicProperty<sbyte>(name);
                        break;
                    case TypeCode.Int16:
                        property = new DynamicProperty<short>(name);
                        break;
                    case TypeCode.Int32:
                        property = new DynamicProperty<int>(name);
                        break;
                    case TypeCode.Int64:
                        property = new DynamicProperty<long>(name);
                        break;
                    case TypeCode.Single:
                        property = new DynamicProperty<float>(name);
                        break;
                    case TypeCode.Double:
                        property = new DynamicProperty<double>(name);
                        break;
                    case TypeCode.Boolean:
                        property = new DynamicProperty<bool>(name);
                        break;
                    case TypeCode.Char:
                        property = new DynamicProperty<char>(name);
                        break;
                    case TypeCode.String:
                        property = new DynamicProperty<string>(name);
                        break;
                    case TypeCode.Object:
                        if (typeof(TinyObject) == type)
                        {
                            property = new ObjectDynamicProperty(name);
                        }
                        else if (typeof(TinyList) == type)
                        {
                            property = new ListDynamicProperty(name);
                        }
                        else if (typeof(TinyEntity.Reference) == type)
                        {
                            property = new MutableContainerDynamicProperty<TinyEntity.Reference>(name);
                        }
                        else if (typeof(TinyEnum.Reference) == type)
                        {
                            property = new MutableContainerDynamicProperty<TinyEnum.Reference>(name);
                        }
                        else if (typeof(Texture2D) == type)
                        {
                            property = new DynamicProperty<Texture2D>(name);
                        }
                        else if (typeof(Sprite) == type)
                        {
                            property = new DynamicProperty<Sprite>(name);
                        }
                        else if (typeof(Object).IsAssignableFrom(type))
                        {
                            property = new DynamicProperty<Object>(name);
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                        break;
                    case TypeCode.DBNull:
                    case TypeCode.Empty:
                    case TypeCode.Decimal:
                    case TypeCode.DateTime:
                        throw new NotImplementedException();
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                typedPropertyByName.Add(name, property);
                return property;
            }

            #endregion

            #region Field Properties

            private interface IFieldProperty
            {
                int Index { get; set; }
            }

            private static readonly Dictionary<TinyId, IProperty> s_FieldPropertyCache =
                new Dictionary<TinyId, IProperty>();

            private class FieldProperty<TValue> : ValueClassProperty<PropertiesContainer, TValue>, IFieldProperty, ITinyValueProperty
            {
                public int Index { get; set; }
                public override bool IsReadOnly => false;
                public bool IsDynamic => false;
                public bool IsEditorOnly { get; }
                public bool IsVersioned { get; }

                public FieldProperty(int index, string name, bool editorOnly, bool versioned) : base(name, null, null)
                {
                    Index = index;
                    IsEditorOnly = editorOnly;
                    IsVersioned = versioned;
                }

                public override TValue GetValue(PropertiesContainer container)
                {
                    var value = container.m_FieldValues[Index].Value;

                    if (!(value is TValue))
                    {
                        return default(TValue);
                    }

                    return (TValue) value;
                }

                public override void SetValue(PropertiesContainer container, TValue value)
                {
                    container.VersionStorage?.IncrementVersion(this, container);
                    container.m_FieldValues[Index].Value = value;
                }

                public bool IsOverridden(IPropertyContainer container)
                {
                    var c = (PropertiesContainer) container;
                    return c.m_FieldValues[Index].Overridden;
                }
            }
            
            private class ContainerFieldProperty<TValue> : ClassValueClassProperty<PropertiesContainer, TValue>, IFieldProperty, ITinyValueProperty
                where TValue : class, IPropertyContainer
            {
                public int Index { get; set; }
                public override bool IsReadOnly => false;
                public bool IsDynamic => false;
                public bool IsEditorOnly { get; }
                public bool IsVersioned { get; }

                public ContainerFieldProperty(int index, string name, bool editorOnly, bool versioned)
                    : base(name, null, null)
                {
                    Index = index;
                    IsEditorOnly = editorOnly;
                    IsVersioned = versioned;
                }

                public override TValue GetValue(PropertiesContainer container)
                {
                    var r = container.m_FieldValues[Index].Value;

                    if (null == r || typeof(TValue) != r.GetType())
                    {
                        return default(TValue);
                    }

                    return (TValue) r;
                }

                public override void SetValue(PropertiesContainer container, TValue value)
                {
                    if (ValueEquals(GetValue(container), value))
                    {
                        return;
                    }
                    
                    container.m_FieldValues[Index].Value = value;
                    container.VersionStorage?.IncrementVersion(this, container);
                }

                public bool IsOverridden(IPropertyContainer container)
                {
                    var c = (PropertiesContainer) container;
                    return c.m_FieldValues[Index].Overridden;
                }
            }
            
            private class StructValueFieldProperty<TValue> : StructValueClassProperty<PropertiesContainer, TValue>, IFieldProperty, ITinyValueProperty
                where TValue : struct, IPropertyContainer
            {
                public int Index { get; set; }
                public override bool IsReadOnly => false;
                public bool IsDynamic => false;
                public bool IsEditorOnly { get; }
                public bool IsVersioned { get; }

                public StructValueFieldProperty(int index, string name, bool editorOnly, bool versioned)
                    : base(name, null, null, MakeRef)
                {
                    Index = index;
                    IsEditorOnly = editorOnly;
                    IsVersioned = versioned;
                }

                public override TValue GetValue(PropertiesContainer container)
                {
                    var r = container.m_FieldValues[Index].Value;

                    if (null == r || typeof(TValue) != r.GetType())
                    {
                        return default(TValue);
                    }

                    return (TValue) r;
                }

                public override void SetValue(PropertiesContainer container, TValue value)
                {
                    if (ValueEquals(GetValue(container), value))
                    {
                        return;
                    }
                    
                    container.m_FieldValues[Index].Value = value;
                    container.VersionStorage?.IncrementVersion(this, container);
                }

                private static void MakeRef(ByRef byRef, StructValueClassProperty<PropertiesContainer, TValue> property, PropertiesContainer container, IPropertyVisitor visitor)
                {
                    var p = property as StructValueFieldProperty<TValue>;
                    
                    var value = container.m_FieldValues[p.Index].Value;
                    var typed = (TValue?) value ?? default(TValue);
                    
                    byRef(property, container, ref typed, visitor);

                    // @HACK
                    // If the original struct has not changed
                    // We can take the return value from typed
                    if (value == container.m_FieldValues[p.Index].Value)
                    {
                        container.m_FieldValues[p.Index].Value = typed;
                    }
                    else
                    {
                        // The actual value has been changed in some way
                        // This implies that `SetValue` was called on the property and the ref was not updated
                        // Don't attempt to reassign, since we can lose our changes
                    }
                }

                public bool IsOverridden(IPropertyContainer container)
                {
                    var c = (PropertiesContainer) container;
                    return c.m_FieldValues[Index].Overridden;
                }
            }

            private class ObjectFieldProperty : ClassValueClassProperty<PropertiesContainer, TinyObject>, IFieldProperty, ITinyValueProperty
            {
                public int Index { get; set; }
                public override bool IsReadOnly => false;
                public bool IsDynamic => false;
                public bool IsEditorOnly { get; }
                public bool IsVersioned { get; }

                public ObjectFieldProperty(int index, string name, bool editorOnly, bool versioned)
                    : base(name, null, null)
                {
                    Index = index;
                    IsEditorOnly = editorOnly;
                    IsVersioned = versioned;
                }

                public override TinyObject GetValue(PropertiesContainer container)
                {
                    var value = container.m_FieldValues[Index].Value;

                    if (null == value || typeof(TinyObject) != value.GetType())
                    {
                        return null;
                    }

                    return (TinyObject) value;
                }

                public override void SetValue(PropertiesContainer container, TinyObject value)
                {
                    if (null == value)
                    {
                        throw new NullReferenceException();
                    }
                    
                    if (ValueEquals(GetValue(container), value))
                    {
                        return;
                    }

                    var obj = container.m_FieldValues[Index].Value as TinyObject;
                    obj?.CopyFrom(value);
                    container.VersionStorage?.IncrementVersion(this, container);
                }

                public bool IsOverridden(IPropertyContainer container)
                {
                    var c = (PropertiesContainer) container;
                    return c.m_FieldValues[Index].Overridden;
                }
            }

            private class ListFieldProperty : ClassValueClassProperty<PropertiesContainer, TinyList>, IFieldProperty, ITinyValueProperty
            {
                public int Index { get; set; }
                public override bool IsReadOnly => false;
                public bool IsDynamic => false;
                public bool IsEditorOnly { get; }
                public bool IsVersioned { get; }

                public ListFieldProperty(int index, string name, bool editorOnly, bool versioned)
                    : base(name, null, null)
                {
                    Index = index;
                    IsEditorOnly = editorOnly;
                    IsVersioned = versioned;
                }

                public override TinyList GetValue(PropertiesContainer container)
                {
                    var value = container.m_FieldValues[Index].Value;

                    if (null == value || typeof(TinyList) != value.GetType())
                    {
                        return null;
                    }

                    return (TinyList) value;
                }

                public override void SetValue(PropertiesContainer container, TinyList value)
                {
                    if (null == value)
                    {
                        throw new NullReferenceException();
                    }

                    var obj = container.m_FieldValues[Index].Value as TinyList;
                    obj?.CopyFrom(value);
                    container.VersionStorage?.IncrementVersion(this, container);
                }

                public bool IsOverridden(IPropertyContainer container)
                {
                    var c = (PropertiesContainer) container;
                    return c.m_FieldValues[Index].Overridden;
                }
            }

            private static IProperty CreateFieldProperty(TinyId fieldId, int index, string name, TinyType type, bool array, bool editorOnly, bool versioned)
            {
                IProperty property;
                s_FieldPropertyCache.TryGetValue(fieldId, out property);

                if (array)
                {
                    property = property is ListFieldProperty && property.Name.Equals(name) ? property : new ListFieldProperty(index, name, editorOnly, versioned);
                }
                else
                {
                    switch (type.TypeCode)
                    {
                        case TinyTypeCode.Unknown:
                            break;
                        case TinyTypeCode.Int8:
                            property = property is FieldProperty<sbyte> && property.Name.Equals(name)
                                ? property
                                : new FieldProperty<sbyte>(index, name, editorOnly, versioned);
                            break;
                        case TinyTypeCode.Int16:
                            property = property is FieldProperty<short> && property.Name.Equals(name)
                                ? property
                                : new FieldProperty<short>(index, name, editorOnly, versioned);
                            break;
                        case TinyTypeCode.Int32:
                            property = property is FieldProperty<int> && property.Name.Equals(name)
                                ? property
                                : new FieldProperty<int>(index, name, editorOnly, versioned);
                            break;
                        case TinyTypeCode.Int64:
                            property = property is FieldProperty<long> && property.Name.Equals(name)
                                ? property
                                : new FieldProperty<long>(index, name, editorOnly, versioned);
                            break;
                        case TinyTypeCode.UInt8:
                            property = property is FieldProperty<byte> && property.Name.Equals(name)
                                ? property
                                : new FieldProperty<byte>(index, name, editorOnly, versioned);
                            break;
                        case TinyTypeCode.UInt16:
                            property = property is FieldProperty<ushort> && property.Name.Equals(name)
                                ? property
                                : new FieldProperty<ushort>(index, name, editorOnly, versioned);
                            break;
                        case TinyTypeCode.UInt32:
                            property = property is FieldProperty<uint> && property.Name.Equals(name)
                                ? property
                                : new FieldProperty<uint>(index, name, editorOnly, versioned);
                            break;
                        case TinyTypeCode.UInt64:
                            property = property is FieldProperty<ulong> && property.Name.Equals(name)
                                ? property
                                : new FieldProperty<ulong>(index, name, editorOnly, versioned);
                            break;
                        case TinyTypeCode.Float32:
                            property = property is FieldProperty<float> && property.Name.Equals(name)
                                ? property
                                : new FieldProperty<float>(index, name, editorOnly, versioned);
                            break;
                        case TinyTypeCode.Float64:
                            property = property is FieldProperty<double> && property.Name.Equals(name)
                                ? property
                                : new FieldProperty<double>(index, name, editorOnly, versioned);
                            break;
                        case TinyTypeCode.Boolean:
                            property = property is FieldProperty<bool> && property.Name.Equals(name)
                                ? property
                                : new FieldProperty<bool>(index, name, editorOnly, versioned);
                            break;
                        case TinyTypeCode.String:
                            property = property is FieldProperty<string> && property.Name.Equals(name)
                                ? property
                                : new FieldProperty<string>(index, name, editorOnly, versioned);
                            break;
                        case TinyTypeCode.Configuration:
                        case TinyTypeCode.Component:
                            throw new NotSupportedException();
                        case TinyTypeCode.Struct:
                            property = property is ObjectFieldProperty && property.Name.Equals(name)
                                ? property
                                : new ObjectFieldProperty(index, name, editorOnly, versioned);
                            break;
                        case TinyTypeCode.Enum:
                            property =
                                property is StructValueFieldProperty<TinyEnum.Reference> &&
                                property.Name.Equals(name)
                                    ? property
                                    : new StructValueFieldProperty<TinyEnum.Reference>(index, name, editorOnly, versioned);
                            break;
                        case TinyTypeCode.EntityReference:
                            property = property is StructValueFieldProperty<TinyEntity.Reference> && property.Name.Equals(name)
                                ? property
                                : new StructValueFieldProperty<TinyEntity.Reference>(index, name, editorOnly, versioned);
                            break;
                        case TinyTypeCode.UnityObject:
                            if (type.Id == TinyType.Texture2DEntity.Id)
                            {
                                property = property is FieldProperty<Texture2D> && property.Name.Equals(name)
                                    ? property
                                    : new FieldProperty<Texture2D>(index, name, editorOnly, versioned);
                            }
                            else if (type.Id == TinyType.SpriteEntity.Id)
                            {
                                property = property is FieldProperty<Sprite> && property.Name.Equals(name)
                                    ? property
                                    : new FieldProperty<Sprite>(index, name, editorOnly, versioned);
                            }
                            else if (type.Id == TinyType.TileEntity.Id)
                            {
                                property = property is FieldProperty<TileBase> && property.Name.Equals(name) ? property : new FieldProperty<TileBase>(index, name, editorOnly, versioned);
                            }
                            else if (type.Id == TinyType.TilemapEntity.Id)
                            {
                                property = property is FieldProperty<Tilemap> && property.Name.Equals(name) ? property : new FieldProperty<Tilemap>(index, name, editorOnly, versioned);
                            }
                            else if (type.Id == TinyType.AudioClipEntity.Id)
                            {
                                property = property is FieldProperty<AudioClip> && property.Name.Equals(name) ? property : new FieldProperty<AudioClip>(index, name, editorOnly, versioned);
                            }
                            else if (type.Id == TinyType.AnimationClipEntity.Id)
                            {
                                property = property is FieldProperty<AnimationClip> && property.Name.Equals(name) ? property : new FieldProperty<AnimationClip>(index, name, editorOnly, versioned);
                            }
                            else if (type.Id == TinyType.FontEntity.Id)
                            {
                                property = property is FieldProperty<TMPro.TMP_FontAsset> && property.Name.Equals(name)
                                    ? property
                                    : new FieldProperty<TMPro.TMP_FontAsset>(index, name, editorOnly, versioned);
                            }
                            else
                            {
                                property = property is FieldProperty<Object> && property.Name.Equals(name)
                                    ? property
                                    : new FieldProperty<Object>(index, name, editorOnly, versioned);
                            }

                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                var fieldProperty = property as IFieldProperty;
                if (null != fieldProperty)
                {
                    fieldProperty.Index = index;
                }

                s_FieldPropertyCache[fieldId] = property;

                return property;
            }

            #endregion

            private class FieldValue
            {
                /// <summary>
                /// The field Id that this value maps to
                /// NOTE: This is to survive field renaming
                /// </summary>
                public TinyId Id;

                /// <summary>
                /// Raw value (untyped)
                /// </summary>
                public object Value;

                /// <summary>
                /// Is this value in it's default state
                /// NOTE: This is only ever true for values derived from a TinyType
                /// </summary>
                public bool Overridden;
            }

            private readonly TinyObject m_Object;
            private readonly List<FieldValue> m_FieldValues;
            private Dictionary<string, object> m_DynamicValues;

            public TinyObject ParentObject => m_Object;

            /// <summary>
            /// Each dynamic object will have it's own PropertyBag instance (no sharing or re-use)
            /// </summary>
            private readonly PropertyBag m_PropertyBag;

            public IVersionStorage VersionStorage => this;
            public IPropertyBag PropertyBag => m_PropertyBag;

            /// <summary>
            /// Returns true if any values have been explicitly overridden
            /// </summary>
            public bool IsOverridden
            {
                get
                {
                    if (m_DynamicValues?.Count > 0)
                    {
                        return true;
                    }
                    
                    foreach (var value in m_FieldValues)
                    {
                        if (value.Overridden)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            /// <summary>
            /// Initializes a new PropertiesContainer
            /// </summary>
            /// <param name="object">The host class for this instance</param>
            public PropertiesContainer(TinyObject @object)
            {
                m_Object = @object;
                m_PropertyBag = new PropertyBag();
                m_FieldValues = new List<FieldValue>();
            }

            /// <summary>
            /// This method does a lot of magic
            /// You can associate ANY value with a name and the correct property will be generated for you
            /// </summary>
            public object this[string key]
            {
                get
                {
                    // Incurs a dictionary lookup
                    var property = m_PropertyBag.FindProperty(key);

                    // This null check is intentional so that we don't allocate the error string every time we call the indexer.
                    if (null == property)
                    {
                        // Must assert to avoid undefined behaviour
                        Assert.IsNotNull(property, GetPropertyKeyNotFoundErrorString(key));
                    }

                    var container = (IPropertyContainer) this;

                    // This works well since the implementation details are abstracted
                    // i.e. We don't care how the value is unpacked (List, Dictionary, NativeArray etc)
                    return (property as IValueProperty)?.GetObjectValue(container);
                }
                set
                {
                    // Incurs a dictionary lookup
                    var property = m_PropertyBag.FindProperty(key);

                    if (null == property)
                    {
                        // Auto-generate a dynamic property for the user
                        var type = value?.GetType() ?? typeof(object);
                        property = CreateDynamicProperty(key, type);
                        Assert.IsNotNull(property);
                        m_PropertyBag.AddProperty(property);

                        if (null == m_DynamicValues)
                        {
                            m_DynamicValues = new Dictionary<string, object>();
                        }

                        // Setup the underlying storage
                        // This breaks our abstraction but must be done at some point
                        if (typeof(TinyObject) == type)
                        {
                            var obj = new TinyObject(m_Object.Registry, TinyType.Reference.None);
                            obj.CopyFrom(value as TinyObject);
                            m_DynamicValues.Add(key, obj);

                        }
                        else if (typeof(TinyList) == type)
                        {
                            var obj = new TinyList(m_Object.Registry, TinyType.Reference.None);
                            obj.CopyFrom(value as TinyList);
                            m_DynamicValues.Add(key, obj);
                        }
                        else
                        {
                            m_DynamicValues.Add(key, value);
                        }
                    }
                    else
                    {
                        // @TODO There is an unhandled case here when we encounter a type mis-match, we need to detect this and throw
                        try
                        {
                            (property as IValueClassProperty).SetObjectValue(this, value);
                        }
                        catch (InvalidCastException)
                        {
                            Debug.LogError($"Could not cast {value.GetType()} to {(property as IValueProperty).ValueType}. Value is '{value}'.");
                            throw;
                        }
                    }
                }
            }

            public void SetOverridden(string key, bool overridden)
            {
                var index = (m_PropertyBag.FindProperty(key) as IFieldProperty)?.Index ?? -1;
                if (index >= 0)
                {
                    m_FieldValues[index].Overridden = overridden;
                }
            }

            public bool HasProperty(string key)
            {
                return m_PropertyBag.FindProperty(key) != null;
            }

            public void RemoveProperty(string key)
            {
                m_DynamicValues.Remove(key);
                m_PropertyBag.RemoveProperty(m_PropertyBag.FindProperty(key));
            }

            public void Reset(TinyType type, TinyObject defaultValue)
            {
                if (null == type)
                {
                    return;
                }

                // The default value for this type
                var typeDefaultValue = !m_Object.IsDefaultValue ? defaultValue ?? type.DefaultValue as TinyObject : null;

                var fields = type.Fields;
                for (var i = 0; i < fields.Count; i++)
                {
                    var field = fields[i];
                    var fieldType = field.FieldType.Dereference(m_Object.Registry);
                    var fieldValue = m_FieldValues[i];
                    fieldValue.Overridden = false;

                    // The default value for this field
                    var fieldDefaultValue = typeDefaultValue?[field.Name];

                    if (fieldType.IsPrimitive || fieldType.IsEnum)
                    {
                        (m_PropertyBag.FindProperty(field.Name) as IValueClassProperty).SetObjectValue(this, fieldDefaultValue);
                    }
                    else
                    {
                        (fieldValue.Value as TinyObject)?.Reset(fieldDefaultValue as TinyObject);
                    }
                }
            }

            public void Refresh(TinyType type, TinyObject defaultValue, bool skipTypeCheck = false)
            {
                Assert.IsNotNull(type);

                var fields = type.Fields;

                Assert.IsTrue(type.TypeCode == TinyTypeCode.Struct ||
                              type.TypeCode == TinyTypeCode.Component ||
                              type.TypeCode == TinyTypeCode.Enum ||
                              type.TypeCode == TinyTypeCode.Configuration);

                // Rebuild all fields and re-map the indicies correctly
                MigrateFields(fields, m_FieldValues);

                // Dynamically rebuild the property bag                
                m_PropertyBag.Clear();

                // Migrate dynamic values
                if (null != m_DynamicValues)
                {
                    for (var i = 0; i < fields.Count; i++)
                    {
                        object dynamicValue;
                        if (!m_DynamicValues.TryGetValue(fields[i].Name, out dynamicValue))
                        {
                            continue;
                        }

                        m_FieldValues[i].Value = dynamicValue;
                        m_FieldValues[i].Overridden = true;
                    }

                    m_DynamicValues = null;
                }

                for (var i = 0; i < fields.Count; i++)
                {
                    var field = fields[i];
                    var fieldName = field.Name;
                    var fieldType = field.FieldType.Dereference(type.Registry);
                    var fieldValue = m_FieldValues[i];

                    if (null == fieldType)
                    {
                        continue;
                    }

                    // Force the field type to be refreshed if needed
                    if (!skipTypeCheck)
                    {
                        fieldType.Refresh();
                    }

                    // The default value for this field
                    var fieldDefaultValue = null != defaultValue ? defaultValue[fieldName] : TinyType.CreateInstance(fieldType);

                    if (!fieldValue.Overridden && (fieldType.IsPrimitive || fieldType.IsEnum) && !field.Array)
                    {
                        fieldValue.Value = fieldDefaultValue;
                    }
                    else
                    {
                        fieldValue.Value = MigrateFieldValue(m_Object.Registry, this, fieldValue.Value, fieldType, field.Array, fieldDefaultValue);
                    }

                    // @HACK 
                    if (fieldValue.Value is TinyObject)
                    {
                        (fieldValue.Value as TinyObject).Name = fieldName;
                    }
                    else if (fieldValue.Value is TinyList)
                    {
                        (fieldValue.Value as TinyList).Name = fieldName;
                    }

                    m_PropertyBag.AddProperty(CreateFieldProperty(field.Id, i, fieldName, fieldType, field.Array, field.EditorOnly, field.Versioned));

                    IncrementVersion(null, this);
                }
            }

            private static void MigrateFields(IList<TinyField> fields, List<FieldValue> fieldValues)
            {
                var fieldsCount = fields.Count;
                var dataListCount = fieldValues.Count;

                if (dataListCount == 0)
                {
                    fieldValues.Capacity = fieldsCount;
                }

                for (var f = 0; f < fieldsCount; f++)
                {
                    var fieldId = fields[f].Id;

                    var p = f;

                    // We have no guarantee that the field order matches our property order (i.e. We assume the user can re-order field definitions)
                    for (; p < dataListCount; p++)
                    {
                        if (fieldId != fieldValues[p].Id)
                        {
                            continue;
                        }

                        break;
                    }

                    // The property was not found; this is a new field that was added, create a new corresponding property
                    if (p >= dataListCount)
                    {
                        fieldValues.Add(new FieldValue {Id = fieldId, Value = null, Overridden = false});
                        dataListCount++;
                    }

                    // This property exists and is sorted
                    if (p == f)
                    {
                        continue;
                    }

                    // Swap the property in to its correct place
                    var fieldValue = fieldValues[f];
                    fieldValues[f] = fieldValues[p];
                    fieldValues[p] = fieldValue;
                }

                // Remove any excess properties
                if (dataListCount > fieldsCount)
                {
                    fieldValues.RemoveRange(fields.Count, dataListCount - fieldsCount);
                }
            }

            public static object MigrateFieldValue(IRegistry registry, IVersionStorage versionStorage, object value, TinyType type, bool array, object defaultValue = null, bool skipTypeCheck = false)
            {
                if (array)
                {
                    var list = value as TinyList;
                    if (null == list)
                    {
                        list = new TinyList(registry, (TinyType.Reference)type, versionStorage, false);
                        list.Refresh(defaultValue as TinyList, skipTypeCheck);
                    }
                    else
                    {
                        list.Type = (TinyType.Reference) type;
                        list.Refresh(defaultValue as TinyList, skipTypeCheck);
                    }

                    return list;
                }

                switch (type.TypeCode)
                {
                    case TinyTypeCode.Unknown:
                        break;
                    case TinyTypeCode.Int8:
                        return TryChangeType<sbyte>(value);
                    case TinyTypeCode.Int16:
                        return TryChangeType<short>(value);
                    case TinyTypeCode.Int32:
                        return TryChangeType<int>(value);
                    case TinyTypeCode.Int64:
                        return TryChangeType<long>(value);
                    case TinyTypeCode.UInt8:
                        return TryChangeType<byte>(value);
                    case TinyTypeCode.UInt16:
                        return TryChangeType<ushort>(value);
                    case TinyTypeCode.UInt32:
                        return TryChangeType<uint>(value);
                    case TinyTypeCode.UInt64:
                        return TryChangeType<ulong>(value);
                    case TinyTypeCode.Float32:
                        return TryChangeType<float>(value);
                    case TinyTypeCode.Float64:
                        return TryChangeType<double>(value);
                    case TinyTypeCode.Boolean:
                        return TryChangeType<bool>(value);
                    case TinyTypeCode.String:
                        return TryChangeType<string>(value) ?? string.Empty;
                    case TinyTypeCode.Configuration:
                    case TinyTypeCode.Component:
                        // Components can not be fields, they can only exist at the entity level
                        throw new NotSupportedException();
                    case TinyTypeCode.Struct:
                    {
                        var obj = value as TinyObject;
                        if (null == obj)
                        {
                            obj = new TinyObject(registry, (TinyType.Reference) type, versionStorage, false);
                            obj.Refresh(defaultValue as TinyObject, skipTypeCheck);
                        }
                        else
                        {
                            obj.Type = (TinyType.Reference) type;
                            obj.Refresh(defaultValue as TinyObject, skipTypeCheck);
                        }

                        return obj;
                    }

                    case TinyTypeCode.Enum:
                    {
                        if (value is TinyEnum.Reference enumRef)
                        {
                            return new TinyEnum.Reference(type, enumRef.Id);
                        }

                        if (null == defaultValue)
                        {
                            return new TinyEnum.Reference(type, type.Fields.First().Id);
                        }

                        return defaultValue is TinyEnum.Reference
                            ? new TinyEnum.Reference(type, ((TinyEnum.Reference) defaultValue).Id)
                            : new TinyEnum.Reference(type, type.Fields.First().Id);
                    }

                    case TinyTypeCode.EntityReference:
                    {
                        if (value is TinyEntity.Reference)
                        {
                            return value;
                        }

                        return defaultValue is TinyEntity.Reference
                            ? defaultValue
                            : TinyEntity.Reference.None;
                    }
                    case TinyTypeCode.UnityObject:
                    {
                        if (value is Object)
                        {
                            return value;
                        }

                        return defaultValue is Object
                            ? defaultValue
                            : null;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return null;
            }

            private static object TryChangeType<T>(object value)
            {
                if (!(value is IConvertible))
                {
                    return default(T);
                }

                try
                {
                    return Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    // ignored
                }

                return default(T);
            }

            public void IncrementVersion<TContainer>(IProperty property, TContainer container)
                where TContainer : IPropertyContainer
            {
                // Early exit if property is not versioned
                if (property is ITinyValueProperty p && !p.IsVersioned)
                {
                    return;
                }

                if (property is IFieldProperty fieldProperty)
                {
                    // One of our direct properties has Overridden
                    m_FieldValues[fieldProperty.Index].Overridden = true;
                }
                else
                {
                    // A property of one of our sub objects has Overridden
                    for (var i = 0; i < m_FieldValues.Count; i++)
                    {
                        var fieldValue = m_FieldValues[i];
                        
                        if (ReferenceEquals(container, fieldValue.Value))
                        {
                            fieldValue.Overridden = true;
                        }
                    }
                }

                // Propagate version change up the tree
                var c = (IPropertyContainer) m_Object;
                m_Object.VersionStorage.IncrementVersion(s_PropertiesProperty, c);
            }
            
            // This method exists only to lazily compute the error string, when needed.
            private static string GetPropertyKeyNotFoundErrorString(string key)
            {
                return $"Property '{key}' does not exist on object";
            }
        }
    }
}
