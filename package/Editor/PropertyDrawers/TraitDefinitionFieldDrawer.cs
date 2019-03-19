using System;
using Unity.AI.Planner.DomainLanguage.TraitBased;
using UnityEngine;
using UnityEngine.AI.Planner.DomainLanguage.TraitBased;

namespace UnityEditor.AI.Planner.DomainLanguage.TraitBased
{
    [CustomPropertyDrawer(typeof(TraitDefinitionField))]
    class TraitDefinitionFieldDrawer : PropertyDrawer
    {
        static string[][] s_DefaultTypes =
        {
            // These two arrays must stay in sync
            new[]
            {
                "bool",
                "float",
                "int",
                "string",
                "Transform",
                "DomainObject",
                "Other..."
            },
            new[]
            {
                "System.Boolean",
                "System.Single",
                "System.Int64",
                "System.String",
                "UnityEngine.Transform,UnityEngine",
                typeof(DomainObjectID).FullName,
                "Other..."
            }
        };

        // Draw the property inside the given rect
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
                EditorGUILayout.PropertyField(property.FindPropertyRelative("m_Name"));

                var typeProperty = property.FindPropertyRelative("m_Type");
                var index = Array.IndexOf(s_DefaultTypes[1], typeProperty.stringValue);
                if (index < 0 && !string.IsNullOrEmpty(typeProperty.stringValue))
                {
                    EditorGUILayout.PropertyField(typeProperty);
                    return;
                }

                EditorGUI.BeginChangeCheck();
                index = EditorGUILayout.Popup("Type", index, s_DefaultTypes[0]);
                if (EditorGUI.EndChangeCheck())
                    typeProperty.stringValue = index == s_DefaultTypes[1].Length - 1 ? "Custom" : s_DefaultTypes[1][index];
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return -2f;
        }
    }
}
