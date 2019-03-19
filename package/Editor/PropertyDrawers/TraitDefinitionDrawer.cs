using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.AI.Planner.DomainLanguage.TraitBased;
using UnityEditor.AI.Planner.Utility;
using UnityEngine;
using UnityEngine.AI.Planner.DomainLanguage.TraitBased;

namespace UnityEditor.AI.Planner.DomainLanguage.TraitBased
{
    [CustomPropertyDrawer(typeof(TraitDefinition))]
    class TraitDefinitionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, null, property);
            EditorGUILayout.PropertyField(property);

            if (!property.isExpanded)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                var dynamic = property.FindPropertyRelative("m_Dynamic");
                EditorGUILayout.PropertyField(dynamic);

                var fieldsProperty = property.FindPropertyRelative("m_Fields");

                var nameProperty = property.FindPropertyRelative("m_Name");
                if (!dynamic.boolValue)
                {
                    var traitDefinitions = new List<Type>();
                    typeof(ICustomTrait).GetImplementationsOfInterface(traitDefinitions);
                    var traitDefinitionNames = traitDefinitions.Select(t => t.Name).ToArray();
                    var index = Array.IndexOf(traitDefinitionNames, nameProperty.stringValue);

                    EditorGUI.BeginChangeCheck();
                    index = EditorGUILayout.Popup("Name", index, traitDefinitionNames);
                    if (EditorGUI.EndChangeCheck())
                    {
                        nameProperty.stringValue = traitDefinitionNames[index];

                        var traitType = traitDefinitions[index];
                        var i = 0;
                        fieldsProperty.ClearArray();
                        foreach (var f in traitType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                        {
                            fieldsProperty.InsertArrayElementAtIndex(i);
                            var fieldProperty = fieldsProperty.GetArrayElementAtIndex(i);
                            var fieldNameProperty = fieldProperty.FindPropertyRelative("m_Name");
                            fieldNameProperty.stringValue = f.Name;
                            var fieldTypeProperty = fieldProperty.FindPropertyRelative("m_Type");
                            fieldTypeProperty.stringValue = f.PropertyType.AssemblyQualifiedName;
                        }
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(nameProperty);
                }

//                if (dynamic.boolValue)
//                {
                    EditorGUILayout.PropertyField(fieldsProperty, true);
//                    return;
//                }

            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return -2f;
        }
    }
}
