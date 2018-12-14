

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace Unity.Tiny
{
    [Flags]
    internal enum TinyModuleOptions
    {
        None = 0,

        /// <summary>
        /// This module should not be exposed in the editor
        /// </summary>
        ReadOnly = 1 << 0,

        /// <summary>
        /// This module MUST be included in ALL projects
        /// </summary>
        Required = 1 << 1,

        /// <summary>
        /// This module is the main module for a project
        /// </summary>
        ProjectModule = 1 << 2
    }

    /// <summary>
    /// A module can be thought of as a .csharp file
    /// It should define collections of included types, entityGroups, systems etc.
    /// It currently references component and struct types
    /// </summary>
    internal sealed partial class TinyModule : TinyRegistryObjectBase, IPersistentObject
    {
        public const int CurrentSerializedVersion = 1;

        static partial void InitializeCustomProperties()
        {
            TypeIdProperty = new ValueClassProperty<TinyModule, TinyTypeId>("$TypeId",
                c => TinyTypeId.Module,
                null
            ).WithAttribute(InspectorAttributes.HideInInspector)
             .WithAttribute(InspectorAttributes.Readonly);
            
            PersistenceIdProperty
                .WithAttribute(SerializationAttributes.Transient)
                .WithAttribute(SerializationAttributes.NonSerializedForPersistence);
            
            NameProperty
                .WithAttribute(InspectorAttributes.HideInInspector)
                .WithAttribute(InspectorAttributes.Readonly);

            SerializedVersionProperty
                .WithAttribute(SerializationAttributes.Transient);
            
            MetadataFileGUIDProperty
                .WithAttribute(InspectorAttributes.HideInInspector)
                .WithAttribute(InspectorAttributes.Readonly);

            AssetsProperty = new ClassListClassProperty<TinyModule, TinyAsset>(
                "Assets",
                c => c.m_Assets,
                c => new TinyAsset(c)
            );

            EntityGroupsProperty
                .WithAttribute(SerializationAttributes.Transient)
                .WithAttribute(SerializationAttributes.NonSerializedForPersistence);
            
            ComponentsProperty
                .WithAttribute(SerializationAttributes.Transient)
                .WithAttribute(SerializationAttributes.NonSerializedForPersistence);
            
            StructsProperty
                .WithAttribute(SerializationAttributes.Transient)
                .WithAttribute(SerializationAttributes.NonSerializedForPersistence);
            
            ConfigurationsProperty
                .WithAttribute(SerializationAttributes.Transient)
                .WithAttribute(SerializationAttributes.NonSerializedForPersistence);
            
            EnumsProperty
                .WithAttribute(SerializationAttributes.Transient)
                .WithAttribute(SerializationAttributes.NonSerializedForPersistence);
        }

        public override IPropertyBag PropertyBag => s_PropertyBag;

        public bool IsReadOnly => (Options & TinyModuleOptions.ReadOnly) != 0;
        public bool IsRequired => (Options & TinyModuleOptions.Required) != 0;
        public bool IsProjectModule => (Options & TinyModuleOptions.ProjectModule) != 0;
        
        public override string Name
        {
            get { return NameProperty.GetValue(this); }
            set { NameProperty.SetValue(this, value); }
        }

        public Reference Ref => (Reference)this;
        
        public PropertyList<TinyModule, Reference> Dependencies => new PropertyList<TinyModule, Reference>(DependenciesProperty, this);
        public PropertyList<TinyModule, TinyType.Reference> Configurations => new PropertyList<TinyModule, TinyType.Reference>(ConfigurationsProperty, this);
        public PropertyList<TinyModule, TinyType.Reference> Components => new PropertyList<TinyModule, TinyType.Reference>(ComponentsProperty, this);
        public PropertyList<TinyModule, TinyType.Reference> Structs => new PropertyList<TinyModule, TinyType.Reference>(StructsProperty, this);
        public PropertyList<TinyModule, TinyType.Reference> Enums => new PropertyList<TinyModule, TinyType.Reference>(EnumsProperty, this);

        public IEnumerable<TinyType.Reference> Types
        {
            get
            {
                foreach (var r in Configurations)
                {
                    yield return r;
                }

                foreach (var r in Components)
                {
                    yield return r;
                }

                foreach (var r in Structs)
                {
                    yield return r;
                }

                foreach (var r in Enums)
                {
                    yield return r;
                }
            }
        }

        public PropertyList<TinyModule, TinyEntityGroup.Reference> EntityGroups => new PropertyList<TinyModule, TinyEntityGroup.Reference>(EntityGroupsProperty, this);
        public PropertyList<TinyModule, TinyAsset> Assets => new PropertyList<TinyModule, TinyAsset>(AssetsProperty, this);
        
        public TinyEntityGroup.Reference StartupEntityGroup
        {
            get { return StartupEntityGroupProperty.GetValue(this); }
            set { StartupEntityGroupProperty.SetValue(this, value); }
        }

        public TinyModule(IRegistry registry, IVersionStorage versionStorage) : base(registry, versionStorage)
        {
        }
        
        public static IPropertyContainer Migrate(IPropertyContainer container, IRegistry registry)
        {
            var version = container.GetValueOrDefault("SerializedVersion", 0);
            
            if (version == CurrentSerializedVersion)
            {
                return container;
            }

            var migration = new MigrationContainer(container);

            if (version == 0)
            {
                // TinyScript object was deprecated and replaced with a simple UnityEngine.TextAsset reference
                if (migration.HasProperty("Scripts"))
                {
                    var scripts = migration.GetContainerList("Scripts");
                    
                    // Clear the list and allow the post accept step to re-populate with migrated scripts
                    // See CommandStream.FrontEnd.MigrateLegacyScripts
                    scripts.Clear();
                } 
            }
            
            migration.CreateOrSetValue("SerializedVersion", CurrentSerializedVersion);
            
            return migration;
        }

        /// <summary>
        /// Enumerates root persistent objects that may reside in separate files
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPersistentObject> EnumeratePersistentDependencies()
        {
            foreach (var r in Types)
            {
                yield return r.Dereference(Registry);
            }
            
            foreach (var r in EntityGroups)
            {
                yield return r.Dereference(Registry);
            }
        }

        public IEnumerable<IPropertyContainer> EnumerateContainers()
        {
            yield return this;
        }

        public override void Refresh()
        {
            for (var i = 0; i < m_Dependencies.Count; i++)
            {
                var s = m_Dependencies[i].Dereference(Registry);
                if (null != s)
                {
                    m_Dependencies[i] = (Reference) s;
                }
            }

            for (var i = 0; i < m_Configurations.Count; i++)
            {
                var s = m_Configurations[i].Dereference(Registry);
                if (null != s)
                {
                    m_Configurations[i] = (TinyType.Reference) s;
                }
            }

            for (var i = 0; i < m_Components.Count; i++)
            {
                var s = m_Components[i].Dereference(Registry);
                if (null != s)
                {
                    m_Components[i] = (TinyType.Reference) s;
                }
            }

            for (var i = 0; i < m_Structs.Count; i++)
            {
                var s = m_Structs[i].Dereference(Registry);
                if (null != s)
                {
                    m_Structs[i] = (TinyType.Reference) s;
                }
            }

            for (var i = 0; i < m_Enums.Count; i++)
            {
                var s = m_Enums[i].Dereference(Registry);
                if (null != s)
                {
                    m_Enums[i] = (TinyType.Reference) s;
                }
            }

            for (var i = 0; i < m_EntityGroups.Count; i++)
            {
                var s = m_EntityGroups[i].Dereference(Registry);
                if (null != s)
                {
                    m_EntityGroups[i] = (TinyEntityGroup.Reference) s;
                }
            }
        }

        public void AddExplicitModuleDependency(Reference module)
        {
            if (DependenciesProperty.Contains(this, module))
            {
                return;
            }

            DependenciesProperty.Add(this, module);
        }

        public bool ContainsExplicitModuleDependency(Reference module)
        {
            return DependenciesProperty.Contains(this, module);
        }

        public void RemoveExplicitModuleDependency(Reference module)
        {
            DependenciesProperty.Remove(this, module);
        }

        public void AddConfigurationReference(TinyType.Reference type)
        {
            ConfigurationsProperty.Add(this, type);
        }

        public void AddComponentReference(TinyType.Reference type)
        {
            ComponentsProperty.Add(this, type);
        }

        public void AddStructReference(TinyType.Reference type)
        {
            StructsProperty.Add(this, type);
        }

        public void AddEnumReference(TinyType.Reference type)
        {
            EnumsProperty.Add(this, type);
        }

        /// <summary>
        /// Adds a reference to this registry object in the acceleration structure (if it is not referenced already)
        /// </summary>
        public void TryAddObjectReference(IRegistryObject obj)
        {
            switch (obj)
            {
                case TinyType type:
                {
                    if (!Types.Contains(type.Ref))
                    {
                        AddTypeReference(type);
                    }
                }
                    break;

                case TinyEntityGroup group:
                {
                    if (!EntityGroups.Contains(group.Ref))
                    {
                        AddEntityGroupReference(group.Ref);
                    }
                }
                break;
            }
        }
        
        /// <summary>
        /// Removes a reference to this registry object (if it is referenced already)
        /// </summary>
        public void TryRemoveObjectReference(IRegistryObject obj)
        {
            switch (obj)
            {
                case TinyType type:
                {
                    if (Types.Contains(type.Ref))
                    {
                        RemoveTypeReference(type.Ref);
                    }
                }
                    break;

                case TinyEntityGroup group:
                {
                    if (EntityGroups.Contains(group.Ref))
                    {
                        RemoveEntityGroupReference(group.Ref);
                    }
                }
                break;
            }
        }

        /// <summary>
        /// Adds a reference to this type for the project
        /// </summary>
        /// <param name="type"></param>
        private void AddTypeReference(TinyType type)
        {
            switch (type.TypeCode)
            {
                case TinyTypeCode.Configuration:
                    ConfigurationsProperty.Add(this, type.Ref);
                    break;
                case TinyTypeCode.Component:
                    ComponentsProperty.Add(this, type.Ref);
                    break;
                case TinyTypeCode.Struct:
                    StructsProperty.Add(this, type.Ref);
                    break;
                case TinyTypeCode.Enum:
                    EnumsProperty.Add(this, type.Ref);
                    break;
            }
        }

        public void RemoveTypeReference(TinyType.Reference type)
        {
            ConfigurationsProperty.Remove(this, type);
            ComponentsProperty.Remove(this, type);
            StructsProperty.Remove(this, type);
            EnumsProperty.Remove(this, type);
        }

        public void AddEntityGroupReference(TinyEntityGroup.Reference entityGroup)
        {
            EntityGroupsProperty.Add(this, entityGroup);

            if (StartupEntityGroup.Equals(TinyEntityGroup.Reference.None))
            {
                StartupEntityGroup = entityGroup;
            }
        }

        public void RemoveEntityGroupReference(TinyEntityGroup.Reference entityGroup)
        {
            if (EntityGroupsProperty.Contains(this, entityGroup))
            {
                EntityGroupsProperty.Remove(this, entityGroup);
            }

            if (StartupEntityGroup.Equals(entityGroup))
            {
                StartupEntityGroup = m_EntityGroups.FirstOrDefault();
            }
        }
        
        public TinyAsset AddAsset(Object @object)
        {
            Assert.IsNotNull(@object);

            var asset = new TinyAsset(this) { Object = @object };

            var index = AssetsProperty.IndexOf(this, asset);
            
            if (index != -1)
            {
                // return the original instance
                return AssetsProperty.GetAt(this, index);
            }

            AssetsProperty.Add(this, asset);

            return asset;
        }

        public void RemoveAsset(Object @object)
        {
            var index = m_Assets.FindIndex(a => a.Object == @object);
            
            if (index < 0)
            {
                return;
            }

            AssetsProperty.RemoveAt(this, index);
        }

        public TinyAsset GetAsset(Object @object)
        {
            return m_Assets.Find(a => a.Object == @object);
        }

        public TinyAsset GetOrAddAsset(Object @object)
        {
            return GetAsset(@object) ?? AddAsset(@object);
        }
        
        /// <summary>
        /// @NOTE Includes self in the returned elements
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TinyModule> EnumerateDependencies()
        {
            return EnumerateRefDependencies().Deref(Registry);
        }

        public IEnumerable<Reference> EnumerateRefDependencies()
        {
            var visited = new HashSet<Reference>();

            foreach (var dependency in EnumerateRefDependencies(visited))
            {
                yield return dependency;
            }
        }

        private IEnumerable<Reference> EnumerateRefDependencies(ISet<Reference> visited)
        {
            if (visited.Add((Reference) this))
            {
                yield return (Reference) this;
            }
            else
            {
                yield break;
            }

            foreach (var reference in Dependencies)
            {
                var module = reference.Dereference(Registry);

                if (null == module)
                {
                    continue;
                }

                foreach (var dependency in module.EnumerateRefDependencies(visited))
                {
                    yield return dependency;
                }
            }
        }

        /// <summary>
        /// Returns a list of modules that explicitly depend on the given module
        /// </summary>
        public static IEnumerable<TinyModule> GetExplicitDependantModules(IRegistry registry, Reference module)
        {
            return registry.FindAllByType<TinyModule>().Where(m => m.ContainsExplicitModuleDependency(module)).ToList();
        }

        /// <inheritdoc cref="IPropertyContainer"/>
        /// <summary>
        /// Weak reference to a module
        /// </summary>
        public partial struct Reference : IReference<TinyModule>, IEquatable<Reference>
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

            public TinyModule Dereference(IRegistry registry)
            {
                return registry.Dereference<Reference, TinyModule>(this);
            }

            public static explicit operator Reference(TinyModule @object)
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

