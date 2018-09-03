#if (NET_4_6 || NET_STANDARD_2_0)

using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using Unity.Properties;
using Unity.Properties.Serialization;
using UnityEditor;
using UnityEngine;

namespace Unity.Properties.Editor.Serialization
{

#if ENABLE_PROPERTIES_DEBUG_EDITOR_WINDOWS
    
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
                    m_AssemblyPath = EditorGUILayout.TextField("Assembly file path: ", m_AssemblyPath);

                    if ( ! string.IsNullOrWhiteSpace(m_AssemblyPath) && File.Exists(m_AssemblyPath))
                    {
                        var propertyTypeTree = ReflectionPropertyTree.Read(m_AssemblyPath);

                        m_JsonContent = JsonSchema.ToJson(new JsonSchema()
                        {
                            PropertyTypeNodes = propertyTypeTree
                        });

                        var validator = new JsonSchemaValidator();
                        var validationResult = validator.ValidatePropertyDefinition(m_JsonContent);
                        Debug.Log($"Json Schema validation results: {validationResult.IsValid}, {validationResult.Error}");

                        m_JsonContent = JsonSchema.ToJson(propertyTypeTree);
                        
                        if (! string.IsNullOrEmpty(m_JsonContent))
                        {
                            var backend = new CSharpGenerationBackend();
                            backend.Generate(propertyTypeTree);

                            var generationFilePath = Path.GetDirectoryName(m_AssemblyPath);
                            if (generationFilePath != null)
                            {
                                var jsonFilePath = Path.Combine(
                                    generationFilePath,
                                    $"GeneratedSchema{Path.GetFileNameWithoutExtension(m_AssemblyPath)}.json");
                                File.WriteAllText(jsonFilePath, m_JsonContent);

                                Debug.Log($"Generated JSON schema written to: {jsonFilePath}");

                                var generatedFilePath = Path.Combine(
                                    generationFilePath,
                                    $"GeneratedSchema{Path.GetFileNameWithoutExtension(m_AssemblyPath)}.cs");
                                File.WriteAllText(generatedFilePath, backend.Code);

                                Debug.Log($"Generated code written to: {generatedFilePath}");
                            }
                        }
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

#endif

}

#endif // (NET_4_6 || NET_STANDARD_2_0)
