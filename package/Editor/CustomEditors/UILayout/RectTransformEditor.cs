
﻿
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

using Unity.Tiny.Runtime.UILayout;

namespace Unity.Tiny
{
    [TinyCustomEditor(CoreGuids.UILayout.RectTransform)]
    [UsedImplicitly]
    internal class RectTransformEditor : ComponentEditor
    {
        private static class Styles
        {
            public static GUIStyle measuringLabelStyle = "PreOverlayLabel";

            public static GUIContent anchorsContent = EditorGUIUtilityBridge.TrTextContent("Anchors");
            public static GUIContent anchorMinContent = EditorGUIUtilityBridge.TrTextContent("Min", "The normalized position in the parent rectangle that the lower left corner is anchored to.");
            public static GUIContent anchorMaxContent = EditorGUIUtilityBridge.TrTextContent("Max", "The normalized position in the parent rectangle that the upper right corner is anchored to.");
            public static GUIContent pivotContent = EditorGUIUtilityBridge.TrTextContent("Pivot", "The pivot point specified in normalized values between 0 and 1. The pivot point is the origin of this rectangle. Rotation and scaling is around this point.");
            public static GUIContent rawEditContent;
            public static GUIContent blueprintContent;

            static Styles()
            {
                rawEditContent = EditorGUIUtility.IconContent("RectTransformRaw", "|Raw edit mode. When enabled, editing pivot and anchor values will not counter-adjust the position and size of the rectangle in order to make it stay in place.");
                blueprintContent = EditorGUIUtility.IconContent("RectTransformBlueprint", "|Blueprint mode. Edit RectTransforms as if they were not rotated and scaled. This enables snapping too.");
            }
        }

        #region Static
        public static void SetPivotSmart(TinyEntity entity, TinyRectTransform rt, float value, int axis, bool smart)
        {
            var rect = entity.View.gameObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                smart = false;
            }

            var cornerBefore = GetRectReferenceCorner(rect);

            var rectPivot = rt.pivot;
            rectPivot[axis] = value;
            rt.pivot = rectPivot;

            if (smart)
            {
                var cornerAfter = GetRectReferenceCorner(rect);
                var cornerOffset = cornerAfter - cornerBefore;
                var anchoredPosition = rt.anchoredPosition;
                anchoredPosition -= (Vector2)cornerOffset;
                rt.anchoredPosition = anchoredPosition;

                var pos = rect.transform.position;
                pos.z -= cornerOffset.z;
                rect.transform.position = pos;
            }
        }

        public static void SetAnchorSmart(TinyEntity entity, TinyRectTransform rt, float value, int axis, bool isMax, bool smart)
        {
            SetAnchorSmart(entity, rt, value, axis, isMax, smart, false, false, false);
        }

        public static void SetAnchorSmart(TinyEntity entity, TinyRectTransform rt, float value, int axis, bool isMax, bool smart, bool enforceExactValue)
        {
            SetAnchorSmart(entity, rt, value, axis, isMax, smart, enforceExactValue, false, false);
        }
        #endregion

        #region Constants
        private const string k_ShowAnchorPropsPrefName = "RectTransformEditor.showAnchorProperties";
        private const string k_LockRectPrefName = "RectTransformEditor.lockRect";
        private const float k_DropdownSize = 49;
        #endregion

        #region Fields
        private static Vector2 s_StartDragAnchorMin = Vector2.zero;
        private static Vector2 s_StartDragAnchorMax = Vector2.zero;
        private static GUIContent[] s_AxisLabels = { new GUIContent("X"), new GUIContent("Y") };
        private static int s_FloatFieldHash = "EditorTextField".GetHashCode();
        private bool m_RawEditMode = false;
        private bool m_ShowLayoutOptions = false;
        private TinyRectTransform m_Editable;
        private TinyRectTransform m_Actual;
        private List<TinyRectTransform> m_TinyTargets = new List<TinyRectTransform>();
        #endregion

        #region Properties
        private TinyObject MainRectTransform { get; set; }
        private TinyObject Shadow { get; set; }
        #endregion

        public RectTransformEditor(TinyContext tinyContext)
            :base(tinyContext)
        {
            var registry = tinyContext.Registry;
            Shadow = new TinyObject(registry, TypeRefs.UILayout.RectTransform);
            Shadow.Refresh();
        }

