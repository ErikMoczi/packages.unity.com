using System.ComponentModel;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using System.IO;

namespace UnityEditor.AddressableAssets
{
    [Description("Advanced Packed Content")]
    public class AdvancedAssetBundleAssetGroupProcessor : AssetBundleAssetGroupProcessor
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
        public AddressableAssetSettings.ProfileSettings.ProfileValue bundleLoadProvider;


        internal override void Initialize(AddressableAssetSettings settings)
        {
            if(buildPath == null)
                buildPath = settings.profileSettings.CreateProfileValue("StreamingAsssetsBuildPath");
            if(loadPrefix == null)
                loadPrefix = settings.profileSettings.CreateProfileValue("StreamingAssetsLoadPrefix");
            if(bundleLoadProvider == null)
                bundleLoadProvider = settings.profileSettings.CreateProfileValue(typeof(RemoteAssetBundleProvider).FullName, true);
        }

        /// <summary>
        /// TODO - doc
        /// </summary>
        public BundleMode bundleMode = BundleMode.PackTogether;
        internal override string displayName { get { return "Advanced Packed Content"; } }
        internal override void SerializeForHash(System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter, Stream stream)
        {
            formatter.Serialize(stream, bundleMode);
            formatter.Serialize(stream, buildPath);
            formatter.Serialize(stream, loadPrefix);
            formatter.Serialize(stream, bundleLoadProvider);
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
            return bundleLoadProvider.Evaluate(settings.profileSettings, settings.activeProfile);
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
            bool modified = false;
            var newBundleMode = (BundleMode)EditorGUILayout.EnumPopup("Packing Mode", bundleMode);
            if (newBundleMode != bundleMode)
            {
                bundleMode = newBundleMode;
                modified = true;
            }
            modified |= ProfileSettingsEditor.ValueGUI(settings, "Build Path", buildPath);
            modified |= ProfileSettingsEditor.ValueGUI(settings, "Load Prefix", loadPrefix);
            modified |= ProfileSettingsEditor.ValueGUI(settings, "Load Method", bundleLoadProvider);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            if (modified)
                settings.PostModificationEvent(AddressableAssetSettings.ModificationEvent.GroupProcessorModified, this);
        }
    }
}
