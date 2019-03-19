using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AI.Planner.Utility;
using UnityEngine;
using UnityEngine.AI.Planner.DomainLanguage.TraitBased;

namespace UnityEditor.AI.Planner.DomainLanguage.TraitBased
{
    [CustomPropertyDrawer(typeof(ParameterDefinition))]
    class ParameterDefinitionDrawer : PropertyDrawer
    {
        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUILayout.PropertyField(property);

            if (!property.isExpanded)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative("m_Name"));

                var planDefinition = (PlanDefinition)property.serializedObject.targetObject;
                var domainDefinition = planDefinition.DomainDefinition;
                var aliasDefinitions = domainDefinition.AliasDefinitions.ToList();
                var aliasesList = new List<string> { "Select to populate..." };

                foreach (var aliasDefinition in aliasDefinitions)
                    aliasesList.Add(aliasDefinition.Name);

                var aliases = aliasesList.ToArray();

                var traitTypesProperty = property.FindPropertyRelative("m_IncludeTraitTypes");
                EditorGUILayout.PropertyField(traitTypesProperty, new GUIContent("Required Traits"));

                if (traitTypesProperty.isExpanded)
                    TraitTypesPropertyField(aliases, aliasDefinitions, traitTypesProperty);

                traitTypesProperty = property.FindPropertyRelative("m_ExcludeTraitTypes");
                EditorGUILayout.PropertyField(traitTypesProperty, new GUIContent("Prohibited Traits"));

                if (traitTypesProperty.isExpanded)
                    TraitTypesPropertyField(aliases, aliasDefinitions, traitTypesProperty);
            }
        }

        static void TraitTypesPropertyField(string[] aliases, List<AliasDefinition> aliasDefinitions, SerializedProperty traitTypesProperty)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUI.BeginChangeCheck();
                var setAliasIndex = EditorGUILayout.Popup("Alias", 0, aliases);
                if (EditorGUI.EndChangeCheck() && setAliasIndex > 0)
                {
                    var aliasDefinition = aliasDefinitions[setAliasIndex - 1];
                    traitTypesProperty.ClearArray();
                    var i = 0;
                    foreach (var traitType in aliasDefinition.TraitTypes)
                    {
                        traitTypesProperty.InsertArrayElementAtIndex(i);
                        traitTypesProperty.GetArrayElementAtIndex(i).stringValue = traitType;
                        i++;
                    }
                }

                traitTypesProperty.ForEachArrayElement(traitType =>
                {
                    EditorGUILayout.PropertyField(traitType);
                }, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return -2f;
        }
    }
}