        #region ComponentEditor

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            var target = context.MainTarget<TinyEntity>();
            var transformNodeTypeRef = TypeRefs.Core2D.TransformNode;
            if (!target.HasComponent(transformNodeTypeRef))
            {
                EditorGUILayout.HelpBox("A TransformNode component is needed with the RectTransform.", MessageType.Warning);
                AddComponentToTargetButton(context, transformNodeTypeRef);
                return true;
            }

            var transformPositionTypeRef = TypeRefs.Core2D.TransformLocalPosition;
            if (!target.HasComponent(transformPositionTypeRef))
            {
                EditorGUILayout.HelpBox("A TransformLocalPosition component is needed with the RectTransform.", MessageType.Warning);
                AddComponentToTargetButton(context, transformPositionTypeRef);
                return true;
            }

            var optionsTypeRef = TypeRefs.Core2D.Sprite2DRendererOptions;
            if (target.HasComponent(TypeRefs.Core2D.Sprite2DRenderer) && !target.HasComponent(optionsTypeRef))
            {
                EditorGUILayout.HelpBox("A Sprite2DRendererOption component is needed with the RectTransform when there is a Sprite2DRenderer component.", MessageType.Warning);
                AddComponentToTargetButton(context, optionsTypeRef);
                return true;
            }

            var uiCanvasTypeRef = TypeRefs.UILayout.UICanvas;
            if (target.HasComponent(uiCanvasTypeRef) && target.Parent().Equals(TinyEntity.Reference.None))
            {
                EditorGUILayout.HelpBox("Some values are driven by the UICanvas.", MessageType.None);
                GUI.enabled = false;
            }

