using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.ResourceManagement;

namespace UnityEditor.AddressableAssets
{
    /// <summary>
    /// Schema used for bundled asset groups.
    /// </summary>
    [CreateAssetMenu(fileName = "BundledAssetGroupSchema.asset", menuName = "Addressable Assets/Group Schemas/Bundled Assets")]
    public class BundledAssetGroupSchema : AddressableAssetGroupSchema, IHostingServiceConfigurationProvider, ISerializationCallbackReceiver
    {
        /// <summary>
        /// Defines how bundles are created.
        /// </summary>
        public enum BundlePackingMode
        {
            /// <summary>
            /// Pack all entries into as few bundles as possible (Scenes are put into separate bundles).
            /// </summary>
            PackTogether,
            /// <summary>
            /// Create a bundle per entry.  This is useful if each entry is a folder as all sub entries will go to the same bundle.
            /// </summary>
            PackSeparately
        }

        [SerializeField]
        private bool m_useAssetBundleCache = true;
        public bool UseAssetBundleCache
        {
            get { return m_useAssetBundleCache; }
            set
            {
                m_useAssetBundleCache = value;
                SetDirty(true);
            }
        }

        [SerializeField]
        private ProfileValueReference m_buildPath = new ProfileValueReference();
        /// <summary>
        /// The path to copy asset bundles to.
        /// </summary>
        public ProfileValueReference BuildPath
        {
            get { return m_buildPath; }
        }

        [SerializeField]
        private ProfileValueReference m_loadPath = new ProfileValueReference();
        /// <summary>
        /// The path to load bundles from.
        /// </summary>
        public ProfileValueReference LoadPath
        {
            get { return m_loadPath; }
        }

        [SerializeField]
        private BundlePackingMode m_bundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
        /// <summary>
        /// The bundle mode.
        /// </summary>
        public BundlePackingMode BundleMode
        {
            get { return m_bundleMode; }
            set
            {
                m_bundleMode = value;
                SetDirty(true);
            }
        }

        /// <inheritdoc/>
        public string HostingServicesContentRoot
        {
            get
            {
                return BuildPath.GetValue(Group.Settings);
            }
        }

        [SerializeField]
        [SerializedTypeRestriction(type = typeof(IResourceProvider))]
        private SerializedType m_assetBundleProviderType;
        /// <summary>
        /// The provider type to use for loading asset bundles.
        /// </summary>
        public SerializedType AssetBundleProviderType { get { return m_assetBundleProviderType; } }

        /// <summary>
        /// Set default values taken from the assigned group.
        /// </summary>
        /// <param name="group">The group this schema has been added to.</param>
        protected override void OnSetGroup(AddressableAssetGroup group)
        {
            BuildPath.SetVariableByName(group.Settings, AddressableAssetSettings.kLocalBuildPath);
            LoadPath.SetVariableByName(group.Settings, AddressableAssetSettings.kLocalLoadPath);
            m_assetBundleProviderType.Value = typeof(AssetBundleProvider);
        }

        /// <summary>
        /// Impementation of ISerializationCallbackReceiver, does nothing.
        /// </summary>
        public void OnBeforeSerialize()
        {
            
        }

        /// <summary>
        /// Impementation of ISerializationCallbackReceiver, used to set callbacks for ProfileValueReference changes.
        /// </summary>
        public void OnAfterDeserialize()
        {
            BuildPath.OnValueChanged += (s)=> SetDirty(true);
            LoadPath.OnValueChanged += (s) => SetDirty(true);
            if(m_assetBundleProviderType.Value == null)
                m_assetBundleProviderType.Value = typeof(AssetBundleProvider);
        }
    }
}