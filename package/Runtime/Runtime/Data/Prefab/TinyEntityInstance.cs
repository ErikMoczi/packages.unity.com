using System;
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    [Flags]
    internal enum EntityModificationFlags
    {
        Enabled = 1,
        Name = 2,
        Layer = 4,
        Static = 8,
        
        All = Enabled | Name | Layer | Static
    }

    internal static class EntityModificationFlagExtensions
    {
        public static IEnumerable<EntityModificationFlags> EnumerateFlags(this EntityModificationFlags flags)
        {
            if (flags.HasFlag(EntityModificationFlags.Enabled))
            {
                yield return EntityModificationFlags.Enabled;
            }
            
            if (flags.HasFlag(EntityModificationFlags.Name))
            {
                yield return EntityModificationFlags.Name;
            }
            
            if (flags.HasFlag(EntityModificationFlags.Layer))
            {
                yield return EntityModificationFlags.Layer;
            }
            
            if (flags.HasFlag(EntityModificationFlags.Static))
            {
                yield return EntityModificationFlags.Static;
            }
        }
    }

    /// <summary>
    /// An entity instance holds a reference to the source prefab entity
    /// This is used to track per entity modifications from the an original source entity
    /// </summary>
    internal sealed class TinyEntityInstance : IPropertyContainer
    {
        private static class Properties
        {
            public static readonly StructValueClassProperty<TinyEntityInstance, TinyPrefabInstance.Reference> PrefabInstance = new StructValueClassProperty<TinyEntityInstance, TinyPrefabInstance.Reference>(
                nameof(PrefabInstance),
                c => c.m_PrefabInstance,
                (c, v) => c.m_PrefabInstance = v,
                (m, p, c, v) => m(p, c, ref c.m_PrefabInstance, v)
            );
            
            public static readonly StructValueClassProperty<TinyEntityInstance, TinyEntity.Reference> Source = new StructValueClassProperty<TinyEntityInstance, TinyEntity.Reference>(
                nameof(Source),
                c => c.m_Source,
                (c, v) => c.m_Source = v,
                (m, p, c, v) => m(p, c, ref c.m_Source, v)
            );
            
            public static readonly ValueClassProperty<TinyEntityInstance, int> EntityModificationFlags = new ValueClassProperty<TinyEntityInstance, int>(
                nameof(EntityModificationFlags),
                c => (int) c.m_EntityModificationFlags,
                (c, v) => c.m_EntityModificationFlags = (EntityModificationFlags) v
            );

            public static readonly ClassListClassProperty<TinyEntityInstance, IPropertyModification> Modifications = new ClassListClassProperty<TinyEntityInstance, IPropertyModification>(
                nameof(Modifications),
                c => c.m_Modifications
            );

            public static readonly StructListClassProperty<TinyEntityInstance, TinyType.Reference> RemovedComponents = new StructListClassProperty<TinyEntityInstance, TinyType.Reference>(
                nameof(RemovedComponents),
                c => c.m_RemovedComponents
            );

            public static readonly ClassPropertyBag<TinyEntityInstance> PropertyBag = new ClassPropertyBag<TinyEntityInstance>(
                PrefabInstance,
                Source,
                EntityModificationFlags,
                Modifications,
                RemovedComponents
            );

            static Properties()
            {
                PrefabInstance.AddAttribute(SerializationAttributes.NonSerializedForPersistence);
            }
        }

        public IVersionStorage VersionStorage { get; }
        public IPropertyBag PropertyBag => Properties.PropertyBag;

        private TinyPrefabInstance.Reference m_PrefabInstance;
        private TinyEntity.Reference m_Source;
        private EntityModificationFlags m_EntityModificationFlags = 0;
        private readonly List<IPropertyModification> m_Modifications = new List<IPropertyModification>();
        private readonly List<TinyType.Reference> m_RemovedComponents = new List<TinyType.Reference>();

        /// <summary>
        /// The instance that this entity belongs to
        ///
        /// Used for high level operations on the prefab as a whole (e.g. duplication, remapping etc)
        /// </summary>
        public TinyPrefabInstance.Reference PrefabInstance
        {
            get => Properties.PrefabInstance.GetValue(this);
            set => Properties.PrefabInstance.SetValue(this, value);
        }
        
        /// <summary>
        /// Reference to the specific entity of the `Prefab` that this entity is an instance of
        /// </summary>
        public TinyEntity.Reference Source
        {
            get => Properties.Source.GetValue(this);
            set => Properties.Source.SetValue(this, value);
        }
        
        public EntityModificationFlags EntityModificationFlags
        {
            get => (EntityModificationFlags) Properties.EntityModificationFlags.GetValue(this);
            set => Properties.EntityModificationFlags.SetValue(this, (int) value);
        }

        public PropertyList<TinyEntityInstance, IPropertyModification> Modifications => new PropertyList<TinyEntityInstance, IPropertyModification>(Properties.Modifications, this);
        public PropertyList<TinyEntityInstance, TinyType.Reference> RemovedComponents => new PropertyList<TinyEntityInstance, TinyType.Reference>(Properties.RemovedComponents, this);

        public TinyEntityInstance(IVersionStorage versionStorage)
        {
            VersionStorage = versionStorage;
        }
        
        public void SetModification<TValue>(TinyType.Reference component, string path, TValue value)
        {
            // @TODO Optimize with a hash lookup
            for (var i = 0; i < m_Modifications.Count; i++)
            {
                var modification = m_Modifications[i];

                if (!Equals(modification.Target, component) || !string.Equals(modification.Path, path))
                {
                    continue;
                }

                if (modification.TypeCode == PropertyModificationConverter.GetSerializedTypeId(typeof(TValue)))
                {
                    modification.Value = value;
                    m_Modifications[i] = modification;
                    return;
                }
                
                // The type has changed in some way, remove it and re-add it
                m_Modifications.RemoveAt(i);
                break;
            }

            if (typeof(IPropertyContainer).IsAssignableFrom(typeof(TValue)))
            {
                m_Modifications.Add(new ContainerPropertyModification
                {
                    Target = component,
                    Path = path,
                    Value = value as IPropertyContainer
                });
            }
            else
            {
                m_Modifications.Add(new ValuePropertyModification<TValue>
                {
                    Target = component,
                    Path = path,
                    Value = value
                });
            }
        }

        internal void CopyFrom(TinyEntityInstance other)
        {
            Source = other.Source;
            PrefabInstance = other.PrefabInstance;

            EntityModificationFlags = other.EntityModificationFlags;
            
            Modifications.Clear();
            foreach (var modification in other.Modifications)
            {
                // @HACK
                // this is a workaround to force a new instance creation of the modification
                Modifications.Add(PropertyModificationConverter.Convert(modification));
            }
            
            RemovedComponents.Clear();
            foreach (var removed in other.RemovedComponents)
            {
                RemovedComponents.Add(removed);
            }
        }
    }
}