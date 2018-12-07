using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using UnityEditor;
using NSubstitute;
using NUnit.Framework.Constraints;

namespace UnityEditor.Experimental.U2D.Animation.Test
{
    public class GUIWrapperState
    {
        public int mouseButton = -1;
        public int mouseDownButton = -1;
        public int mouseUpButton = -1;
        public int clickCount = 0;
        public bool isShiftDown = false;
        public bool isAltDown = false;
        public bool isActionKeyDown = false;
        public KeyCode keyDown = KeyCode.None;
        public int hotControl = 0;
        public int multiStepHotControl = 0;
        public bool viewToolActive = false;
    }

    public class BaseViewTest
    {
        private const float kPickDistance = 5f;

        public int nearestControl { get { return m_NearestDistance <= kPickDistance ? m_NearestControl : 0; } set { m_NearestControl = value; } }
        public IGUIWrapper guiWrapper { get { return m_GUIWrapper; } }
        public Vector2 mousePosition { get { return m_MousePosition; }  set { m_MousePosition = value; } }
        public EventType eventType { get { return m_EventType; } set { m_EventType = value; } }
        private int m_HotControl = 0;

        private IGUIWrapper m_GUIWrapper;
        private float m_NearestDistance;
        private int m_NearestControl;
        private int m_CurrentControlID;
        private Vector2 m_MousePosition;
        private EventType m_EventType = EventType.Layout;

        [SetUp]
        public void SetupBase()
        {
            m_EventType = EventType.Layout;
            m_NearestDistance = kPickDistance;
            m_NearestControl = GetDefaultControlID();
            m_CurrentControlID = 0;
            m_MousePosition = Vector2.zero;
            m_HotControl = 0;

            m_GUIWrapper = Substitute.For<IGUIWrapper>();
            m_GUIWrapper.GetControlID(Arg.Any<int>(), Arg.Any<FocusType>()).Returns(x => GetControlID((int)x[0], (FocusType)x[1]));
            m_GUIWrapper.mousePosition.Returns(x => m_MousePosition);
            m_GUIWrapper.eventType.Returns(x => m_EventType);
            m_GUIWrapper.GUIToWorld(Arg.Any<Vector2>()).Returns(x => (Vector3)(Vector2)x[0]);
            m_GUIWrapper.GUIToWorld(Arg.Any<Vector2>(), Arg.Any<Vector3>(), Arg.Any<Vector3>()).Returns(x => (Vector3)(Vector2)x[0]);
            m_GUIWrapper.IsControlNearest(Arg.Any<int>()).Returns(x => (int)x[0] == nearestControl);
            m_GUIWrapper.IsControlHot(Arg.Any<int>()).Returns(x => (int)x[0] == m_HotControl);
            m_GUIWrapper.When(x => m_GUIWrapper.LayoutControl(Arg.Any<int>(), Arg.Any<float>())).Do(x =>
                {
                    if (m_EventType != EventType.Layout)
                        return;

                    int controlId = (int)x[0];
                    float distance = (float)x[1];

                    if (distance <= m_NearestDistance)
                    {
                        m_NearestDistance = distance;
                        m_NearestControl = controlId;
                    }
                });
            m_GUIWrapper.DistanceToCircle(Arg.Any<Vector3>(), Arg.Any<float>()).Returns(x =>
                {
                    Vector2 center = (Vector3)x[0];
                    float radius = (float)x[1];

                    float dist = (center - m_MousePosition).magnitude;
                    if (dist < radius)
                        return 0f;
                    return dist - radius;
                });
            m_GUIWrapper.DistanceToSegment(Arg.Any<Vector3>(), Arg.Any<Vector3>()).Returns(x => HandleUtility.DistancePointToLineSegment(m_MousePosition, (Vector3)x[0], (Vector3)x[1]));
            m_GUIWrapper.DistanceToSegmentClamp(Arg.Any<Vector3>(), Arg.Any<Vector3>()).Returns(x => MathUtility.DistanceToSegmentClamp(m_MousePosition, (Vector3)x[0], (Vector3)x[1]));

            Vector3 sliderPos;
            m_GUIWrapper.DoSlider(Arg.Any<int>(), Arg.Any<SliderData>(), out sliderPos).ReturnsForAnyArgs(x => (int)x[0] == nearestControl);
            m_GUIWrapper.GetHandleSize(Arg.Any<Vector3>()).ReturnsForAnyArgs(x => 1f);
        }

        protected void SetGUIWrapperState(GUIWrapperState state)
        {
            int clickCount = 0;

            if (state.mouseDownButton >= 0)
                clickCount = Mathf.Max(1, state.clickCount);

            m_GUIWrapper.mouseButton.Returns(state.mouseButton);
            m_GUIWrapper.clickCount.Returns(clickCount);
            m_GUIWrapper.isShiftDown.Returns(state.isShiftDown);
            m_GUIWrapper.isAltDown.Returns(state.isAltDown);
            m_GUIWrapper.isActionKeyDown.Returns(state.isActionKeyDown);
            m_GUIWrapper.IsMouseDown(state.mouseDownButton).Returns(true);
            m_GUIWrapper.IsMouseUp(state.mouseUpButton).Returns(true);

            if (state.keyDown != KeyCode.None)
                m_GUIWrapper.IsKeyDown(state.keyDown).Returns(true);

            m_HotControl = state.hotControl;
            m_GUIWrapper.IsMultiStepControlHot(state.multiStepHotControl).Returns(true);
            m_GUIWrapper.IsViewToolActive().Returns(x => state.viewToolActive);
        }

        protected virtual int GetControlID(int hashCode, FocusType focusType)
        {
            return ++m_CurrentControlID;
        }

        protected virtual int GetDefaultControlID() { return 0; }
    }
}
