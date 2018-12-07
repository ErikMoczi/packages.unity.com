

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Tilemaps;
using Unity.Properties;
using Unity.Tiny.Attributes;
using Object = UnityEngine.Object;

namespace Unity.Tiny
{
    internal sealed class TinyList : IPropertyContainer, IVersionStorage, IVersioned, IEnumerable
    {
        private readonly IRegistry m_Registry;
        public int Version { get; private set; }
        private readonly IVersionStorage m_SharedVersionStorage;

        private int m_CurrentTypeVersion;
        private TinyType.Reference m_Type;

        private static readonly StructValueClassProperty<TinyList, TinyType.Reference> s_TypeProperty =
            new StructValueClassProperty<TinyList, TinyType.Reference>("Type",
                /* GET */ c => c.m_Type,
                /* SET */ (c, v) =>
                {
                    c.m_Type = v;
                    c.m_CurrentTypeVersion = -1;
                },
                /* REF */ (m, p, c, v) => m(p, c, ref c.m_Type, v)
            ).WithAttribute(InspectorAttributes.HideInInspector);

        private static readonly ValueClassProperty<TinyList, int> s_CountProperty =
            new ValueClassProperty<TinyList, int>("Count",
                /* GET */ c => c.m_ItemsProperty?.Count(c) ?? 0,
                /* SET */ (c, v) => c.Resize(v)
                ).WithAttribute(InspectorAttributes.HideInInspector)
                 .WithAttribute(SerializationAttributes.NonSerializedForPersistence)
                 .WithAttribute(PrefabAttributes.CustomField);

        public TinyType.Reference Type
        {
            get => s_TypeProperty.GetValue(this);
            set => s_TypeProperty.SetValue(this, value);
        }

        private IList m_Items;
        private IListClassProperty m_ItemsProperty;

        private readonly PropertyBag m_PropertyBag = new PropertyBag(
            s_TypeProperty,
            s_CountProperty
        );

        public IPropertyBag PropertyBag => m_PropertyBag;
        public IVersionStorage VersionStorage => this;

        public int Count => m_ItemsProperty.Count(this);

        public string Name { get; set; }

        public object this[int index]
        {
            get => m_ItemsProperty.GetObjectAt(this, index);
            set => m_ItemsProperty.SetObjectAt(this, index, value);
        }

        public TinyList(IRegistry registry, TinyType.Reference type, IVersionStorage versionStorage = null, bool refresh = true)
        {
            m_Registry = registry;
            m_Type = type;
            m_SharedVersionStorage = versionStorage;
            
            if (refresh)
            {
                Refresh();
            }
        }

        public IEnumerator GetEnumerator()
        {
            return m_Items?.GetEnumerator() ?? Enumerable.Empty<IList>().GetEnumerator();
        }
        
        public void IncrementVersion<TContainer>(IProperty property, TContainer container)
            where TContainer : IPropertyContainer
        {
            Version++;
            m_SharedVersionStorage?.IncrementVersion(m_ItemsProperty, this);
        }
        
        /// <summary>
        /// Adds an item to the list
        ///
        /// * If the list has no type the type will be inferred from the given object
        /// * If the list has no type and `null` is added the type will be set as `object`
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="Exception">If the given object is not assignable to the list type</exception>
        public void Add(object obj)
        {
            if (null == m_Items)
            {
                // Special case when adding an element and we have no items
                // Dynamically create the list and properties to be strongly typed. We must use activator in this situation
                var type = obj?.GetType() ?? typeof(object);
                m_Items = Activator.CreateInstance(typeof(List<>).MakeGenericType(type)) as IList;
                m_ItemsProperty = CreateItemsProperty(type);
                m_PropertyBag.AddProperty(m_ItemsProperty);
            }
            
            if (obj is TinyObject tinyObject)
            {
                // Special case for tiny object. We DON'T want to retain the given instance.
                // Instead we create a new object and deep copy the values in. This way the object
                // Will propagate version changes to this list
                var v = new TinyObject(m_Registry, m_Type, this);
                v.CopyFrom(tinyObject);
                var typedList = (IListClassProperty<TinyList, TinyObject>) m_ItemsProperty;
                typedList.Add(this, v);
            }
            else
            {
                try
                {
                    var converted = Convert(obj, m_ItemsProperty.ItemType);
                    m_ItemsProperty.AddObject(this, converted);
                }
                catch (Exception e)
                {
                    throw new Exception($"TinyList.Add Type mismatch expected instance of Type=[{m_ItemsProperty.ItemType}] received Type=[{obj?.GetType()}]", e);
                }
            }
        }
        
        // TODO: this belongs in the Property API
        private static object Convert(object v, Type toType)
        {
            switch (v)
            {
                case null:
                    return toType.IsClass ? null : Activator.CreateInstance(toType);
                // handle fake nulls
                case Object unityObject when !unityObject:
                    Assert.IsTrue(toType.IsClass);
                    return null;
            }

