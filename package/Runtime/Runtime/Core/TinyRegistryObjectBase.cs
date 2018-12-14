using System.IO;
using Unity.Properties;

namespace Unity.Tiny
{
    /// <inheritdoc cref="IRegistryObject" />
    /// <summary>
    /// Base class for Tiny registry objects
    /// </summary>
    internal abstract partial class TinyRegistryObjectBase : IVersionStorage, IRegistryObject, IOriginator, INamed
    {
        /// <summary>
        /// @NOTE This is NOT included as a Property by default. You must opt-in from the inheriting class
        /// </summary>
        public static ValueClassProperty<TinyRegistryObjectBase, TinyExportFlags> ExportFlagsProperty;

        /// <summary>
        /// @NOTE This is NOT included as a Property by default. You must opt-in from the inheriting class
        /// </summary>
        public static ClassValueClassProperty<TinyRegistryObjectBase, TinyDocumentation> DocumentationProperty;

        static partial void InitializeCustomProperties()
        {
            IdProperty
                .WithAttribute(InspectorAttributes.HideInInspector)
                .WithAttribute(InspectorAttributes.Readonly);
                
            ExportFlagsProperty = new ValueClassProperty<TinyRegistryObjectBase, TinyExportFlags>(nameof(ExportFlags),
                c => c.m_ExportFlags,
                (c, v) => c.m_ExportFlags = v
            );
                
            DocumentationProperty = new ClassValueClassProperty<TinyRegistryObjectBase, TinyDocumentation>(nameof(Documentation),
                c => c.m_Documentation ?? (c.m_Documentation = new TinyDocumentation(c)),
                null
            ); 
        }

        public virtual IPropertyBag PropertyBag => s_PropertyBag;
        public virtual IVersionStorage VersionStorage => this;
        
        protected readonly IVersionStorage SharedVersionStorage;
        private TinyDocumentation m_Documentation;
        private TinyExportFlags m_ExportFlags;

        public IRegistry Registry { get; }
        public int Version { get; protected set; }
        public bool IsRuntimeIncluded => 0 != (ExportFlags & TinyExportFlags.RuntimeIncluded);
        public bool IsDefinedInScript => 0 != (ExportFlags & TinyExportFlags.DefinedInScript);
        
        public TinyDocumentation Documentation => DocumentationProperty.GetValue(this);
        
        public abstract string Name { get; set; }
        
        public TinyExportFlags ExportFlags
        {
            get { return ExportFlagsProperty.GetValue(this); }
            set { ExportFlagsProperty.SetValue(this, value); }
        }
        
        protected TinyRegistryObjectBase(IRegistry registry, IVersionStorage versionStorage)
        {
            Registry = registry;
            SharedVersionStorage = versionStorage;
        }

        private class CommandMemento : IMemento
        {
            public int Version { get; }
            private MemoryStream Data { get; }

            public CommandMemento(TinyRegistryObjectBase obj)
            {
                Version = obj.Version;
                Data = new MemoryStream();
                Serialization.CommandStream.CommandBackEnd.Persist(Data, obj);
            }

            public void Restore(TinyRegistryObjectBase obj)
            {
                Data.Position = 0;
                
                // Pull out any transient state that we don't want to lose
                // This is extracted as a partial `IPropertyContainer`
                var state = Serialization.TransientState.Persist(obj);

                // As a precaution and to protect against future changes we fetch the Id
                // The `obj` is being unregistered and should be "destroyed"
                var id = obj.Id;
                
                // Accept the data stream in to the registry
                // This will invoke `unregister` and "destroy" then re-create it from scratch
                Serialization.CommandStream.CommandFrontEnd.Accept(Data, obj.Registry);
                
                // Find our newly created object by Id
                var instance = obj.Registry.FindById(id) as IPropertyContainer;
                
                // Apply our transient state values
                PropertyContainer.Transfer(state, instance);
            }
        }
        
        public IMemento Save()
        {
            return new CommandMemento(this);
        }

        public void Restore(IMemento memento)
        {
            ((CommandMemento)memento).Restore(this);
            Version = memento.Version;
        }

        public virtual void IncrementVersion<TContainer>(IProperty property, TContainer container) 
            where TContainer : IPropertyContainer
        {
            Version++;
            SharedVersionStorage.IncrementVersion(property, this);
        }

        public virtual void Refresh()
        {
            
        }
    }
}

