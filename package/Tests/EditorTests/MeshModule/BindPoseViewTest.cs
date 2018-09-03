using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using UnityEditor;
using NSubstitute;
using NUnit.Framework.Constraints;
using UnityEditor.Experimental.U2D.Animation.Test.MeshModule.Base;

namespace UnityEditor.Experimental.U2D.Animation.Test.MeshModule.BindPose
{
    [TestFixture]
    public class BindPoseViewTest : BaseViewTest
    {
        public class BaseTestCase
        {
            public string name;
            public bool expected;
            public int hoveredBone = -1;
            public int hoveredBoneNode = -1;
            public int selectedBone = -1;
            public int forceNearestControl = -1;
            public EventType eventType = EventType.Layout;
            public GUIWrapperState state;

            public override string ToString()
            {
                return name;
            }
        }

        public class HoverTestCase : BaseTestCase
        {
            public int expectedHoveredBone;

            public override string ToString()
            {
                return name + "_Bone_" + expectedHoveredBone + "_IsHovered";
            }
        }

        public class ActionTestCase : BaseTestCase
        {
            public override string ToString()
            {
                return name + (expected ? "_Activates" : "_DoesNotActivate");
            }
        }

        public class SelectTestCase : ActionTestCase
        {
            public bool expectedAdditive;

            public override string ToString()
            {
                string nameOverride = base.ToString();

                if (expected)
                    nameOverride += (expectedAdditive ? "_Additive" : "_NotAdditive");

                return nameOverride;
            }
        }

        public class BindPoseActionTestCase : ActionTestCase
        {
            public BindPoseAction action;
            public bool eventOutsideWindow = false;
        }

        private const int kDefaultControlID = 2000;

        private BindPoseView m_BindPoseView;
        private ISelection m_Selection;

        protected override int GetDefaultControlID()
        {
            return kDefaultControlID;
        }

        [SetUp]
        public void Setup()
        {
            m_Selection = Substitute.For<ISelection>();
            m_BindPoseView = new BindPoseView(guiWrapper);
            m_BindPoseView.selection = m_Selection;
            m_BindPoseView.defaultControlID = kDefaultControlID;
            m_BindPoseView.SetupLayout();
        }

        private void SetupBaseTestCase(BaseTestCase testCase)
        {
            eventType = testCase.eventType;

            if (testCase.hoveredBone >= 0)
                m_BindPoseView.LayoutBone(mousePosition - Vector2.one * 1000f, mousePosition + Vector2.one * 1000f, testCase.hoveredBone);

            if (testCase.hoveredBoneNode >= 0)
                m_BindPoseView.LayoutBone(mousePosition, mousePosition + Vector2.one, testCase.hoveredBoneNode);

            if (testCase.forceNearestControl >= 0)
                nearestControl = testCase.forceNearestControl;

            SetGUIWrapperState(testCase.state);

            if (testCase.selectedBone >= 0)
            {
                m_Selection.Count.Returns(1);
                m_Selection.single.Returns(testCase.selectedBone);
                m_Selection.IsSelected(testCase.selectedBone).Returns(true);
            }
        }

        private void SetupBindPoseActionTestCase(BindPoseActionTestCase testCase)
        {
            guiWrapper.IsControlHot(Arg.Any<int>()).Returns(true);
            guiWrapper.IsEventOutsideWindow().Returns(testCase.eventOutsideWindow);
        }

        private static IEnumerable<HoverTestCase> LayoutCases()
        {
            yield return new HoverTestCase() { name = "LayoutBone_1", expectedHoveredBone = 1, hoveredBone = 1, state = new GUIWrapperState()};
            yield return new HoverTestCase() { name = "LayoutBoneNode_1", expectedHoveredBone = 1, hoveredBoneNode = 1, state = new GUIWrapperState()};
            yield return new HoverTestCase() { name = "LayoutBone_2_LayoutBoneNode_1", expectedHoveredBone = 1, hoveredBone = 2, hoveredBoneNode = 1, state = new GUIWrapperState()};
        }

        [Test]
        public void Layout([ValueSource("LayoutCases")] HoverTestCase testCase)
        {
            SetupBaseTestCase(testCase);
            Assert.AreEqual(testCase.expectedHoveredBone, m_BindPoseView.hoveredBone, "Bone should be hovered");
        }

