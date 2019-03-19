using System;
using System.Collections.Generic;
using UnityEditor.AI.Planner.Utility;
using UnityEngine;
using UnityEngine.AI.Planner.DomainLanguage.TraitBased;

namespace UnityEditor.AI.Planner.DomainLanguage.TraitBased
{
    [CustomPropertyDrawer(typeof(StateTerminationDefinition))]
    class StateTerminationDefinitionDrawer : ConditionBasedDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var planDefinition = (PlanDefinition)property.serializedObject.targetObject;
            if (!planDefinition.DomainDefinition)
                return;

            EditorGUILayout.PropertyField(property);

            if (!property.isExpanded)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative("m_Name"));

                var parameters = property.FindPropertyRelative("m_ObjectParameters");
                EditorGUILayout.PropertyField(parameters);

                var terminalStateDefinition = this.GetValue<StateTerminationDefinition>(property); //EditorHelper.GetTargetObjectOfSerializedProperty(property);
                var objectParameters = new List<ParameterDefinition> { terminalStateDefinition.ObjectParameters };

                var operatorWidth = GUILayout.Width(40f);

                var criteriaProperty = property.FindPropertyRelative("m_Criteria");
                EditorGUILayout.PropertyField(criteriaProperty);
                if (criteriaProperty.isExpanded)
                    using (new EditorGUI.IndentLevelScope())
                    {
                        criteriaProperty.ForEachArrayElement(precondition =>
                        {
                            using (new EditorGUI.IndentLevelScope())
                            {
                                EditorGUILayout.BeginHorizontal();
                                GUILayout.FlexibleSpace();

                                var operandA = precondition.FindPropertyRelative("m_OperandA");
                                OperandPropertyField(operandA, objectParameters);

                                var @operator = precondition.FindPropertyRelative("m_Operator");
                                string[] operators =
                                {
                                    "==",
                                    "!=",
                                    "<",
                                    ">",
                                    "<=",
                                    ">=",
                                };
                                var indentLevel = EditorGUI.indentLevel;
                                EditorGUI.indentLevel = 0;
                                var opIndex = EditorGUILayout.Popup(Array.IndexOf(operators, @operator.stringValue),
                                    operators,
                                    operatorWidth);
                                if (opIndex >= 0)
                                    @operator.stringValue = operators[opIndex];
                                EditorGUI.indentLevel = indentLevel;

                                OperandPropertyField(precondition.FindPropertyRelative("m_OperandB"), objectParameters,
                                    GetPossibleValues(operandA));

                                EditorGUILayout.EndHorizontal();
                            }
                        }, true);
                    }

                EditorGUILayout.Space();
            }
        }
    }
}