            return v.GetType() == toType ? v : System.Convert.ChangeType(v, toType);
        }

        public void RemoveAt(int index)
        {
            m_ItemsProperty?.RemoveAt(this, index);
        }

        public void Clear()
        {
            m_ItemsProperty?.Clear(this);
        }

        public void Refresh(TinyList defaultValue = null, bool skipTypeCheck = false)
        {
            if (null == m_Registry)
            {
                return;
            }
            
            var type = Type.Dereference(m_Registry);

            if (null == type)
            {
                return;
            }

            // Force the type to be refreshed
            if (!skipTypeCheck)
            {
                type.Refresh();
            }

            if (m_CurrentTypeVersion == type.Version)
            {
                return;
            }

            // Migrate the values
            m_Items = MigrateListValue(m_Registry, this, m_Items as IList, type);

            // Rebuild the default value property
            if (null != m_ItemsProperty)
            {
                m_PropertyBag.RemoveProperty(m_ItemsProperty);
            }

            m_ItemsProperty = CreateItemsProperty(type);

            if (null != m_ItemsProperty)
            {
                m_PropertyBag.AddProperty(m_ItemsProperty);
            }

            m_CurrentTypeVersion = type.Version;
        }

        private static IList MigrateListValue(IRegistry registry, IVersionStorage version, IList value, TinyType type)
        {
            var result = TinyType.CreateListInstance(type);
            for (var i = 0; i < value?.Count; i++)
            {
                result.Add(TinyObject.PropertiesContainer.MigrateFieldValue(registry, version, value[i], type, false));
            }

            return result;
        }
        
        private IListClassProperty CreateItemsProperty(Type type)
        {
            var typeCode = System.Type.GetTypeCode(type);
            
            switch (typeCode)
            {
                case TypeCode.Empty:
                case TypeCode.Decimal:                
                case TypeCode.DateTime:
                    break;
                case TypeCode.SByte:
                    return CreateSimpleListProperty<sbyte>();
                case TypeCode.Int16:
                    return CreateSimpleListProperty<short>();
                case TypeCode.Int32:
                    return CreateSimpleListProperty<int>();
                case TypeCode.Int64:
                    return CreateSimpleListProperty<long>();
                case TypeCode.Byte:
                    return CreateSimpleListProperty<byte>();
                case TypeCode.UInt16:
                    return CreateSimpleListProperty<ushort>();
                case TypeCode.UInt32:
                    return CreateSimpleListProperty<uint>();
                case TypeCode.UInt64:
                    return CreateSimpleListProperty<ulong>();
                case TypeCode.Single:
                    return CreateSimpleListProperty<float>();
                case TypeCode.Double:
                    return CreateSimpleListProperty<double>();
                case TypeCode.Boolean:
                    return CreateSimpleListProperty<bool>();
                case TypeCode.Char:
                    return CreateSimpleListProperty<char>();
                case TypeCode.String:
                    return CreateSimpleListProperty<string>();
                case TypeCode.Object:
                {
                    if (typeof(TinyObject) == type)
                    {
                        return CreateContainerListProperty<TinyObject>();
                    }
                    
                    if (typeof(TinyEntity.Reference) == type)
                    {
                        return CreateMutableContainerListProperty<TinyEntity.Reference>();
                    }
                    
                    if (typeof(TinyEnum.Reference) == type)
                    {
                        return CreateMutableContainerListProperty<TinyEnum.Reference>();
                    }

                    if (typeof(Texture2D).IsAssignableFrom(type))
                    {
                        return CreateSimpleListProperty<Texture2D>();
                    }
                    
                    if (typeof(Sprite).IsAssignableFrom(type))
                    {
                        return CreateSimpleListProperty<Sprite>();
                    }

                    if (typeof(Tile).IsAssignableFrom(type))
                    {
                        return CreateSimpleListProperty<Tile>();
                    }

                    if (typeof(Tilemap).IsAssignableFrom(type))
                    {
                        return CreateSimpleListProperty<Tilemap>();
                    }

                    if (typeof(AudioClip).IsAssignableFrom(type))
                    {
                        return CreateSimpleListProperty<AudioClip>();
                    }

                    if (typeof(AnimationClip).IsAssignableFrom(type))
                    {
                        return CreateSimpleListProperty<AnimationClip>();
                    }

                    if (typeof(TMPro.TMP_FontAsset).IsAssignableFrom(type))
                    {
                        return CreateSimpleListProperty<TMPro.TMP_FontAsset>();
                    }

                    if (typeof(Object).IsAssignableFrom(type))
                    {
                        return CreateSimpleListProperty<Object>();
                    }
                    
                    return CreateSimpleListProperty<object>();
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeCode), typeCode, null);
            }

            return null;
        }

