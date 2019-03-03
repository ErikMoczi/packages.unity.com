using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;

namespace UnityEditor.U2D
{
    public class SpriteShapeToolEditor
    {
        private static class Contents
        {
            public static readonly GUIContent tangentStraightIcon = SpriteShapeEditorGUI.IconContent("TangentStraight", "Straight line from point to point.");
            public static readonly GUIContent tangentCurvedIcon = SpriteShapeEditorGUI.IconContent("TangentCurved", "Tangents mirror each others angle.");
            public static readonly GUIContent tangentAsymmetricIcon = SpriteShapeEditorGUI.IconContent("TangentAssymetric", "Tangents are not linked.");
            public static readonly GUIContent tangentStraightIconPro = SpriteShapeEditorGUI.IconContent("TangentStraightPro", "Straight line from point to point.");
            public static readonly GUIContent tangentCurvedIconPro = SpriteShapeEditorGUI.IconContent("TangentCurvedPro", "Tangents mirror each others angle.");
            public static readonly GUIContent tangentAsymmetricIconPro = SpriteShapeEditorGUI.IconContent("TangentAssymetricPro", "Tangents are not linked.");
            public static readonly GUIContent positionLabel = new GUIContent("Position", "Position of the Control Point");
            public static readonly GUIContent leftTangentLabel = new GUIContent("Left Tangent", "Left Tangent end point.");
            public static readonly GUIContent rightTangentLabel = new GUIContent("Right Tangent", "Right Tangent end point.");
            public static readonly GUIContent enableSnapLabel = new GUIContent("Snapping", "Snap points using the snap settings");
            public static readonly GUIContent pointModeLabel = new GUIContent("Mode");

            public static readonly GUIContent heightLabel = new GUIContent("Height", "Height override for control point.");
            public static readonly GUIContent spriteIndexLabel = new GUIContent("Sprite Variant", "Index of the sprite variant at this control point");
            public static readonly GUIContent cornerLabel = new GUIContent("Corner", "Set if Corner is automatic or disabled.");
            public static readonly GUIContent pointLabel = new GUIContent("Point");

            public static readonly int[] cornerValues = { 0, 1 };
            public static readonly GUIContent[] cornerOptions = { new GUIContent("Disabled"), new GUIContent("Automatic") };

            public static readonly int[] variantValues = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31 };
            public static readonly GUIContent[] variantOptions = { new GUIContent("0"), new GUIContent("1"), new GUIContent("2"), new GUIContent("3"), new GUIContent("4"), new GUIContent("5"), new GUIContent("6"),
                new GUIContent("7"), new GUIContent("8"), new GUIContent("9"), new GUIContent("10"), new GUIContent("11"), new GUIContent("12"), new GUIContent("13"), new GUIContent("14"), new GUIContent("15"),
                new GUIContent("16"), new GUIContent("17"), new GUIContent("18"), new GUIContent("19"), new GUIContent("20"), new GUIContent("21"), new GUIContent("22"), new GUIContent("23"), new GUIContent("24"),
                new GUIContent("25"), new GUIContent("26"), new GUIContent("27"), new GUIContent("28"), new GUIContent("29"), new GUIContent("30"), new GUIContent("31") };

            public static readonly GUIContent xLabel = new GUIContent("X");
            public static readonly GUIContent yLabel = new GUIContent("Y");
            public static readonly GUIContent zLabel = new GUIContent("Z");
        }

        Spline m_Spline;

        public Spline spline
        {
            get { return m_Spline; }
        }

        public SpriteShapeToolEditor()
        {
            RegisterCallbacks();
        }

        public void OnDisable()
        {
            UnregisterCallbacks();
        }

