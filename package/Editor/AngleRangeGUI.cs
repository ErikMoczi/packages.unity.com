using UnityEditor;
using UnityEngine;
using UnityEditor.Sprites;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.U2D
{
    public class AngleRangeGUI
    {
        public const float kRangeWidth = 10f;
        private static Color kHightlightColor = new Color(0.25f, 0.5f, 1.0f);
        private static Color kNoKeboardFocusColor = new Color(0.3f, 0.5f, 0.65f);

        public class AngleFieldState
        {
            public Rect rect;
        }

        private static class Contents
        {
            public static readonly GUIContent addRangeTooltip = new GUIContent("", "Click to add a new range");
        }

        private static int s_SelectRangeControlID = -1;
        private static int selectRangeControlID
        {
            get
            {
                if (s_SelectRangeControlID == -1)
                    s_SelectRangeControlID = GUIUtility.GetControlID("SelectRange".GetHashCode(), FocusType.Passive);

                return s_SelectRangeControlID;
            }
        }

        private static int s_HoveredRange = -1;
        private static int s_HoveredRangeId = -1;

        private static void GrabKeyboardFocus()
        {
            GUIUtility.keyboardControl = selectRangeControlID;
        }

        private static void ReleaseKeyboardFocus()
        {
            if (GUIUtility.keyboardControl == selectRangeControlID)
                GUIUtility.keyboardControl = 0;
        }

        private static bool HasKeyboardFocus()
        {
            return GUIUtility.keyboardControl == selectRangeControlID;
        }

        public static int HandleAddRange(Rect rect, SerializedProperty rangeListProperty, int selected, float radius, float angleOffset)
        {
            int controlId = GUIUtility.GetControlID("HandleAddRange".GetHashCode(), FocusType.Passive);
            EventType eventType = Event.current.GetTypeForControl(controlId);

            AngleFieldState state = GetAngleFieldState(controlId);

            if (eventType == EventType.Repaint)
            {
                state.rect = rect;
            }

            Vector2 mousePos = Event.current.mousePosition;

            float angle = Mathf.RoundToInt(SpriteShapeHandleUtility.PosToAngle(mousePos, state.rect.center, -angleOffset));

            float emptyRangeStart;
            float emptyRangeEnd;

            if (!GetNewRangeBounds(rangeListProperty, angle, out emptyRangeStart, out emptyRangeEnd))
                return selected;

            if (eventType == EventType.Layout && HandleUtility.nearestControl == 0)
            {
                float distance = SpriteShapeHandleUtility.DistanceToArcWidth(mousePos, state.rect.center, emptyRangeStart, emptyRangeEnd, radius, kRangeWidth, angleOffset);
                HandleUtility.AddControl(controlId, distance);
            }

            if (GUIUtility.hotControl == 0 && HandleUtility.nearestControl == controlId && eventType == EventType.MouseMove)
            {
                HandleUtility.Repaint();
            }

            if (GUIUtility.hotControl == 0 && HandleUtility.nearestControl == controlId)
            {
                if (eventType == EventType.Repaint)
                    EditorGUI.LabelField(new Rect(mousePos, new Vector2(1f, 20f)), Contents.addRangeTooltip);

                if (eventType == EventType.Repaint)
                    SpriteShapeHandleUtility.DrawRangeOutline(emptyRangeStart, emptyRangeEnd, angleOffset, state.rect.center, radius, kRangeWidth - 1f);
            }

            if (HandleUtility.nearestControl == controlId && eventType == EventType.MouseDown && Event.current.button == 0)
            {
                selected = HandleAddRangeFromAngle(rangeListProperty, angle, selected);
            }

            return selected;
        }

        public static int HandleAddRangeFromAngle(SerializedProperty rangeListProperty, float angle, int selected)
        {
            float emptyRangeStart;
            float emptyRangeEnd;

            if (!GetNewRangeBounds(rangeListProperty, angle, out emptyRangeStart, out emptyRangeEnd))
                return selected;

            rangeListProperty.InsertArrayElementAtIndex(rangeListProperty.arraySize);

            SerializedProperty newRange = rangeListProperty.GetArrayElementAtIndex(rangeListProperty.arraySize - 1);
            newRange.FindPropertyRelative("m_Start").floatValue = emptyRangeStart;
            newRange.FindPropertyRelative("m_End").floatValue = emptyRangeEnd;
            newRange.FindPropertyRelative("m_Sprites").arraySize = 0;

            selected = rangeListProperty.arraySize - 1;

            ValidateRange(rangeListProperty, selected, emptyRangeStart, emptyRangeEnd);

            GrabKeyboardFocus();
            GUI.changed = true;

            return selected;
        }

        public static int HandleRemoveRange(SerializedProperty rangeListProperty, int selected)
        {
            EventType eventType = Event.current.type;

            if (HasKeyboardFocus() && selected >= 0 && selected < rangeListProperty.arraySize && eventType == EventType.KeyDown &&
                (Event.current.keyCode == KeyCode.Delete || Event.current.keyCode == KeyCode.Backspace))
            {
                rangeListProperty.DeleteArrayElementAtIndex(selected);
                selected = -1;
                ReleaseKeyboardFocus();
                Event.current.Use();
                GUI.changed = true;
            }

            return selected;
        }

        public static bool GetNewRangeBounds(SerializedProperty rangeListProperty, float angle, out float emptyRangeStart, out float emptyRangeEnd)
        {
            emptyRangeStart = float.MinValue;
            emptyRangeEnd = float.MaxValue;

            int index = GetRangeIndexFromAngle(rangeListProperty, angle);

            if (index != -1)
                return false;

            FindMinMaxFromRangeList(rangeListProperty, out emptyRangeEnd, out emptyRangeStart);

            if (angle < emptyRangeStart)
                emptyRangeStart -= 360f;

            if (angle > emptyRangeEnd)
                emptyRangeEnd += 360f;

            for (int i = 0; i < rangeListProperty.arraySize; ++i)
            {
                SerializedProperty rangeProperty = rangeListProperty.GetArrayElementAtIndex(i);
                SerializedProperty startProperty = rangeProperty.FindPropertyRelative("m_Start");
                SerializedProperty endProperty = rangeProperty.FindPropertyRelative("m_End");

                float start = startProperty.floatValue;
                float end = endProperty.floatValue;

                if (angle > end)
                    emptyRangeStart = Mathf.Max(emptyRangeStart, end);

                if (angle < start)
                    emptyRangeEnd = Mathf.Min(emptyRangeEnd, start);
            }

            float rangeLength = emptyRangeEnd - emptyRangeStart;

            if (rangeLength > 90f)
            {
                emptyRangeStart = Mathf.Max(angle - 45f, emptyRangeStart);
                emptyRangeEnd = Mathf.Min(angle + 45f, emptyRangeEnd);
            }

            return true;
        }

        private static int GetRangeIndexFromAngle(SerializedProperty rangeListProperty, float angle)
        {
            for (int i = 0; i < rangeListProperty.arraySize; ++i)
            {
                SerializedProperty rangeProperty = rangeListProperty.GetArrayElementAtIndex(i);
                SerializedProperty startProperty = rangeProperty.FindPropertyRelative("m_Start");
                SerializedProperty endProperty = rangeProperty.FindPropertyRelative("m_End");

                float start = startProperty.floatValue;
                float end = endProperty.floatValue;
                float range = end - start;

                float angle2 = Mathf.Repeat(angle - start, 360f);

                if (angle2 >= 0f && angle2 <= range)
                    return i;
            }

            return -1;
        }

        public static int AngleRangeListField(SerializedProperty rangeListProperty, int selected, float angleOffset, float radius, bool snap, Color gradientMin, Color gradientMid, Color gradientMax)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, radius * 2f);
            return AngleRangeListField(rect, rangeListProperty, selected, angleOffset, radius, snap, gradientMin, gradientMid, gradientMax);
        }

        public static int AngleRangeListField(Rect rect, SerializedProperty rangeListProperty, int selected, float angleOffset, float radius, bool snap, Color gradientMin, Color gradientMid, Color gradientMax)
        {
            int id = GUIUtility.GetControlID("AngleRangeListField".GetHashCode(), FocusType.Passive);
            EventType eventType = Event.current.GetTypeForControl(id);

            AngleFieldState state = GetAngleFieldState(id);

            if (eventType == EventType.Repaint)
            {
                state.rect = rect;
            }
            else if (eventType == EventType.Layout)
            {
                s_HoveredRange = -1;
                s_HoveredRangeId = -1;
            }

            for (int i = 0; i < rangeListProperty.arraySize; ++i)
            {
                int controlId = GUIUtility.GetControlID("AngleRangeField".GetHashCode(), FocusType.Passive);

                SerializedProperty rangeProperty = rangeListProperty.GetArrayElementAtIndex(i);
                SerializedProperty startProperty = rangeProperty.FindPropertyRelative("m_Start");
                SerializedProperty endProperty = rangeProperty.FindPropertyRelative("m_End");

                if (eventType == EventType.Layout)
                {
                    float distance = SpriteShapeHandleUtility.DistanceToArcWidth(Event.current.mousePosition, state.rect.center, startProperty.floatValue, endProperty.floatValue, radius, kRangeWidth, angleOffset);

                    HandleUtility.AddControl(controlId, distance);

                    if (HandleUtility.nearestControl == controlId)
                    {
                        s_HoveredRange = i;
                        s_HoveredRangeId = controlId;
                    }
                }

                if (selected == i)
                    continue;

                using (new EditorGUI.DisabledScope(true))
                {
                    float midAngle = (endProperty.floatValue - startProperty.floatValue) * 0.5f + startProperty.floatValue;
                    float t = 2f * (midAngle + 180f) / 360f;
                    Color color = gradientMin;

                    if (t < 1f)
                        color = Color.Lerp(gradientMin, gradientMid, t);
                    else
                        color = Color.Lerp(gradientMid, gradientMax, t - 1f);

                    AngleRangeField(rect, startProperty, endProperty, angleOffset, radius, snap, false, false, color);
                }
            }

            bool deleteSelected = false;

            if (selected >= 0 && selected < rangeListProperty.arraySize)
            {
                SerializedProperty rangeProperty = rangeListProperty.GetArrayElementAtIndex(selected);
                SerializedProperty startProperty = rangeProperty.FindPropertyRelative("m_Start");
                SerializedProperty endProperty = rangeProperty.FindPropertyRelative("m_End");

                float prevStart = startProperty.floatValue;
                float prevEnd = endProperty.floatValue;

                if (eventType == EventType.MouseUp && prevStart == prevEnd)
                {
                    deleteSelected = true;
                }

                EditorGUI.BeginChangeCheck();

                Color color = kNoKeboardFocusColor;

                if (HasKeyboardFocus())
                    color = kHightlightColor;

                AngleRangeField(rect, startProperty, endProperty, angleOffset, radius, snap, false, false, color);

                if (EditorGUI.EndChangeCheck())
                    ValidateRange(rangeListProperty, selected, prevStart, prevEnd);
            }

            if (deleteSelected)
            {
                rangeListProperty.DeleteArrayElementAtIndex(selected);

                ReleaseKeyboardFocus();

                selected = -1;

                GUI.changed = true;
            }

            if (GUIUtility.hotControl == 0 && HandleUtility.nearestControl == s_HoveredRangeId)
            {
                if (eventType == EventType.MouseDown)
                {
                    selected = s_HoveredRange;

                    GrabKeyboardFocus();

                    GUI.changed = true;
                }

                if (eventType == EventType.Repaint && selected != s_HoveredRange)
                {
                    SerializedProperty rangeProperty = rangeListProperty.GetArrayElementAtIndex(s_HoveredRange);
                    SerializedProperty startProperty = rangeProperty.FindPropertyRelative("m_Start");
                    SerializedProperty endProperty = rangeProperty.FindPropertyRelative("m_End");

                    SpriteShapeHandleUtility.DrawRangeOutline(startProperty.floatValue, endProperty.floatValue, angleOffset, state.rect.center, radius, kRangeWidth);
                }
            }

            return selected;
        }

        private static void FindMinMaxFromRangeList(SerializedProperty rangeListProperty, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;

            for (int i = 0; i < rangeListProperty.arraySize; ++i)
            {
                SerializedProperty rangeProperty = rangeListProperty.GetArrayElementAtIndex(i);
                SerializedProperty startProperty = rangeProperty.FindPropertyRelative("m_Start");
                SerializedProperty endProperty = rangeProperty.FindPropertyRelative("m_End");

                min = Mathf.Min(startProperty.floatValue, min);
                max = Mathf.Max(endProperty.floatValue, max);
            }
        }

        public static void ValidateRange(SerializedProperty rangeListProperty, int index, float prevStart, float prevEnd)
        {
            SerializedProperty rangeProperty = rangeListProperty.GetArrayElementAtIndex(index);
            SerializedProperty startProperty = rangeProperty.FindPropertyRelative("m_Start");
            SerializedProperty endProperty = rangeProperty.FindPropertyRelative("m_End");

            float start = startProperty.floatValue;
            float end = endProperty.floatValue;

            for (int i = 0; i < rangeListProperty.arraySize; ++i)
            {
                if (i == index)
                {
                    if ((start > 180f && end > 180f) || (start < -180f && end < -180f))
                    {
                        start = Mathf.Repeat(start + 180f, 360f) - 180f;
                        end = Mathf.Repeat(end + 180f, 360f) - 180f;
                    }
                }
                else
                {
                    SerializedProperty otherRangeProperty = rangeListProperty.GetArrayElementAtIndex(i);
                    SerializedProperty otherStartProperty = otherRangeProperty.FindPropertyRelative("m_Start");
                    SerializedProperty otherEndProperty = otherRangeProperty.FindPropertyRelative("m_End");

                    ValidateRangeStartEnd(ref start, ref end, prevStart, prevEnd, otherStartProperty.floatValue, otherEndProperty.floatValue);
                }
            }

            startProperty.floatValue = start;
            endProperty.floatValue = end;
        }

        public static void ValidateRangeStartEnd(ref float start, ref float end, float prevStart, float prevEnd, float otherStart, float otherEnd)
        {
            float min = Mathf.Min(start, otherStart);
            float max = Mathf.Max(end, otherEnd);

            start -= min;
            end -= min;
            otherStart -= min;
            otherEnd -= min;
            prevStart -= min;
            prevEnd -= min;

            if (prevEnd != end)
                end = Mathf.Clamp(end, start, otherStart > start ? otherStart : 360f);

            start += min - max;
            end += min - max;
            otherStart += min - max;
            otherEnd += min - max;
            prevStart += min - max;
            prevEnd += min - max;

            if (prevStart != start)
                start = Mathf.Clamp(start, otherEnd < end ? otherEnd : -360f, end);

            start += max;
            end += max;
        }

        public static void AngleRangeField(SerializedProperty startProperty, SerializedProperty endProperty, float angleOffset, float radius, bool snap, bool drawLine, bool drawCircle, Color rangeColor)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, radius * 2f);
            AngleRangeField(rect, startProperty, endProperty, angleOffset, radius, snap, drawLine, drawCircle, rangeColor);
        }

        public static void AngleRangeField(Rect rect, SerializedProperty startProperty, SerializedProperty endProperty, float angleOffset, float radius, bool snap, bool drawLine, bool drawCircle, Color rangeColor)
        {
            Color activeColor = Handles.color;

            if (Event.current.type == EventType.Repaint)
            {
                float range = endProperty.floatValue - startProperty.floatValue;

                Color color = Handles.color;
                Handles.color = rangeColor;
                if (range < 0f)
                    Handles.color = Color.red;

                SpriteShapeHandleUtility.DrawSolidArc(rect.center, Vector3.forward, Quaternion.AngleAxis(startProperty.floatValue + angleOffset, Vector3.forward) * Vector3.right, range, radius, kRangeWidth);
                Handles.color = color;
            }

            EditorGUI.BeginChangeCheck();

            float handleSize = 15f;

            int leftId = GUIUtility.GetControlID("RangeLeft".GetHashCode(), FocusType.Passive);
            AngleField(rect, leftId, startProperty, angleOffset, new Vector2(-3.5f, -7.5f), startProperty.floatValue + angleOffset + 90f, handleSize, radius, snap, drawLine, drawCircle, SpriteShapeHandleUtility.RangeLeftCap);

            if (EditorGUI.EndChangeCheck())
                startProperty.floatValue = Mathf.Clamp(startProperty.floatValue, endProperty.floatValue - 360f, endProperty.floatValue);

            EditorGUI.BeginChangeCheck();

            int rightId = GUIUtility.GetControlID("RangeRight".GetHashCode(), FocusType.Passive);
            AngleField(rect, rightId, endProperty, angleOffset, new Vector2(3.5f, -7.5f), endProperty.floatValue + angleOffset + 90f, handleSize, radius, snap, drawLine, false, SpriteShapeHandleUtility.RangeRightCap);

            if (EditorGUI.EndChangeCheck())
                endProperty.floatValue = Mathf.Clamp(endProperty.floatValue, startProperty.floatValue, startProperty.floatValue + 360f);

            Handles.color = activeColor;
        }

        public static void AngleField(int id, SerializedProperty property, float angleOffset, Vector2 handleOffset, float handleAngle, float handleSize, float radius, bool snap, bool drawLine, bool drawCircle, Handles.CapFunction drawCapFunction)
        {
            EditorGUI.BeginChangeCheck();
            float value = AngleField(id, property.floatValue, angleOffset, handleOffset, handleAngle, handleSize, radius, snap, drawLine, drawCircle, drawCapFunction);
            if (EditorGUI.EndChangeCheck())
            {
                property.floatValue = value;
            }
        }

        public static void AngleField(Rect r, int id, SerializedProperty property, float angleOffset, Vector2 handleOffset, float handleAngle, float handleSize, float radius, bool snap, bool drawLine, bool drawCircle, Handles.CapFunction drawCapFunction)
        {
            EditorGUI.BeginChangeCheck();
            float value = AngleField(r, id, property.floatValue, angleOffset, handleOffset, handleAngle, handleSize, radius, snap, drawLine, drawCircle, drawCapFunction);
            if (EditorGUI.EndChangeCheck())
            {
                property.floatValue = value;
            }
        }

        public static float AngleField(int id, float angle, float angleOffset, Vector2 handleOffset, float handleAngle, float radius, float handleSize, bool snap, bool drawLine, bool drawCircle, Handles.CapFunction drawCapFunction)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, radius * 2f);
            return AngleField(rect, id, angle, angleOffset, handleOffset, handleAngle, radius, handleSize, snap, drawLine, drawCircle, drawCapFunction);
        }

        public static float AngleField(Rect rect, int id, float angle, float angleOffset, Vector2 handleOffset, float handleAngle, float handleSize, float radius, bool snap, bool drawLine, bool drawCircle, Handles.CapFunction drawCapFunction)
        {
            AngleFieldState state = GetAngleFieldState(id);

            if (Event.current.type == EventType.Repaint)
            {
                state.rect = rect;
            }

            float offsetedAngle = angle + angleOffset;
            Vector2 pos = new Vector2(Mathf.Cos(offsetedAngle * Mathf.Deg2Rad), Mathf.Sin(offsetedAngle * Mathf.Deg2Rad)) * radius + state.rect.center;

            if (Event.current.type == EventType.Repaint)
            {
                if (drawCircle)
                    Handles.DrawWireDisc(state.rect.center, Vector3.forward, radius);

                if (drawLine)
                    Handles.DrawLine(state.rect.center, pos);
            }

            if (GUI.enabled)
            {
                EditorGUI.BeginChangeCheck();

                Quaternion rotation = Quaternion.AngleAxis(handleAngle, Vector3.forward);
                Vector2 posNew = SpriteShapeHandleUtility.Slider2D(id, pos, rotation * handleOffset, rotation, handleSize, drawCapFunction);

                if (EditorGUI.EndChangeCheck())
                {
                    Vector2 v1 = pos - state.rect.center;
                    Vector2 v2 = posNew - state.rect.center;

                    float angleDirection = Mathf.Sign(Vector3.Dot(Vector3.forward, Vector3.Cross(v1, v2)));
                    float angleIncrement = Vector2.Angle(v1, v2);

                    angle += angleIncrement * angleDirection;

                    if (snap)
                        angle = Mathf.RoundToInt(angle);
                }
            }

            return angle;
        }

        public static AngleFieldState GetAngleFieldState(int id)
        {
            return (AngleFieldState)GUIUtility.GetStateObject(typeof(AngleFieldState), id);
        }
    }
}
