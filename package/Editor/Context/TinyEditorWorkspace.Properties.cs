
using System;
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    internal partial class TinyEditorWorkspace : IPropertyContainer
    {
        public static StructListClassProperty<TinyEditorWorkspace, TinyEntityGroup.Reference> OpenedEntityGroupsProperty { get; private set; }
        public static ValueClassProperty<TinyEditorWorkspace, TinyEntityGroup.Reference> ActiveEntityGroupProperty { get; private set; }
        public static ValueClassProperty<TinyEditorWorkspace, TinyBuildConfiguration> BuildConfigurationProperty { get; private set; }
        public static ValueClassProperty<TinyEditorWorkspace, bool> PreviewProperty { get; private set; }
        public static ValueClassProperty<TinyEditorWorkspace, bool> AutoConnectProfilerProperty { get; private set; }
        public static ValueClassProperty<TinyEditorWorkspace, TinyPlatform> PlatformProperty { get; private set; }

        private static ClassPropertyBag<TinyEditorWorkspace> s_PropertyBag { get; set; }

        /// <inheritdoc cref="Unity.Properties.IPropertyContainer.PropertyBag" />
        public IPropertyBag PropertyBag => s_PropertyBag;
        /// <inheritdoc cref="Unity.Properties.IPropertyContainer.VersionStorage" />
        public IVersionStorage VersionStorage => null;

        private static void InitializeProperties()
        {
            OpenedEntityGroupsProperty = new StructListClassProperty<TinyEditorWorkspace, TinyEntityGroup.Reference>(
                "OpenedEntityGroups"
                ,c => c.m_OpenedEntityGroups
            );

            ActiveEntityGroupProperty = new ValueClassProperty<TinyEditorWorkspace, TinyEntityGroup.Reference>(
                "ActiveEntityGroup"
                ,c => c.m_ActiveEntityGroup
                ,(c, v) => c.m_ActiveEntityGroup = v
            );

            BuildConfigurationProperty = new ValueClassProperty<TinyEditorWorkspace, TinyBuildConfiguration>(
                "BuildConfiguration"
                ,c => c.m_BuildConfiguration
                ,(c, v) => c.m_BuildConfiguration = v
            );

            PreviewProperty = new ValueClassProperty<TinyEditorWorkspace, bool>(
                "Preview"
                ,c => c.m_Preview
                ,(c, v) => c.m_Preview = v
            );

            AutoConnectProfilerProperty = new ValueClassProperty<TinyEditorWorkspace, bool>(
                "AutoConnectProfiler"
                ,c => c.m_AutoConnectProfiler
                ,(c, v) => c.m_AutoConnectProfiler = v
            );

            PlatformProperty = new ValueClassProperty<TinyEditorWorkspace, TinyPlatform>(
                "Platform"
                ,c => c.m_Platform
                ,(c, v) => c.m_Platform = v
            );
        }

        static partial void InitializeCustomProperties();

        private static void InitializePropertyBag()
        {
            s_PropertyBag = new ClassPropertyBag<TinyEditorWorkspace>(
                OpenedEntityGroupsProperty,
                ActiveEntityGroupProperty,
                BuildConfigurationProperty,
                PreviewProperty,
                AutoConnectProfilerProperty,
                PlatformProperty
            );
        }

        static TinyEditorWorkspace()
        {
            InitializeProperties();
            InitializeCustomProperties();
            InitializePropertyBag();
        }

        private readonly List<TinyEntityGroup.Reference> m_OpenedEntityGroups = new List<TinyEntityGroup.Reference>();
        private TinyEntityGroup.Reference m_ActiveEntityGroup;
        private TinyBuildConfiguration m_BuildConfiguration = TinyBuildConfiguration.Development;
        private bool m_Preview = true;
        private bool m_AutoConnectProfiler = true;
        private TinyPlatform m_Platform = TinyPlatform.Html5;

        public PropertyList<TinyEditorWorkspace, TinyEntityGroup.Reference> OpenedEntityGroups => new PropertyList<TinyEditorWorkspace, TinyEntityGroup.Reference>(OpenedEntityGroupsProperty, this);

        public TinyEntityGroup.Reference ActiveEntityGroup
        {
            get { return ActiveEntityGroupProperty.GetValue(this); }
            set { ActiveEntityGroupProperty.SetValue(this, value); }
        }

        public TinyBuildConfiguration BuildConfiguration
        {
            get { return BuildConfigurationProperty.GetValue(this); }
            set { BuildConfigurationProperty.SetValue(this, value); }
        }

        public bool Preview
        {
            get { return PreviewProperty.GetValue(this); }
            set { PreviewProperty.SetValue(this, value); }
        }

        public bool AutoConnectProfiler
        {
            get { return AutoConnectProfilerProperty.GetValue(this); }
            set { AutoConnectProfilerProperty.SetValue(this, value); }
        }

        public TinyPlatform Platform
        {
            get { return PlatformProperty.GetValue(this); }
            set { PlatformProperty.SetValue(this, value); }
        }
    }
}
