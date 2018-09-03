using NUnit.Framework;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.TestTools;
using UnityEditor;
using UnityEditor.U2D;
using UnityEditor.Experimental.U2D.Common;

public class SpriteShapeGUITests
{
    private SpriteShape m_SpriteShape;
    private SpriteShapeGUIWindow m_Window;

    private static KeyCode[] s_DeleteKeys = new KeyCode[] { KeyCode.Backspace, KeyCode.Delete };

    private static IEnumerable<TestCaseData> DeleteKeyCases()
    {
        foreach (var key in s_DeleteKeys)
        {
            yield return new TestCaseData(key);
        }
    }

    [OneTimeSetUp]
    public void SetUp()
    {
        m_Window = EditorWindow.GetWindow<SpriteShapeGUIWindow>();
        m_Window.position = new Rect(Vector2.one * 100f, Vector2.one * 220f);
        m_Window.selectedRange = -1;
        m_Window.Show(true);
        m_Window.Focus();
        InternalEditorBridge.RepaintImmediately(m_Window);
        ResetRanges();
    }

    [TearDown]
    public void TearDown()
    {
        ResetRanges();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        m_Window.Close();
    }

    private void ResetRanges()
    {
        SerializedProperty rangesProp = m_Window.serializedObject.FindProperty("m_Angles");
        m_Window.serializedObject.Update();
        rangesProp.arraySize = 0;
        m_Window.serializedObject.ApplyModifiedProperties();
    }

    private void MouseClick(Vector2 position, int button)
    {
        position += Vector2.up * 22f;
        Event ev = new Event { mousePosition = position, button = button };
        ev.type = EventType.MouseDown;
        m_Window.SendEvent(ev);
        ev.type = EventType.MouseUp;
        m_Window.SendEvent(ev);

        InternalEditorBridge.RepaintImmediately(m_Window);
    }

    private void MouseDrag(Vector2 position, Vector2 position2, int button)
    {
        MouseDrag(position, new Vector2[] { position2 }, button);
    }

    private void MouseDrag(Vector2 position, Vector2[] positions, int button)
    {
        position += Vector2.up * 22f;
        Event ev = new Event { button = button };
        ev.mousePosition = position;
        ev.type = EventType.MouseDown;
        m_Window.SendEvent(ev);

        Vector2 lastPosition = position;
        foreach (Vector2 p in positions)
        {
            Vector2 pos = p + Vector2.up * 22f;
            ev.mousePosition = pos;
            ev.delta = p - lastPosition;
            ev.type = EventType.MouseDrag;
            m_Window.SendEvent(ev);
            lastPosition = pos;
        }

        ev.mousePosition = lastPosition;
        ev.delta = Vector2.zero;
        ev.type = EventType.MouseUp;
        m_Window.SendEvent(ev);

        InternalEditorBridge.RepaintImmediately(m_Window);
    }

    private void ClickAtAngle(float angle)
    {
        MouseClick(PositionFromAngle(m_Window.radius - AngleRangeGUI.kRangeWidth * 0.5f, angle), 0);
    }

    private void DragStartHandle(int rangeIndex, Vector2 position)
    {
        DragStartHandle(rangeIndex, new Vector2[] { position });
    }

    private void DragStartHandle(int rangeIndex, Vector2[] positions)
    {
        float angle = GetRangeStart(rangeIndex);
        MouseDrag(PositionFromAngle(m_Window.radius + 2.5f, angle - 1f), positions, 0);
    }

    private void DragEndHandle(int rangeIndex, Vector2 position)
    {
        DragEndHandle(rangeIndex, new Vector2[] { position });
    }

    private void DragEndHandle(int rangeIndex, Vector2[] positions)
    {
        float angle = GetRangeEnd(rangeIndex);
        MouseDrag(PositionFromAngle(m_Window.radius + 2.5f, angle + 1f), positions, 0);
    }

    private Vector2 PositionFromAngle(float radius, float angle)
    {
        float offsetedAngle = angle + m_Window.angleOffset;
        return new Vector2(Mathf.Cos(offsetedAngle * Mathf.Deg2Rad), Mathf.Sin(offsetedAngle * Mathf.Deg2Rad)) * radius + m_Window.center;
    }

