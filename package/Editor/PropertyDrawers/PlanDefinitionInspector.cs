using System;
using UnityEditor.AI.Planner.Utility;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.AI.Planner.DomainLanguage.TraitBased;

namespace UnityEditor.AI.Planner.DomainLanguage.TraitBased
{
    [CustomEditor(typeof(PlanDefinition))]
    class PlanDefinitionInspector : Editor
    {
        public static readonly string MissingDomainDefinitionMessage = "You must assign a domain definition first in order to edit a plan definition.";

        public override void OnInspectorGUI()
        {
            var planDefinition = (PlanDefinition)target;
            var assetPath = AssetDatabase.GetAssetPath(planDefinition);
            var assetOnDisk = !string.IsNullOrEmpty(assetPath);
            var editable = !assetOnDisk || AssetDatabaseUtility.IsEditable(assetPath);

            if (!editable)
            {
                EditorGUILayout.HelpBox("This file is currently read-only. You probably need to check it out from version control.",
                    MessageType.Info);
            }

            var domainDefinitionAssigned = planDefinition.DomainDefinition != null;
            if (!domainDefinitionAssigned)
                EditorGUILayout.HelpBox(MissingDomainDefinitionMessage, MessageType.Error);

            GUI.enabled = editable && assetOnDisk && domainDefinitionAssigned;
            if (GUILayout.Button("Edit Plan Definition"))
            {
                PlanEditorWindow.ShowWindow(planDefinition);
            }

            GUI.enabled = domainDefinitionAssigned;
            if (GUILayout.Button("Generate Classes"))
            {
                planDefinition.GenerateClasses();
            }

            EditorGUILayout.Separator();

            GUI.enabled = editable;

            DrawDefaultInspector();
        }

        [OnOpenAsset(0)]
        static bool OnOpenAsset(int instanceID, int line)
        {
            var path = AssetDatabase.GetAssetPath(instanceID);
            var planDefinition = AssetDatabase.LoadAssetAtPath<PlanDefinition>(path);

            if (planDefinition && AssetDatabaseUtility.IsEditable(path))
            {
                var domainDefinitionAssigned = planDefinition.DomainDefinition != null;

                if (domainDefinitionAssigned)
                {
                    PlanEditorWindow.ShowWindow(planDefinition);
                    return true;
                }

                Debug.LogError(MissingDomainDefinitionMessage, planDefinition);
            }

            return false;
        }
    }
}
