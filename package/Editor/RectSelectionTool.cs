using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace UnityEditor.U2D
{
    public class RectSelectionTool
    {
        Vector3 m_StartPoint;
        Rect m_Rect;
        GUIStyle m_GuiStyle;

        public Rect Do(int controlId, Vector3 origin)
        {
            if (m_GuiStyle == null)
                m_GuiStyle = GUI.skin.FindStyle("selectionRect");

            Handles.BeginGUI();

            Event currentEvent = Event.current;

            Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);

            Vector3 forward = Camera.current.transform.forward;
            Vector3 up = Camera.current.transform.up;
            Vector3 right = Camera.current.transform.right;

            Plane plane = new Plane(forward, origin);

            float distance;
            if (!plane.Raycast(ray, out distance))
                return new Rect();

            Vector3 worldPoint = ray.GetPoint(distance);

            EventType eventType = currentEvent.GetTypeForControl(controlId);

            if (eventType == EventType.Layout)
                HandleUtility.AddDefaultControl(controlId);

            if (eventType == EventType.MouseDown && currentEvent.button == 0)
            {
                m_StartPoint = worldPoint;
                m_Rect = new Rect(m_StartPoint, Vector3.zero);
            }

            if (GUIUtility.hotControl == controlId && eventType == EventType.Repaint)
                m_GuiStyle.Draw(m_Rect, GUIContent.none, false, false, false, false);

            EditorGUI.BeginChangeCheck();

            worldPoint = Handles.Slider2D(controlId, worldPoint, forward, up, right, 1f, (int cid, Vector3 p, Quaternion q, float s, EventType et) => { }, Vector2.zero);

            if (EditorGUI.EndChangeCheck())
                m_Rect = FromToRect(HandleUtility.WorldToGUIPoint(m_StartPoint), HandleUtility.WorldToGUIPoint(worldPoint));

            Handles.EndGUI();

            return m_Rect;
        }

        Rect FromToRect(Vector2 start, Vector2 end)
        {
            Rect r = new Rect(start.x, start.y, end.x - start.x, end.y - start.y);
            if (r.width < 0)
            {
                r.x += r.width;
                r.width = -r.width;
            }
            if (r.height < 0)
            {
                r.y += r.height;
                r.height = -r.height;
            }
            return r;
        }
    }
}
