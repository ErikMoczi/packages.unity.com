

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEngine;

using Unity.Tiny.Runtime.UILayout;

namespace Unity.Tiny
{
    internal class LayoutDropdownWindow : PopupWindowContent
    {
        private static class Styles
        {
            private static bool ProSkin => EditorGUIUtility.isProSkin;

            public static Color tableHeaderColor   => ProSkin ? new Color(0.18f, 0.18f, 0.18f, 1) : new Color(0.8f, 0.8f, 0.8f, 1);
            public static Color tableLineColor     => ProSkin ? new Color(1, 1, 1, 0.3f) : new Color(0, 0, 0, 0.5f);
            public static Color parentColor        => ProSkin ? new Color(0.4f, 0.4f, 0.4f, 1) : new Color(0.55f, 0.55f, 0.55f, 1);
            public static Color selfColor          => ProSkin ? new Color(0.6f, 0.6f, 0.6f, 1) : new Color(0.2f, 0.2f, 0.2f, 1);
            public static Color simpleAnchorColor  => ProSkin ? new Color(0.7f, 0.3f, 0.3f, 1) : new Color(0.8f, 0.3f, 0.3f, 1);
            public static Color stretchAnchorColor => ProSkin ? new Color(0.0f, 0.6f, 0.8f, 1) : new Color(0.2f, 0.5f, 0.9f, 1);
            public static Color anchorCornerColor  => ProSkin ? new Color(0.8f, 0.6f, 0.0f, 1) : new Color(0.6f, 0.4f, 0.0f, 1);
            public static Color pivotColor         => ProSkin ? new Color(0.0f, 0.6f, 0.8f, 1) : new Color(0.2f, 0.5f, 0.9f, 1);

            public static GUIStyle label => new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.LowerCenter
            };

            public static GUIStyle frame = new GUIStyle()
            {
                border = new RectOffset(2, 2, 2, 2)
            };

            static Styles()
            {
                var tex = new Texture2D(4, 4);
                tex.SetPixels(new [] {
                    Color.white, Color.white, Color.white, Color.white,
                    Color.white, Color.clear, Color.clear, Color.white,
                    Color.white, Color.clear, Color.clear, Color.white,
                    Color.white, Color.white, Color.white, Color.white
                });
                tex.filterMode = FilterMode.Point;
                tex.Apply();
                tex.hideFlags = HideFlags.HideAndDontSave;
                frame.normal.background = tex;
            }
        }
        public enum LayoutMode { Undefined = -1, Min = 0, Middle = 1, Max = 2, Stretch = 3 }

        #region Constants
        private static readonly float[] k_PivotsForModes = { 0, 0.5f, 1, 0.5f, 0.5f }; // Only for actual modes, not for Undefined.
        private static readonly string[] k_HLabels = { "custom", "left", "center", "right", "stretch", "%" };
        private static readonly string[] k_VLabels = { "custom", "top", "middle", "bottom", "stretch", "%" };
        private const int k_TopPartHeight = 38;
        #endregion

        #region Fields
        private TinyRectTransform m_Main;
        private readonly Vector2[,] m_InitValues;
        #endregion

        #region Properties
        private TinyEntity Entity { get; }
        private List<TinyEntity> Entities { get; }
        private List<TinyRectTransform> RectTransforms { get; }
        #endregion

        public LayoutDropdownWindow(IEnumerable<TinyEntity> entities, IEnumerable<TinyRectTransform> rectTransforms)
        {
            Entities = entities.ToList();
            Entity = Entities[0];

            RectTransforms = rectTransforms.ToList();
            m_Main = RectTransforms[0];

            m_InitValues = new Vector2[RectTransforms.Count, 4];

            for (var i = 0; i < RectTransforms.Count; ++i)
            {
                var rt = RectTransforms[i];
                m_InitValues[i, 0] = rt.anchorMin;
                m_InitValues[i, 1] = rt.anchorMax;
                m_InitValues[i, 2] = rt.anchoredPosition;
                m_InitValues[i, 3] = rt.sizeDelta;
            }
        }

