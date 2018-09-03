using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;

public class ShapeEditorWindow : EditorWindow
{
    private const float kWindowHeaderHeight = 20f;
    public const float kTangentLength = 20f;
    public const float kShapeMargin = 10f;
    public const float kShapeEdgeLength = 200f;

    // This Menuitem is for debugging purposes
    [MenuItem("Window/ShapeEditorTestWindow")]
    static void Init()
    {
        ShapeEditorWindow window = (ShapeEditorWindow)EditorWindow.GetWindow(typeof(ShapeEditorWindow));
        window.Show();
    }

    internal UnityEditor.U2D.ShapeEditor m_ShapeEditor;
    public List<ShapeEditorPoint> m_Points;
    public int repaintCounter { get; set; }

    public virtual void OnEnable()
    {
        m_Points = new List<ShapeEditorPoint>();

        // Rectangle shape of size kShapeEdgeLength in top-left window corner with margin of kShapeMargin and tangents of kTangentLength
        m_Points.Add(new ShapeEditorPoint(Vector2.one * kShapeMargin, Vector3.up * kTangentLength, Vector3.right * kTangentLength, UnityEditor.U2D.ShapeEditor.TangentMode.Broken));
        m_Points.Add(new ShapeEditorPoint(Vector2.right * kShapeEdgeLength + Vector2.one * kShapeMargin, Vector3.left * kTangentLength, Vector3.up * kTangentLength, UnityEditor.U2D.ShapeEditor.TangentMode.Broken));
        m_Points.Add(new ShapeEditorPoint(Vector2.one * kShapeEdgeLength + Vector2.one * kShapeMargin, Vector3.down * kTangentLength, Vector3.left * kTangentLength, UnityEditor.U2D.ShapeEditor.TangentMode.Broken));
        m_Points.Add(new ShapeEditorPoint(Vector2.up * kShapeEdgeLength + Vector2.one * kShapeMargin, Vector3.right * kTangentLength, Vector3.down * kTangentLength, UnityEditor.U2D.ShapeEditor.TangentMode.Broken));

        wantsMouseMove = true;
        openEnded = false;
        InitShapeEditor();
        repaintCounter = 0;
    }

    public void InitShapeEditor()
    {
        m_ShapeEditor = new UnityEditor.U2D.ShapeEditor();
        m_ShapeEditor.GetPosition = i => m_Points[i].m_Position;
        m_ShapeEditor.GetLeftTangent = i => m_Points[i].m_LeftTangent;
        m_ShapeEditor.GetRightTangent = i => m_Points[i].m_RightTangent;
        m_ShapeEditor.GetTangentMode = i => m_Points[i].m_Mode;
        m_ShapeEditor.GetPointCount = () => m_Points.Count;
        m_ShapeEditor.SetPosition = (i, vector3) => m_Points[i].m_Position = vector3;
        m_ShapeEditor.SetLeftTangent = (i, vector3) => m_Points[i].m_LeftTangent = vector3;
        m_ShapeEditor.SetRightTangent = (i, vector3) => m_Points[i].m_RightTangent = vector3;
        m_ShapeEditor.SetTangentMode = (i, mode) => m_Points[i].m_Mode = mode;
        m_ShapeEditor.ScreenToWorld = vector2 => new Vector3(vector2.x, vector2.y, 0);
        m_ShapeEditor.WorldToScreen = vector3 => new Vector2(vector3.x, vector3.y);
        m_ShapeEditor.RemovePointAt = i => m_Points.RemoveAt(i);
        m_ShapeEditor.InsertPointAt = (i, vector3) => m_Points.Insert(i, new ShapeEditorPoint(vector3, Vector3.zero, Vector3.zero, UnityEditor.U2D.ShapeEditor.TangentMode.Linear));
        m_ShapeEditor.OpenEnded = () => openEnded;
    }

    public void OnDisable()
    {
        m_ShapeEditor = null;
    }

    public void OnGUI()
    {
        if (Event.current.type == EventType.Repaint)
            ++repaintCounter;

        EditorGUI.BeginChangeCheck();
        m_ShapeEditor.OnGUI();
        if (EditorGUI.EndChangeCheck())
        {
            m_ShapeEditor.SetDirty();
            Repaint();
        }
    }

    public bool openEnded { get; set; }


    public Vector2 GetEdgeWindowPosition(int edgeIndex)
    {
        int nextIndex = SplineUtility.NextIndex(edgeIndex, m_Points.Count);
        Vector2 start = m_Points[edgeIndex].m_Position;
        Vector2 startTangent = (Vector2)m_Points[edgeIndex].m_RightTangent + start;
        Vector2 end = m_Points[nextIndex].m_Position;
        Vector2 endTangent = (Vector2)m_Points[nextIndex].m_LeftTangent + end;

        return BezierUtility.BezierPoint(start, startTangent, endTangent, end, 0.5f) + Vector3.up * kWindowHeaderHeight;
    }

    public Vector2 GetPointWindowPosition(int pointIndex)
    {
        Vector2 localPosition = m_Points[pointIndex].m_Position;
        Vector2 screenPosition = m_ShapeEditor.WorldToScreen(localPosition);
        return screenPosition + new Vector2(0, kWindowHeaderHeight);
    }

    public Vector2 GetLeftTangentWindowPosition(int pointIndex)
    {
        Vector2 localPosition = m_Points[pointIndex].m_Position + m_Points[pointIndex].m_LeftTangent;
        Vector2 screenPosition = m_ShapeEditor.WorldToScreen(localPosition);
        return screenPosition + new Vector2(0, kWindowHeaderHeight);
    }

    public Vector2 GetRightTangentWindowPosition(int pointIndex)
    {
        Vector2 localPosition = m_Points[pointIndex].m_Position + m_Points[pointIndex].m_RightTangent;
        Vector2 screenPosition = m_ShapeEditor.WorldToScreen(localPosition);
        return screenPosition + new Vector2(0, kWindowHeaderHeight);
    }

    public void ClickWindow(Vector2 windowPosition)
    {
        var ev = new Event();

        ev.type = EventType.MouseDown;
        ev.mousePosition = windowPosition;
        SendEvent(ev);
        ev.type = EventType.MouseUp;
        ev.mousePosition = windowPosition;
        SendEvent(ev);
    }

    public void DragInWindow(Vector2 dragStart, Vector2 dragEnd, bool shift = false, bool command = false, bool control = false)
    {
        var ev = new Event();
        ev.shift = shift;
        ev.command = command;
        ev.control = control;
        ev.type = EventType.MouseDown;
        ev.mousePosition = dragStart;
        SendEvent(ev);
        ev.type = EventType.MouseDrag;
        ev.delta = dragStart - ev.mousePosition;
        ev.mousePosition = dragStart;
        SendEvent(ev);
        ev.type = EventType.MouseDrag;
        ev.delta = dragEnd - ev.mousePosition;
        ev.mousePosition = dragEnd;
        SendEvent(ev);
        ev.type = EventType.MouseUp;
        ev.delta = dragEnd - ev.mousePosition;
        ev.mousePosition = dragEnd;
        SendEvent(ev);
    }
}
