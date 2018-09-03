using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.U2D.Interface;
using UnityEngine.Experimental.U2D;
using UnityEditor.U2D.Interface;
using UnityEditor.Experimental.U2D;
using System;
using System.Collections.Generic;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal class SliderWeightTool : IGUITool
    {
        private const float kHelpBoxHeight = 44f;

        private static class Contents
        {
            public static readonly GUIContent mode = new GUIContent("Mode");
            public static readonly GUIContent selectedBone = new GUIContent("Bone", "");
            public static readonly GUIContent autoNormalize = new GUIContent("Normalize");
            public static readonly GUIContent amount = new GUIContent("Amount", "");
            public static readonly GUIContent helpMessage = new GUIContent("Select a bone.");
        }

        public WeightEditor weightEditor { get; set; }
        public int controlID { get { return -1; } }

        public float GetInspectorHeight()
        {
            float height = MeshModuleUtility.kEditorLineHeight * 4f + 2f;

            if (weightEditor.boneIndex == -1)
                height += kHelpBoxHeight;

            if (weightEditor.mode == WeightEditorMode.Smooth)
                height = MeshModuleUtility.kEditorLineHeight * 3f + 2f;

            return height;
        }

        public void OnInspectorGUI()
        {
            weightEditor.mode = (WeightEditorMode)EditorGUILayout.EnumPopup(Contents.mode, weightEditor.mode);

            if (weightEditor.mode != WeightEditorMode.Smooth)
                weightEditor.boneIndex = EditorGUILayout.Popup(Contents.selectedBone, weightEditor.boneIndex, weightEditor.boneNames);

            weightEditor.autoNormalize = EditorGUILayout.Toggle(Contents.autoNormalize, weightEditor.autoNormalize);

            if (GUI.GetNameOfFocusedControl() == "slider" &&
                (Event.current.type == EventType.MouseUp && Event.current.button == 0 ||
                 Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter) ||
                 Event.current.type == EventType.Ignore))
            {
                weightEditor.OnEditEnd();
                m_SliderValue = 0f;
                m_EditStarted = false;
                GUI.changed = true;
            }

            GUI.SetNextControlName("slider");

            float min = -1f;
            float max = 1f;

            if (weightEditor.mode == WeightEditorMode.Smooth)
            {
                min = 0f;
                max = 8f;
            }

            EditorGUIUtility.labelWidth = 70f;

            EditorGUI.BeginChangeCheck();

            using (new EditorGUI.DisabledGroupScope(weightEditor.boneIndex == -1 && weightEditor.mode != WeightEditorMode.Smooth))
            {
                m_SliderValue = EditorGUILayout.Slider(Contents.amount, m_SliderValue, min, max);
            }

            EditorGUIUtility.labelWidth = 0f;

            if (EditorGUI.EndChangeCheck())
            {
                if (!m_EditStarted)
                {
                    weightEditor.OnEditStart(false);
                    m_EditStarted = true;
                }

                weightEditor.DoEdit(m_SliderValue);
            }

            if (weightEditor.boneIndex == -1 && weightEditor.mode != WeightEditorMode.Smooth)
                EditorGUILayout.HelpBox(Contents.helpMessage.text, MessageType.Info, true);
        }

        public void OnGUI()
        {
        }

        private float m_SliderValue;
        private bool m_EditStarted = false;
    }
}
