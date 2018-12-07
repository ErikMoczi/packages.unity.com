
using System;
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    internal partial class TinyModule : IPropertyContainer
    {
        public static ValueClassProperty<TinyModule, TinyTypeId> TypeIdProperty { get; private set; }
        public static ValueClassProperty<TinyModule, string> PersistenceIdProperty { get; private set; }
        public static ValueClassProperty<TinyModule, string> NameProperty { get; private set; }
        public static ValueClassProperty<TinyModule, int> SerializedVersionProperty { get; private set; }
        public static ValueClassProperty<TinyModule, string> NamespaceProperty { get; private set; }
        public static ValueClassProperty<TinyModule, string> MetadataFileGUIDProperty { get; private set; }
        public static ValueClassProperty<TinyModule, TinyModuleOptions> OptionsProperty { get; private set; }
        public static StructListClassProperty<TinyModule, Reference> DependenciesProperty { get; private set; }
        public static StructListClassProperty<TinyModule, TinyType.Reference> ConfigurationsProperty { get; private set; }
        public static StructListClassProperty<TinyModule, TinyType.Reference> ComponentsProperty { get; private set; }
        public static StructListClassProperty<TinyModule, TinyType.Reference> StructsProperty { get; private set; }
        public static StructListClassProperty<TinyModule, TinyType.Reference> EnumsProperty { get; private set; }
        public static StructListClassProperty<TinyModule, TinyEntityGroup.Reference> EntityGroupsProperty { get; private set; }
        public static ClassListClassProperty<TinyModule, TinyAsset> AssetsProperty { get; private set; }
        public static StructValueClassProperty<TinyModule, TinyEntityGroup.Reference> StartupEntityGroupProperty { get; private set; }

        private static ClassPropertyBag<TinyModule> s_PropertyBag { get; set; }

        private static void InitializeProperties()
        {
            PersistenceIdProperty = new ValueClassProperty<TinyModule, string>(
                "PersistenceId"
                ,c => c.m_PersistenceId
                ,(c, v) => c.m_PersistenceId = v
            );

            NameProperty = new ValueClassProperty<TinyModule, string>(
                "Name"
                ,c => c.m_Name
                ,(c, v) => c.m_Name = v
            );

            SerializedVersionProperty = new ValueClassProperty<TinyModule, int>(
                "SerializedVersion"
                ,c => c.m_SerializedVersion
                ,(c, v) => c.m_SerializedVersion = v
            );

            NamespaceProperty = new ValueClassProperty<TinyModule, string>(
                "Namespace"
                ,c => c.m_Namespace
                ,(c, v) => c.m_Namespace = v
            );

            MetadataFileGUIDProperty = new ValueClassProperty<TinyModule, string>(
                "MetadataFileGUID"
                ,c => c.m_MetadataFileGUID
                ,(c, v) => c.m_MetadataFileGUID = v
            );

            OptionsProperty = new ValueClassProperty<TinyModule, TinyModuleOptions>(
                "Options"
                ,c => c.m_Options
                ,(c, v) => c.m_Options = v
            );

            DependenciesProperty = new StructListClassProperty<TinyModule, Reference>(
                "Dependencies"
                ,c => c.m_Dependencies
            );

            ConfigurationsProperty = new StructListClassProperty<TinyModule, TinyType.Reference>(
                "Configurations"
                ,c => c.m_Configurations
            );

            ComponentsProperty = new StructListClassProperty<TinyModule, TinyType.Reference>(
                "Components"
                ,c => c.m_Components
            );

            StructsProperty = new StructListClassProperty<TinyModule, TinyType.Reference>(
                "Structs"
                ,c => c.m_Structs
            );

            EnumsProperty = new StructListClassProperty<TinyModule, TinyType.Reference>(
                "Enums"
                ,c => c.m_Enums
            );

            EntityGroupsProperty = new StructListClassProperty<TinyModule, TinyEntityGroup.Reference>(
                "EntityGroups"
                ,c => c.m_EntityGroups
            );
            
            StartupEntityGroupProperty = new StructValueClassProperty<TinyModule, TinyEntityGroup.Reference>(
                "StartupEntityGroup"
                ,c => c.m_StartupEntityGroup
                ,(c, v) => c.m_StartupEntityGroup = v
                ,(m, p, c, v) => m(p, c, ref c.m_StartupEntityGroup, v)
            );
        }

        /// <summary>
        /// Implement this partial method to initialize custom properties
        /// </summary>
        static partial void InitializeCustomProperties();

        private static void InitializePropertyBag()
        {
            s_PropertyBag = new ClassPropertyBag<TinyModule>(
                TypeIdProperty,
                PersistenceIdProperty,
                IdProperty,
                NameProperty,
                SerializedVersionProperty,
                ExportFlagsProperty,
                DocumentationProperty,
                NamespaceProperty,
                MetadataFileGUIDProperty,
                OptionsProperty,
                DependenciesProperty,
                ConfigurationsProperty,
                ComponentsProperty,
                StructsProperty,
                EnumsProperty,
                EntityGroupsProperty,
                AssetsProperty,
                StartupEntityGroupProperty
            );
        }

        static TinyModule()
        {
            InitializeProperties();
            InitializeCustomProperties();
            InitializePropertyBag();
        }

        private string m_PersistenceId;
        private string m_Name;
        private int m_SerializedVersion;
        private string m_Namespace;
        private string m_MetadataFileGUID;
        private TinyModuleOptions m_Options;
        private readonly List<Reference> m_Dependencies = new List<Reference>();
        private readonly List<TinyType.Reference> m_Configurations = new List<TinyType.Reference>();
        private readonly List<TinyType.Reference> m_Components = new List<TinyType.Reference>();
        private readonly List<TinyType.Reference> m_Structs = new List<TinyType.Reference>();
        private readonly List<TinyType.Reference> m_Enums = new List<TinyType.Reference>();
        private readonly List<TinyEntityGroup.Reference> m_EntityGroups = new List<TinyEntityGroup.Reference>();
        private readonly List<TinyAsset> m_Assets = new List<TinyAsset>();
        private TinyEntityGroup.Reference m_StartupEntityGroup;

        public string PersistenceId
        {
            get { return PersistenceIdProperty.GetValue(this); }
            set { PersistenceIdProperty.SetValue(this, value); }
        }

        public int SerializedVersion
        {
            get { return SerializedVersionProperty.GetValue(this); }
            set { SerializedVersionProperty.SetValue(this, value); }
        }

        public string Namespace
        {
            get { return NamespaceProperty.GetValue(this); }
            set { NamespaceProperty.SetValue(this, value); }
        }

        public string MetadataFileGUID
        {
            get { return MetadataFileGUIDProperty.GetValue(this); }
            set { MetadataFileGUIDProperty.SetValue(this, value); }
        }

        public TinyModuleOptions Options
        {
            get { return OptionsProperty.GetValue(this); }
            set { OptionsProperty.SetValue(this, value); }
        }

        public partial struct Reference : IStructPropertyContainer<Reference>
        {
            public static ValueStructProperty<Reference, TinyId> IdProperty { get; private set; }
            public static ValueStructProperty<Reference, string> NameProperty { get; private set; }

            private static StructPropertyBag<Reference> s_PropertyBag { get; set; }

            public IPropertyBag PropertyBag => s_PropertyBag;
            public IVersionStorage VersionStorage => null;

            private static void InitializeProperties()
            {
                IdProperty = new ValueStructProperty<Reference, TinyId>(
                    "Id"
                    ,(ref Reference c) => c.m_Id
                    ,(ref Reference c, TinyId v) => c.m_Id = v
                );

                NameProperty = new ValueStructProperty<Reference, string>(
                    "Name"
                    ,(ref Reference c) => c.m_Name
                    ,(ref Reference c, string v) => c.m_Name = v
                );
            }

            /// <summary>
            /// Implement this partial method to initialize custom properties
            /// </summary>
            static partial void InitializeCustomProperties();

            private static void InitializePropertyBag()
            {
                s_PropertyBag = new StructPropertyBag<Reference>(
                    IdProperty,
                    NameProperty
                );
            }

            static Reference()
            {
                InitializeProperties();
                InitializeCustomProperties();
                InitializePropertyBag();
            }

            private TinyId m_Id;
            private string m_Name;

            public TinyId Id
            {
                get { return IdProperty.GetValue(ref this); }
                set { IdProperty.SetValue(ref this, value); }
            }

            public string Name
            {
                get { return NameProperty.GetValue(ref this); }
                set { NameProperty.SetValue(ref this, value); }
            }


            public void MakeRef<TContext>(ByRef<Reference, TContext> byRef, TContext context)
            {
                byRef(ref this, context);
            }

            public TReturn MakeRef<TContext, TReturn>(ByRef<Reference, TContext, TReturn> byRef, TContext context)
            {
                return byRef(ref this, context);
            }
        }    }
}
