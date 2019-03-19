using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Planner.Agent;
using UnityEditor.AI.Planner.Utility;
using UnityEngine;
using UnityEngine.AI.Planner.DomainLanguage.TraitBased;

namespace UnityEditor.AI.Planner.DomainLanguage.TraitBased
{
    [CustomPropertyDrawer(typeof(ActionDefinition))]
    class ActionDefinitionDrawer : ConditionBasedDrawer
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

                var parameters = property.FindPropertyRelative("m_Parameters");
                EditorGUILayout.PropertyField(parameters, true);

                var actionDefinition = this.GetValue<ActionDefinition>(property);
                var actionParameters = actionDefinition.Parameters.ToList();

                var operatorWidth = GUILayout.Width(40f);

                var preconditionsProperty = property.FindPropertyRelative("m_Preconditions");
                EditorGUILayout.PropertyField(preconditionsProperty);
                if (preconditionsProperty.isExpanded)
                    using (new EditorGUI.IndentLevelScope())
                    {
                        preconditionsProperty.ForEachArrayElement(precondition =>
                        {
                            using (new EditorGUI.IndentLevelScope())
                            {
                                EditorGUILayout.BeginHorizontal();
                                GUILayout.FlexibleSpace();

                                var operandA = precondition.FindPropertyRelative("m_OperandA");
                                OperandPropertyField(operandA, actionParameters);

                                var @operator = precondition.FindPropertyRelative("m_Operator");
                                string[] operators =
                                {
                                    "==",
                                    "!=",
                                    "<",
                                    ">",
                                    "<=",
                                    ">="
                                };
                                var indentLevel = EditorGUI.indentLevel;
                                EditorGUI.indentLevel = 0;
                                var opIndex = EditorGUILayout.Popup(Array.IndexOf(operators, @operator.stringValue),
                                    operators,
                                    operatorWidth);
                                if (opIndex >= 0)
                                    @operator.stringValue = operators[opIndex];
                                EditorGUI.indentLevel = indentLevel;

                                OperandPropertyField(precondition.FindPropertyRelative("m_OperandB"), actionParameters,
                                    GetPossibleValues(operandA));

                                EditorGUILayout.EndHorizontal();
                            }
                        }, true);
                    }

                var effectsProperty = property.FindPropertyRelative("m_Effects");
                EditorGUILayout.PropertyField(effectsProperty);
                if (effectsProperty.isExpanded)
                    using (new EditorGUI.IndentLevelScope())
                    {

                        EditorGUILayout.LabelField("Created Objects");
                        var createdObjectsProperty = property.FindPropertyRelative("m_CreatedObjects");
                        using (new EditorGUI.IndentLevelScope())
                        {
                            createdObjectsProperty.ForEachArrayElement(
                                createdObject => { EditorGUILayout.PropertyField(createdObject); }, true);
                        }

                        var effectsParameters = actionDefinition.CreatedObjects.ToList();
                        effectsParameters.AddRange(actionParameters);

                        EditorGUILayout.LabelField("Changes");
                        using (new EditorGUI.IndentLevelScope())
                        {
                            effectsProperty.ForEachArrayElement(effect =>
                            {
                                using (new EditorGUI.IndentLevelScope())
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    GUILayout.FlexibleSpace();

                                    var operandA = effect.FindPropertyRelative("m_OperandA");
                                    OperandPropertyField(operandA, effectsParameters);

                                    var @operator = effect.FindPropertyRelative("m_Operator");
                                    string[] operators =
                                    {
                                        "=",
                                        "+=",
                                        "-="
                                    };
                                    var indentLevel = EditorGUI.indentLevel;
                                    EditorGUI.indentLevel = 0;
                                    var opIndex = EditorGUILayout.Popup(Array.IndexOf(operators, @operator.stringValue),
                                        operators,
                                        operatorWidth);
                                    if (opIndex >= 0)
                                        @operator.stringValue = operators[opIndex];
                                    EditorGUI.indentLevel = indentLevel;

                                    OperandPropertyField(effect.FindPropertyRelative("m_OperandB"), effectsParameters,
                                        GetPossibleValues(operandA));

                                    EditorGUILayout.EndHorizontal();
                                }
                            }, true);
                        }

                        var deletedObjectsProperty = property.FindPropertyRelative("m_RemovedObjects");
                        EditorGUILayout.LabelField("Removed Objects");

                        using (new EditorGUI.IndentLevelScope())
                        {
                            deletedObjectsProperty.ForEachArrayElement(deletedObject =>
                            {
                                EditorGUILayout.BeginHorizontal();
                                GUILayout.FlexibleSpace();
                                OperandPropertyField(deletedObject, effectsParameters);
                                EditorGUILayout.EndHorizontal();
                            }, true);
                        }

                    }

                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(property.FindPropertyRelative("m_Reward"), new GUIContent("Cost (-) / Reward (+)"));

                var operationalActionType = property.FindPropertyRelative("m_OperationalActionType");
                var operationalActions = new List<Type>();
                typeof(IOperationalAction).GetImplementationsOfInterface(operationalActions);
                var operationalActionNames = operationalActions.Where(t => !t.IsGenericType).Select(t => t.Name).ToArray();

                var operationalActionIndex = Array.IndexOf(operationalActionNames, operationalActionType.stringValue);
                EditorGUI.BeginChangeCheck();
                operationalActionIndex = EditorGUILayout.Popup("Operational Action", operationalActionIndex, operationalActionNames);
                if (EditorGUI.EndChangeCheck())
                    operationalActionType.stringValue = operationalActionNames[operationalActionIndex];
            }
        }
    }
}