    private void Key(KeyCode key)
    {
        Event ev = new Event { keyCode = key, type = EventType.KeyDown };
        m_Window.SendEvent(ev);
        ev.type = EventType.KeyUp;
        m_Window.SendEvent(ev);

        InternalEditorBridge.RepaintImmediately(m_Window);
    }

    private int GetRangeCount()
    {
        SerializedProperty rangesProp = m_Window.serializedObject.FindProperty("m_Angles");
        return rangesProp.arraySize;
    }

    private float GetRangeStart(int rangeIndex)
    {
        SerializedProperty rangesProp = m_Window.serializedObject.FindProperty("m_Angles");
        return rangesProp.GetArrayElementAtIndex(rangeIndex).FindPropertyRelative("m_Start").floatValue;
    }

    private float GetRangeEnd(int rangeIndex)
    {
        SerializedProperty rangesProp = m_Window.serializedObject.FindProperty("m_Angles");
        return rangesProp.GetArrayElementAtIndex(rangeIndex).FindPropertyRelative("m_End").floatValue;
    }

    [Test]
    public void CreateSingleRangeIs90DegAndSelected()
    {
        ClickAtAngle(0);

        Assert.AreEqual(1, GetRangeCount());
        Assert.AreEqual(90f, GetRangeEnd(0) - GetRangeStart(0));
        Assert.AreEqual(0, m_Window.selectedRange);
    }

    [Test]
    public void CreateRangeInBetweenTwoRangesFillsTheGapIfGapIsLessThan90Deg()
    {
        ClickAtAngle(0);
        ClickAtAngle(-120);
        ClickAtAngle(-50);

        Assert.AreEqual(3, GetRangeCount());
        Assert.AreEqual(-45, GetRangeStart(0));
        Assert.AreEqual(45, GetRangeEnd(0));
    }

    [Test]
    public void CreateRangeInBetweenTwoRangesDoesntFillTheGapIfGapIsGreaterThan90Deg()
    {
        ClickAtAngle(120);
        ClickAtAngle(-120);
        ClickAtAngle(0);

        Assert.AreEqual(3, GetRangeCount());
        Assert.AreEqual(75.0f, GetRangeStart(0));
        Assert.AreEqual(165.0f, GetRangeEnd(0));
    }

    [Test]
    public void CreateRangeInBetweenTwoRangesFillsTheGapIfGapIsLessThan90DegWrapAround180()
    {
        ClickAtAngle(100);
        ClickAtAngle(-100);
        ClickAtAngle(-170);

        Assert.AreEqual(3, GetRangeCount());
        Assert.AreEqual(GetRangeEnd(0) - 360f, GetRangeStart(2));
        Assert.AreEqual(GetRangeStart(1), GetRangeEnd(2));
    }

    [Test]
    public void CreateRangeInBetweenTwoRangesFillsTheGapIfGapIsLessThan90DegWrapAroundMinus180()
    {
        ClickAtAngle(120);
        ClickAtAngle(-120);
        ClickAtAngle(170);

        Assert.AreEqual(3, GetRangeCount());
        Assert.AreEqual(GetRangeEnd(0), GetRangeStart(2));
        Assert.AreEqual(GetRangeStart(1) + 360f, GetRangeEnd(2));
    }

    [Test]
    public void UnselectRange()
    {
        ClickAtAngle(0);
        MouseClick(m_Window.center, 0);

        Assert.AreEqual(0, m_Window.selectedRange);
    }

    [Test]
    public void ModifyRange()
    {
        ClickAtAngle(0);
        DragEndHandle(0, new Vector2(186f, 186f));

        Assert.AreEqual(127f, GetRangeEnd(0));
    }

    [Test]
    public void ModifyRangeEndCanBeGreaterThan180()
    {
        ClickAtAngle(0);
        DragEndHandle(0, new Vector2[] { new Vector2(184f, 184f), new Vector2(40f, 190f) });

        Assert.AreEqual(245f, GetRangeEnd(0));
    }

