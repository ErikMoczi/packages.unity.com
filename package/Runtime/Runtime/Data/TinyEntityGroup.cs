

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;

namespace Unity.Tiny
{
    internal sealed partial class TinyEntityGroup : TinyRegistryObjectBase, IPersistentObject
    {
        public static StructListClassProperty<TinyEntityGroup, TinyEntity.Reference> EntitiesProperty { get; set; }
        public static StructListClassProperty<TinyEntityGroup, TinyPrefabInstance.Reference> PrefabInstancesProperty { get; set; }

        static partial void InitializeCustomProperties()
        {
            TypeIdProperty = new ValueClassProperty<TinyEntityGroup, TinyTypeId>(
                    "$TypeId",
                    c => TinyTypeId.EntityGroup,
                    null
                )
                .WithAttribute(InspectorAttributes.HideInInspector)
                .WithAttribute(InspectorAttributes.Readonly);
            
            NameProperty
                .WithAttribute(InspectorAttributes.HideInInspector)
                .WithAttribute(InspectorAttributes.Readonly)
                .WithAttribute(SerializationAttributes.Transient);
            
            PersistenceIdProperty
                .WithAttribute(SerializationAttributes.Transient)
                .WithAttribute(SerializationAttributes.NonSerializedForPersistence);

            EntitiesProperty = new StructListClassProperty<TinyEntityGroup, TinyEntity.Reference>(
                "Entities",
                c => c.m_Entities ?? (c.m_Entities = new List<TinyEntity.Reference>())
            );
            
            PrefabInstancesProperty = new StructListClassProperty<TinyEntityGroup, TinyPrefabInstance.Reference>(
                "PrefabInstances",
                c => c.m_PrefabInstances
            );
        }
        
        public override IPropertyBag PropertyBag => s_PropertyBag;
        
        private List<TinyEntity.Reference> m_Entities;
        private readonly List<TinyPrefabInstance.Reference> m_PrefabInstances = new List<TinyPrefabInstance.Reference>();
        
        public override string Name
        {
            get => NameProperty.GetValue(this);
            set => NameProperty.SetValue(this, value);
        }

        public Reference Ref => (Reference)this;

        public PropertyList<TinyEntityGroup, TinyEntity.Reference> Entities => new PropertyList<TinyEntityGroup, TinyEntity.Reference>(EntitiesProperty, this);
        public PropertyList<TinyEntityGroup, TinyPrefabInstance.Reference> PrefabInstances => new PropertyList<TinyEntityGroup, TinyPrefabInstance.Reference>(PrefabInstancesProperty, this);

        public TinyEntityGroup(IRegistry registry, IVersionStorage versionStorage)
            : base(registry, versionStorage)
        {
        }

        public IEnumerable<IPropertyContainer> EnumerateContainers()
        {
            yield return this;
            
            foreach (var @ref in PrefabInstances)
            {
                var i = @ref.Dereference(Registry);
                if (null == i) continue;
                yield return i;
            }
            
            foreach (var @ref in Entities)
            {
                var e = @ref.Dereference(Registry);
                if (null == e) continue;
                yield return e;
            }
        }

        public void AddEntityReference(TinyEntity.Reference entity)
        {
            EntitiesProperty.Add(this, entity);
        }

        public void RemoveEntityReference(TinyEntity.Reference entity)
        {
            EntitiesProperty.Remove(this, entity);
        }

        public void ClearEntityReferences()
        {
            EntitiesProperty.Clear(this);
        }

        public override void Refresh()
        {
            if (null == m_Entities)
            {
                return;
            }

            for (var i = 0; i < m_Entities.Count; i++)
            {
                var s = m_Entities[i].Dereference(Registry);
                if (null != s)
                {
                    m_Entities[i] = (TinyEntity.Reference) s;
                }
            }
        }

        public override void Restore(IMemento memento)
        {
            base.Restore(memento);
            (Registry.FindById(Id) as TinyEntityGroup)?.ValidateEntities();
        }

        private void ValidateEntities()
        {
            if (Entities.Deref(Registry).All(e => e.EntityGroup == this))
            {
                return;
            }
            
            using (Registry.DontTrackChanges())
            {
                var entitiesToRemove = ListPool<TinyEntity.Reference>.Get();
                
                try
                {
                    entitiesToRemove.AddRange(Entities.Deref(Registry).Where(e => e.EntityGroup != this).Select(e => e.Ref));

                    foreach (var reference in entitiesToRemove)
                    {
                        RemoveEntityReference(reference);
                    }
                }
                finally
                {
                    ListPool<TinyEntity.Reference>.Release(entitiesToRemove);
                }
            }
        }

        /// <inheritdoc cref="IPropertyContainer"/>
        /// <summary>
        /// Weak reference to an entity group
        /// </summary>
        public partial struct Reference : IReference<TinyEntityGroup>, IEquatable<Reference>
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

            public TinyEntityGroup Dereference(IRegistry registry)
            {
                return registry.Dereference<Reference, TinyEntityGroup>(this);
            }

            public static explicit operator Reference(TinyEntityGroup @object)
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

