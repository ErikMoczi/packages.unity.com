#if (NET_4_6 || NET_STANDARD_2_0)
using System;
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    internal partial class TinyAsset : IPropertyContainer
    {
        /// <summary>
        /// <see cref="TinyAsset.Name" /> property.
        /// </summary>
        public static ValueClassProperty<TinyAsset, string> NameProperty { get; private set; }
        /// <summary>
        /// <see cref="TinyAsset.Object" /> property.
        /// </summary>
        public static ValueClassProperty<TinyAsset, UnityEngine.Object> ObjectProperty { get; private set; }
        /// <summary>
        /// <see cref="TinyAsset.ExportSettings" /> property.
        /// </summary>
        public static ClassValueClassProperty<TinyAsset, TinyAssetExportSettings> ExportSettingsProperty { get; private set; }

        private static ClassPropertyBag<TinyAsset> s_PropertyBag { get; set; }

        /// <inheritdoc cref="Unity.Properties.IPropertyContainer.PropertyBag" />
        public IPropertyBag PropertyBag => s_PropertyBag;

        private static void InitializeProperties()
        {
            NameProperty = new ValueClassProperty<TinyAsset, string>(
                "Name"
                ,c => c.m_Name
                ,(c, v) => c.m_Name = v
            );

            ObjectProperty = new ValueClassProperty<TinyAsset, UnityEngine.Object>(
                "Object"
                ,c => c.m_Object
                ,(c, v) => c.m_Object = v
            );

            ExportSettingsProperty = new ClassValueClassProperty<TinyAsset, TinyAssetExportSettings>(
                "ExportSettings"
                ,c => c.m_ExportSettings
                ,(c, v) => c.m_ExportSettings = v
            );
        }

        static partial void InitializeCustomProperties();

        private static void InitializePropertyBag()
        {
            s_PropertyBag = new ClassPropertyBag<TinyAsset>(
                NameProperty,
                ObjectProperty,
                ExportSettingsProperty
            );
        }

        static TinyAsset()
        {
            InitializeProperties();
            InitializeCustomProperties();
            InitializePropertyBag();
        }

        private string m_Name;
        private UnityEngine.Object m_Object;
        private TinyAssetExportSettings m_ExportSettings;

        /// <summary>
        /// Unity object referenced by this Tiny asset.
        /// </summary>
        public UnityEngine.Object Object
        {
            get { return ObjectProperty.GetValue(this); }
            set { ObjectProperty.SetValue(this, value); }
        }

        /// <summary>
        /// Export settings associated with this asset.
        /// </summary>
        public TinyAssetExportSettings ExportSettings
        {
            get { return ExportSettingsProperty.GetValue(this); }
            set { ExportSettingsProperty.SetValue(this, value); }
        }
    }
}
#endif // (NET_4_6 || NET_STANDARD_2_0)
