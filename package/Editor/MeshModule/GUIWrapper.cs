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
        void UseCurrentEvent();
        float DistanceToSegment(Vector2 p1, Vector2 p2);
        float DistanceToCircle(Vector2 center, float radius);
        Vector2 GUIToWorld(Vector2 guiPosition);
        void Repaint();
        bool IsRepainting();
        bool IsEventOutsideWindow();
        void SetGuiChanged(bool changed);
    }

    internal class GUIWrapper : IGUIWrapper
    {
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

        public bool DoSlider(int id, Vector2 position, out Vector2 newPosition)
        {
            EditorGUI.BeginChangeCheck();

            newPosition = Slider2D.Do(id, position, null);

            return EditorGUI.EndChangeCheck();
        }

        public void UseCurrentEvent()
        {
            Event.current.Use();
        }

        public float DistanceToSegment(Vector2 p1, Vector2 p2)
        {
            return MeshModuleUtility.DistanceToSegment(p1, p2);
        }

        public float DistanceToCircle(Vector2 center, float radius)
        {
            return HandleUtility.DistanceToCircle(center, radius);
        }

        public Vector2 GUIToWorld(Vector2 guiPosition)
        {
            return MeshModuleUtility.GUIToWorld(guiPosition);
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
    }
}
