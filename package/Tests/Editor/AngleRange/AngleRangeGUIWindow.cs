using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditor.U2D;

public class SpriteShapeGUIWindow : EditorWindow
{
    private SpriteShape m_SpriteShape;
    private SerializedObject m_SerializedObject;
    private int m_SelectedRange = 0;
    private int m_OldNearestControl;

    public SpriteShape spriteShape
    {
        get { return m_SpriteShape; }
    }

    public SerializedObject serializedObject
    {
        get { return m_SerializedObject; }
    }

    public int selectedRange
    {
        get { return m_SelectedRange; }
        set { m_SelectedRange = value; }
    }

    public float radius { get; private set; }
    public float angleOffset { get; private set; }
    public Vector2 center { get; private set; }

    //[MenuItem("Window/SpriteShapeGUIWindow")]
    private static void Init()
    {
        SpriteShapeGUIWindow window = (SpriteShapeGUIWindow)EditorWindow.GetWindow(typeof(SpriteShapeGUIWindow));
        window.Show();
    }

    private void OnEnable()
    {
        Undo.undoRedoPerformed += UndoRedoPerformed;
        m_SpriteShape = Resources.Load<SpriteShape>("Empty");
        m_SerializedObject = new SerializedObject(m_SpriteShape);

        wantsMouseMove = true;
    }

    private void OnDestroy()
    {
        Undo.undoRedoPerformed -= UndoRedoPerformed;
    }

    private void UndoRedoPerformed()
    {
        Repaint();
    }

    private void OnGUI()
    {
        angleOffset = -90f;
        radius = 100f;
        Color color1 = new Color32(10, 46, 42, 255);
        Color color2 = new Color32(33, 151, 138, 255);
        SerializedProperty rangesProp = m_SerializedObject.FindProperty("m_Angles");

        m_SerializedObject.Update();

        Rect rect = new Rect(Vector2.zero, Vector2.one * 220f);
        center = rect.center;
        m_SelectedRange = AngleRangeGUI.AngleRangeListField(rect, rangesProp, m_SelectedRange, angleOffset, radius, true, color1, color2, color1);
        m_SelectedRange = AngleRangeGUI.HandleAddRange(rect, rangesProp, m_SelectedRange, radius, angleOffset);
        m_SelectedRange = AngleRangeGUI.HandleRemoveRange(rangesProp, m_SelectedRange);

        m_SerializedObject.ApplyModifiedProperties();

        HandleRepaintOnHover();
    }

    private void HandleRepaintOnHover()
    {
        if (!wantsMouseMove || GUIUtility.hotControl != 0)
            return;

        if (Event.current.type == EventType.Layout)
        {
            if (HandleUtility.nearestControl != m_OldNearestControl)
                Repaint();
        }

        m_OldNearestControl = HandleUtility.nearestControl;
    }
}
