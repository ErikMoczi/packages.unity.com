using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Unity.Tiny
{
    [CustomEditor(typeof(TinyScriptableObject), true)]
    internal class TinyScriptableObjectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            var asset = target as TinyScriptableObject;
            
            if (!asset || asset.Icon == null)
            {
                return null;
            }
                
            var tex = new Texture2D(width, height);
            EditorUtility.CopySerialized(asset.Icon, tex);
            return tex;
        }
    }
}