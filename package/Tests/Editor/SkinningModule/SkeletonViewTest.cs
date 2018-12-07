using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using UnityEditor;
using NSubstitute;
using NUnit.Framework.Constraints;

namespace UnityEditor.Experimental.U2D.Animation.Test.Skeleton
{
    [TestFixture]
    public class SkeletonViewTest : BaseViewTest
    {
        const int kInvalidID = -1;
        const int kCreateBoneControlID = 4;

        public class BaseTestCase
        {
            public string name;
            public SkeletonMode mode = SkeletonMode.Disabled;
            public bool expected;
            public int hoveredBody = -1;
            public int hoveredJoint = -1;
            public int hoveredTail = -1;
            public int forceNearestControl = -1;
            public EventType eventType = EventType.Layout;
            public GUIWrapperState state = new GUIWrapperState();

            public override string ToString()
            {
                var n = name;

                if (!string.IsNullOrEmpty(n))
                    n += "_";
                
                n += "Mode:" + mode ;

                if (forceNearestControl > -1)
                    n += "_NearestControl:" + forceNearestControl;
                if (hoveredBody > -1)
                    n += "_HoveredBody:" + hoveredBody;
                if (hoveredJoint > -1)
                    n += "_HoveredJoint:" + hoveredJoint;
                if (hoveredTail > -1)
                    n += "_HoveredTail:" + hoveredTail;
                if (state.hotControl > 0)
                    n += "_HotControl:" + state.hotControl;
                if (state.multiStepHotControl > 0)
                    n += "_MultiStepHotControl:" + state.multiStepHotControl;
                if (state.mouseDownButton > -1)
                    n += "_MouseButtonDown:" + state.mouseDownButton;
                if (state.mouseUpButton > -1)
                    n += "_MouseButtonUp:" + state.mouseUpButton;
                if (state.mouseButton > -1)
                    n += "_MouseButton:" + state.mouseButton;
                if (state.isShiftDown)
                    n += "_ShiftDown";
                if (state.isAltDown)
                    n += "_AltDown";
                if (state.isActionKeyDown)
                    n += "_ActionKeyDown";

                return n;
            }
        }

        public class HoverTestCase : BaseTestCase
        {
            public int expectedHoveredBone;

            public override string ToString()
            {
                return base.ToString() + "_HoveredBone:" + expectedHoveredBone;
            }
        }

