using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.U2D.Interface;
using UnityEngine.U2D.Interface;
using System.Collections.Generic;

namespace UnityEditor.Experimental.U2D.Animation
{
    public interface IGUIWrapper
    {
        Vector2 mousePosition { get; }
        int mouseButton { get; }
        int clickCount { get; }
        bool isShiftDown { get; }
        bool isAltDown { get; }
        bool isActionKeyDown { get; }
        EventType eventType { get; }
        string commandName { get; }
        bool IsMouseDown(int button);
        bool IsMouseUp(int button);
        bool IsKeyDown(KeyCode keyCode);
        int GetControlID(int hint, FocusType focusType);
        void LayoutControl(int controlID, float distance);
        bool IsControlNearest(int controlID);
        bool IsControlHot(int controlID);
        bool DoSlider(int id, Vector2 position, out Vector2 newPosition);
        bool DoSlider(int id, Vector3 position, Vector3 forward, Vector3 up, Vector3 right, out Vector3 newPosition);
        void UseCurrentEvent();
        float DistanceToSegment(Vector3 p1, Vector3 p2);
        float DistanceToCircle(Vector3 center, float radius);
        Vector2 GUIToWorld(Vector2 guiPosition);
        Vector3 GUIToWorld(Vector2 guiPosition, Vector3 planeNormal, Vector3 planePosition);
        void Repaint();
        bool IsRepainting();
        bool IsEventOutsideWindow();
        void SetGuiChanged(bool changed);
        float GetHandleSize(Vector3 position);
        bool IsViewToolActive();
    }

    internal class GUIWrapper : IGUIWrapper
    {
        private Handles.CapFunction nullCap = (int c, Vector3 p , Quaternion r, float s, EventType ev) => {};

        public Vector2 mousePosition
        {
            get { return Event.current.mousePosition; }
        }

        public int mouseButton
        {
            get { return Event.current.button; }
        }

        public int clickCount
        {
            get { return Event.current.clickCount; }
        }

        public bool isShiftDown
        {
            get { return Event.current.shift; }
        }

        public bool isAltDown
        {
            get { return Event.current.alt; }
        }

        public bool isActionKeyDown
        {
            get { return EditorGUI.actionKey; }
        }

        public EventType eventType
        {
            get { return Event.current.type; }
        }

        public string commandName
        {
            get { return Event.current.commandName; }
        }

        public bool IsMouseDown(int button)
        {
            return Event.current.type == EventType.MouseDown && Event.current.button == button;
        }

        public bool IsMouseUp(int button)
        {
            return Event.current.type == EventType.MouseUp && Event.current.button == button;
        }

        public bool IsKeyDown(KeyCode keyCode)
        {
            return Event.current.type == EventType.KeyDown && Event.current.keyCode == keyCode;
        }

        public int GetControlID(int hint, FocusType focusType)
        {
            return GUIUtility.GetControlID(hint, focusType);
        }

        public void LayoutControl(int controlID, float distance)
        {
            if (Event.current.type == EventType.Layout)
                HandleUtility.AddControl(controlID, distance);
        }

        public bool IsControlNearest(int controlID)
        {
            return HandleUtility.nearestControl == controlID;
        }

        public bool IsControlHot(int controlID)
        {
            return GUIUtility.hotControl == controlID;
        }

        //Slider for EditorWindows
        public bool DoSlider(int id, Vector2 position, out Vector2 newPosition)
        {
            EditorGUI.BeginChangeCheck();
            newPosition = Slider2D.Do(id, position, null);
            return EditorGUI.EndChangeCheck();
        }

        //Slider for SceneView
        public bool DoSlider(int id, Vector3 position, Vector3 forward, Vector3 up, Vector3 right, out Vector3 newPosition)
        {
            newPosition = position;

            EditorGUI.BeginChangeCheck();

            newPosition = Handles.Slider2D(id, position, forward, up, right, 1f, nullCap, Vector2.zero);

            return EditorGUI.EndChangeCheck();
        }

        public void UseCurrentEvent()
        {
            Event.current.Use();
        }

        public float DistanceToSegment(Vector3 p1, Vector3 p2)
        {
            p1 = HandleUtility.WorldToGUIPoint(p1);
            p2 = HandleUtility.WorldToGUIPoint(p2);

            var point = Event.current.mousePosition;

            var distance = HandleUtility.DistancePointToLineSegment(point, p1, p2);
            if (distance < 0)
                distance = 0f;

            return distance;
        }

        public float DistanceToCircle(Vector3 center, float radius)
        {
            return HandleUtility.DistanceToCircle(center, radius);
        }

        public Vector2 GUIToWorld(Vector2 guiPosition)
        {
            return MeshModuleUtility.GUIToWorld(guiPosition);
        }

        public Vector3 GUIToWorld(Vector2 guiPosition, Vector3 planeNormal, Vector3 planePosition)
        {
            return MeshModuleUtility.GUIToWorld(guiPosition, planeNormal, planePosition);
        }

        public void Repaint()
        {
            HandleUtility.Repaint();
        }

        public bool IsRepainting()
        {
            return eventType == EventType.Repaint;
        }

        public void SetGuiChanged(bool changed)
        {
            GUI.changed = true;
        }

        public bool IsEventOutsideWindow()
        {
            return Event.current.type == EventType.Ignore;
        }

        public float GetHandleSize(Vector3 position)
        {
            return HandleUtility.GetHandleSize(position);
        }

        public bool IsViewToolActive()
        {
            return Tools.current == Tool.View || isAltDown || mouseButton == 1 || mouseButton == 2;
        }
    }
}
