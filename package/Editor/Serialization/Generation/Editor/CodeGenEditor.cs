#if NET_4_6
using System;
using System.Collections.Generic;
using Unity.Properties;
using Unity.Properties.Serialization;
using UnityEditor;
using UnityEngine;

namespace Unity.Properties.Editor.Serialization
{
    public class CodeGenEditorWindow : EditorWindow
    {
        private Vector2 m_SchemaEditorScroll;
        private Vector2 m_SchemaJsonScroll;
        private Vector2 m_CodeJsonScroll;

        private Vector2 m_DataEditorScroll;
        private Vector2 m_DataJsonnScroll;

        private string m_SchemaJson = @"
[{
    ""SchemaVersion"": 1,
        ""Types"": [
        {
            ""TypeId"": ""1"",
            ""Name"": ""HelloWorld"",
            ""Properties"": {
                ""Data"": {
                    ""TypeId"": ""int"",
                    ""DefaultValue"": ""5""
                }
            }
        }
        ]
    }]
";
        private string m_CodeContent;

        [MenuItem("Properties/CodeGen/CSharp")]
        public static void ShowCodeGen()
        {
            var window = GetWindow<CodeGenEditorWindow>();

            window.m_CodeContent = string.Empty;

            window.minSize = new Vector2(450, 200);
            window.titleContent = new GUIContent("JSON -> CSharp Generation");
        }
        
        private void OnEnable()
        {
            m_CodeContent = string.Empty;
        }

        private void OnGUI()
        {
            var halfWidth = position.width * 0.5f;

            using (new EditorGUILayout.VerticalScope())
            {
                GUILayout.Label("Json Schema", EditorStyles.largeLabel);

                if (GUILayout.Button(">> TO CODE", GUILayout.Width(120)))
                {
                    var backend = new CSharpGenerationBackend();
                    var result = JsonSchema.FromJson(m_SchemaJson);
                    backend.Generate(result);

                    m_CodeContent = backend.Code.ToString();
                }
            }

            m_SchemaJsonScroll = EditorGUILayout.BeginScrollView(
                m_SchemaJsonScroll,
                GUILayout.Height(position.height)
                );
            m_SchemaJson = EditorGUI.TextArea(
                new Rect(0, 0, halfWidth, position.height),
                m_SchemaJson,
                EditorStyles.textArea
                );
            EditorGUILayout.EndScrollView();
            EditorGUI.SelectableLabel(
                new Rect(halfWidth, 0, halfWidth, position.height),
                m_CodeContent,
                EditorStyles.textArea
                );
        }
    }
}
#endif // NET_4_6