        #region API
        internal static void DrawLayoutModeHeadersOutsideRect(Rect rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            var hMode = GetLayoutModeForAxis(anchorMin, anchorMax, 0);
            var vMode = GetLayoutModeForAxis(anchorMin, anchorMax, 1);
            vMode = SwappedVMode(vMode);
            DrawLayoutModeHeaderOutsideRect(rect, 0, hMode);
            DrawLayoutModeHeaderOutsideRect(rect, 1, vMode);
        }

        internal static void DrawLayoutMode(Rect rect,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var hMode = GetLayoutModeForAxis(anchorMin, anchorMax, 0);
            var vMode = GetLayoutModeForAxis(anchorMin, anchorMax, 1);
            vMode = SwappedVMode(vMode);
            DrawLayoutMode(rect, hMode, vMode);
        }

        internal static void DrawLayoutMode(Rect position, LayoutMode hMode, LayoutMode vMode)
        {
            DrawLayoutMode(position, hMode, vMode, false, false);
        }

        internal static void DrawLayoutMode(Rect position, LayoutMode hMode, LayoutMode vMode, bool doPivot)
        {
            DrawLayoutMode(position, hMode, vMode, doPivot, false);
        }

        internal static void DrawLayoutMode(Rect position, LayoutMode hMode, LayoutMode vMode, bool doPivot, bool doPosition)
        {
            var oldColor = GUI.color;

            // Make parent size the largest possible square, but enforce it's an uneven number.
            var parentWidth = (int)Mathf.Min(position.width, position.height);
            if (parentWidth % 2 == 0)
                parentWidth--;

            var selfWidth = parentWidth / 2;
            if (selfWidth % 2 == 0)
                selfWidth++;

            var parentSize = parentWidth * Vector2.one;
            var selfSize = selfWidth * Vector2.one;
            var padding = (position.size - parentSize) / 2;
            padding.x = Mathf.Floor(padding.x);
            padding.y = Mathf.Floor(padding.y);
            var padding2 = (position.size - selfSize) / 2;
            padding2.x = Mathf.Floor(padding2.x);
            padding2.y = Mathf.Floor(padding2.y);

            var outer = new Rect(position.x + padding.x, position.y + padding.y, parentSize.x, parentSize.y);
            var inner = new Rect(position.x + padding2.x, position.y + padding2.y, selfSize.x, selfSize.y);
            if (doPosition)
            {
                for (var axis = 0; axis < 2; axis++)
                {
                    var mode = (axis == 0 ? hMode : vMode);

                    switch (mode) {
                        case LayoutMode.Min:
                        {
                            var center = inner.center;
                            center[axis] += outer.min[axis] - inner.min[axis];
                            inner.center = center;
                            break;
                        }
                        case LayoutMode.Middle:

                            // TODO
                            break;
                        case LayoutMode.Max:
                        {
                            var center = inner.center;
                            center[axis] += outer.max[axis] - inner.max[axis];
                            inner.center = center;
                            break;
                        }
                        case LayoutMode.Stretch:
                            var innerMin = inner.min;
                            var innerMax = inner.max;
                            innerMin[axis] = outer.min[axis];
                            innerMax[axis] = outer.max[axis];
                            inner.min = innerMin;
                            inner.max = innerMax;
                            break;
                        case LayoutMode.Undefined:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            var anchor = new Rect();
            var min = Vector2.zero;
            var max = Vector2.zero;
            for (var axis = 0; axis < 2; axis++)
            {
                var mode = (axis == 0 ? hMode : vMode);

                switch (mode) {
                    case LayoutMode.Min:
                        min[axis] = outer.min[axis] + 0.5f;
                        max[axis] = outer.min[axis] + 0.5f;
                        break;
                    case LayoutMode.Middle:
                        min[axis] = outer.center[axis];
                        max[axis] = outer.center[axis];
                        break;
                    case LayoutMode.Max:
                        min[axis] = outer.max[axis] - 0.5f;
                        max[axis] = outer.max[axis] - 0.5f;
                        break;
                    case LayoutMode.Stretch:
                        min[axis] = outer.min[axis] + 0.5f;
                        max[axis] = outer.max[axis] - 0.5f;
                        break;
                    case LayoutMode.Undefined:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            anchor.min = min;
            anchor.max = max;

            // Draw parent rect
            if (Event.current.type == EventType.Repaint)
            {
                GUI.color = Styles.parentColor * oldColor;
                Styles.frame.Draw(outer, false, false, false, false);
            }

            // Draw anchor lines
            if (hMode != LayoutMode.Undefined && hMode != LayoutMode.Stretch)
            {
                GUI.color = Styles.simpleAnchorColor * oldColor;
                GUI.DrawTexture(new Rect(anchor.xMin - 0.5f, outer.y + 1, 1, outer.height - 2), EditorGUIUtility.whiteTexture);
                GUI.DrawTexture(new Rect(anchor.xMax - 0.5f, outer.y + 1, 1, outer.height - 2), EditorGUIUtility.whiteTexture);
            }
            if (vMode != LayoutMode.Undefined && vMode != LayoutMode.Stretch)
            {
                GUI.color = Styles.simpleAnchorColor * oldColor;
                GUI.DrawTexture(new Rect(outer.x + 1, anchor.yMin - 0.5f, outer.width - 2, 1), EditorGUIUtility.whiteTexture);
                GUI.DrawTexture(new Rect(outer.x + 1, anchor.yMax - 0.5f, outer.width - 2, 1), EditorGUIUtility.whiteTexture);
            }

            // Draw stretch mode arrows
            if (hMode == LayoutMode.Stretch)
            {
                GUI.color = Styles.stretchAnchorColor * oldColor;
                DrawArrow(new Rect(inner.x + 1, inner.center.y - 0.5f, inner.width - 2, 1));
            }
            if (vMode == LayoutMode.Stretch)
            {
                GUI.color = Styles.stretchAnchorColor * oldColor;
                DrawArrow(new Rect(inner.center.x - 0.5f, inner.y + 1, 1, inner.height - 2));
            }

            // Draw self rect
            if (Event.current.type == EventType.Repaint)
            {
                GUI.color = Styles.selfColor * oldColor;
                Styles.frame.Draw(inner, false, false, false, false);
            }

            // Draw pivot
            if (doPivot && hMode != LayoutMode.Undefined && vMode != LayoutMode.Undefined)
            {
                var pivot = new Vector2(
                        Mathf.Lerp(inner.xMin + 0.5f, inner.xMax - 0.5f, k_PivotsForModes[(int)hMode]),
                        Mathf.Lerp(inner.yMin + 0.5f, inner.yMax - 0.5f, k_PivotsForModes[(int)vMode])
                        );

                GUI.color = Styles.pivotColor * oldColor;
                GUI.DrawTexture(new Rect(pivot.x - 2.5f, pivot.y - 1.5f, 5, 3), EditorGUIUtility.whiteTexture);
                GUI.DrawTexture(new Rect(pivot.x - 1.5f, pivot.y - 2.5f, 3, 5), EditorGUIUtility.whiteTexture);
            }

            // Draw anchor corners
            if (hMode != LayoutMode.Undefined && vMode != LayoutMode.Undefined)
            {
                GUI.color = Styles.anchorCornerColor * oldColor;
                GUI.DrawTexture(new Rect(anchor.xMin - 1.5f, anchor.yMin - 1.5f, 2, 2), EditorGUIUtility.whiteTexture);
                GUI.DrawTexture(new Rect(anchor.xMax - 0.5f, anchor.yMin - 1.5f, 2, 2), EditorGUIUtility.whiteTexture);
                GUI.DrawTexture(new Rect(anchor.xMin - 1.5f, anchor.yMax - 0.5f, 2, 2), EditorGUIUtility.whiteTexture);
                GUI.DrawTexture(new Rect(anchor.xMax - 0.5f, anchor.yMax - 0.5f, 2, 2), EditorGUIUtility.whiteTexture);
            }

            GUI.color = oldColor;
        }
        #endregion

        #region PopupWindowContent
        public override void OnOpen()
        {
            EditorApplication.modifierKeysChanged += editorWindow.Repaint;
        }

        public override void OnClose()
        {
            EditorApplication.modifierKeysChanged -= editorWindow.Repaint;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(262, 262 + k_TopPartHeight);
        }

        public override void OnGUI(Rect rect)
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
                editorWindow.Close();

            GUI.Label(new Rect(rect.x + 5, rect.y + 3, rect.width - 10, 16), EditorGUIUtility.TrTextContent("Anchor Presets"), EditorStyles.boldLabel);
            GUI.Label(new Rect(rect.x + 5, rect.y + 3 + 16, rect.width - 10, 16), EditorGUIUtility.TrTextContent("Shift: Also set pivot     Alt: Also set position"), EditorStyles.label);

            var oldColor = GUI.color;
            GUI.color = Styles.tableLineColor * oldColor;
            GUI.DrawTexture(new Rect(0, k_TopPartHeight - 1, 400, 1), EditorGUIUtility.whiteTexture);
            GUI.color = oldColor;

            GUI.BeginGroup(new Rect(rect.x, rect.y + k_TopPartHeight, rect.width, rect.height - k_TopPartHeight));
            TableGUI(rect);
            GUI.EndGroup();
        }
        #endregion

        #region Implementation
        private static LayoutMode SwappedVMode(LayoutMode vMode)
        {
            switch (vMode) {
                case LayoutMode.Min:
                    return LayoutMode.Max;
                case LayoutMode.Max:
                    return LayoutMode.Min;
                case LayoutMode.Undefined:
                case LayoutMode.Middle:
                case LayoutMode.Stretch:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(vMode), vMode, null);
            }

            return vMode;
        }

        private static void DrawLayoutModeHeaderOutsideRect(Rect position, int axis, LayoutMode mode)
        {
            var headerRect = new Rect(position.x, position.y - 16, position.width, 16);

            var normalMatrix = GUI.matrix;
            if (axis == 1)
                GUIUtility.RotateAroundPivot(-90, position.center);

            var index = (int)(mode) + 1;
            GUI.Label(headerRect, axis == 0 ? k_HLabels[index] : k_VLabels[index], Styles.label);

            GUI.matrix = normalMatrix;
        }

        private void TableGUI(Rect rect)
        {
            const int padding = 6;
            const int size = 31 + padding * 2;
            const int spacing = 0;
            var groupings = new [] { 15, 30, 30, 30, 45, 45 };

            var oldColor = GUI.color;

            const int headerW = 62;
            GUI.color = Styles.tableHeaderColor * oldColor;
            GUI.DrawTexture(new Rect(0, 0, 400, headerW), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(0, 0, headerW, 400), EditorGUIUtility.whiteTexture);
            GUI.color = Styles.tableLineColor * oldColor;
            GUI.DrawTexture(new Rect(0, headerW, 400, 1), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(headerW, 0, 1, 400), EditorGUIUtility.whiteTexture);
            GUI.color = oldColor;

            var hMode = GetLayoutModeForAxis(m_Main.anchorMin, m_Main.anchorMax, 0);
            var vMode = GetLayoutModeForAxis(m_Main.anchorMin, m_Main.anchorMax, 1);
            vMode = SwappedVMode(vMode);

            var doPivot = Event.current.shift;
            var doPosition = Event.current.alt;

            const int number = 5;

            for (var i = 0; i < number; i++)
            {
                var cellHMode = (LayoutMode)(i - 1);

                for (var j = 0; j < number; j++)
                {
                    var cellVMode = (LayoutMode)(j - 1);

                    if (i == 0 && j == 0 && vMode >= 0 && hMode >= 0)
                        continue;

                    var position = new Rect(
                            i * (size + spacing) + groupings[i],
                            j * (size + spacing) + groupings[j],
                            size,
                            size);

                    if (j == 0 && !(i == 0 && hMode != LayoutMode.Undefined))
                        DrawLayoutModeHeaderOutsideRect(position, 0, cellHMode);
                    if (i == 0 && !(j == 0 && vMode != LayoutMode.Undefined))
                        DrawLayoutModeHeaderOutsideRect(position, 1, cellVMode);

                    var selected = (cellHMode == hMode) && (cellVMode == vMode);

                    var selectedHeader = i == 0 && cellVMode == vMode || j == 0 && cellHMode == hMode;

                    if (Event.current.type == EventType.Repaint)
                    {
                        if (selected)
                        {
                            GUI.color = Color.white * oldColor;
                            Styles.frame.Draw(position, false, false, false, false);
                        }
                        else if (selectedHeader)
                        {
                            GUI.color = new Color(1, 1, 1, 0.7f) * oldColor;
                            Styles.frame.Draw(position, false, false, false, false);
                        }
                    }

                    DrawLayoutMode(
                        new Rect(position.x + padding, position.y + padding, position.width - padding * 2, position.height - padding * 2),
                        cellHMode, cellVMode,
                        doPivot, doPosition);

                    var clickCount = Event.current.clickCount;
                    if (GUI.Button(position, GUIContent.none, GUIStyle.none))
                    {
                        SetLayoutModeForAxis(m_Main.anchorMin, 0, cellHMode, doPivot, doPosition, m_InitValues);
                        SetLayoutModeForAxis(m_Main.anchorMin, 1, SwappedVMode(cellVMode), doPivot, doPosition, m_InitValues);


                        if (Entity.Registry.Context.Caretaker.HasObjectChanged(Entity))
                        {
                            Entity.Registry.Context.GetManager<IBindingsManager>().Transfer(Entity);
                            TransformInvertedBindings.SyncTransform(Entity.View.transform, Entity.View);
                        }

                        if (clickCount == 2)
                            editorWindow.Close();
                        else
                            editorWindow.Repaint();
                    }
                }
            }
            GUI.color = oldColor;
        }

        private static LayoutMode GetLayoutModeForAxis(
            Vector2 anchorMin,
            Vector2 anchorMax,
            int axis)
        {
            if (anchorMin[axis] == 0 && anchorMax[axis] == 0)
                return LayoutMode.Min;
            if (anchorMin[axis] == 0.5f && anchorMax[axis] == 0.5f)
                return LayoutMode.Middle;
            if (anchorMin[axis] == 1 && anchorMax[axis] == 1)
                return LayoutMode.Max;
            if (anchorMin[axis] == 0 && anchorMax[axis] == 1)
                return LayoutMode.Stretch;
            return LayoutMode.Undefined;
        }

        private void SetLayoutModeForAxis(
            Vector2 anchorMin,
            int axis, LayoutMode layoutMode,
            bool doPivot, bool doPosition, Vector2[,] defaultValues
            )
        {
            for (var i = 0; i < RectTransforms.Count; ++i)
            {
                var entity = Entities[i];
                var rt = RectTransforms[i];
                if (doPosition)
                {
                    if (defaultValues != null && defaultValues.Length > i)
                    {
                        var temp = rt.anchorMin;
                        temp[axis] = defaultValues[i, 0][axis];
                        rt.anchorMin = temp;

                        temp = rt.anchorMax;
                        temp[axis] = defaultValues[i, 1][axis];
                        rt.anchorMax = temp;

                        temp = rt.anchoredPosition;
                        temp[axis] = defaultValues[i, 2][axis];
                        rt.anchoredPosition = temp;

                        temp = rt.sizeDelta;
                        temp[axis] = defaultValues[i, 3][axis];
                        rt.sizeDelta = temp;
                    }
                }

                if (doPivot && layoutMode != LayoutMode.Undefined)
                {
                    RectTransformEditor.SetPivotSmart(entity, rt, k_PivotsForModes[(int)layoutMode], axis, true);
                }

                var refPosition = Vector2.zero;
                switch (layoutMode)
                {
                    case LayoutMode.Min:
                        RectTransformEditor.SetAnchorSmart(entity, rt, 0, axis, false, true, true);
                        RectTransformEditor.SetAnchorSmart(entity, rt, 0, axis, true, true, true);
                        refPosition = rt.offsetMin;
                        break;
                    case LayoutMode.Middle:
                        RectTransformEditor.SetAnchorSmart(entity, rt, 0.5f, axis, false, true, true);
                        RectTransformEditor.SetAnchorSmart(entity, rt, 0.5f, axis, true, true, true);
                        refPosition = (rt.offsetMin + rt.offsetMax) * 0.5f;
                        break;
                    case LayoutMode.Max:
                        RectTransformEditor.SetAnchorSmart(entity, rt, 1, axis, false, true, true);
                        RectTransformEditor.SetAnchorSmart(entity, rt, 1, axis, true, true, true);
                        refPosition = rt.offsetMax;
                        break;
                    case LayoutMode.Stretch:
                        RectTransformEditor.SetAnchorSmart(entity, rt, 0, axis, false, true, true);
                        RectTransformEditor.SetAnchorSmart(entity, rt, 1, axis, true, true, true);
                        refPosition = (rt.offsetMin + rt.offsetMax) * 0.5f;
                        break;
                    case LayoutMode.Undefined:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(layoutMode), layoutMode, null);
                }

                if (doPosition)
                {
                    // Handle position
                    var rectPosition = rt.anchoredPosition;
                    rectPosition[axis] -= refPosition[axis];
                    rt.anchoredPosition = rectPosition;

                    // Handle sizeDelta
                    if (layoutMode == LayoutMode.Stretch)
                    {
                        var rectSizeDelta = rt.sizeDelta;
                        rectSizeDelta[axis] = 0;
                        rt.sizeDelta = rectSizeDelta;
                    }
                }
                RectTransformEditor.PartialBindings(entity);
            }
        }

        private static void DrawArrow(Rect lineRect)
        {
            GUI.DrawTexture(lineRect, EditorGUIUtility.whiteTexture);
            if (lineRect.width == 1)
            {
                GUI.DrawTexture(new Rect(lineRect.x - 1, lineRect.y + 1, 3, 1), EditorGUIUtility.whiteTexture);
                GUI.DrawTexture(new Rect(lineRect.x - 2, lineRect.y + 2, 5, 1), EditorGUIUtility.whiteTexture);
                GUI.DrawTexture(new Rect(lineRect.x - 1, lineRect.yMax - 2, 3, 1), EditorGUIUtility.whiteTexture);
                GUI.DrawTexture(new Rect(lineRect.x - 2, lineRect.yMax - 3, 5, 1), EditorGUIUtility.whiteTexture);
            }
            else
            {
                GUI.DrawTexture(new Rect(lineRect.x + 1, lineRect.y - 1, 1, 3), EditorGUIUtility.whiteTexture);
                GUI.DrawTexture(new Rect(lineRect.x + 2, lineRect.y - 2, 1, 5), EditorGUIUtility.whiteTexture);
                GUI.DrawTexture(new Rect(lineRect.xMax - 2, lineRect.y - 1, 1, 3), EditorGUIUtility.whiteTexture);
                GUI.DrawTexture(new Rect(lineRect.xMax - 3, lineRect.y - 2, 1, 5), EditorGUIUtility.whiteTexture);
            }
        }
        #endregion
    }
}

