using UnityEngine;

namespace UnityEditor.Experimental.U2D.Common
{
    internal static class InternalEditorBridge
    {
        public static EditorWindow GetCurrentInspectorWindow()
        {
        	return InspectorWindow.s_CurrentInspectorWindow;
        }

        public static Vector3 GetSnapSettingMove()
        {
        	return SnapSettings.move;
        }

        public static void RenderSortingLayerFields(SerializedProperty order, SerializedProperty layer)
        {
        	SortingLayerEditorUtility.RenderSortingLayerFields(order, layer);
        }

        public static void RepaintImmediately(EditorWindow window)
        {
        	window.RepaintImmediately();
        }

        public static ISpriteEditorDataProvider GetISpriteEditorDataProviderFromPath(string importedAsset)
        {
            return AssetImporter.GetAtPath(importedAsset) as ISpriteEditorDataProvider;
        }

        public static void GenerateOutline(Texture2D texture, Rect rect, float detail, byte alphaTolerance, bool holeDetection, out Vector2[][] paths)
        {
            SpriteUtility.GenerateOutline(texture, rect, detail, alphaTolerance, holeDetection, out paths);
        }

        public static bool DoesHardwareSupportsFullNPOT()
        {
            return ShaderUtil.hardwareSupportsFullNPOT;
        }

        public static Texture2D CreateTemporaryDuplicate(Texture2D tex, int width, int height)
        {
            return UnityEditor.SpriteUtility.CreateTemporaryDuplicate(tex, width, height);
        }

        public static void ShowSpriteEditorWindow()
        {
            var window = EditorWindow.GetWindow<SpriteEditorWindow>();
            window.Show();
        }

        public static void ApplyWireMaterial()
        {
            HandleUtility.ApplyWireMaterial();
        }
    }
}