        private static IEnumerable<ActionTestCase> RotateBoneCases()
        {
            yield return new ActionTestCase() { name = "HoveredBone_Click", expected = true, hoveredBone = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { name = "HoveredBoneNode_Click", expected = false, hoveredBoneNode = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { name = "HoveredBone_Click_AltIsDown", expected = false, hoveredBone = 100, state = new GUIWrapperState() { mouseDownButton = 0, isAltDown = true }};
            yield return new ActionTestCase() { name = "HoveredBone_Click_HotControlSet", expected = false, hoveredBone = 100, state = new GUIWrapperState() { mouseDownButton = 0, hotControl = 30 }};
        }

        [Test]
        public void RotateBone([ValueSource("RotateBoneCases")] ActionTestCase testCase)
        {
            SetupBaseTestCase(testCase);

            Vector2 lookAtPosition;
            Assert.AreEqual(testCase.expected, m_BindPoseView.DoRotateBone(out lookAtPosition));
        }

        private static IEnumerable<ActionTestCase> MoveBoneCases()
        {
            yield return new ActionTestCase() { name = "HoveredBoneNode_Click", expected = true, hoveredBoneNode = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { name = "HoveredBone_Click", expected = false, hoveredBone = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { name = "HoveredBoneNode_Click_AltIsDown", expected = false, hoveredBoneNode = 100, state = new GUIWrapperState() { mouseDownButton = 0, isAltDown = true }};
            yield return new ActionTestCase() { name = "HoveredBoneNode_Click_HotControlSet", expected = false, hoveredBoneNode = 100, state = new GUIWrapperState() { mouseDownButton = 0, hotControl = 30 }};
        }

        [Test]
        public void MoveBone([ValueSource("MoveBoneCases")] ActionTestCase testCase)
        {
            SetupBaseTestCase(testCase);

            Vector2 worldPosition;
            Assert.AreEqual(testCase.expected, m_BindPoseView.DoMoveBone(out worldPosition));
        }

        private static IEnumerable<ActionTestCase> SelectBoneCases()
        {
            yield return new ActionTestCase() { name = "HoveredBone_Click", expected = true, hoveredBone = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { name = "SelectedBone_SameHoveredBone_Click", expected = false, selectedBone = 100, hoveredBone = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { name = "HoveredBone_Click_AltIsDown", expected = false, hoveredBone = 100, state = new GUIWrapperState() { mouseDownButton = 0, isAltDown = true }};
            yield return new ActionTestCase() { name = "HoveredBone_Click_HotControlSet", expected = false, hoveredBone = 100, state = new GUIWrapperState() { mouseDownButton = 0, hotControl = 30 }};
        }

        [Test]
        public void SelectBone([ValueSource("SelectBoneCases")] ActionTestCase testCase)
        {
            SetupBaseTestCase(testCase);

            Assert.AreEqual(testCase.expected, m_BindPoseView.DoSelectBone());

            if (testCase.expected)
                guiWrapper.Received(1).Repaint();
            else
                guiWrapper.DidNotReceive().Repaint();
        }

        private static IEnumerable<BindPoseActionTestCase> ActionTriggeringCases()
        {
            yield return new BindPoseActionTestCase() { name = "RotateBone_HoveredBone_MouseDown", expected = true, action = BindPoseAction.RotateBone, hoveredBone = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new BindPoseActionTestCase() { name = "RotateBone_HoveredBoneNode_MouseDown", expected = false, action = BindPoseAction.RotateBone, hoveredBoneNode = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new BindPoseActionTestCase() { name = "MoveBone_HoveredBoneNode_MouseDown", expected = true, action = BindPoseAction.MoveBone, hoveredBoneNode = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new BindPoseActionTestCase() { name = "MoveBone_HoveredBone_MouseDown", expected = false, action = BindPoseAction.MoveBone, hoveredBone = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
        }

        [Test]
        public void ActionTriggering([ValueSource("ActionTriggeringCases")] BindPoseActionTestCase testCase)
        {
            SetupBaseTestCase(testCase);
            SetupBindPoseActionTestCase(testCase);

            Assert.AreEqual(testCase.expected, m_BindPoseView.IsActionTriggering(testCase.action));
        }

        private static IEnumerable<BindPoseActionTestCase> ActionFinishingCases()
        {
            yield return new BindPoseActionTestCase() { name = "RotateBone_MouseUp", expected = true, action = BindPoseAction.RotateBone, state = new GUIWrapperState() { mouseUpButton = 0 }};
            yield return new BindPoseActionTestCase() { name = "RotateBone_MouseUp_EventOutsideWindow", expected = true, action = BindPoseAction.RotateBone, eventOutsideWindow = true, state = new GUIWrapperState()};
            yield return new BindPoseActionTestCase() { name = "MoveBone_MouseUp", expected = true, action = BindPoseAction.MoveBone, state = new GUIWrapperState() { mouseUpButton = 0 }};
            yield return new BindPoseActionTestCase() { name = "MoveBone_MouseUp_EventOutsideWindow", expected = true, action = BindPoseAction.MoveBone, eventOutsideWindow = true, state = new GUIWrapperState()};
        }

        [Test]
        public void ActionFinishing([ValueSource("ActionFinishingCases")] BindPoseActionTestCase testCase)
        {
            SetupBaseTestCase(testCase);
            SetupBindPoseActionTestCase(testCase);

            Assert.AreEqual(testCase.expected, m_BindPoseView.IsActionFinishing(testCase.action));
        }
    }
}
