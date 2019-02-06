

using System;
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    internal sealed partial class TinyProject : TinyRegistryObjectBase, IPersistentObject
    {
        public const int CurrentSerializedVersion = 7;
        public const string MainProjectName = "Main";
        
        static partial void InitializeCustomProperties()
        {
            TypeIdProperty = new ValueClassProperty<TinyProject, TinyTypeId>("$TypeId",
                    c => TinyTypeId.Project,
                    null
                ).WithAttribute(InspectorAttributes.HideInInspector)
                 .WithAttribute(InspectorAttributes.Readonly);
            
            PersistenceIdProperty
                .WithAttribute(SerializationAttributes.Transient)
                .WithAttribute(SerializationAttributes.NonSerializedForPersistence);

            SerializedVersionProperty
                .WithAttribute(SerializationAttributes.Transient);

            LastSerializedVersionProperty
                .WithAttribute(SerializationAttributes.Transient)
                .WithAttribute(SerializationAttributes.NonSerializedForPersistence);
            
            NameProperty
                .WithAttribute(InspectorAttributes.HideInInspector)
                .WithAttribute(InspectorAttributes.Readonly)
                .WithAttribute(SerializationAttributes.Transient);
        }
        
        public override IPropertyBag PropertyBag => s_PropertyBag;

        public override string Name
        {
            get { return NameProperty.GetValue(this); }
            set { NameProperty.SetValue(this, value); }
        }

        public Reference Ref => (Reference)this;

        public static IPropertyContainer Migrate(IPropertyContainer container, IRegistry registry)
        {
            var version = container.GetValueOrDefault("SerializedVersion", 0);
            
            var migration = new MigrationContainer(container);
            
            migration.CreateOrSetValue("LastSerializedVersion", version);
            
            if (version == CurrentSerializedVersion)
            {
                return migration;
            }

            migration.CreateOrSetValue("SerializedVersion", CurrentSerializedVersion);

            return migration;
        }

        public IEnumerable<IPersistentObject> EnumeratePersistentDependencies()
        {
            var module = Module.Dereference(Registry);

            foreach (var obj in module.EnumeratePersistentDependencies())
            {
                yield return obj;
            }
        }

        public IEnumerable<IPropertyContainer> EnumerateContainers()
        {
            yield return this;
            
            var module = Module.Dereference(Registry);
            if (module != null)
            {
                foreach (var c in module.EnumerateContainers())
                {
                    yield return c;
                }
            }

            var configuration = Configuration.Dereference(Registry);
            if (configuration != null)
            {
                yield return configuration;
            }
        }
        
        public TinyProject(IRegistry registry, IVersionStorage versionStorage) : base(registry, versionStorage)
        {
            m_Settings = new TinyProjectSettings(this);
        }

        public override void IncrementVersion<TContainer>(IProperty property, TContainer container)
        {
            Version++;
            if (container is TinyProjectSettings)
            {
                SharedVersionStorage?.IncrementVersion(SettingsProperty, this);
            }
            else if (container is TinyModule.Reference)
            {
                SharedVersionStorage?.IncrementVersion(ModuleProperty, this);
            }
            else
            {
                SharedVersionStorage?.IncrementVersion(property, this);
            }
        }

        /// <inheritdoc cref="IPropertyContainer"/>
        /// <summary>
        /// Weak reference to a project
        /// </summary>
        public partial struct Reference : IReference<TinyProject>, IEquatable<Reference>
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

            public TinyProject Dereference(IRegistry registry)
            {
                return registry.Dereference<Reference, TinyProject>(this);
            }

            public static explicit operator Reference(TinyProject @object)
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

