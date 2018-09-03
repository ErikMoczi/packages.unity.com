using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using UnityEditor;
using NSubstitute;
using NUnit.Framework.Constraints;
using UnityEditor.Experimental.U2D.Animation;
using UnityEditor.Experimental.U2D.Animation.Test.MeshModule.Base;

namespace UnityEditor.Experimental.U2D.Animation.Test.BoneGizmo
{
    [TestFixture]
    public class BoneGizmoViewTests : BaseViewTest
    {
        public class BaseTestCase
        {
            public string name;
            public bool expected;
            public int transformIndex = -1;
            public float hoverPositionRatio = 0f;
            public int forceNearestControl = -1;
            public EventType eventType = EventType.Layout;
            public GUIWrapperState state;

            public override string ToString()
            {
                return name;
            }
        }

        public class ActionTestCase : BaseTestCase
        {
            public BoneGizmoAction expectedAction;

            public override string ToString()
            {
                return name + (expected ? "_Activates" : "_DoesNotActivate");
            }
        }

        public class SelectTestCase : BaseTestCase
        {
            public BoneGizmoSelectionMode expectedSelectionMode;

            public override string ToString()
            {
                string nameOverride = base.ToString();

                if (expected)
                    nameOverride += "_" + expectedSelectionMode;

                return nameOverride;
            }
        }

        private BoneGizmoView m_BoneGizmoView;
        private List<Transform> m_Transforms = new List<Transform>();

        [SetUp]
        public void Setup()
        {
            m_BoneGizmoView = new BoneGizmoView(guiWrapper);
            m_BoneGizmoView.SetupLayout();

            var transform1 = new GameObject("T1").transform;
            var transform2 = new GameObject("T2").transform;
            var transform3 = new GameObject("T3").transform;

            transform1.position = Vector3.zero;
            transform2.position = Vector3.right;
            transform3.position = Vector3.right * 2;

            transform2.SetParent(transform1, true);
            transform3.SetParent(transform2, true);

            m_Transforms.Add(transform1);
            m_Transforms.Add(transform2);
            m_Transforms.Add(transform3);
        }

        [TearDown]
        public void TearDown()
        {
            Selection.activeGameObject = null;

            foreach(var t in m_Transforms)
            {
                if(t != null)
                    GameObject.DestroyImmediate(t.gameObject);
            }

            m_Transforms.Clear();
        }

        private void SetupBaseTestCase(BaseTestCase testCase)
        {
            SetGUIWrapperState(testCase.state);

            eventType = testCase.eventType;

            if (testCase.transformIndex != -1)
            {
                var transform = m_Transforms[testCase.transformIndex];
                mousePosition = Vector3.Lerp(transform.position, transform.TransformPoint(Vector3.right), testCase.hoverPositionRatio);

                foreach(var t in m_Transforms)
                    m_BoneGizmoView.LayoutBone(t, 1f);
            }

            if (testCase.forceNearestControl >= 0)
                nearestControl = testCase.forceNearestControl;
        }

        private void SetupActionTestCase(ActionTestCase testCase)
        {
            SetupBaseTestCase(testCase);

            var transform = m_Transforms[testCase.transformIndex];
            Assert.IsTrue(m_BoneGizmoView.IsActionActive(transform, testCase.expectedAction), testCase.expectedAction +" action should be active");
        }

