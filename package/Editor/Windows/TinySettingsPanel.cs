

using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal abstract class TinySettingsPanel : TinyPanel
    {
        protected static void AssetNameField(IPersistentObject obj)
        {
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField("Project Name", obj.Name);

                var path = AssetDatabase.GUIDToAssetPath(obj.PersistenceId);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = false;
                    EditorGUILayout.ObjectField("Project Asset", AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object)), typeof(UnityEngine.Object), false);
                    GUI.enabled = true;
                }
            }
        }
        
        protected static string NamespaceField(string label, string @namespace)
        {
            EditorGUI.BeginChangeCheck();
            
            var newNamespace = EditorGUILayout.DelayedTextField(label, @namespace);
            
            if (!EditorGUI.EndChangeCheck())
            {
                return @namespace;
            }

            if (string.IsNullOrEmpty(newNamespace))
            {
                Debug.LogWarning("Namespace can not be empty.");
                return @namespace;
            }
            
            if (!TinyUtility.IsValidNamespaceName(newNamespace))
            {
                Debug.LogWarning($"{newNamespace} is not a valid namespace. Must be a valid code name");
                return @namespace;
            }

            return newNamespace;
        }
        
        protected static void TextureSettingsField(TinyTextureSettings textureSettings)
        {
            textureSettings.FormatType = (TextureFormatType) EditorGUILayout.EnumPopup("Default Texture Format", textureSettings.FormatType);
                
            switch (textureSettings.FormatType)
            {
                case TextureFormatType.JPG:
                    textureSettings.JpgCompressionQuality = EditorGUILayout.IntSlider("Compression Quality", textureSettings.JpgCompressionQuality, 1, 100);
                    break;
                case TextureFormatType.WebP:
                    textureSettings.WebPCompressionQuality = EditorGUILayout.IntSlider("Compression Quality", textureSettings.WebPCompressionQuality, 1, 100);
                    break;
                case TextureFormatType.Source:
                    break;
                case TextureFormatType.PNG:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            EditorGUILayout.Space();
        }

        protected static string DescriptionField(string label, string summary)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                summary = EditorGUILayout.TextArea(summary, EditorStyles.textArea, GUILayout.Height(50));
            }

            return summary;
        }
    }
}

