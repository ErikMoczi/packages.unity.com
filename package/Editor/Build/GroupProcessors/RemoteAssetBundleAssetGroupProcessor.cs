using System.ComponentModel;
using UnityEngine;
using UnityEngine.ResourceManagement;
using System.IO;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;

namespace UnityEditor.AddressableAssets
{
    [Description("Remote Packed Content")]
    public class RemoteAssetBundleAssetGroupProcessor : AssetBundleAssetGroupProcessor
    {
        [SerializeField]
        protected string m_buildPathId;
        [SerializeField]
        protected string m_loadPrefixId;

        public string buildPathId
        {
            get
            {
                if (string.IsNullOrEmpty(m_buildPathId))
                {
                    m_buildPathId = AddressableAssetSettings.ProfileSettings.TryGetProfileID("LocalBuildPath");
                }
                return m_buildPathId;
            }
        }
        public string loadPrefixId
        {
            get
            {
                if (string.IsNullOrEmpty(m_loadPrefixId))
                {
                    m_loadPrefixId = AddressableAssetSettings.ProfileSettings.TryGetProfileID("LocalLoadPrefix");
                }
                return m_loadPrefixId;
            }
        }
        public BundleMode bundleMode = BundleMode.PackTogether;

        internal override string displayName { get { return "Remote Packed Content"; } }
        internal override void Initialize(AddressableAssetSettings settings)
        {
        }

        internal override void SerializeForHash(System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter, Stream stream)
        {
            formatter.Serialize(stream, bundleMode);
            formatter.Serialize(stream, buildPathId);
            formatter.Serialize(stream, loadPrefixId);
        }

        protected override string GetBuildPath(AddressableAssetSettings settings)
        {
            return AddressableAssetSettings.ProfileSettings.ProfileIDData.Evaluate(settings.profileSettings, settings.activeProfileId, buildPathId);
        }

        protected override string GetBundleLoadPath(AddressableAssetSettings settings, string postfixPath)
        {
            return AddressableAssetSettings.ProfileSettings.ProfileIDData.Evaluate(settings.profileSettings, settings.activeProfileId, loadPrefixId) + "/" + postfixPath;
        }

        protected override BundleMode GetBundleMode(AddressableAssetSettings settings)
        {
            return bundleMode;
        }

        internal override int GetPriority(AddressableAssetSettings aaSettings, AddressableAssetGroup group)
        {
            return 0;
        }

        internal override bool Validate(AddressableAssetSettings aaSettings, AddressableAssetGroup assetGroup)
        {
            bool valid = true;
            if (string.IsNullOrEmpty(loadPrefixId))
            {
                Debug.LogWarningFormat("Asset Group '{0}' has invalid loadPrefix", assetGroup.name);
                valid = false;
            }
            var bp = GetBuildPath(aaSettings);
            if (string.IsNullOrEmpty(bp))
            {
                Debug.LogWarningFormat("Asset Group '{0}' has invalid buildPath", assetGroup.name);
                valid = false;
            }
            return valid;
        }

        [SerializeField]
        Vector2 position = new Vector2();
        internal override void OnDrawGUI(AddressableAssetSettings settings, Rect rect)
        {
            GUILayout.BeginArea(rect);
            position = EditorGUILayout.BeginScrollView(position, false, false, GUILayout.MaxWidth(rect.width));
            bool oldWrap = EditorStyles.label.wordWrap;
            EditorStyles.label.wordWrap = true;
            EditorGUILayout.LabelField("Assets in this group can either be packed together or separately and will be downloaded from a URL via UnityWebRequest.");
            EditorStyles.label.wordWrap = oldWrap;
            var newBundleMode = (BundleMode)EditorGUILayout.EnumPopup("Packing Mode", bundleMode);
            if (newBundleMode != bundleMode)
                bundleMode = newBundleMode;

            var newBP = ProfilesWindow.ValueGUI(settings, "Build Path", buildPathId);
            var newLP = ProfilesWindow.ValueGUI(settings, "Load Prefix", loadPrefixId);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            if (newBP != buildPathId || newLP != loadPrefixId)
            {
                m_buildPathId = newBP;
                m_loadPrefixId = newLP;
                settings.PostModificationEvent(AddressableAssetSettings.ModificationEvent.GroupProcessorModified, this);
            }
        }
    }
}
