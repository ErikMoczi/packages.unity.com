using System;
using UnityEditor.AI.Planner.Utility;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.AI.Planner.DomainLanguage.TraitBased;

namespace UnityEditor.AI.Planner.DomainLanguage.TraitBased
{
    [CustomEditor(typeof(DomainDefinition))]
    class DomainDefinitionInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            var domain = (DomainDefinition)target;
            var assetPath = AssetDatabase.GetAssetPath(domain);
            var assetOnDisk = !string.IsNullOrEmpty(assetPath);
            var editable = !assetOnDisk || AssetDatabaseUtility.IsEditable(assetPath);

            if (!editable)
            {
                EditorGUILayout.HelpBox("This file is currently read-only. You probably need to check it out from version control.",
                    MessageType.Info);
            }

            GUI.enabled = editable && assetOnDisk;
            if (GUILayout.Button("Edit Domain Definition"))
            {
                DomainEditorWindow.ShowWindow(domain);
            }

            GUI.enabled = true;
            if (GUILayout.Button("Generate Classes"))
            {
                domain.GenerateClasses();
            }

            EditorGUILayout.Separator();

            GUI.enabled = editable;
            DrawDefaultInspector();
        }

        [OnOpenAsset(0)]
        static bool OnOpenAsset(int instanceID, int line)
        {
            string path = AssetDatabase.GetAssetPath(instanceID);
            var domain = AssetDatabase.LoadAssetAtPath<DomainDefinition>(path);

            if (domain && AssetDatabaseUtility.IsEditable(path))
            {
                DomainEditorWindow.ShowWindow(domain);
                return true;
            }

            return false;
        }
    }
}
