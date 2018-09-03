using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.U2D.Interface;
using UnityEditor.U2D.Interface;
using UnityEvent = UnityEngine.Event;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class ShapeEditorOpenEndedTests
{
    private ShapeEditorWindow m_Window;

    public class ShapeEditorOpenEndedWindow : ShapeEditorWindow
    {
        // This Menuitem is for debugging purposes
        //[MenuItem("Window/ShapeEditorOpenEndedWindow")]
        static void InitShapeEditorOpenEndedWindow()
        {
            ShapeEditorOpenEndedWindow window = (ShapeEditorOpenEndedWindow)EditorWindow.GetWindow(typeof(ShapeEditorOpenEndedWindow));
            window.Show();
        }

        public override void OnEnable()
        {
            base.OnEnable();
            openEnded = true;
        }
    }

    [SetUp]
    public void Setup()
    {
        m_Window = ShapeEditorOpenEndedWindow.CreateInstance<ShapeEditorOpenEndedWindow>();
        m_Window.Show(true);
    }

    [TearDown]
    public void Teardown()
    {
        m_Window.Close();
        Object.DestroyImmediate(m_Window);
    }

    [Test]
    public void ShapeEditorInsertPoint()
    {
        //Act
        //Insert point by clicking right edge
        m_Window.ClickWindow(m_Window.GetEdgeWindowPosition(1));

        //Assert
        Assert.AreEqual(m_Window.m_Points.Count, 5);
    }

    [Test]
    public void ShapeEditorMoveLeftTangent()
    {
        const int kPointIndex = 2;
        Vector3 oldTangent = m_Window.m_Points[2].m_LeftTangent;

        //Act
        //Click on point to select it
        m_Window.ClickWindow(m_Window.GetPointWindowPosition(kPointIndex));
        //Drag its left tangent to move it
        m_Window.DragInWindow(
            m_Window.GetLeftTangentWindowPosition(kPointIndex),
            m_Window.GetPointWindowPosition(kPointIndex) + Vector2.one
            );

        //Assert
        Assert.AreNotEqual(m_Window.m_Points[kPointIndex].m_LeftTangent, oldTangent);
    }

    [Test]
    public void ShapeEditorMovePoint()
    {
        //Act
        const int kPointIndex = 2;
        Vector3 oldPoint = m_Window.m_Points[kPointIndex].m_Position;

        //Click on point to select it
        m_Window.ClickWindow(m_Window.GetPointWindowPosition(kPointIndex));
        //Drag it to move it
        m_Window.DragInWindow(
            m_Window.GetPointWindowPosition(kPointIndex),
            m_Window.GetPointWindowPosition(kPointIndex) + Vector2.one
            );

        //Assert
        Assert.AreNotEqual(m_Window.m_Points[kPointIndex].m_Position, oldPoint);
    }

    [Test]
    public void ShapeEditorMoveRightTangent()
    {
        //Act
        const int kPointIndex = 2;
        Vector3 oldTangent = m_Window.m_Points[kPointIndex].m_RightTangent;

        //Click on point to select it
        m_Window.ClickWindow(m_Window.GetPointWindowPosition(kPointIndex));
        //Drag its right tangent to move it
        m_Window.DragInWindow(
            m_Window.GetRightTangentWindowPosition(kPointIndex),
            m_Window.GetPointWindowPosition(kPointIndex) + Vector2.one
            );

        //Assert
        Assert.AreNotEqual(m_Window.m_Points[kPointIndex].m_RightTangent, oldTangent);
    }

    [Test]
    public void ShapeEditorRemovePoint()
    {
        //Act
        //Click on point to select it
        m_Window.ClickWindow(m_Window.GetPointWindowPosition(1));

        //Send delete event to remove selected point
        var ev = new UnityEvent();
        ev.type = EventType.ValidateCommand;
        ev.command = true;
        ev.commandName = "SoftDelete";
        m_Window.SendEvent(ev);
        ev.type = EventType.ExecuteCommand;
        ev.command = true;
        ev.commandName = "SoftDelete";
        m_Window.SendEvent(ev);

        //Assert
        Assert.AreEqual(m_Window.m_Points.Count, 3);
    }
}
