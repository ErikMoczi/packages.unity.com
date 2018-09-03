using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditorInternal;

namespace UnityEditor.U2D
{
    public class SplineEditor
    {
        private static class Contents
        {
            public static readonly GUIContent tangentStraightIcon = SpriteShapeEditorGUI.IconContent("TangentStraight", "Straight line from point to point.");
            public static readonly GUIContent tangentCurvedIcon = SpriteShapeEditorGUI.IconContent("TangentCurved", "Tangents mirror each others angle.");
            public static readonly GUIContent tangentAsymmetricIcon = SpriteShapeEditorGUI.IconContent("TangentAssymetric", "Tangents are not linked.");
            public static readonly GUIContent tangentStraightIconPro = SpriteShapeEditorGUI.IconContent("TangentStraightPro", "Straight line from point to point.");
            public static readonly GUIContent tangentCurvedIconPro = SpriteShapeEditorGUI.IconContent("TangentCurvedPro", "Tangents mirror each others angle.");
            public static readonly GUIContent tangentAsymmetricIconPro = SpriteShapeEditorGUI.IconContent("TangentAssymetricPro", "Tangents are not linked.");
            public static readonly GUIContent positionLabel = new GUIContent("Point Position", "Position of Control Point");
            public static readonly GUIContent leftTangentLabel = new GUIContent("Left Tangent", "Left Tangent end point.");
            public static readonly GUIContent rightTangentLabel = new GUIContent("Right Tangent", "Right Tangent end point.");
            public static readonly GUIContent enableSnapLabel = new GUIContent("Snapping", "Snap points using the snap settings");
            public static readonly GUIContent pointModeLabel = new GUIContent("Point Mode");

            public static readonly GUIContent heightLabel = new GUIContent("Height", "Height override for control point.");
            public static readonly GUIContent bevelSizeLabel = new GUIContent("Bevel Size", "Length of the curve around the corners.");
            public static readonly GUIContent bevelCutoffLabel = new GUIContent("Bevel Cutoff", "Angle at which corners turn to bevels.");
            public static readonly GUIContent spriteIndexLabel = new GUIContent("Sprite Index", "Index of sprite at this control point");
            public static readonly GUIContent cornerLabel = new GUIContent("Corner", "Set if Corner is automatic or disabled.");

            public static readonly int[] cornerValues = { 0, 1 };
            public static readonly GUIContent[] cornerOptions = { new GUIContent("Disabled"), new GUIContent("Automatic") };

            public static readonly GUIContent xLabel = new GUIContent("X");
            public static readonly GUIContent yLabel = new GUIContent("Y");
            public static readonly GUIContent zLabel = new GUIContent("Z");
        }

        Editor m_CurrentEditor;
        Spline m_Spline;
        SpriteShape m_SpriteShape;

        public Spline spline
        {
            get { return m_Spline; }
        }

        public SplineEditor(Editor editor, SpriteShape spriteShape)
        {
            m_CurrentEditor = editor;
            m_SpriteShape = spriteShape;

            Undo.undoRedoPerformed += OnUndoRedo;
        }

        public void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        void OnUndoRedo()
        {
            SceneView.RepaintAll();
        }

        public void OnInspectorGUI(Spline spline)
        {
            m_Spline = spline;

            EditorGUI.BeginChangeCheck();

            DoTangentGUI();

            if (EditMode.IsOwner(m_CurrentEditor))
                SnappingUtility.enabled = EditorGUILayout.Toggle(Contents.enableSnapLabel, SnappingUtility.enabled);

            if (ShapeEditorCache.GetSelection().Count > 0)
                DoPointInspector();

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(m_CurrentEditor.target);

            HandleHotKeys();
        }

        public void HandleHotKeys()
        {
            if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.M)
            {
                CycleTangentMode();
                Event.current.Use();
                GUI.changed = true;
            }