        private void RegisterCallbacks()
        {
            UnregisterCallbacks();

            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void UnregisterCallbacks()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void RegisterUndo(string name)
        {
            Debug.Assert(SpriteShapeTool.instance.isActive);
            Undo.RegisterCompleteObjectUndo(SplineEditorCache.GetTarget(), name);
        }

        private void SetDirty()
        {
            Debug.Assert(SpriteShapeTool.instance.isActive);
            EditorUtility.SetDirty(SplineEditorCache.GetTarget());
        }

        private void OnUndoRedo()
        {
            SceneView.RepaintAll();
        }

        public void OnInspectorGUI(Spline spline)
        {
            m_Spline = spline;

            EditorGUI.BeginChangeCheck();
           
            if (GUI.enabled && SplineEditorCache.GetSelection().Count > 0)
            {
                EditorGUILayout.LabelField(Contents.pointLabel, EditorStyles.boldLabel);

                EditorGUI.indentLevel += 1;
                DoTangentGUI();
                DoPointInspector();
                SnappingUtility.enabled = EditorGUILayout.Toggle(Contents.enableSnapLabel, SnappingUtility.enabled);
                EditorGUI.indentLevel -= 1;
            }                

            if (EditorGUI.EndChangeCheck())
                SetDirty();
        }

        private void DoTangentGUI()
        {
            ISelection selection = SplineEditorCache.GetSelection();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PrefixLabel(Contents.pointModeLabel);

            ShapeTangentMode? tangentMode = null;

            if (selection.single != -1)
                tangentMode = m_Spline.GetTangentMode(selection.single);
            else
            {
                foreach (int index in selection)
                {
                    if (tangentMode == null)
                        tangentMode = m_Spline.GetTangentMode(index);
                    else
                    {
                        if (tangentMode != m_Spline.GetTangentMode(index))
                        {
                            tangentMode = null;
                            break;
                        }
                    }
                }

            }

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

            if (selection.single != -1)
            {
                if (GUILayout.Toggle(tangentMode == ShapeTangentMode.Linear, tangentStraightIcon, new GUIStyle("Button"), GUILayout.Height(23), GUILayout.Width(29)))
                    tangentMode = ShapeTangentMode.Linear;

                if (GUILayout.Toggle(tangentMode == ShapeTangentMode.Continuous, tangentCurvedIcon, new GUIStyle("Button"), GUILayout.Height(23), GUILayout.Width(29)))
                    tangentMode = ShapeTangentMode.Continuous;

                if (GUILayout.Toggle(tangentMode == ShapeTangentMode.Broken, tangentAsymmetricIcon, new GUIStyle("Button"), GUILayout.Height(23), GUILayout.Width(29)))
                    tangentMode = ShapeTangentMode.Broken;

                if (tangentMode.HasValue && prevTangentMode.HasValue && tangentMode.Value != prevTangentMode.Value)
                {
                    RegisterUndo("Edit Tangent Mode");
                    SpriteShapeTool.instance.SetTangentMode(selection.single, tangentMode.Value);
                }
            }
            else
            {
                EditorGUI.BeginChangeCheck();

                if (GUILayout.Toggle(tangentMode == ShapeTangentMode.Linear, tangentStraightIcon, new GUIStyle("Button"), GUILayout.Height(23), GUILayout.Width(29)))
                    tangentMode = ShapeTangentMode.Linear;

                if (GUILayout.Toggle(tangentMode == ShapeTangentMode.Continuous, tangentCurvedIcon, new GUIStyle("Button"), GUILayout.Height(23), GUILayout.Width(29)))
                    tangentMode = ShapeTangentMode.Continuous;

                if (GUILayout.Toggle(tangentMode == ShapeTangentMode.Broken, tangentAsymmetricIcon, new GUIStyle("Button"), GUILayout.Height(23), GUILayout.Width(29)))
                    tangentMode = ShapeTangentMode.Broken;

                if (EditorGUI.EndChangeCheck())
                {
                    if (tangentMode.HasValue && prevTangentMode.HasValue && tangentMode.Value != prevTangentMode.Value)
                    {
                        RegisterUndo("Edit Tangent Mode");
                        
                        foreach (int index in selection)
                            SpriteShapeTool.instance.SetTangentMode(index, tangentMode.Value);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DoPointInspector()
        {
            var selection = SplineEditorCache.GetSelection();
            var positions = new List<Vector3>();
            var heights = new List<float>();
            var spriteIndices = new List<int>();
            var corners = new List<bool>();

            foreach (int index in selection)
            {
                positions.Add(m_Spline.GetPosition(index));
                heights.Add(m_Spline.GetHeight(index));
                spriteIndices.Add(m_Spline.GetSpriteIndex(index));
                corners.Add(m_Spline.GetCorner(index));
            }

            EditorGUIUtility.wideMode = true;

            EditorGUI.BeginChangeCheck();

            positions = MultiVector2Field(Contents.positionLabel, positions, 1.5f);

            if (EditorGUI.EndChangeCheck())
            {
                RegisterUndo("Inspector");

                for (int index = 0; index < positions.Count; index++)
                    m_Spline.SetPosition(selection.ElementAt(index), positions[index]);
                SceneView.RepaintAll();
            }

            EditorGUIUtility.wideMode = false;

            bool mixedValue = EditorGUI.showMixedValue;

            EditorGUI.BeginChangeCheck();
            
            var heightValue = heights.FirstOrDefault();
            EditorGUI.showMixedValue = heights.All( h => Mathf.Approximately(h, heightValue) ) == false;

            heightValue = EditorGUILayout.Slider(Contents.heightLabel, heightValue, 0.1f, 4.0f);

            if (EditorGUI.EndChangeCheck())
            {
                RegisterUndo("Inspector");

                foreach (var index in selection)
                    m_Spline.SetHeight(index, heightValue);
            }

            EditorGUI.BeginChangeCheck();

            var spriteIndexValue = spriteIndices.FirstOrDefault();
            EditorGUI.showMixedValue = spriteIndices.All( i => Mathf.Approximately(i, spriteIndexValue) ) == false;

            spriteIndexValue = EditorGUILayout.IntPopup(Contents.spriteIndexLabel, spriteIndexValue, Contents.variantOptions, Contents.variantValues);

            if (EditorGUI.EndChangeCheck())
            {
                RegisterUndo("Inspector");

                foreach (var index in selection)
                    m_Spline.SetSpriteIndex(index, spriteIndexValue);
            }

            EditorGUI.BeginChangeCheck();

            var cornerValue = corners.FirstOrDefault();
            EditorGUI.showMixedValue = corners.All( v => (v == cornerValue) ) == false;

            cornerValue = EditorGUILayout.IntPopup(Contents.cornerLabel, cornerValue ? 1 : 0, Contents.cornerOptions, Contents.cornerValues) > 0;

            if (EditorGUI.EndChangeCheck())
            {
                RegisterUndo("Inspector");

                foreach (var index in selection)
                    m_Spline.SetCorner(index, cornerValue);
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
