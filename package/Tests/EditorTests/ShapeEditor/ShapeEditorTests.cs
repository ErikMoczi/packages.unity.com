using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Interface;
using UnityEditor.U2D.Interface;
using UnityEvent = UnityEngine.Event;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class ShapeEditorTests
{
    private ShapeEditorWindow m_Window;

    [SetUp]
    public void Setup()
    {
        m_Window = ShapeEditorWindow.CreateInstance<ShapeEditorWindow>();
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

    [Test]
    public void ShapeEditorDragEdge()
    {
        //Act
        //Insert point by clicking right edge
        Vector3 p1 = m_Window.m_Points[1].m_Position;
        Vector3 p2 = m_Window.m_Points[2].m_Position;

        m_Window.DragInWindow(
            m_Window.GetEdgeWindowPosition(1),
            m_Window.GetEdgeWindowPosition(1) + Vector2.down * 10,
            false, true, false);

        Vector3 deltaP1 = m_Window.m_Points[1].m_Position - p1;
        Vector3 deltaP2 = m_Window.m_Points[2].m_Position - p2;
        //Assert
        Assert.IsTrue(deltaP1 == deltaP2 && deltaP1.sqrMagnitude > 0f);
    }
}