            if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.N)
            {
                CycleSpriteIndex();
                Event.current.Use();
                GUI.changed = true;
            }
        }

        public void CycleSpriteIndex()
        {
            ISelection selection = ShapeEditorCache.GetSelection();
            if (selection.single == -1)
                return;

            int nextIndex = SplineUtility.NextIndex(selection.single, m_Spline.GetPointCount());
            float angle = SpriteShapeHandleUtility.PosToAngle(m_Spline.GetPosition(selection.single), m_Spline.GetPosition(nextIndex), 0f);
            int angleRangeIndex = SpriteShapeEditorUtility.GetRangeIndexFromAngle(m_SpriteShape, angle);
            if (angleRangeIndex == -1)
                return;

            AngleRange angleRange = m_SpriteShape.angleRanges[angleRangeIndex];

            int spriteIndex = (m_Spline.GetSpriteIndex(selection.single) + 1) % angleRange.sprites.Count;

            Undo.RecordObject(m_CurrentEditor.target, "Edit Sprite Index");

            m_Spline.SetSpriteIndex(selection.single, spriteIndex);

            EditorUtility.SetDirty(m_CurrentEditor.target);
        }

        void DoTangentGUI()
        {
            ISelection selection = ShapeEditorCache.GetSelection();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PrefixLabel(Contents.pointModeLabel);

            ShapeTangentMode? tangentMode = null;

            if (selection.single != -1)
                tangentMode = m_Spline.GetTangentMode(selection.single);

            ShapeTangentMode? prevTangentMode = tangentMode;

            GUIContent tangentStraightIcon = Contents.tangentStraightIcon;
            GUIContent tangentCurvedIcon = Contents.tangentCurvedIcon;
            GUIContent tangentAsymmetricIcon = Contents.tangentAsymmetricIcon;

            if (EditorGUIUtility.isProSkin)
            {
                tangentStraightIcon = Contents.tangentStraightIconPro;
                tangentCurvedIcon = Contents.tangentCurvedIconPro;
                tangentAsymmetricIcon = Contents.tangentAsymmetricIconPro;
            }

            using (new EditorGUI.DisabledScope(selection.single == -1))
            {
                if (GUILayout.Toggle(tangentMode == ShapeTangentMode.Linear, tangentStraightIcon, new GUIStyle("Button"), GUILayout.Height(23), GUILayout.Width(29)))
                    tangentMode = ShapeTangentMode.Linear;

                if (GUILayout.Toggle(tangentMode == ShapeTangentMode.Continuous, tangentCurvedIcon, new GUIStyle("Button"), GUILayout.Height(23), GUILayout.Width(29)))
                    tangentMode = ShapeTangentMode.Continuous;

                if (GUILayout.Toggle(tangentMode == ShapeTangentMode.Broken, tangentAsymmetricIcon, new GUIStyle("Button"), GUILayout.Height(23), GUILayout.Width(29)))
                    tangentMode = ShapeTangentMode.Broken;

                if (tangentMode.HasValue && prevTangentMode.HasValue && tangentMode.Value != prevTangentMode.Value)
                {
                    Undo.RecordObject(m_CurrentEditor.target, "Edit Tangent Mode");
                    RefreshTangentsAfterModeChange(selection.single, tangentMode.Value);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private static ShapeTangentMode GetNextTangentMode(ShapeTangentMode current)
        {
            return (ShapeTangentMode)((((int)current) + 1) % Enum.GetValues(typeof(ShapeTangentMode)).Length);
        }

        private void CycleTangentMode()
        {
            ISelection selection = ShapeEditorCache.GetSelection();
            if (selection.single == -1)
                return;

            Undo.RecordObject(m_CurrentEditor.target, "Edit Tangent Mode");

            ShapeTangentMode oldMode = m_Spline.GetTangentMode(selection.single);
            ShapeTangentMode newMode = GetNextTangentMode(oldMode);

            RefreshTangentsAfterModeChange(selection.single, newMode);
        }

        private void RefreshTangentsAfterModeChange(int pointIndex, ShapeTangentMode newMode)
        {
            m_Spline.SetTangentMode(pointIndex, newMode);
            m_Spline.SetLeftTangent(pointIndex, ShapeEditorCache.instance.leftTangent);
            m_Spline.SetRightTangent(pointIndex, ShapeEditorCache.instance.rightTangent);

            if (newMode == ShapeTangentMode.Continuous)
            {
                bool setFromRightTangent = ShapeEditorCache.instance.rightTangentChanged;

                if (m_Spline.GetRightTangent(pointIndex).sqrMagnitude == 0f)
                    setFromRightTangent = false;
                if (m_Spline.GetLeftTangent(pointIndex).sqrMagnitude == 0f)
                    setFromRightTangent = true;

                if (setFromRightTangent)
                    m_Spline.SetLeftTangent(pointIndex, m_Spline.GetRightTangent(pointIndex) * -1f);
                else
                    m_Spline.SetRightTangent(pointIndex, m_Spline.GetLeftTangent(pointIndex) * -1f);
            }

            if (newMode == ShapeTangentMode.Continuous || newMode == ShapeTangentMode.Broken)
            {
                if (m_Spline.GetLeftTangent(pointIndex).sqrMagnitude == 0f && m_Spline.GetRightTangent(pointIndex).sqrMagnitude == 0f)
                    ResetTangents(pointIndex);
            }

            EditorUtility.SetDirty(m_CurrentEditor.target);
        }

        private void ResetTangents(int pointIndex)
        {
            Vector3 position = m_Spline.GetPosition(pointIndex);
            Vector3 positionNext = m_Spline.GetPosition(SplineUtility.NextIndex(pointIndex, m_Spline.GetPointCount()));
            Vector3 positionPrev = m_Spline.GetPosition(SplineUtility.PreviousIndex(pointIndex, m_Spline.GetPointCount()));
            Vector3 forward = (m_CurrentEditor.target as SpriteShapeController).transform.forward;
            float scale = Mathf.Min((positionNext - position).magnitude, (positionPrev - position).magnitude) * 0.33f;

            Vector3 leftTangent = (positionPrev - position).normalized * scale;
            Vector3 rightTangent = (positionNext - position).normalized * scale;

            if (m_Spline.GetTangentMode(pointIndex) == ShapeTangentMode.Continuous)
                SplineUtility.CalculateTangents(position, positionPrev, positionNext, forward, scale, out rightTangent, out leftTangent);

            m_Spline.SetLeftTangent(pointIndex, leftTangent);
            m_Spline.SetRightTangent(pointIndex, rightTangent);
        }

        private void DoPointInspector()
        {
            ISelection selection = ShapeEditorCache.GetSelection();

            List<Vector3> positions = new List<Vector3>();
            List<float> heights = new List<float>();
            List<float> bevelCutoffs = new List<float>();
            List<float> bevelSizes = new List<float>();
            List<int> spriteIndices = new List<int>();
            List<bool> corners = new List<bool>();

            foreach (int index in selection)
            {
                positions.Add(m_Spline.GetPosition(index));
                heights.Add(m_Spline.GetHeight(index));
                bevelCutoffs.Add(m_Spline.GetBevelCutoff(index));
                bevelSizes.Add(m_Spline.GetBevelSize(index));
                spriteIndices.Add(m_Spline.GetSpriteIndex(index));
                corners.Add(m_Spline.GetCorner(index));
            }

            EditorGUIUtility.wideMode = true;

            EditorGUI.BeginChangeCheck();

            positions = MultiVector2Field(Contents.positionLabel, positions, 1.5f);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_CurrentEditor.target, "Undo Inspector");

                for (int index = 0; index < positions.Count; index++)
                    m_Spline.SetPosition(selection.ElementAt(index), positions[index]);
                SceneView.RepaintAll();
            }

            EditorGUIUtility.wideMode = false;

            bool mixedValue = EditorGUI.showMixedValue;

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = !heights.All(v => Mathf.Approximately(v, heights.First()));
            float height = EditorGUILayout.Slider(Contents.heightLabel, heights[0], 0.1f, 2.0f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_CurrentEditor.target, "Undo Inspector");

                foreach (int index in selection)
                    m_Spline.SetHeight(index, height);
            }

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = !bevelCutoffs.All(v => Mathf.Approximately(v, bevelCutoffs.First()));
            float cornerTolerance = EditorGUILayout.Slider(Contents.bevelCutoffLabel, bevelCutoffs[0], 0, 180.0f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_CurrentEditor.target, "Undo Inspector");

                foreach (int index in selection)
                    m_Spline.SetBevelCutoff(index, cornerTolerance);
            }

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = !bevelSizes.All(v => Mathf.Approximately(v, bevelSizes.First()));
            float bevelLength = EditorGUILayout.Slider(Contents.bevelSizeLabel, bevelSizes[0], 0.0f, 0.5f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_CurrentEditor.target, "Undo Inspector");

                foreach (int index in selection)
                    m_Spline.SetBevelSize(index, bevelLength);
            }

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = !spriteIndices.All(v => (v == spriteIndices.First()));
            int spriteIndex = EditorGUILayout.IntSlider(Contents.spriteIndexLabel, spriteIndices[0], 0, 63);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_CurrentEditor.target, "Undo Inspector");

                foreach (int index in selection)
                    m_Spline.SetSpriteIndex(index, spriteIndex);
            }

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = !corners.All(v => (v == corners.First()));
            int val = (int)EditorGUILayout.IntPopup(Contents.cornerLabel, corners[0] ? 1 : 0, Contents.cornerOptions, Contents.cornerValues);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_CurrentEditor.target, "Undo Inspector");

                foreach (int index in selection)
                    m_Spline.SetCorner(index, (val > 0) ? true : false);
            }

            EditorGUI.showMixedValue = mixedValue;
        }

        private List<Vector3> MultiVector2Field(GUIContent label, List<Vector3> values, float floatWidth)
        {
            float kSpacingSubLabel = 2.0f;
            float kMiniLabelW = 13;

            if (!values.Any())
                return values;

            Rect position = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            int id = GUIUtility.GetControlID("Vector2Field".GetHashCode(), FocusType.Passive, position);
            position = SpriteShapeEditorGUI.MultiFieldPrefixLabel(position, id, label, 2);
            position.height = EditorGUIUtility.singleLineHeight;

            float w = (position.width - kSpacingSubLabel) / floatWidth;
            Rect nr = new Rect(position);
            nr.width = w;
            float t = EditorGUIUtility.labelWidth;
            int l = EditorGUI.indentLevel;
            EditorGUIUtility.labelWidth = kMiniLabelW;
            EditorGUI.indentLevel = 0;
            bool mixedValue = EditorGUI.showMixedValue;

            bool equalX = values.All(v => Mathf.Approximately(v.x, values.First().x));
            bool equalY = values.All(v => Mathf.Approximately(v.y, values.First().y));

            EditorGUI.showMixedValue = !equalX;
            EditorGUI.BeginChangeCheck();
            float x = EditorGUI.FloatField(nr, Contents.xLabel, values[0].x);
            if (EditorGUI.EndChangeCheck())
                for (int i = 0; i < values.Count; i++)
                    values[i] = new Vector3(x, values[i].y, values[i].z);

            nr.x += w + kSpacingSubLabel;

            EditorGUI.showMixedValue = !equalY;
            EditorGUI.BeginChangeCheck();
            float y = EditorGUI.FloatField(nr, Contents.yLabel, values[0].y);
            if (EditorGUI.EndChangeCheck())
                for (int i = 0; i < values.Count; i++)
                    values[i] = new Vector3(values[i].x, y, values[i].z);

            EditorGUI.showMixedValue = mixedValue;
            EditorGUIUtility.labelWidth = t;
            EditorGUI.indentLevel = l;

            return values;
        }
    }
}
