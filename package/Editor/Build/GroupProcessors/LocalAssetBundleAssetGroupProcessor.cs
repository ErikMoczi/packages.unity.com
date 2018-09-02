using System.ComponentModel;
using UnityEngine;
using UnityEngine.ResourceManagement;

namespace UnityEditor.AddressableAssets
{
    /// <summary>
    /// TODO - doc
    /// </summary>
    [Description("Local Packed Content")]
    public class LocalAssetBundleAssetGroupProcessor : AssetBundleAssetGroupProcessor
    {
        [SerializeField]
        Vector2 position = new Vector2();
        internal override void OnDrawGUI(AddressableAssetSettings settings, Rect rect)
        {
            GUILayout.BeginArea(rect);
            position = EditorGUILayout.BeginScrollView(position, false, false, GUILayout.MaxWidth(rect.width));
            EditorGUILayout.LabelField("Assets in this group will be packed together in the StreamingAssets folder and will delivered with the game.");
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        internal override string displayName { get { return "Local Packed Content"; } }


        protected override string GetBuildPath(AddressableAssetSettings settings)
        {
            return "Assets/StreamingAssets";
        }

        protected override string GetBundleLoadPath(AddressableAssetSettings settings, string bundleName)
        {
            return "{UnityEngine.Application.streamingAssetsPath}/" + bundleName;
        }

        protected override string GetBundleLoadProvider(AddressableAssetSettings settings)
        {
            return typeof(LocalAssetBundleProvider).FullName;
        }

        protected override BundleMode GetBundleMode(AddressableAssetSettings settings)
        {
            return BundleMode.PackTogether;
        }
    }
}
