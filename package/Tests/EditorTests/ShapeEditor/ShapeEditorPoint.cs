using UnityEditor;
using UnityEngine;

public class ShapeEditorPoint
{
    public Vector3 m_Position;
    public Vector3 m_LeftTangent;
    public Vector3 m_RightTangent;
    public UnityEditor.U2D.ShapeEditor.TangentMode m_Mode;

    internal ShapeEditorPoint(Vector3 p, Vector3 lt, Vector3 rt, UnityEditor.U2D.ShapeEditor.TangentMode mode)
    {
        m_Position = p;
        m_LeftTangent = lt;
        m_RightTangent = rt;
        m_Mode = mode;
    }
}