        public class ActionTestCase : BaseTestCase
        {
            public override string ToString()
            {
                return base.ToString() + (expected ? "_Activates" : "_DoesNotActivate");
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

        public enum CommandEventType
        {
            Other = (int)EventType.Layout,
            Validate = (int)EventType.ValidateCommand,
            Execute = (int)EventType.ExecuteCommand
        }

        public class CommandTestCase : ActionTestCase
        {
            public CommandEventType commandEventType = CommandEventType.Other;
            public string commandName;

            public override string ToString()
            {
                var n = base.ToString() + "_CommandType:" + commandEventType;

                if (!string.IsNullOrEmpty(commandName))
                    n += "_CommandName:" + commandName;

                return n;
            }
        }

        public class SkeletonActionTestCase : ActionTestCase
        {
            public SkeletonAction action;
            public bool eventOutsideWindow = false;

            public override string ToString()
            {
                return "Action:" + action + "_" + base.ToString();
            }
        }

        private const int kDefaultControlID = 2000;

        private SkeletonView m_SkeletonView;

        protected override int GetDefaultControlID()
        {
            return kDefaultControlID;
        }

        [SetUp]
        public void Setup()
        {
            m_SkeletonView = new SkeletonView(guiWrapper);
            m_SkeletonView.InvalidID = kInvalidID;
            m_SkeletonView.defaultControlID = kDefaultControlID;
            m_SkeletonView.BeginLayout();
        }

        private void SetupBaseTestCase(BaseTestCase testCase)
        {
            m_SkeletonView.mode = testCase.mode;

            eventType = testCase.eventType;

            if (testCase.hoveredBody >= 0)
                m_SkeletonView.LayoutBone(testCase.hoveredBody, mousePosition - Vector2.one * 1000f, mousePosition + Vector2.one * 1000f, Vector3.forward, Vector3.up, Vector3.right,  false);

            if (testCase.hoveredJoint >= 0)
                m_SkeletonView.LayoutBone(testCase.hoveredJoint, mousePosition, mousePosition + Vector2.one * 1000f, Vector3.forward, Vector3.up, Vector3.right, false);

            if (testCase.hoveredTail >= 0)
                m_SkeletonView.LayoutBone(testCase.hoveredTail, mousePosition - Vector2.up * 1000f, mousePosition, Vector3.forward, Vector3.up, Vector3.right, true);

            if (testCase.forceNearestControl >= 0)
                nearestControl = testCase.forceNearestControl;

            SetGUIWrapperState(testCase.state);

            m_SkeletonView.EndLayout();
        }

        private void SetupSkeletonActionTestCase(SkeletonActionTestCase testCase)
        {
            guiWrapper.IsControlHot(Arg.Any<int>()).Returns(true);
            guiWrapper.IsEventOutsideWindow().Returns(testCase.eventOutsideWindow);
        }

        private void SetupCommandTestCase(CommandTestCase commandTestCase)
        {
            eventType = (EventType)commandTestCase.commandEventType;
            guiWrapper.commandName.Returns(commandTestCase.commandName);
        }

        private static IEnumerable<HoverTestCase> LayoutCases()
        {
            yield return new HoverTestCase() { mode = SkeletonMode.Disabled, expectedHoveredBone = kInvalidID, hoveredBody = 1};
            yield return new HoverTestCase() { mode = SkeletonMode.EditPose, expectedHoveredBone = 1, hoveredBody = 1};
            yield return new HoverTestCase() { mode = SkeletonMode.EditPose, expectedHoveredBone = 1, hoveredJoint = 1};
            yield return new HoverTestCase() { mode = SkeletonMode.EditPose, expectedHoveredBone = 2, hoveredBody = 1, hoveredJoint = 2};
            yield return new HoverTestCase() { mode = SkeletonMode.EditPose, expectedHoveredBone = 1, hoveredJoint = 1, hoveredTail = 2};
            yield return new HoverTestCase() { mode = SkeletonMode.EditJoints, expectedHoveredBone = 2, hoveredJoint = 1, hoveredTail = 2};
        }

        [Test]
        public void Layout([ValueSource("LayoutCases")] HoverTestCase testCase)
        {
            SetupBaseTestCase(testCase);
            Assert.AreEqual(testCase.expectedHoveredBone, m_SkeletonView.hoveredBoneID, "Bone should be hovered");
        }

        private static IEnumerable<ActionTestCase> RotateBoneCases()
        {
            yield return new ActionTestCase() { mode = SkeletonMode.EditPose, expected = true, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditPose, expected = false, hoveredJoint = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditPose, expected = true, hoveredTail = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditPose, expected = false, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0, isAltDown = true }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditPose, expected = false, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0, hotControl = 30 }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditJoints, expected = false, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.CreateBone, expected = false, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.Selection, expected = false, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.SplitBone, expected = false, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
        }

        [Test]
        public void RotateBone([ValueSource("RotateBoneCases")] ActionTestCase testCase)
        {
            SetupBaseTestCase(testCase);

            float deltaAngle;
            Assert.AreEqual(testCase.expected, m_SkeletonView.DoRotateBone(Vector2.zero, Vector3.back, out deltaAngle));
        }

        private static IEnumerable<ActionTestCase> MoveBoneCases()
        {
            yield return new ActionTestCase() { mode = SkeletonMode.EditPose, expected = true, hoveredJoint = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditPose, expected = false, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditPose, expected = false, hoveredTail = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditPose, expected = false, hoveredJoint = 100, state = new GUIWrapperState() { mouseDownButton = 0, isAltDown = true }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditPose, expected = false, hoveredJoint = 100, state = new GUIWrapperState() { mouseDownButton = 0, hotControl = 30 }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditJoints, expected = false, hoveredJoint = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.CreateBone, expected = false, hoveredJoint = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.Selection, expected = false, hoveredJoint = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.SplitBone, expected = false, hoveredJoint = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
        }

        [Test]
        public void MoveBone([ValueSource("MoveBoneCases")] ActionTestCase testCase)
        {
            SetupBaseTestCase(testCase);

            Vector3 deltaPosition;
            Assert.AreEqual(testCase.expected, m_SkeletonView.DoMoveBone(out deltaPosition));
        }

        private static IEnumerable<ActionTestCase> FreeMoveBoneCases()
        {
            yield return new ActionTestCase() { mode = SkeletonMode.EditJoints, expected = true, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditJoints, expected = false, hoveredJoint = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditJoints, expected = false, hoveredTail = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditJoints, expected = false, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0, isAltDown = true }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditJoints, expected = false, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0, hotControl = 30 }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditPose, expected = false, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.CreateBone, expected = false, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.Selection, expected = false, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.SplitBone, expected = false, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
        }