            var root = GetRootEntity(target);
            if (null == root.GetComponent(uiCanvasTypeRef))
            {
                EditorGUILayout.HelpBox("No UICanvas found on the root entity.", MessageType.Warning);
                if (root == target)
                {
                    AddComponentToTargetButton(context, uiCanvasTypeRef);
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    try
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Go to root entity"))
                        {
                            EditorApplication.delayCall += () => Selection.activeGameObject = root.View.gameObject;
                        }
                        GUILayout.FlexibleSpace();
                    }
                    finally
                    {
                        EditorGUILayout.EndHorizontal();
                    }
                }
                GUI.enabled = false;
            }

            MainRectTransform = context.Value;

            var entities = context.Targets.OfType<TinyEntity>().ToList();
            m_Actual = new TinyRectTransform(MainRectTransform);
            m_Editable = GetEditableCopy(m_Actual);
            m_TinyTargets = entities.Select(rt => new TinyRectTransform(rt.GetComponent(CoreIds.UILayout.RectTransform))).ToList();

            m_RawEditMode = EditorPrefs.GetBool(k_LockRectPrefName, false);
            m_ShowLayoutOptions = EditorPrefs.GetBool(k_ShowAnchorPropsPrefName, false);

            if (!EditorGUIUtility.wideMode)
            {
                EditorGUIUtility.wideMode = true;
                EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth - 212;
            }

            var anyWithoutParent = entities.Any(e =>
            {
                if (!e.HasTransformNode())
                {
                    return true;
                }

                var parent = e.Parent().Dereference(e.Registry);
                return parent?.GetComponent(TypeRefs.UILayout.RectTransform) == null;
            });

            LayoutDropdownButton(anyWithoutParent, ref context);

            // Position and Size Delta
            SmartPositionAndSizeFields(anyWithoutParent);

            // Anchor and pivot fields
            SmartAnchorFields(ref context);
            SmartPivotField(ref context);

            if (PushChanges(ref context))
            {
                PartialBindings(ref context);
            }

            EditorGUIUtility.labelWidth = 0;
            return true;
        }
        #endregion

        #region Implementation
        private TinyRectTransform GetEditableCopy(TinyRectTransform rt)
        {
            var cache = new TinyRectTransform(Shadow);
            cache.CopyFrom(rt);
            return cache;
        }

        private bool PushChanges(ref UIVisitContext<TinyObject> context)
        {
            bool changed = false;

            changed |= AssignAndPushChange("anchorMin", m_Actual.anchorMin, m_Editable.anchorMin, ref context);
            changed |= AssignAndPushChange("anchorMax", m_Actual.anchorMax, m_Editable.anchorMax, ref context);
            changed |= AssignAndPushChange("sizeDelta", m_Actual.sizeDelta, m_Editable.sizeDelta, ref context);
            changed |= AssignAndPushChange("anchoredPosition", m_Actual.anchoredPosition, m_Editable.anchoredPosition, ref context);
            changed |= AssignAndPushChange("pivot", m_Actual.pivot, m_Editable.pivot, ref context);
            return changed;
        }

        private void PartialBindings(ref UIVisitContext<TinyObject> context)
        {
            foreach (var entity in context.Targets.OfType<TinyEntity>())
            {
                PartialBindings(entity);
            }
        }

        internal static void PartialBindings(TinyEntity entity)
        {
            var tinyRT = new TinyRectTransform(entity.GetComponent(TypeRefs.UILayout.RectTransform));
            var rt = entity.View.transform as RectTransform;

            rt.anchorMin = tinyRT.anchorMin;
            rt.anchorMax = tinyRT.anchorMax;
            rt.sizeDelta = tinyRT.sizeDelta;
            rt.anchoredPosition = tinyRT.anchoredPosition;
            rt.pivot = tinyRT.pivot;
            TransformInvertedBindings.SyncTransform(rt, entity.View);
        }

        private bool AssignAndPushChange(string property, Vector2 previousValue, Vector2 newValue, ref UIVisitContext<TinyObject> context)
        {
            bool changed = false;
            if (previousValue != newValue)
            {
                MainRectTransform.AssignIfDifferent(property, newValue);
                context.Visitor.ChangeTracker.PushChange(MainRectTransform.Properties, MainRectTransform.Properties.PropertyBag.FindProperty(property));
                changed = true;
            }

            return changed;
        }

        private void LayoutDropdownButton(bool anyWithoutParent, ref UIVisitContext<TinyObject> context)
        {
            var dropdownPosition = GUILayoutUtility.GetRect(0, 0);
            dropdownPosition.x += 17;
            dropdownPosition.y += 17;
            dropdownPosition.height = k_DropdownSize;
            dropdownPosition.width = k_DropdownSize;

            using (new EditorGUI.DisabledScope(anyWithoutParent))
            {
                var oldColor = GUI.color;
                GUI.color = new Color(1, 1, 1, 0.6f) * oldColor;
                if (EditorGUI.DropdownButton(dropdownPosition, GUIContent.none, FocusType.Passive, "box"))
                {
                    GUIUtility.keyboardControl = 0;
                    var window = new LayoutDropdownWindow(context.Targets.OfType<TinyEntity>(), m_TinyTargets);

                    PopupWindowBridge.Show(dropdownPosition, window);
                }
                GUI.color = oldColor;
            }

            if (!anyWithoutParent)
            {
                LayoutDropdownWindow.DrawLayoutMode(new RectOffset(7, 7, 7, 7).Remove(dropdownPosition), m_Editable.anchorMin, m_Editable.anchorMax);
                LayoutDropdownWindow.DrawLayoutModeHeadersOutsideRect(dropdownPosition, m_Editable.anchorMin, m_Editable.anchorMax);
            }
        }

        private void SmartPositionAndSizeFields(bool anyWithoutParent)
        {
            var oldMix = EditorGUI.showMixedValue;
            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * 4);

            rect.height = EditorGUIUtility.singleLineHeight * 2;

            var anyStretchX = m_TinyTargets.Any(rt => rt.anchorMin.x != rt.anchorMax.x);
            var anyStretchY = m_TinyTargets.Any(rt => rt.anchorMin.y != rt.anchorMax.y);
            var anyNonStretchX = m_TinyTargets.Any(rt => rt.anchorMin.x == rt.anchorMax.x);
            var anyNonStretchY = m_TinyTargets.Any(rt => rt.anchorMin.y == rt.anchorMax.y);

            var rect2 = GetColumnRect(rect, 0);
            if (anyNonStretchX || anyWithoutParent)
            {
                EditorGUI.showMixedValue = m_TinyTargets.Select(rt => rt.anchoredPosition.x).Distinct().Count() > 1;
                m_Editable.anchoredPosition = new Vector2(FloatFieldWithLabelAbove(rect2, EditorGUIUtilityBridge.TrTextContent("Pos X"), m_Editable.anchoredPosition.x), m_Editable.anchoredPosition.y);
            }
            else
            {
                EditorGUI.showMixedValue = m_TinyTargets.Select(rt => rt.offsetMin.x).Distinct().Count() > 1;
                m_Editable.offsetMin = new Vector2(FloatFieldWithLabelAbove(rect2, EditorGUIUtilityBridge.TrTextContent("Left"), m_Editable.offsetMin.x), m_Editable.offsetMin.y);
            }

            rect2 = GetColumnRect(rect, 1);
            if (anyNonStretchY || anyWithoutParent)
            {
                EditorGUI.showMixedValue = m_TinyTargets.Select(rt => rt.anchoredPosition.y).Distinct().Count() > 1;
                m_Editable.anchoredPosition = new Vector2(m_Editable.anchoredPosition.x, FloatFieldWithLabelAbove(rect2, EditorGUIUtilityBridge.TrTextContent("Pos Y"), m_Editable.anchoredPosition.y));
            }
            else
            {
                EditorGUI.showMixedValue = m_TinyTargets.Select(rt => rt.offsetMax.y).Distinct().Count() > 1;
                m_Editable.offsetMax = new Vector2(m_Editable.offsetMax.x, - FloatFieldWithLabelAbove(rect2, EditorGUIUtilityBridge.TrTextContent("Top"), -m_Editable.offsetMax.y));
            }

            rect.y += EditorGUIUtility.singleLineHeight * 2;

            rect2 = GetColumnRect(rect, 0);
            if (anyNonStretchX || anyWithoutParent)
            {
                EditorGUI.showMixedValue = m_TinyTargets.Select(rt => rt.sizeDelta.x).Distinct().Count() > 1;
                var content = anyStretchX ? EditorGUIUtilityBridge.TrTextContent("W Delta") : EditorGUIUtilityBridge.TrTextContent("Width");
                m_Editable.sizeDelta = new Vector2(FloatFieldWithLabelAbove(rect2, content, m_Editable.sizeDelta.x), m_Editable.sizeDelta.y);
            }
            else
            {
                EditorGUI.showMixedValue = m_TinyTargets.Select(rt => rt.offsetMax.x).Distinct().Count() > 1;
                m_Editable.offsetMax = new Vector2(- FloatFieldWithLabelAbove(rect2, EditorGUIUtilityBridge.TrTextContent("Right"), -m_Editable.offsetMax.x), m_Editable.offsetMax.y);
            }

            rect2 = GetColumnRect(rect, 1);
            if (anyNonStretchY || anyWithoutParent)
            {
                EditorGUI.showMixedValue = m_TinyTargets.Select(rt => rt.sizeDelta.y).Distinct().Count() > 1;
                var content = anyStretchY ? EditorGUIUtilityBridge.TrTextContent("H Delta") : EditorGUIUtilityBridge.TrTextContent("Height");
                m_Editable.sizeDelta = new Vector2(m_Editable.sizeDelta.x, FloatFieldWithLabelAbove(rect2, content, m_Editable.sizeDelta.y));
            }
            else
            {
                EditorGUI.showMixedValue = m_TinyTargets.Select(rt => rt.offsetMin.y).Distinct().Count() > 1;
                m_Editable.offsetMin = new Vector2(m_Editable.offsetMin.x, FloatFieldWithLabelAbove(rect2, EditorGUIUtilityBridge.TrTextContent("Bottom"), m_Editable.offsetMin.y));
            }

            rect2 = rect;
            rect2.height = EditorGUIUtility.singleLineHeight;
            rect2.y += EditorGUIUtility.singleLineHeight;
            rect2.yMin -= 2;
            rect2.xMin = rect2.xMax - 26;
            rect2.x -= rect2.width;
            BlueprintButton(rect2);

            rect2.x += rect2.width;
            RawButton(rect2);

            EditorGUI.showMixedValue = oldMix;
        }

        private void RawButton(Rect position)
        {
            EditorGUI.BeginChangeCheck();
            m_RawEditMode = GUI.Toggle(position, m_RawEditMode, Styles.rawEditContent, "ButtonRight");
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool(k_LockRectPrefName, m_RawEditMode);
        }

        private static void BlueprintButton(Rect position)
        {
            EditorGUI.BeginChangeCheck();
            var newValue = GUI.Toggle(position, Tools.rectBlueprintMode, Styles.blueprintContent, "ButtonLeft");
            if (EditorGUI.EndChangeCheck())
            {
                Tools.rectBlueprintMode = newValue;
                ToolsBridge.RepaintAllToolViews();
            }
        }

        private static Rect GetColumnRect(Rect totalRect, int column)
        {
            totalRect.xMin += EditorGUIUtility.labelWidth - 1;
            var rect = totalRect;
            rect.xMin += (totalRect.width - 4) * (column / 3f) + column * 2;
            rect.width = (totalRect.width - 4) / 3f;
            return rect;
        }

        private static float FloatFieldWithLabelAbove(Rect position, GUIContent label, float value)
        {
            var id = GUIUtility.GetControlID(s_FloatFieldHash, FocusType.Keyboard, position);
            var positionLabel = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var positionField = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.HandlePrefixLabel(position, positionLabel, label, id);
            value = EditorGUIBridge.DoFloatField(positionField, positionLabel, id, value);
            return value;
        }

        private void SmartAnchorFields(ref UIVisitContext<TinyObject> context)
        {
            var target = context.MainTarget<TinyEntity>();
            var anchorRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * (m_ShowLayoutOptions ? 3 : 1));
            anchorRect.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.BeginChangeCheck();
            m_ShowLayoutOptions = EditorGUI.Foldout(anchorRect, m_ShowLayoutOptions, Styles.anchorsContent);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool(k_ShowAnchorPropsPrefName, m_ShowLayoutOptions);

            if (!m_ShowLayoutOptions)
                return;

            EditorGUI.indentLevel++;

            anchorRect.y += EditorGUIUtility.singleLineHeight;

            var newAnchorMin = Vector2Field(anchorRect, Styles.anchorMinContent, m_Editable.anchorMin);
            SetAnchorSmart(target, m_Editable, newAnchorMin.x, 0, false, !m_RawEditMode, true);
            SetAnchorSmart(target, m_Editable, newAnchorMin.y, 1, false, !m_RawEditMode, true);

            anchorRect.y += EditorGUIUtility.singleLineHeight;
            var newAnchorMax = Vector2Field(anchorRect, Styles.anchorMaxContent, m_Editable.anchorMax);
            SetAnchorSmart(target, m_Editable, newAnchorMax.x, 0, true, !m_RawEditMode, true);
            SetAnchorSmart(target, m_Editable, newAnchorMax.y, 1, true, !m_RawEditMode, true);

            EditorGUI.indentLevel--;
        }

        private void SmartPivotField(ref UIVisitContext<TinyObject> context)
        {
            var target = context.MainTarget<TinyEntity>();
            var newPivot = Vector2Field(EditorGUILayout.GetControlRect(), Styles.pivotContent, m_Editable.pivot);
            SetPivotSmart(target, m_Editable, newPivot.x, 0, !m_RawEditMode);
            SetPivotSmart(target, m_Editable, newPivot.y, 1, !m_RawEditMode);
        }

        private static Vector3 GetRectReferenceCorner(RectTransform gui)
        {
            return (Vector3)gui.rect.min + gui.transform.localPosition;
        }

        private static float Round(float value)
        {
            return Mathf.Floor(0.5f + value);
        }

        private static bool AnchorAllowedOutsideParent(int axis, int minmax)
        {
            // Allow dragging outside if action key is held down (same key that disables snapping).
            // Also allow when not dragging at all - for e.g. typing values into the Inspector.
            if (EditorGUI.actionKey || EditorGUIUtility.hotControl == 0)
                return true;
            // Also allow if drag started outside of range to begin with.
            var value = (minmax == 0 ? s_StartDragAnchorMin[axis] : s_StartDragAnchorMax[axis]);
            return (value < -0.001f || value > 1.001f);
        }

        private static bool ShouldDoIntSnapping()
        {
            // We don't support WorldSpace as of yet.
            return true;
        }

         private static Vector2 Vector2Field(Rect position,
             GUIContent label,
             Vector2 value)
        {
            EditorGUI.PrefixLabel(position, -1, label);
            var t = EditorGUIUtility.labelWidth;
            var l = EditorGUI.indentLevel;
            var r0 = GetColumnRect(position, 0);
            var r1 = GetColumnRect(position, 1);
            EditorGUIUtility.labelWidth = EditorGUIBridge.MiniLabelWidth;
            EditorGUI.indentLevel = 0;

            value.x = EditorGUI.FloatField(r0, s_AxisLabels[0], value.x);
            value.y = EditorGUI.FloatField(r1, s_AxisLabels[1], value.y);

            EditorGUIUtility.labelWidth = t;
            EditorGUI.indentLevel = l;
            return value;
        }

        private static void SetAnchorSmart(TinyEntity entity, TinyRectTransform rt, float value, int axis, bool isMax, bool smart, bool enforceExactValue, bool enforceMinNoLargerThanMax, bool moveTogether)
        {
            RectTransform parent = null;
            var parentTransform = entity.View.gameObject.GetComponent<Transform>().parent;
            if (parentTransform == null)
            {
                smart = false;
            }
            else
            {
                parent = parentTransform.GetComponent<RectTransform>();
                if (parent == null)
                    smart = false;
            }

            var clampToParent = !AnchorAllowedOutsideParent(axis, isMax ? 1 : 0);
            if (clampToParent)
            {
                value = Mathf.Clamp01(value);
            }

            if (enforceMinNoLargerThanMax)
            {
                value = isMax ? Mathf.Max(value, rt.anchorMin[axis]) : Mathf.Min(value, rt.anchorMax[axis]);
            }

            float offsetSizePixels = 0;
            float offsetPositionPixels = 0;
            if (smart)
            {
                var oldValue = isMax ? rt.anchorMax[axis] : rt.anchorMin[axis];

                offsetSizePixels = (value - oldValue) * parent.rect.size[axis];

                // Ensure offset is in whole pixels.
                // Note: In this particular instance we want to use Mathf.Round (which rounds towards nearest even number)
                // instead of Round from this class which always rounds down.
                // This makes the position of rect more stable when their anchors are changed.
                float roundingDelta = 0;
                if (ShouldDoIntSnapping())
                    roundingDelta = Mathf.Round(offsetSizePixels) - offsetSizePixels;
                offsetSizePixels += roundingDelta;

                if (!enforceExactValue)
                {
                    value += roundingDelta / parent.rect.size[axis];

                    // Snap value to whole percent if close
                    if (Mathf.Abs(Round(value * 1000) - value * 1000) < 0.1f)
                        value = Round(value * 1000) * 0.001f;

                    if (clampToParent)
                        value = Mathf.Clamp01(value);
                    if (enforceMinNoLargerThanMax)
                    {
                        value = isMax ? Mathf.Max(value, rt.anchorMin[axis]) : Mathf.Min(value, rt.anchorMax[axis]);
                    }
                }

                if (moveTogether)
                    offsetPositionPixels = offsetSizePixels;
                else
                    offsetPositionPixels = (isMax ? offsetSizePixels * rt.pivot[axis] : (offsetSizePixels * (1 - rt.pivot[axis])));
            }

            if (isMax)
            {
                var rectAnchorMax = rt.anchorMax;
                rectAnchorMax[axis] = value;
                rt.anchorMax = rectAnchorMax;

                var other = rt.anchorMin;
                if (moveTogether)
                    other[axis] = s_StartDragAnchorMin[axis] + rectAnchorMax[axis] - s_StartDragAnchorMax[axis];
                rt.anchorMin = other;
            }
            else
            {
                var rectAnchorMin = rt.anchorMin;
                rectAnchorMin[axis] = value;
                rt.anchorMin = rectAnchorMin;

                var other = rt.anchorMax;
                if (moveTogether)
                    other[axis] = s_StartDragAnchorMax[axis] + rectAnchorMin[axis] - s_StartDragAnchorMin[axis];
                rt.anchorMax = other;
            }

            if (smart)
            {
                var rectPosition = rt.anchoredPosition;
                rectPosition[axis] -= offsetPositionPixels;
                rt.anchoredPosition = rectPosition;

                if (!moveTogether)
                {
                    var rectSizeDelta = rt.sizeDelta;
                    rectSizeDelta[axis] += offsetSizePixels * (isMax ? -1 : 1);
                    rt.sizeDelta = rectSizeDelta;
                }
            }
        }
        #endregion
    }
}
