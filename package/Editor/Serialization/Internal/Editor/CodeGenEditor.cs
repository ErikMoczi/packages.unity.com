#if (NET_4_6 || NET_STANDARD_2_0)

using System;
using System.Collections.Generic;
using System.IO;
using Unity.Properties;
using Unity.Properties.Serialization;
using UnityEditor;
using UnityEngine;

namespace Unity.Properties.Editor.Serialization
{

#if ENABLE_PROPERTIES_DEBUG_EDITOR_WINDOWS

    public class CodeGenEditorWindow : EditorWindow
    {
        private Vector2 m_SchemaJsonScroll;

        private string m_SchemaJson = $@"
{{
    ""Version"": ""{JsonSchema.CurrentVersion}"",
    ""Types"":
    [
      {{
        ""Name"": ""HelloWorld"",
        ""Properties"":
        [
          {{
            ""Name"": ""Data"",
            ""Type"": ""int"",
            ""DefaultValue"": ""5"",
          }}
        ]
      }}
    ]
}}
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

                    backend.Generate(result.PropertyTypeNodes);

                    m_CodeContent = backend.Code;
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

#endif

}

#endif // (NET_4_6 || NET_STANDARD_2_0)