        [Test]
        public void FreeMoveBone([ValueSource("FreeMoveBoneCases")] ActionTestCase testCase)
        {
            SetupBaseTestCase(testCase);

            Vector3 deltaPosition;
            Assert.AreEqual(testCase.expected, m_SkeletonView.DoFreeMoveBone(out deltaPosition));
        }

        private static IEnumerable<ActionTestCase> MoveJointCases()
        {
            yield return new ActionTestCase() { mode = SkeletonMode.EditJoints, expected = true, hoveredJoint = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditJoints, expected = false, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditJoints, expected = false, hoveredTail = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditJoints, expected = false, hoveredJoint = 100, state = new GUIWrapperState() { mouseDownButton = 0, isAltDown = true }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditPose, expected = false, hoveredJoint = 100, state = new GUIWrapperState() { mouseDownButton = 0, hotControl = 30 }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditPose, expected = false, hoveredJoint = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.CreateBone, expected = true, hoveredJoint = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.Selection, expected = false, hoveredJoint = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.SplitBone, expected = true, hoveredJoint = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
        }

        [Test]
        public void MoveJoint([ValueSource("MoveJointCases")] ActionTestCase testCase)
        {
            SetupBaseTestCase(testCase);

            Vector3 deltaPosition;
            Assert.AreEqual(testCase.expected, m_SkeletonView.DoMoveJoint(out deltaPosition));
        }

        private static IEnumerable<ActionTestCase> MoveEndPositionCases()
        {
            yield return new ActionTestCase() { mode = SkeletonMode.EditJoints, expected = true, hoveredTail = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditJoints, expected = false, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditJoints, expected = false, hoveredJoint = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditJoints, expected = false, hoveredTail = 100, state = new GUIWrapperState() { mouseDownButton = 0, isAltDown = true }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditPose, expected = false, hoveredJoint = 100, state = new GUIWrapperState() { mouseDownButton = 0, hotControl = 30 }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditPose, expected = false, hoveredTail = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.CreateBone, expected = false, hoveredTail = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.Selection, expected = false, hoveredTail = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.SplitBone, expected = true, hoveredTail = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
        }

        [Test]
        public void MoveEndPosition([ValueSource("MoveEndPositionCases")] ActionTestCase testCase)
        {
            SetupBaseTestCase(testCase);

            Vector3 deltaPosition;
            Assert.AreEqual(testCase.expected, m_SkeletonView.DoMoveEndPosition(out deltaPosition));
        }

        private static IEnumerable<SelectTestCase> SelectBoneCases()
        {
            yield return new SelectTestCase() { mode = SkeletonMode.EditPose, expected = true, expectedAdditive = false, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new SelectTestCase() { mode = SkeletonMode.EditPose, expected = true, expectedAdditive = true, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0, isActionKeyDown = true }};
            yield return new SelectTestCase() { mode = SkeletonMode.EditPose, expected = false, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0, isAltDown = true }};
            yield return new SelectTestCase() { mode = SkeletonMode.EditPose, expected = false, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0, hotControl = 30 }};
            yield return new SelectTestCase() { mode = SkeletonMode.Disabled, expected = false, expectedAdditive = false, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new SelectTestCase() { mode = SkeletonMode.EditJoints, expected = true, expectedAdditive = false, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new SelectTestCase() { mode = SkeletonMode.CreateBone, expected = true, expectedAdditive = false, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new SelectTestCase() { mode = SkeletonMode.Selection, expected = true, expectedAdditive = false, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new SelectTestCase() { mode = SkeletonMode.SplitBone, expected = false, expectedAdditive = false, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
        }

        [Test]
        public void SelectBone([ValueSource("SelectBoneCases")] SelectTestCase testCase)
        {
            SetupBaseTestCase(testCase);

            int selected;
            bool additive;
            Assert.AreEqual(testCase.expected, m_SkeletonView.DoSelectBone(out selected, out additive));

            if (testCase.expected)
            {
                Assert.AreEqual(testCase.hoveredBody, selected, "Selected bone does not match");
                Assert.AreEqual(testCase.expectedAdditive, additive, "Selection expected additive value does not match");
            }
        }

        private static IEnumerable<ActionTestCase> CreateBoneStartCases()
        {
            yield return new ActionTestCase() { mode = SkeletonMode.CreateBone, expected = true, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.CreateBone, expected = false, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.CreateBone, expected = true, hoveredTail = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.CreateBone, expected = false, state = new GUIWrapperState() { mouseDownButton = 0, isAltDown = true  }};
            yield return new ActionTestCase() { mode = SkeletonMode.CreateBone, expected = false, state = new GUIWrapperState() { mouseDownButton = 0, hotControl = 30 }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditPose, expected = false, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditJoints, expected = false, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.Selection, expected = false, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.SplitBone, expected = false, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.SplitBone, expected = false, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.Disabled, expected = false, state = new GUIWrapperState() { mouseDownButton = 0 }};
        }

        [Test]
        public void CreateBoneStart([ValueSource("CreateBoneStartCases")] ActionTestCase testCase)
        {
            SetupBaseTestCase(testCase);

            Vector3 position;
            Assert.AreEqual(testCase.expected, m_SkeletonView.DoCreateBoneStart(out position));
        }

        private static IEnumerable<ActionTestCase> CreateBoneCases()
        {
            yield return new ActionTestCase() { mode = SkeletonMode.CreateBone, expected = true, state = new GUIWrapperState() { mouseDownButton = 0, multiStepHotControl = kCreateBoneControlID }};
            yield return new ActionTestCase() { mode = SkeletonMode.CreateBone, expected = false, state = new GUIWrapperState() { mouseDownButton = 0, multiStepHotControl = kCreateBoneControlID, isAltDown = true }};
            yield return new ActionTestCase() { mode = SkeletonMode.CreateBone, expected = false, state = new GUIWrapperState() { mouseDownButton = 0, multiStepHotControl = 0 }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditPose, expected = false, state = new GUIWrapperState() { mouseDownButton = 0, multiStepHotControl = kCreateBoneControlID }};
            yield return new ActionTestCase() { mode = SkeletonMode.EditJoints, expected = false, state = new GUIWrapperState() { mouseDownButton = 0, multiStepHotControl = kCreateBoneControlID }};
            yield return new ActionTestCase() { mode = SkeletonMode.Selection, expected = false, state = new GUIWrapperState() { mouseDownButton = 0, multiStepHotControl = kCreateBoneControlID }};
            yield return new ActionTestCase() { mode = SkeletonMode.SplitBone, expected = false, state = new GUIWrapperState() { mouseDownButton = 0, multiStepHotControl = kCreateBoneControlID }};
            yield return new ActionTestCase() { mode = SkeletonMode.Disabled, expected = false, state = new GUIWrapperState() { mouseDownButton = 0, multiStepHotControl = kCreateBoneControlID }};
        }

        [Test]
        public void CreateBone([ValueSource("CreateBoneCases")] ActionTestCase testCase)
        {
            SetupBaseTestCase(testCase);

            Vector3 position;
            Assert.AreEqual(testCase.expected, m_SkeletonView.DoCreateBone(out position));
        }

        private static IEnumerable<CommandTestCase> RemoveBoneCases()
        {
            yield return new CommandTestCase() { expected = true, mode = SkeletonMode.EditJoints, commandEventType = CommandEventType.Validate, commandName = SkeletonView.kDeleteCommandName };
            yield return new CommandTestCase() { expected = true, mode = SkeletonMode.EditJoints, commandEventType = CommandEventType.Execute, commandName = SkeletonView.kDeleteCommandName };
            yield return new CommandTestCase() { expected = false, mode = SkeletonMode.EditJoints, commandEventType = CommandEventType.Other };
            yield return new CommandTestCase() { expected = false, mode = SkeletonMode.EditJoints, commandEventType = CommandEventType.Validate, commandName = SkeletonView.kDeleteCommandName, state = new GUIWrapperState() { isAltDown = true } };
            yield return new CommandTestCase() { expected = false, mode = SkeletonMode.EditJoints, commandEventType = CommandEventType.Execute, commandName = SkeletonView.kDeleteCommandName, state = new GUIWrapperState() { isAltDown = true } };
            yield return new CommandTestCase() { expected = false, mode = SkeletonMode.EditJoints, commandEventType = CommandEventType.Validate, commandName = SkeletonView.kDeleteCommandName, state = new GUIWrapperState() { hotControl = 30 } };
            yield return new CommandTestCase() { expected = false, mode = SkeletonMode.EditJoints, commandEventType = CommandEventType.Execute, commandName = SkeletonView.kDeleteCommandName, state = new GUIWrapperState() { hotControl = 30 } };
            yield return new CommandTestCase() { expected = true, mode = SkeletonMode.SplitBone, commandEventType = CommandEventType.Validate, commandName = SkeletonView.kDeleteCommandName };
            yield return new CommandTestCase() { expected = true, mode = SkeletonMode.SplitBone, commandEventType = CommandEventType.Execute, commandName = SkeletonView.kDeleteCommandName };
            yield return new CommandTestCase() { expected = true, mode = SkeletonMode.CreateBone, commandEventType = CommandEventType.Validate, commandName = SkeletonView.kDeleteCommandName };
            yield return new CommandTestCase() { expected = true, mode = SkeletonMode.CreateBone, commandEventType = CommandEventType.Execute, commandName = SkeletonView.kDeleteCommandName };

            yield return new CommandTestCase() { expected = false, mode = SkeletonMode.EditPose, commandEventType = CommandEventType.Validate, commandName = SkeletonView.kDeleteCommandName };
            yield return new CommandTestCase() { expected = false, mode = SkeletonMode.EditPose, commandEventType = CommandEventType.Execute, commandName = SkeletonView.kDeleteCommandName };
            yield return new CommandTestCase() { expected = false, mode = SkeletonMode.Disabled, commandEventType = CommandEventType.Validate, commandName = SkeletonView.kDeleteCommandName };
            yield return new CommandTestCase() { expected = false, mode = SkeletonMode.Disabled, commandEventType = CommandEventType.Execute, commandName = SkeletonView.kDeleteCommandName };
            yield return new CommandTestCase() { expected = false, mode = SkeletonMode.Selection, commandEventType = CommandEventType.Validate, commandName = SkeletonView.kDeleteCommandName };
            yield return new CommandTestCase() { expected = false, mode = SkeletonMode.Selection, commandEventType = CommandEventType.Execute, commandName = SkeletonView.kDeleteCommandName };
        }

        [Test]
        public void RemoveBone([ValueSource("RemoveBoneCases")] CommandTestCase testCase)
        {
            SetupBaseTestCase(testCase);
            SetupCommandTestCase(testCase);


            bool result = m_SkeletonView.DoRemoveBone();

            if (testCase.commandEventType == CommandEventType.Validate)
            {
                Assert.IsFalse(result);
                if (testCase.expected)
                    guiWrapper.Received(1).UseCurrentEvent();
                else
                    guiWrapper.DidNotReceive().UseCurrentEvent();
            }

            if (testCase.commandEventType == CommandEventType.Execute)
            {
                Assert.AreEqual(testCase.expected, m_SkeletonView.IsActionTriggering(SkeletonAction.Remove));
                Assert.AreEqual(testCase.expected, result);
                if (testCase.expected)
                {
                    guiWrapper.Received(1).UseCurrentEvent();
                    guiWrapper.Received(1).SetGuiChanged(true);
                }
            }

            if (testCase.commandEventType == CommandEventType.Other)
            {
                Assert.AreEqual(testCase.expected, result);
                guiWrapper.DidNotReceive().UseCurrentEvent();
                guiWrapper.DidNotReceiveWithAnyArgs().SetGuiChanged(Arg.Any<bool>());
            }
        }

        private static IEnumerable<SkeletonActionTestCase> ActionTriggeringCases()
        {
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.Disabled, action = SkeletonAction.None, expected = false };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditPose, expected = true, action = SkeletonAction.RotateBone, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditPose, expected = false, action = SkeletonAction.RotateBone, hoveredJoint = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditPose, expected = true, action = SkeletonAction.MoveBone, hoveredJoint = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditPose, expected = false, action = SkeletonAction.MoveBone, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditJoints, expected = true, action = SkeletonAction.FreeMoveBone, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditPose, expected = false, action = SkeletonAction.FreeMoveBone, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditJoints, expected = true, action = SkeletonAction.MoveJoint, hoveredJoint = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditJoints, expected = false, action = SkeletonAction.MoveJoint, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditPose, expected = false, action = SkeletonAction.MoveJoint, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditJoints, expected = true, action = SkeletonAction.MoveEndPosition, hoveredTail = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditJoints, expected = false, action = SkeletonAction.MoveEndPosition, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditPose, expected = false, action = SkeletonAction.MoveEndPosition, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.SplitBone, expected = true, action = SkeletonAction.SplitBone, hoveredBody = 100, state = new GUIWrapperState() { mouseDownButton = 0 } };
        }

        [Test]
        public void ActionTriggering([ValueSource("ActionTriggeringCases")] SkeletonActionTestCase testCase)
        {
            SetupBaseTestCase(testCase);
            SetupSkeletonActionTestCase(testCase);

            Assert.AreEqual(testCase.expected, m_SkeletonView.IsActionTriggering(testCase.action));
        }

        private static IEnumerable<SkeletonActionTestCase> ActionFinishingCases()
        {
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditPose, expected = true, action = SkeletonAction.RotateBone, state = new GUIWrapperState() { mouseUpButton = 0 }};
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditPose, expected = true, action = SkeletonAction.RotateBone, eventOutsideWindow = true};
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditPose, expected = true, action = SkeletonAction.MoveBone, state = new GUIWrapperState() { mouseUpButton = 0 }};
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditPose, expected = true, action = SkeletonAction.MoveBone, eventOutsideWindow = true};
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditJoints, expected = true, action = SkeletonAction.FreeMoveBone, state = new GUIWrapperState() { mouseUpButton = 0 }};
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditPose, expected = false, action = SkeletonAction.FreeMoveBone, state = new GUIWrapperState() { mouseUpButton = 0 }};
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditJoints, expected = true, action = SkeletonAction.MoveJoint, state = new GUIWrapperState() { mouseUpButton = 0 }};
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditPose, expected = false, action = SkeletonAction.MoveJoint, state = new GUIWrapperState() { mouseUpButton = 0 }};
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditJoints, expected = true, action = SkeletonAction.MoveEndPosition, state = new GUIWrapperState() { mouseUpButton = 0 }};
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditPose, expected = false, action = SkeletonAction.MoveEndPosition, state = new GUIWrapperState() { mouseUpButton = 0 }};
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.CreateBone, expected = true, action = SkeletonAction.CreateBone, state = new GUIWrapperState() { mouseDownButton = 0, multiStepHotControl = kCreateBoneControlID }};
        }

        [Test]
        public void ActionFinishing([ValueSource("ActionFinishingCases")] SkeletonActionTestCase testCase)
        {
            SetupBaseTestCase(testCase);
            SetupSkeletonActionTestCase(testCase);

            Assert.AreEqual(testCase.expected, m_SkeletonView.IsActionFinishing(testCase.action));
        }

        private static IEnumerable<SkeletonActionTestCase> ActionActiveCases()
        {
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.Disabled, expected = true, action = SkeletonAction.None };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditPose, expected = true, action = SkeletonAction.None };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditJoints, expected = true, action = SkeletonAction.None };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.CreateBone, expected = true, action = SkeletonAction.None };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.Selection, expected = true, action = SkeletonAction.None };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.SplitBone, expected = true, action = SkeletonAction.None };

            yield return new SkeletonActionTestCase() { mode = SkeletonMode.Disabled, expected = false, action = SkeletonAction.RotateBone, forceNearestControl = 1 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditPose, expected = true, action = SkeletonAction.RotateBone, forceNearestControl = 1 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditPose, expected = false, action = SkeletonAction.RotateBone, };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditJoints, expected = false, action = SkeletonAction.RotateBone, forceNearestControl = 1 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.CreateBone, expected = false, action = SkeletonAction.RotateBone, forceNearestControl = 1 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.Selection, expected = false, action = SkeletonAction.RotateBone, forceNearestControl = 1 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.SplitBone, expected = false, action = SkeletonAction.RotateBone, forceNearestControl = 1 };

            yield return new SkeletonActionTestCase() { mode = SkeletonMode.Disabled, expected = false, action = SkeletonAction.MoveBone, forceNearestControl = 2 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditPose, expected = true, action = SkeletonAction.MoveBone, forceNearestControl = 2 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditPose, expected = false, action = SkeletonAction.MoveBone, };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditJoints, expected = false, action = SkeletonAction.MoveBone, forceNearestControl = 2 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.CreateBone, expected = false, action = SkeletonAction.MoveBone, forceNearestControl = 2 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.Selection, expected = false, action = SkeletonAction.MoveBone, forceNearestControl = 2 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.SplitBone, expected = false, action = SkeletonAction.MoveBone, forceNearestControl = 2 };

            yield return new SkeletonActionTestCase() { mode = SkeletonMode.Disabled, expected = false, action = SkeletonAction.MoveJoint, forceNearestControl = 2 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditPose, expected = false, action = SkeletonAction.MoveJoint, forceNearestControl = 2 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditJoints, expected = true, action = SkeletonAction.MoveJoint, forceNearestControl = 2 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditJoints, expected = false, action = SkeletonAction.MoveJoint, };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.CreateBone, expected = true, action = SkeletonAction.MoveJoint, forceNearestControl = 2 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.Selection, expected = false, action = SkeletonAction.MoveJoint, forceNearestControl = 2 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.SplitBone, expected = true, action = SkeletonAction.MoveJoint, forceNearestControl = 2 };

            yield return new SkeletonActionTestCase() { mode = SkeletonMode.Disabled, expected = false, action = SkeletonAction.MoveEndPosition, forceNearestControl = 3 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditPose, expected = false, action = SkeletonAction.MoveEndPosition, forceNearestControl = 3 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditJoints, expected = true, action = SkeletonAction.MoveEndPosition, forceNearestControl = 3 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditJoints, expected = false, action = SkeletonAction.MoveEndPosition, };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.CreateBone, expected = false, action = SkeletonAction.MoveEndPosition, forceNearestControl = 3 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.Selection, expected = false, action = SkeletonAction.MoveEndPosition, forceNearestControl = 3 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.SplitBone, expected = true, action = SkeletonAction.MoveEndPosition, forceNearestControl = 3 };

            yield return new SkeletonActionTestCase() { mode = SkeletonMode.Disabled, expected = false, action = SkeletonAction.FreeMoveBone, forceNearestControl = 1 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditPose, expected = false, action = SkeletonAction.FreeMoveBone, forceNearestControl = 1 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditJoints, expected = true, action = SkeletonAction.FreeMoveBone, forceNearestControl = 1 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditJoints, expected = false, action = SkeletonAction.FreeMoveBone, };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.CreateBone, expected = false, action = SkeletonAction.FreeMoveBone, forceNearestControl = 1 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.Selection, expected = false, action = SkeletonAction.FreeMoveBone, forceNearestControl = 1 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.SplitBone, expected = false, action = SkeletonAction.FreeMoveBone, forceNearestControl = 1 };
            
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.Disabled, expected = false, action = SkeletonAction.ChangeLength, forceNearestControl = 3 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditPose, expected = false, action = SkeletonAction.ChangeLength, forceNearestControl = 3 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditPose, expected = false, action = SkeletonAction.ChangeLength, };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.EditJoints, expected = false, action = SkeletonAction.ChangeLength, forceNearestControl = 3 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.CreateBone, expected = false, action = SkeletonAction.ChangeLength, forceNearestControl = 3 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.Selection, expected = false, action = SkeletonAction.ChangeLength, forceNearestControl = 3 };
            yield return new SkeletonActionTestCase() { mode = SkeletonMode.SplitBone, expected = false, action = SkeletonAction.ChangeLength, forceNearestControl = 3 };
        }

        [Test]
        public void ActionActive([ValueSource("ActionActiveCases")] SkeletonActionTestCase testCase)
        {
            SetupBaseTestCase(testCase);
            SetupSkeletonActionTestCase(testCase);

            Assert.AreEqual(testCase.expected, m_SkeletonView.IsActionActive(testCase.action));
        }
    }
}
