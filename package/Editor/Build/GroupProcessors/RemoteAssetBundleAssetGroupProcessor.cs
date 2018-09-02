using System.ComponentModel;
using UnityEngine;
using UnityEngine.ResourceManagement;
using System.IO;

namespace UnityEditor.AddressableAssets
{
    [Description("Remote Packed Content")]
    public class RemoteAssetBundleAssetGroupProcessor : AssetBundleAssetGroupProcessor
    {
        /// <summary>
        /// TODO - doc
        /// </summary>
        public AddressableAssetSettings.ProfileSettings.ProfileValue buildPath;
        /// <summary>
        /// TODO - doc
        /// </summary>
        public AddressableAssetSettings.ProfileSettings.ProfileValue loadPrefix;
        /// <summary>
        /// TODO - doc
        /// </summary>
        public BundleMode bundleMode = BundleMode.PackTogether;

        internal override string displayName { get { return "Remote Packed Content"; } }
        internal override void Initialize(AddressableAssetSettings settings)
        {
            if(buildPath == null)
                buildPath = settings.profileSettings.CreateProfileValue(settings.profileSettings.GetVariableIdFromName("StreamingAsssetsBuildPath"));
            if(loadPrefix == null)
                loadPrefix = settings.profileSettings.CreateProfileValue(settings.profileSettings.GetVariableIdFromName("StreamingAssetsLoadPrefix"));
        }

        internal override void SerializeForHash(System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter, Stream stream)
        {
            formatter.Serialize(stream, bundleMode);
            formatter.Serialize(stream, buildPath);
            formatter.Serialize(stream, loadPrefix);
        }

        protected override string GetBuildPath(AddressableAssetSettings settings)
        {
            return buildPath.Evaluate(settings.profileSettings, settings.activeProfile);
        }

        protected override string GetBundleLoadPath(AddressableAssetSettings settings, string bundleName)
        {
            return loadPrefix.Evaluate(settings.profileSettings, settings.activeProfile) + "/" + bundleName;
        }

        protected override string GetBundleLoadProvider(AddressableAssetSettings settings)
        {
            return typeof(RemoteAssetBundleProvider).FullName;
        }

        protected override BundleMode GetBundleMode(AddressableAssetSettings settings)
        {
            return bundleMode;
        }

        [SerializeField]
        Vector2 position = new Vector2();
        internal override void OnDrawGUI(AddressableAssetSettings settings, Rect rect)
        {
            GUILayout.BeginArea(rect);
            position = EditorGUILayout.BeginScrollView(position, false, false, GUILayout.MaxWidth(rect.width));
            EditorGUILayout.LabelField("Assets in this group can either be packed together or separately and will be downloaded from a URL via UnityWebRequest.");
            bool modified = false;
            var newBundleMode = (BundleMode)EditorGUILayout.EnumPopup("Packing Mode", bundleMode);
            if (newBundleMode != bundleMode)
            {
                bundleMode = newBundleMode;
                modified = true;
            }

            modified |= ProfileSettingsEditor.ValueGUI(settings, "Build Path", buildPath);
            modified |= ProfileSettingsEditor.ValueGUI(settings, "Load Prefix", loadPrefix);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            if(modified)
                settings.PostModificationEvent(AddressableAssetSettings.ModificationEvent.GroupProcessorModified, this);
        }
    }
}