        private static IEnumerable<ActionTestCase> RotateBoneCases()
        {
            yield return new ActionTestCase() { name = "HoverBoneBody_Click", expected = true, transformIndex = 0, hoverPositionRatio = 0.5f, expectedAction = BoneGizmoAction.Rotate, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { name = "HoverBoneHead_Click", expected = false, transformIndex = 0, hoverPositionRatio = 0f, expectedAction = BoneGizmoAction.Move, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { name = "HoverBoneBody_Click_ViewToolActive", expected = false, transformIndex = 0, hoverPositionRatio = 0.5f, expectedAction = BoneGizmoAction.None, state = new GUIWrapperState() { mouseDownButton = 0, viewToolActive = true }};
            yield return new ActionTestCase() { name = "HoverBoneBody_Click_HotControlSet", expected = false, transformIndex = 0, hoverPositionRatio = 0.5f, expectedAction = BoneGizmoAction.None, state = new GUIWrapperState() { mouseDownButton = 0, hotControl = 100 }};
            yield return new ActionTestCase() { name = "HoverBoneBody_Click_OtherControlNearest", expected = false, transformIndex = 0, hoverPositionRatio = 0.5f, forceNearestControl = 100, expectedAction = BoneGizmoAction.None, state = new GUIWrapperState() { mouseDownButton = 0 }};
        }

        [Test]
        public void RotateBone([ValueSource("RotateBoneCases")] ActionTestCase testCase)
        {
            SetupActionTestCase(testCase);

            var transform = m_Transforms[testCase.transformIndex];
            float deltaAngle;
            Assert.AreEqual(testCase.expected, m_BoneGizmoView.DoBoneRotation(transform, out deltaAngle));
        }

        private static IEnumerable<ActionTestCase> MoveBoneCases()
        {
            yield return new ActionTestCase() { name = "HoverBoneHead_Click", expected = true, transformIndex = 0, hoverPositionRatio = 0f, expectedAction = BoneGizmoAction.Move, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { name = "HoverBoneBody_Click", expected = false, transformIndex = 0, hoverPositionRatio = 0.5f, expectedAction = BoneGizmoAction.Rotate, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new ActionTestCase() { name = "HoverBoneHead_Click_ViewToolActive", expected = false, transformIndex = 0, hoverPositionRatio = 0.5f, expectedAction = BoneGizmoAction.None, state = new GUIWrapperState() { mouseDownButton = 0, viewToolActive = true }};
            yield return new ActionTestCase() { name = "HoverBoneHead_Click_HotControlSet", expected = false, transformIndex = 0, hoverPositionRatio = 0.5f, expectedAction = BoneGizmoAction.None, state = new GUIWrapperState() { mouseDownButton = 0, hotControl = 100 }};
            yield return new ActionTestCase() { name = "HoverBoneHead_Click_OtherControlNearest", expected = false, transformIndex = 0, hoverPositionRatio = 0.5f, forceNearestControl = 100, expectedAction = BoneGizmoAction.None, state = new GUIWrapperState() { mouseDownButton = 0 }};
        }

        [Test]
        public void MoveBone([ValueSource("MoveBoneCases")] ActionTestCase testCase)
        {
            SetupActionTestCase(testCase);

            var transform = m_Transforms[testCase.transformIndex];
            Vector3 deltaPosition;
            Assert.AreEqual(testCase.expected, m_BoneGizmoView.DoBonePosition(transform, out deltaPosition));
        }

        private static IEnumerable<SelectTestCase> SelectionTestCases()
        {
            yield return new SelectTestCase() { name = "HoverBoneHead_Click", expected = true, transformIndex = 0, hoverPositionRatio = 0f, expectedSelectionMode = BoneGizmoSelectionMode.Single, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new SelectTestCase() { name = "HoverBoneBody_Click", expected = true, transformIndex = 0, hoverPositionRatio = 0.5f, expectedSelectionMode = BoneGizmoSelectionMode.Single, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new SelectTestCase() { name = "HoverBoneHead_Click_Shift", expected = true, transformIndex = 0, hoverPositionRatio = 0f, expectedSelectionMode = BoneGizmoSelectionMode.Toggle, state = new GUIWrapperState() { mouseDownButton = 0, isShiftDown = true }};
            yield return new SelectTestCase() { name = "HoverBoneBody_Click_Shift", expected = true, transformIndex = 0, hoverPositionRatio = 0.5f, expectedSelectionMode = BoneGizmoSelectionMode.Toggle, state = new GUIWrapperState() { mouseDownButton = 0, isShiftDown = true }};
            yield return new SelectTestCase() { name = "HoverBoneHead_Click_ActionKey", expected = true, transformIndex = 0, hoverPositionRatio = 0f, expectedSelectionMode = BoneGizmoSelectionMode.Toggle, state = new GUIWrapperState() { mouseDownButton = 0, isActionKeyDown = true }};
            yield return new SelectTestCase() { name = "HoverBoneBody_Click_ActionKey", expected = true, transformIndex = 0, hoverPositionRatio = 0.5f, expectedSelectionMode = BoneGizmoSelectionMode.Toggle, state = new GUIWrapperState() { mouseDownButton = 0, isActionKeyDown = true }};
            yield return new SelectTestCase() { name = "HoverBoneHead_Click_ViewToolActive", expected = false, transformIndex = 0, hoverPositionRatio = 0f, state = new GUIWrapperState() { mouseDownButton = 0, viewToolActive = true }};
            yield return new SelectTestCase() { name = "HoverBoneBody_Click_ViewToolActive", expected = false, transformIndex = 0, hoverPositionRatio = 0.5f, state = new GUIWrapperState() { mouseDownButton = 0 , viewToolActive = true}};
            yield return new SelectTestCase() { name = "HoverBoneHead_Click_HotControlSet", expected = false, transformIndex = 0, hoverPositionRatio = 0f, state = new GUIWrapperState() { mouseDownButton = 0, hotControl = 100 }};
            yield return new SelectTestCase() { name = "HoverBoneBody_Click_HotControlSet", expected = false, transformIndex = 0, hoverPositionRatio = 0.5f, state = new GUIWrapperState() { mouseDownButton = 0, hotControl = 100 }};
            yield return new SelectTestCase() { name = "HoverBoneHead_Click_OtherControlNearest", expected = false, transformIndex = 0, hoverPositionRatio = 0f, forceNearestControl = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
            yield return new SelectTestCase() { name = "HoverBoneBody_Click_OtherControlNearest", expected = false, transformIndex = 0, hoverPositionRatio = 0.5f, forceNearestControl = 100, state = new GUIWrapperState() { mouseDownButton = 0 }};
        }

        [Test]
        public void SelectBone([ValueSource("SelectionTestCases")] SelectTestCase testCase)
        {
            SetupBaseTestCase(testCase);

            var transform = m_Transforms[testCase.transformIndex];
            BoneGizmoSelectionMode selectionMode;
            Assert.AreEqual(testCase.expected, m_BoneGizmoView.DoSelection(transform, out selectionMode));
            Assert.AreEqual(testCase.expectedSelectionMode, selectionMode);
        }
    }
}
