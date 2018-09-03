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
    internal class BrushWeightTool : IGUITool
    {
        private const float kHelpBoxHeight = 44f;

        private static class Contents
        {
            public static readonly GUIContent mode = new GUIContent("Mode");
            public static readonly GUIContent selectedBone = new GUIContent("Bone", "");
            public static readonly GUIContent autoNormalize = new GUIContent("Normalize");
            public static readonly GUIContent size = new GUIContent("Size", "");
            public static readonly GUIContent hardness = new GUIContent("Hardness");
            public static readonly GUIContent step = new GUIContent("Step");
            public static readonly GUIContent helpMessage = new GUIContent("Select a bone.");
        }

        private const float k_WheelRadiusSpeed = 1f;

        public WeightEditor weightEditor { get; set; }
        public float hardness { get; set; }
        public float step { get; set; }
        public float radius
        {
            get { return m_CircleVertexSelector.radius; }
            set { m_CircleVertexSelector.radius = value; }
        }

        public int controlID { get { return m_ControlID; } }

        public ISelection selection { get { return m_Selection; } }

        private bool isSecondaryAction
        {
            get
            {
                return weightEditor.mode != WeightEditorMode.Smooth && EditorGUI.actionKey;
            }
        }

        private int m_ControlID = -1;

        public BrushWeightTool()
        {
            radius = 25f;
            step = 20f;
        }

        public float GetInspectorHeight()
        {
            float height = MeshModuleUtility.kEditorLineHeight * 6f + 2f;

            if (weightEditor.boneIndex == -1)
                height += kHelpBoxHeight;

            if (weightEditor.mode == WeightEditorMode.Smooth)
                height = MeshModuleUtility.kEditorLineHeight * 5f + 2f;

            return height;
        }

        public void OnInspectorGUI()
        {
            Debug.Assert(weightEditor != null);

            weightEditor.mode = (WeightEditorMode)EditorGUILayout.EnumPopup(Contents.mode, weightEditor.mode);

            if (weightEditor.mode != WeightEditorMode.Smooth)
                weightEditor.boneIndex = EditorGUILayout.Popup(Contents.selectedBone, weightEditor.boneIndex, MeshModuleUtility.GetBoneNameList(weightEditor.spriteMeshData));

            weightEditor.autoNormalize = EditorGUILayout.Toggle(Contents.autoNormalize, weightEditor.autoNormalize);

            radius = EditorGUILayout.FloatField(Contents.size, radius);
            radius = Mathf.Max(1f, radius);

            EditorGUIUtility.labelWidth = 70f;

            hardness = EditorGUILayout.Slider(Contents.hardness, hardness, 1f, 100f);
            step = EditorGUILayout.Slider(Contents.step, step, 1f, 100f);

            EditorGUIUtility.labelWidth = 0f;

            if (weightEditor.boneIndex == -1 && weightEditor.mode != WeightEditorMode.Smooth)
                EditorGUILayout.HelpBox(Contents.helpMessage.text, MessageType.Info, true);
        }

        public void OnGUI()
        {
            m_ControlID = GUIUtility.GetControlID("BrushWeightEditor".GetHashCode(), FocusType.Passive);

            EventType eventType = Event.current.GetTypeForControl(controlID);

            weightEditor.selection = m_Selection;

            m_CircleVertexSelector.vertices = weightEditor.spriteMeshData.vertices;

            Vector2 position = MeshModuleUtility.GUIToWorld(Event.current.mousePosition);

            if (eventType == EventType.Layout && !Event.current.alt)
            {
                HandleUtility.AddControl(controlID, 0f);
            }

            if (GUIUtility.hotControl == 0 && HandleUtility.nearestControl == controlID &&
                eventType == EventType.MouseDown && Event.current.button == 0 && !Event.current.alt)
            {
                GUIUtility.hotControl = controlID;
                GUI.changed = true;

                m_DeltaAcc = 0f;
                m_LastPosition = position;

                weightEditor.OnEditStart(true);
                OnBrush(hardness / 100f, position);

                Event.current.Use();
            }

            if (GUIUtility.hotControl == controlID && eventType == EventType.MouseUp && Event.current.button == 0)
            {
                GUIUtility.hotControl = 0;

                weightEditor.OnEditEnd();

                GUI.changed = true;
                Event.current.Use();
            }

            if (HandleUtility.nearestControl == controlID && Event.current.shift && eventType == EventType.ScrollWheel)
            {
                float radiusDelta = HandleUtility.niceMouseDeltaZoom * k_WheelRadiusSpeed;
                radius = Mathf.Max(1f, radius + radiusDelta);

                UpdateSelection(position);

                Event.current.Use();
            }

            if (GUIUtility.hotControl == 0 && HandleUtility.nearestControl == controlID && eventType == EventType.MouseMove)
            {
                UpdateSelection(position);

                Event.current.Use();
            }

            if (GUIUtility.hotControl == controlID && eventType == EventType.MouseDrag)
            {
                step = Mathf.Max(step, 1f);

                Vector2 delta = position - m_LastPosition;
                Vector2 direction = delta.normalized;
                Vector2 startPosition = m_LastPosition - direction * m_DeltaAcc;
                float magnitude = delta.magnitude;

                m_DeltaAcc += magnitude;

                if (m_DeltaAcc >= step)
                {
                    Vector2 stepVector = direction * step;
                    Vector2 currentPosition = startPosition;

                    while (m_DeltaAcc >= step)
                    {
                        currentPosition += stepVector;

                        OnBrush(hardness / 100f, currentPosition);

                        m_DeltaAcc -= step;
                    }
                }

                m_LastPosition = position;

                GUI.changed = true;
                Event.current.Use();
            }

            if ((GUIUtility.hotControl == controlID || (GUIUtility.hotControl == 0 && HandleUtility.nearestControl == controlID)) && eventType == EventType.Repaint)
            {
                Color oldColor =  Handles.color;

                Handles.color = Color.white;

                if (isSecondaryAction)
                    Handles.color = Color.red;

                if (GUIUtility.hotControl == controlID)
                    Handles.color = Color.yellow;

                Handles.DrawWireDisc(position, Vector3.forward, radius);

                Handles.color = oldColor;
            }
        }

        private void UpdateSelection(Vector2 position)
        {
            m_Selection.Clear();
            m_Selection.BeginSelection();

            m_CircleVertexSelector.selection = m_Selection;
            m_CircleVertexSelector.position = position;
            m_CircleVertexSelector.Select();

            m_Selection.EndSelection(true);
        }

        private void OnBrush(float hardness, Vector2 position)
        {
            UpdateSelection(position);

            weightEditor.emptySelectionEditsAll = false;

            if (isSecondaryAction)
                hardness *= -1f;

            weightEditor.DoEdit(hardness);
        }

        private float m_DeltaAcc = 0f;
        private Vector2 m_LastPosition = Vector2.zero;
        private SerializableSelection m_Selection = new SerializableSelection();
        private CircleVertexSelector m_CircleVertexSelector = new CircleVertexSelector();
    }
}
