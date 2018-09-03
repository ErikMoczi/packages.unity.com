#if NET_4_6
using System;
using System.IO;
using System.Collections.Generic;
using Unity.Properties;
using Unity.Properties.Serialization;
using UnityEditor;
using UnityEngine;

namespace Unity.Properties.Editor.Serialization
{
    public class JsonGenEditorWindow : EditorWindow
    {
        private Vector2 m_SchemaEditorScroll;
        private Vector2 m_SchemaJsonScroll;
        private Vector2 m_CodeJsonScroll;

        private Vector2 m_DataEditorScroll;
        private Vector2 m_DataJsonnScroll;

        private string m_JsonContent;
        private string m_AssemblyPath;

        [MenuItem("Properties/CodeGen/JsonSchema")]
        public static void ShowCodeGen()
        {
            var window = GetWindow<JsonGenEditorWindow>();

            window.minSize = new Vector2(450, 200);
            window.titleContent = new GUIContent("Assembly -> JSON Generation");
        }
        
        private void OnEnable()
        {
            m_AssemblyPath = string.Empty;
            m_JsonContent = string.Empty;
        }

        private void OnGUI()
        {
            var halfHeight = position.height * 0.1f;

            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    m_AssemblyPath = EditorGUILayout.TextField("Assembly file path:", m_AssemblyPath);

                    if (!string.IsNullOrWhiteSpace(m_AssemblyPath) && File.Exists(m_AssemblyPath))
                    {
                        m_JsonContent = PropertyTypeNode.ToJson(ReflectionJsonSchemaGenerator.Read(m_AssemblyPath));
                    }
                }

                EditorGUI.SelectableLabel(
                    new Rect(0, halfHeight, position.width, position.height - halfHeight),
                    m_JsonContent,
                    EditorStyles.textArea
                );
            }
        }
    }
}
#endif // NET_4_6
