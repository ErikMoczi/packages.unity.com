using System;
using System.Linq;
using UnityEditor.AI.Planner.Utility;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI.Planner.DomainLanguage.TraitBased;
using UnityEngine.SceneManagement;

namespace UnityEditor.AI.Planner.DomainLanguage.TraitBased
{
    [CustomPropertyDrawer(typeof(TraitObjectData))]
    class TraitObjectDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUILayout.PropertyField(property);

            if (!property.isExpanded)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                var planDefinition = property.FindObjectOfType<PlanDefinition>();
                var domainDefinition = planDefinition.DomainDefinition;

                var traitDefinitionNameProperty = property.FindPropertyRelative("m_TraitDefinitionName");
                var traitDefinitionName = traitDefinitionNameProperty.stringValue;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(traitDefinitionNameProperty, new GUIContent("Type"));
                if (EditorGUI.EndChangeCheck())
                {
                    traitDefinitionName = traitDefinitionNameProperty.stringValue;
                    var replacementTraitDefinition = domainDefinition.TraitDefinitions.First(td => td.Name == traitDefinitionName);
                    var traitObjectData = this.GetValue<TraitObjectData>(property);//(TraitObjectData)EditorHelper.GetTargetObjectOfSerializedProperty(property);
                    traitObjectData.ClearFieldValues();
                    traitObjectData.InitializeFieldValues(replacementTraitDefinition, domainDefinition);
                    var serializedObj = property.serializedObject;
                    EditorUtility.SetDirty(serializedObj.targetObject);
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                    serializedObj.Update();
                    GUIUtility.ExitGUI();
                }

                if (domainDefinition == null || domainDefinition.TraitDefinitions == null
                    || domainDefinition.TraitDefinitions.All(td => td.Name != traitDefinitionName))
                    return;

                var traitDefinition = domainDefinition.TraitDefinitions.First(td => td.Name == traitDefinitionName);
                var fieldValuesProperty = property.FindPropertyRelative("m_FieldValues");
                var fieldName = new GUIContent();

                using (new EditorGUI.IndentLevelScope())
                {
                    fieldValuesProperty.ForEachArrayElement(field =>
                    {
                        var fieldLabel = field.FindPropertyRelative("m_Name").stringValue;
                        var traitField = traitDefinition.Fields.First(tf => tf.Name == fieldLabel);

                        FieldValueDrawer.PropertyField(field, domainDefinition.GetType(traitField.Type), fieldName);
                    });
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return -2;
        }
    }
}