        private IListClassProperty CreateItemsProperty(TinyType type)
        {
            switch (type.TypeCode)
            {
                case TinyTypeCode.Unknown:
                    break;
                case TinyTypeCode.Int8:
                    return CreateSimpleListProperty<sbyte>();
                case TinyTypeCode.Int16:
                    return CreateSimpleListProperty<short>();
                case TinyTypeCode.Int32:
                    return CreateSimpleListProperty<int>();
                case TinyTypeCode.Int64:
                    return CreateSimpleListProperty<long>();
                case TinyTypeCode.UInt8:
                    return CreateSimpleListProperty<byte>();
                case TinyTypeCode.UInt16:
                    return CreateSimpleListProperty<ushort>();
                case TinyTypeCode.UInt32:
                    return CreateSimpleListProperty<uint>();
                case TinyTypeCode.UInt64:
                    return CreateSimpleListProperty<ulong>();
                case TinyTypeCode.Float32:
                    return CreateSimpleListProperty<float>();
                case TinyTypeCode.Float64:
                    return CreateSimpleListProperty<double>();
                case TinyTypeCode.Boolean:
                    return CreateSimpleListProperty<bool>();
                case TinyTypeCode.String:
                    return CreateSimpleListProperty<string>();
                case TinyTypeCode.Configuration:
                case TinyTypeCode.Component:
                case TinyTypeCode.Struct:
                {
                    return CreateContainerListProperty<TinyObject>();
                }
                case TinyTypeCode.Enum:
                    return CreateMutableContainerListProperty<TinyEnum.Reference>();
                case TinyTypeCode.EntityReference:
                    return CreateSimpleListProperty<TinyEntity.Reference>();
                case TinyTypeCode.UnityObject:

                    if (type.Id == TinyType.Texture2DEntity.Id)
                    {
                        return CreateSimpleListProperty<Texture2D>();
                    }
                    else if (type.Id == TinyType.SpriteEntity.Id)
                    {
                        return CreateSimpleListProperty<Sprite>();
                    }
                    else if (type.Id == TinyType.TileEntity.Id)
                    {
                        return CreateSimpleListProperty<Tile>();
                    }
                    else if (type.Id == TinyType.TilemapEntity.Id)
                    {
                        return CreateSimpleListProperty<Tilemap>();
                    }
                    else if (type.Id == TinyType.AudioClipEntity.Id)
                    {
                        return CreateSimpleListProperty<AudioClip>();
                    }
                    else if (type.Id == TinyType.AnimationClipEntity.Id)
                    {
                        return CreateSimpleListProperty<AnimationClip>();
                    }
                    else if (type.Id == TinyType.FontEntity.Id)
                    {
                        return CreateSimpleListProperty<TMPro.TMP_FontAsset>();
                    }
                    else
                    {
                        return CreateSimpleListProperty<Object>();
                    }
                    
                default:
                    throw new ArgumentOutOfRangeException(nameof(type.TypeCode), type.TypeCode, null);
            }

            return null;
        }

        private IListClassProperty CreateSimpleListProperty<TValue>()
        {
            return new ValueListClassProperty<TinyList, TValue>("Items",
                container => container.m_Items as IList<TValue>);
        }
        
        private IListClassProperty CreateContainerListProperty<TValue>() 
            where TValue : class, IPropertyContainer
        {
            return new ClassListClassProperty<TinyList, TValue>("Items",
                container => container.m_Items as IList<TValue>,
                container => new TinyObject(m_Registry, m_Type, this) as TValue);
        }
        
        private IListClassProperty CreateMutableContainerListProperty<TValue>() 
            where TValue : struct, IPropertyContainer
        {
            return new StructListClassProperty<TinyList, TValue>("Items",
                container => container.m_Items as IList<TValue>,
                container =>
                {
                    if (typeof(TValue) == typeof(TinyObject))
                    {
                        return (TValue)(object) new TinyObject(m_Registry, m_Type);
                    }
                    else if (typeof(TValue) == typeof(TinyEnum.Reference))
                    {
                        var type = m_Type.Dereference(m_Registry);
                        var id = TinyId.Empty;
                        if (type.Fields.Count > 0)
                        {
                            id = type.Fields[0].Id;
                        }
                        return (TValue)(object)new TinyEnum.Reference(m_Type.Dereference(m_Registry), id);
                    }
                    return default(TValue);
                });
        }

        public void CopyFrom(TinyList other)
        {
            if (ReferenceEquals(this, other))
            {
                return;
            }
            
            Clear();

            Type = other.Type;
            foreach (var item in other)
            {
                Add(item);
            }
        }
        
        private void Resize(int size)
        {
            if (m_Items == null)
            {
                throw new NullReferenceException(nameof(m_Items));
            }

            if (size < 0)
            {
                throw new ArgumentException(nameof(size));
            }

            while (size < m_Items.Count)
            {
                m_Items.RemoveAt(size);
            }
            
            while (size > m_Items.Count)
            {
                m_Items.Add(TinyType.CreateInstance(m_Type.Dereference(m_Registry)));
            }
        }

    }
}