    [Test]
    public void ModifyRangeStartAndEndGreaterThan180WrapToMinus180()
    {
        ClickAtAngle(5);
        DragEndHandle(0, new Vector2[] {
            new Vector2(190f, 190f),
            new Vector2(40f, 190f),
            new Vector2(40f, 50f)
        });
        DragStartHandle(0, new Vector2[] {
            new Vector2(190f, 40f),
            new Vector2(190f, 190f),
            new Vector2(40f, 190f)
        });

        Assert.AreEqual(-40f, GetRangeStart(0));
        Assert.AreEqual(258.0f, GetRangeEnd(0));
    }

    [Test]
    public void ModifyRangeStartAndEndLessThanMinus180WrapTo180()
    {
        ClickAtAngle(-5);
        DragStartHandle(0, new Vector2[] {
            new Vector2(40f, 190f),
            new Vector2(190f, 190f),
            new Vector2(190f, 100f)
        });
        DragEndHandle(0, new Vector2[] {
            new Vector2(40f, 190f),
            new Vector2(40f, 190f),
            new Vector2(190f, 190f)
        });

        Assert.AreEqual(46f, GetRangeStart(0));
        Assert.AreEqual(100f, GetRangeEnd(0));
    }

    [Test]
    public void IncreaseRangeStartUntilEndDeletesRange()
    {
        ClickAtAngle(0);
        DragStartHandle(0, new Vector2(200f, 60f));

        Assert.AreEqual(0, GetRangeCount());
    }

    [Test]
    public void DecreaseRangeEndToStartDeletesRange()
    {
        ClickAtAngle(0);
        DragEndHandle(0, new Vector2(20f, 40f));

        Assert.AreEqual(0, GetRangeCount());
    }

    [Test]
    public void IncreaseRangeEndClampToStart()
    {
        ClickAtAngle(0);
        DragEndHandle(0, new Vector2[] {
            new Vector2(190f, 190f),
            new Vector2(20f, 190f),
            new Vector2(50f, 35f)
        });

        Assert.AreEqual(GetRangeStart(0) + 360f, GetRangeEnd(0));
    }

    [Test]
    public void DecreaseRangeStartClampToEnd()
    {
        ClickAtAngle(0);
        DragStartHandle(0, new Vector2[] {
            new Vector2(20f, 190f),
            new Vector2(190f, 190f),
            new Vector2(170f, 20f)
        });

        Assert.AreEqual(GetRangeEnd(0) - 360f, GetRangeStart(0));
    }

    [Test]
    public void RangeStartClampToPreviousRangeEnd()
    {
        ClickAtAngle(0);
        ClickAtAngle(120);
        DragStartHandle(1, new Vector2[] {
            new Vector2(190f, 10f),
            new Vector2(10f, 10f)
        });

        Assert.AreEqual(GetRangeEnd(0), GetRangeStart(1));
    }

    [Test]
    public void RangeEndClampToNextRangeStart()
    {
        ClickAtAngle(0);
        ClickAtAngle(120);
        ClickAtAngle(0);
        DragEndHandle(0, new Vector2[] {
            new Vector2(190f, 190f),
            new Vector2(10f, 190f)
        });

        Assert.AreEqual(GetRangeStart(1), GetRangeEnd(0));
    }

    [Test]
    public void RangeEndGraterThan180ClampToOtherRangeStartGreaterThanMinus180()
    {
        ClickAtAngle(0);
        ClickAtAngle(-120);
        ClickAtAngle(0);
        DragEndHandle(0, new Vector2[] {
            new Vector2(190f, 190f),
            new Vector2(10f, 190f)
        });

        Assert.AreEqual(GetRangeStart(1) + 360f, GetRangeEnd(0));
    }

    [Test]
    public void RangeStartLessThanMinus180ClampToOtherRangeEndGreaterThanMinus180()
    {
        ClickAtAngle(0);
        ClickAtAngle(-120);
        ClickAtAngle(-120);
        DragStartHandle(1, new Vector2[] {
            new Vector2(190f, 190f),
            new Vector2(190f, 10f),
            new Vector2(10f, 10f)
        });

        Assert.AreEqual(GetRangeEnd(0) - 360f, GetRangeStart(1));
    }

    [Test, TestCaseSource("DeleteKeyCases")]
    public void RemoveRangeKey(KeyCode key)
    {
        ClickAtAngle(0);
        Key(key);

        Assert.AreEqual(0, GetRangeCount());
        Assert.AreEqual(-1, m_Window.selectedRange);
    }
}
