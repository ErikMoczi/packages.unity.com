using System;
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    /// <summary>
    /// A prefab instance holds a reference to the source prefab (EntityGroup) and each entity instance from that group
    /// This container is used to perform instance fix-ups and as a quick lookup for prefab hierarchies
    /// </summary>
    internal sealed class TinyPrefabInstance : TinyRegistryObjectBase
    {
        private static class Properties
        {
            private static readonly ValueClassProperty<TinyPrefabInstance, TinyTypeId> TypeId = new ValueClassProperty<TinyPrefabInstance, TinyTypeId>(
                "$TypeId",
                c => TinyTypeId.PrefabInstance,
                null
            );
            
            public static readonly ValueClassProperty<TinyPrefabInstance, string> Name = new ValueClassProperty<TinyPrefabInstance, string>(
                nameof(Name),
                c => c.m_Name,
                (c, v) => c.m_Name = v
            );
            
            public static readonly StructValueClassProperty<TinyPrefabInstance, TinyEntity.Reference> Parent = new StructValueClassProperty<TinyPrefabInstance, TinyEntity.Reference>(
                nameof(Parent),
                c => c.m_Parent,
                (c, v) => c.m_Parent = v,
                (m, p, c, v) => m(p, c, ref c.m_Parent, v)
            );
            
            public static readonly StructValueClassProperty<TinyPrefabInstance, TinyEntityGroup.Reference> Source = new StructValueClassProperty<TinyPrefabInstance, TinyEntityGroup.Reference>(
                nameof(Source),
                c => c.m_Source,
                (c, v) => c.m_Source = v,
                (m, p, c, v) => m(p, c, ref c.m_Source, v)
            );
            
            public static readonly StructValueClassProperty<TinyPrefabInstance, TinyEntityGroup.Reference> Group = new StructValueClassProperty<TinyPrefabInstance, TinyEntityGroup.Reference>(
                nameof(Group),
                c => c.m_Group,
                (c, v) => c.m_Group = v,
                (m, p, c, v) => m(p, c, ref c.m_Group, v)
            );
            
            public static readonly StructListClassProperty<TinyPrefabInstance, TinyEntity.Reference> Entities = new StructListClassProperty<TinyPrefabInstance, TinyEntity.Reference>(
                nameof(Entities),
                c => c.m_Entities,
                c => new TinyEntity.Reference()
            );
            
            public static readonly ClassPropertyBag<TinyPrefabInstance> PropertyBag = new ClassPropertyBag<TinyPrefabInstance>(
                TypeId,
                IdProperty,
                Name,
                Source, 
                Group,
                Parent,
                Entities
            );

            static Properties()
            {
                Group.AddAttribute(SerializationAttributes.NonSerializedForPersistence);
            }
        }

        public override IPropertyBag PropertyBag => Properties.PropertyBag;

        private string m_Name;
        private TinyEntityGroup.Reference m_Source;
        private TinyEntityGroup.Reference m_Group;
        private TinyEntity.Reference m_Parent;
        
        private readonly IList<TinyEntity.Reference> m_Entities = new List<TinyEntity.Reference>();
        
        public Reference Ref => (Reference) this;
        
        public override string Name
        {
            get => Properties.Name.GetValue(this);
            set => Properties.Name.SetValue(this, value);
        }
        
        /// <summary>
        /// Source EntityGroup that this prefab was spawned from
        /// </summary>
        public TinyEntityGroup.Reference PrefabEntityGroup
        {
            get => Properties.Source.GetValue(this);
            set => Properties.Source.SetValue(this, value);
        }

        /// <summary>
        /// The entity group that this instance belongs to
        /// </summary>
        internal TinyEntityGroup.Reference EntityGroup
        {
            get => Properties.Group.GetValue(this);
            set => Properties.Group.SetValue(this, value);
        }
        
        /// <summary>
        /// The parent entity for this entity group
        /// </summary>
        internal TinyEntity.Reference Parent
        {
            get => Properties.Parent.GetValue(this);
            set => Properties.Parent.SetValue(this, value);
        }

        /// <summary>
        /// List of entities in the current group that are instances of this prefab
        /// </summary>
        public PropertyList<TinyPrefabInstance, TinyEntity.Reference> Entities => new PropertyList<TinyPrefabInstance, TinyEntity.Reference>(Properties.Entities, this);

        public TinyPrefabInstance(IRegistry registry, IVersionStorage versionStorage) : base(registry, versionStorage)
        {
        }
        
        /// <inheritdoc cref="IPropertyContainer"/>
        /// <summary>
        /// Weak reference to an entity
        /// </summary>
        public struct Reference : IStructPropertyContainer<Reference>, IReference<TinyPrefabInstance>, IEquatable<Reference>
        {
            // ReSharper disable once MemberHidesStaticFromOuterClass
            private static class Properties
            {
                public static readonly ValueStructProperty<Reference, TinyId> Id = new ValueStructProperty<Reference, TinyId>(
                    "Id"
                    ,(ref Reference c) => c.m_Id
                    ,(ref Reference c, TinyId v) => c.m_Id = v
                )
                .WithAttribute(InspectorAttributes.HideInInspector)
                .WithAttribute(InspectorAttributes.Readonly);
                    
                public static readonly ValueStructProperty<Reference, string> Name = new ValueStructProperty<Reference, string>(
                    "Name"
                    ,(ref Reference c) => c.m_Name
                    ,(ref Reference c, string v) => c.m_Name = v
                );
                
                public static readonly StructPropertyBag<Reference> PropertyBag = new StructPropertyBag<Reference>(
                    Id,
                    Name
                );
            }
            
            public IPropertyBag PropertyBag => Properties.PropertyBag;
            public IVersionStorage VersionStorage => null;
            
            private TinyId m_Id;
            private string m_Name;

            public TinyId Id
            {
                get => Properties.Id.GetValue(ref this);
                set => Properties.Id.SetValue(ref this, value);
            }

            public string Name
            {
                get => Properties.Name.GetValue(ref this);
                set => Properties.Name.SetValue(ref this, value);
            }

            public static Reference None { get; } = new Reference(TinyId.Empty, string.Empty);
            
            public Reference(TinyId id, string name)
            {
                m_Id = id;
                m_Name = name;
            }

            public TinyPrefabInstance Dereference(IRegistry registry)
            {
                return registry.Dereference<Reference, TinyPrefabInstance>(this);
            }

            public static explicit operator Reference(TinyPrefabInstance @object)
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
                return obj is Reference reference && Equals(reference);
            }

            public override int GetHashCode()
            {
                return m_Id.GetHashCode();
            }
            
            public void MakeRef<TContext>(ByRef<Reference, TContext> byRef, TContext context)
            {
                byRef(ref this, context);
            }

            public TReturn MakeRef<TContext, TReturn>(ByRef<Reference, TContext, TReturn> byRef, TContext context)
            {
                return byRef(ref this, context);
            }
        }
    }
}