using System.Collections.Generic;
using UnityEditor.AI.Planner.Utility;
using UnityEditor.AI.Planner.Visualizer;
using UnityEngine;
using UnityEngine.AI.Planner.DomainLanguage.TraitBased;
using UnityObject = UnityEngine.Object;


namespace UnityEditor.AI.Planner.DomainLanguage.TraitBased
{
    [CustomPropertyDrawer(typeof(PlanningDomainData))]
    class PlanningDomainDataDrawer : PropertyDrawer
    {
        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUILayout.PropertyField(property);

            if (!property.isExpanded)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                var planDefinitionProperty = property.FindPropertyRelative("m_PlanDefinition");
                EditorGUILayout.PropertyField(planDefinitionProperty);

                var planDefinition = planDefinitionProperty.objectReferenceValue as PlanDefinition;
                if (planDefinition)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(GUIContent.none);
                    if (GUILayout.Button("Edit Plan Definition", EditorStyles.miniButtonLeft))
                        PlanEditorWindow.ShowWindow(planDefinition);
                    GUILayout.Space(2f);
                    if (GUILayout.Button("View Plan", EditorStyles.miniButtonRight))
                        PlanVisualizerWindow.ShowWindow();
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    return;
                }

                var initialDomainObjectData = property.FindPropertyRelative("m_InitialDomainObjectData");
                var importedObjects = new HashSet<GameObject>();
                initialDomainObjectData.ForEachArrayElement(domainObject =>
                {
                    var sourceObject = domainObject.FindPropertyRelative("m_SourceObject").objectReferenceValue;
                    if (sourceObject)
                        importedObjects.Add((GameObject)sourceObject);
                });

                // Declare objects by name so we can specify DO links by name.
                initialDomainObjectData.DrawArrayProperty();
                property.FindPropertyRelative("m_InitialStateTraitData").DrawArrayProperty();
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return -2f;
        }
    }
}
