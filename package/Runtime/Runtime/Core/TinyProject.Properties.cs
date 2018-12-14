
using System;
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    internal partial class TinyProject : IPropertyContainer
    {
        public static ValueClassProperty<TinyProject, TinyTypeId> TypeIdProperty { get; private set; }
        public static ValueClassProperty<TinyProject, string> PersistenceIdProperty { get; private set; }
        public static ValueClassProperty<TinyProject, string> NameProperty { get; private set; }
        public static ValueClassProperty<TinyProject, int> SerializedVersionProperty { get; private set; }
        public static ValueClassProperty<TinyProject, int> LastSerializedVersionProperty { get; private set; }
        public static ClassValueClassProperty<TinyProject, TinyProjectSettings> SettingsProperty { get; private set; }
        public static StructValueClassProperty<TinyProject, TinyModule.Reference> ModuleProperty { get; private set; }
        public static StructValueClassProperty<TinyProject, TinyEntity.Reference> ConfigurationProperty { get; private set; }

        private static ClassPropertyBag<TinyProject> s_PropertyBag { get; set; }

        private static void InitializeProperties()
        {
            PersistenceIdProperty = new ValueClassProperty<TinyProject, string>(
                "PersistenceId"
                ,c => c.m_PersistenceId
                ,(c, v) => c.m_PersistenceId = v
            );

            NameProperty = new ValueClassProperty<TinyProject, string>(
                "Name"
                ,c => c.m_Name
                ,(c, v) => c.m_Name = v
            );

            SerializedVersionProperty = new ValueClassProperty<TinyProject, int>(
                "SerializedVersion"
                ,c => c.m_SerializedVersion
                ,(c, v) => c.m_SerializedVersion = v
            );

            LastSerializedVersionProperty = new ValueClassProperty<TinyProject, int>(
                "LastSerializedVersion"
                ,c => c.m_LastSerializedVersion
                ,(c, v) => c.m_LastSerializedVersion = v
            );

            SettingsProperty = new ClassValueClassProperty<TinyProject, TinyProjectSettings>(
                "Settings"
                ,c => c.m_Settings
                ,(c, v) => c.m_Settings = v
            );

            ModuleProperty = new StructValueClassProperty<TinyProject, TinyModule.Reference>(
                "Module"
                ,c => c.m_Module
                ,(c, v) => c.m_Module = v
                ,(m, p, c, v) => m(p, c, ref c.m_Module, v)
            );

            ConfigurationProperty = new StructValueClassProperty<TinyProject, TinyEntity.Reference>(
                "Configuration"
                ,c => c.m_Configuration
                ,(c, v) => c.m_Configuration = v
                ,(m, p, c, v) => m(p, c, ref c.m_Configuration, v)
            );
        }

        /// <summary>
        /// Implement this partial method to initialize custom properties
        /// </summary>
        static partial void InitializeCustomProperties();

        private static void InitializePropertyBag()
        {
            s_PropertyBag = new ClassPropertyBag<TinyProject>(
                TypeIdProperty,
                PersistenceIdProperty,
                IdProperty,
                NameProperty,
                ExportFlagsProperty,
                DocumentationProperty,
                SerializedVersionProperty,
                LastSerializedVersionProperty,
                SettingsProperty,
                ModuleProperty,
                ConfigurationProperty
            );
        }

        static TinyProject()
        {
            InitializeProperties();
            InitializeCustomProperties();
            InitializePropertyBag();
        }

        private string m_PersistenceId;
        private string m_Name;
        private int m_SerializedVersion;
        private int m_LastSerializedVersion;
        private TinyProjectSettings m_Settings;
        private TinyModule.Reference m_Module;
        private TinyEntity.Reference m_Configuration;

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

        public int LastSerializedVersion
        {
            get { return LastSerializedVersionProperty.GetValue(this); }
            set { LastSerializedVersionProperty.SetValue(this, value); }
        }

        public TinyProjectSettings Settings
        {
            get { return SettingsProperty.GetValue(this); }
            set { SettingsProperty.SetValue(this, value); }
        }

        public TinyModule.Reference Module
        {
            get { return ModuleProperty.GetValue(this); }
            set { ModuleProperty.SetValue(this, value); }
        }

        public TinyEntity.Reference Configuration
        {
            get { return ConfigurationProperty.GetValue(this); }
            set { ConfigurationProperty.SetValue(this, value); }
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
