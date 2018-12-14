

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;

namespace Unity.Tiny
{
    internal delegate void TinyEntityComponentEventHandler(TinyEntity entity, TinyObject component);

    /// <inheritdoc cref="TinyRegistryObjectBase" />
    /// <summary>
    /// </summary>
    internal sealed partial class TinyEntity : TinyRegistryObjectBase
    {
        public sealed class ComponentListProperty : ClassListClassProperty<TinyEntity, TinyObject>
        {
            public ComponentListProperty(string name, GetListMethod getList, CreateInstanceMethod createInstance = null) : base(name, getList, createInstance)
            {
            }
        }
        
        public static ComponentListProperty ComponentsProperty { get; private set; }

        static partial void InitializeCustomProperties()
        {
            TypeIdProperty = new ValueClassProperty<TinyEntity, TinyTypeId>(
                    "$TypeId",
                    c => TinyTypeId.Entity,
                    null
                )
                .WithAttribute(InspectorAttributes.HideInInspector)
                .WithAttribute(InspectorAttributes.Readonly);
            
            NameProperty
                .WithAttribute(InspectorAttributes.HideInInspector)
                .WithAttribute(InspectorAttributes.Readonly);
            
            EnabledProperty
                .WithAttribute(InspectorAttributes.HideInInspector);
            
            LayerProperty
                .WithAttribute(InspectorAttributes.HideInInspector);
            
            StaticProperty
                .WithAttribute(InspectorAttributes.HideInInspector);
            
            InstanceProperty
                .WithAttribute(InspectorAttributes.HideInInspector);
            
            ComponentsProperty = new ComponentListProperty(
                nameof(Components),
                c => c.m_Components ?? (c.m_Components = new List<TinyObject>()),
                c =>
                {
                    var component = new TinyObject(c.Registry, (TinyType.Reference) TinyType.NewComponentType, c, true);
                    return component;
                })
                .WithAttribute(InspectorAttributes.DontList);
            
            EntityGroupProperty
                .WithAttribute(SerializationAttributes.NonSerializedForPersistence)
                .WithAttribute(InspectorAttributes.HideInInspector);
        }

        public override IPropertyBag PropertyBag => s_PropertyBag;
        
        private List<TinyObject> m_Components;

        public override string Name
        {
            get { return NameProperty.GetValue(this); }
            set { NameProperty.SetValue(this, value); }
        }

        public Reference Ref => (Reference)this;

        public TinyEntityGroup EntityGroup
        {
            get => EntityGroupProperty.GetValue(this).Dereference(Registry);
            set => EntityGroupProperty.SetValue(this, value.Ref);
        }

        internal TinyEntityView View { get; set; }

        public PropertyList<TinyEntity, TinyObject> Components => new PropertyList<TinyEntity, TinyObject>(ComponentsProperty, this);

        internal event TinyEntityComponentEventHandler OnComponentAdded;
        internal event TinyEntityComponentEventHandler OnComponentRemoved;

        public TinyEntity(IRegistry registry, IVersionStorage versionStorage) : base(registry, versionStorage)
        {
        }

        public override void IncrementVersion<TContainer>(IProperty property, TContainer container)
        {
            Version++;
            if (!ReferenceEquals(container, this))
            {
                SharedVersionStorage.IncrementVersion(ComponentsProperty, this);
            }
            else
            {
                SharedVersionStorage.IncrementVersion(property, container);
            }
        }

        public TinyObject AddComponent(TinyType.Reference type)
        {
            var component = ComponentsProperty.AddNew(this);
            component.Type = type;
            component.Refresh();

            // Implicitly support prefab instances
            if (null != m_Instance)
            {
                if (m_Instance.RemovedComponents.Contains(type))
                {
                    m_Instance.RemovedComponents.Remove(type);
                } 
                
                Registry?.Context?.GetManager<PrefabManager>()?.ApplyComponentModifications(m_Instance.Modifications, component);
            }

            OnComponentAdded?.Invoke(this, component);
            return component;
        }
        
        public TinyObject AddComponent(TinyType.Reference type, int index)
        {
            var component = AddComponent(type);
            if (index >= 0 && index < ComponentsProperty.Count(this))
            {
                ComponentsProperty.Remove(this, component);
                ComponentsProperty.Insert(this, index, component);
            }

            return component;
        }
        
        public TinyObject GetOrAddComponent(TinyType.Reference type)
        {
            var component = GetComponent(type) ?? AddComponent(type);
            return component;
        }
        
        public TinyObject GetOrAddComponent(TinyType.Reference type, int index)
        {
            var component = GetComponent(type) ?? AddComponent(type, index);
            return component;
        }

        public TinyObject GetComponent(TinyType.Reference type)
        {
            for(int i = 0; i < Components.Count; ++i)
            {
                var component = Components[i];
                if (component.Type.Equals(type))
                {
                    return component;
                }
            }
            return null;
        }

        public bool HasComponent(TinyType.Reference type)
        {
            return null != GetComponent(type);
        }

        public int GetComponentIndex(TinyType.Reference type)
        {
            var components = Components;
            var count = components.Count;
            
            for (var i = 0; i < count; ++i)
            {
                if (components[i].Type.Equals(type))
                {
                    return i;
                }
            }

            return -1;
        }

        public TinyObject RemoveComponent(TinyType.Reference type)
        {
            var component = Components.FirstOrDefault(o => o.Type.Equals(type));
            
            // Implicitly support prefab instances
            if (null != m_Instance)
            {
                var source = m_Instance.Source.Dereference(Registry);

                if (source.HasComponent(type))
                {
                    if (!m_Instance.RemovedComponents.Contains(type))
                    {
                        m_Instance.RemovedComponents.Add(type);
                    }
                }
            }
            
            if (null != component)
            {
                // @TODO move to properties
                var e = OnComponentRemoved;
                e?.Invoke(this, component);
                var self = this;
                ComponentsProperty.Remove(self, component);
            }
            return component;
        }

        /// <inheritdoc cref="IPropertyContainer"/>
        /// <summary>
        /// Weak reference to an entity
        /// </summary>
        public partial struct Reference : IReference<TinyEntity>, IEquatable<Reference>
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

            public TinyEntity Dereference(IRegistry registry)
            {
                return registry.Dereference<Reference, TinyEntity>(this);
            }

            public static explicit operator Reference(TinyEntity @object)
            {
                return null == @object ? None : new Reference(@object.Id, @object.Name);
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

