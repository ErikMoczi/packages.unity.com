using System;
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    public abstract partial class TinyAssetExportSettings : IPropertyContainer
    {
        /// <summary>
        /// <see cref="TinyAssetExportSettings.Embedded" /> property.
        /// </summary>
        public static ValueClassProperty<TinyAssetExportSettings, bool> EmbeddedProperty { get; private set; }

        private static ClassPropertyBag<TinyAssetExportSettings> s_PropertyBag { get; set; }

        private static void InitializeProperties()
        {
            EmbeddedProperty = new ValueClassProperty<TinyAssetExportSettings, bool>(
                "Embedded"
                ,c => c.m_Embedded
                ,(c, v) => c.m_Embedded = v
            );
        }

        static partial void InitializeCustomProperties();

        private static void InitializePropertyBag()
        {
            s_PropertyBag = new ClassPropertyBag<TinyAssetExportSettings>(
                EmbeddedProperty
            );
        }

        static TinyAssetExportSettings()
        {
            InitializeProperties();
            InitializeCustomProperties();
            InitializePropertyBag();
        }

        private bool m_Embedded;

        /// <summary>
        /// Whether or not this asset is embedded as a build resource, or can be streamed in.
        /// </summary>
        public bool Embedded
        {
            get { return EmbeddedProperty.GetValue(this); }
            set { EmbeddedProperty.SetValue(this, value); }
        }
    }
}
