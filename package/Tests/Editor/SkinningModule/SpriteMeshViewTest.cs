using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using UnityEditor;
using NSubstitute;
using NUnit.Framework.Constraints;
using UnityEditor.Experimental.U2D.Animation.Test;

namespace UnityEditor.Experimental.U2D.Animation.Test.SpriteMesh
{
    [TestFixture]
    public class SpriteMeshViewTest : BaseViewTest
    {
        public class BaseTestCase
        {
            public string name;
            public bool expected;
            public SpriteMeshViewMode mode;
            public int hoveredVertex = -1;
            public int hoveredEdge = -1;
            public int nonHoveredEdge = -1;
            public int selectedVertex = -1;
            public EventType eventType = EventType.Layout;
            public GUIWrapperState state;

            public override string ToString()
            {
                return name;
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
                string nameOverride = base.ToString();

                if (commandEventType == CommandEventType.Validate)
                    nameOverride += (expected ? "_Validates" : "_IgnoresValidation");

                return nameOverride;
            }
        }

        private const int kDefaultControlID = 2000;
        private const string kDeleteCommandName = "Delete";
        private const string kSoftDeleteCommandName = "SoftDelete";

        private SpriteMeshView m_SpriteMeshView;
        private ISelection<int> m_Selection;

        protected override int GetDefaultControlID()
        {
            return kDefaultControlID;
        }

        [SetUp]
        public void Setup()
        {
            m_Selection = Substitute.For<ISelection<int>>();
            m_SpriteMeshView = new SpriteMeshView(guiWrapper);
            m_SpriteMeshView.selection = m_Selection;
            m_SpriteMeshView.defaultControlID = kDefaultControlID;
            m_SpriteMeshView.frame = new Rect(-Vector2.one * 1000f, Vector2.one * 2000f);
            m_SpriteMeshView.BeginLayout();
        }

        private void SetupBaseTestCase(BaseTestCase testCase)
        {
            m_SpriteMeshView.mode = testCase.mode;
            eventType = EventType.Layout;

            if (testCase.hoveredVertex >= 0)
                m_SpriteMeshView.LayoutVertex(mousePosition, testCase.hoveredVertex);

            if (testCase.nonHoveredEdge >= 0)
                m_SpriteMeshView.LayoutEdge(mousePosition + Vector2.up * 1000f, mousePosition + Vector2.one + Vector2.up * 1000f, testCase.nonHoveredEdge);

            if (testCase.hoveredEdge >= 0)
                m_SpriteMeshView.LayoutEdge(mousePosition, mousePosition + Vector2.one, testCase.hoveredEdge);

            m_SpriteMeshView.EndLayout();

            if (testCase.hoveredVertex >= 0)
                Assert.AreEqual(testCase.hoveredVertex, m_SpriteMeshView.hoveredVertex, "Vertex should be hovered");

            if (testCase.nonHoveredEdge >= 0)
            {
                Assert.AreEqual(-1, m_SpriteMeshView.hoveredEdge, "Edge should not be hovered");
                Assert.AreEqual(testCase.nonHoveredEdge, m_SpriteMeshView.closestEdge, "Edge should be the closest");
            }

            if (testCase.hoveredEdge >= 0)
            {
                Assert.AreEqual(testCase.hoveredEdge, m_SpriteMeshView.hoveredEdge, "Edge should be hovered");
                Assert.AreEqual(testCase.hoveredEdge, m_SpriteMeshView.closestEdge, "Edge should be the closest");
            }

            eventType = testCase.eventType;

            SetGUIWrapperState(testCase.state);

            if (testCase.selectedVertex >= 0)
            {
                m_Selection.Count.Returns(1);
                m_Selection.activeElement.Returns(testCase.selectedVertex);
                m_Selection.Contains(testCase.selectedVertex).Returns(true);
            }
        }

        private void SetupCommandTestCase(CommandTestCase commandTestCase)
        {
            eventType = (EventType)commandTestCase.commandEventType;
            guiWrapper.commandName.Returns(commandTestCase.commandName);
        }

        private static IEnumerable<ActionTestCase> CreateVertexCases()
        {
            yield return new ActionTestCase() { name = "SelectionMode_DoubleClick", expected = true, mode = SpriteMeshViewMode.EditGeometry, state = new GUIWrapperState() { clickCount = 2, mouseDownButton = 0 }};
            yield return new ActionTestCase() { name = "SelectionMode_DoubleClick_AltIsDown", expected = false, mode = SpriteMeshViewMode.EditGeometry, state = new GUIWrapperState() { clickCount = 2, mouseDownButton = 0, isAltDown = true} };
            yield return new ActionTestCase() { name = "SelectionMode_DoubleClick_HotControlSet", expected = false, mode = SpriteMeshViewMode.EditGeometry, state = new GUIWrapperState() { clickCount = 2, mouseDownButton = 0, hotControl = 30 } };
            yield return new ActionTestCase() { name = "SelectionMode_DoubleClick_VertexIsHovered", expected = false, mode = SpriteMeshViewMode.EditGeometry, hoveredVertex = 100, state = new GUIWrapperState() { clickCount = 2, mouseDownButton = 0 } };
            yield return new ActionTestCase() { name = "SelectionMode_DoubleClick_EdgeIsHovered", expected = false, mode = SpriteMeshViewMode.EditGeometry, hoveredEdge = 100, state = new GUIWrapperState() { clickCount = 2, mouseDownButton = 0 } };

            yield return new ActionTestCase() { name = "CreateVertexMode_SingleClick", expected = true, mode = SpriteMeshViewMode.CreateVertex, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new ActionTestCase() { name = "CreateVertexMode_SingleClick_VertexIsHovered", expected = false, mode = SpriteMeshViewMode.CreateVertex, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new ActionTestCase() { name = "CreateVertexMode_SingleClick_EdgeIsHovered", expected = true, mode = SpriteMeshViewMode.CreateVertex, hoveredEdge = 100, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new ActionTestCase() { name = "CreateVertexMode_SingleClick_AltIsDown", expected = false, mode = SpriteMeshViewMode.CreateVertex, state = new GUIWrapperState() { mouseDownButton = 0, isAltDown = true} };
            yield return new ActionTestCase() { name = "CreateVertexMode_SingleClick_HotControlSet", expected = false, mode = SpriteMeshViewMode.CreateVertex, state = new GUIWrapperState() { mouseDownButton = 0, hotControl = 30} };
        }

        [Test]
        public void CreateVertex([ValueSource("CreateVertexCases")] ActionTestCase testCase)
        {
            SetupBaseTestCase(testCase);

            Assert.AreEqual(testCase.expected, m_SpriteMeshView.IsActionTriggered(MeshEditorAction.CreateVertex));

            Assert.AreEqual(testCase.expected, m_SpriteMeshView.DoCreateVertex());
            if (testCase.expected)
            {
                guiWrapper.Received(1).UseCurrentEvent();
                guiWrapper.Received(1).SetGuiChanged(true);
            }
            else
            {
                guiWrapper.DidNotReceiveWithAnyArgs().UseCurrentEvent();
                guiWrapper.DidNotReceiveWithAnyArgs().SetGuiChanged(Arg.Any<bool>());
            }
        }

        private static IEnumerable<BaseTestCase> CreateVertexConsumeMouseEventsCases()
        {
            yield return new BaseTestCase() { name = "CreateVertexMode_MouseMove_UsesEvent", expected = true, mode = SpriteMeshViewMode.CreateVertex, eventType = EventType.MouseMove, state = new GUIWrapperState() };
            yield return new BaseTestCase() { name = "CreateVertexMode_MouseDrag_UsesEvent", expected = true, mode = SpriteMeshViewMode.CreateVertex, eventType = EventType.MouseDrag, state = new GUIWrapperState() { mouseButton = 0 } };
        }

        [Test]
        public void CreateVertexConsumeMouseEvents([ValueSource("CreateVertexConsumeMouseEventsCases")] BaseTestCase testCase)
        {
            SetupBaseTestCase(testCase);
            Assert.IsFalse(m_SpriteMeshView.DoCreateVertex());
            if (testCase.expected)
            {
                guiWrapper.Received(1).UseCurrentEvent();
            }
            else
            {
                guiWrapper.DidNotReceive().UseCurrentEvent();
            }
        }

        private static IEnumerable<SelectTestCase> SelectVertexCases()
        {
            yield return new SelectTestCase() { name = "SelectionMode_HoveredVertex_Click", expected = true, mode = SpriteMeshViewMode.EditGeometry, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new SelectTestCase() { name = "SelectionMode_NoHoveredVertex_Click", expected = false, mode = SpriteMeshViewMode.EditGeometry, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new SelectTestCase() { name = "SelectionMode_SelectedVertex_SameHoveredVertex_Click", expected = true, mode = SpriteMeshViewMode.EditGeometry, selectedVertex = 100, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new SelectTestCase() { name = "SelectionMode_SelectedVertex_SameHoveredVertex_Click_ActionKeyIsDown", expected = true, expectedAdditive = true, mode = SpriteMeshViewMode.EditGeometry, selectedVertex = 100, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0, isActionKeyDown = true } };
            yield return new SelectTestCase() { name = "SelectionMode_HoveredVertex_Click_ActionKeyIsDown", expected = true, expectedAdditive = true, mode = SpriteMeshViewMode.EditGeometry, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0, isActionKeyDown = true } };
            yield return new SelectTestCase() { name = "SelectionMode_HoveredVertex_Click_AltIsDown", expected = false, mode = SpriteMeshViewMode.EditGeometry, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0, isAltDown = true } };
            yield return new SelectTestCase() { name = "SelectionMode_HoveredVertex_Click_HotControlSet", expected = false, mode = SpriteMeshViewMode.EditGeometry, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0, hotControl = 30 } };

            yield return new SelectTestCase() { name = "CreateVertexMode_HoveredVertex_Click", expected = true, mode = SpriteMeshViewMode.CreateVertex, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new SelectTestCase() { name = "CreateVertexMode_NoHoveredVertex_Click", expected = false, mode = SpriteMeshViewMode.CreateVertex, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new SelectTestCase() { name = "CreateVertexMode_SelectedVertex_SameHoveredVertex_Click", expected = true, mode = SpriteMeshViewMode.CreateVertex, selectedVertex = 100, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new SelectTestCase() { name = "CreateVertexMode_SelectedVertex_SameHoveredVertex_Click_ActionKeyIsDown", expected = true, expectedAdditive = true, mode = SpriteMeshViewMode.CreateVertex, selectedVertex = 100, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0, isActionKeyDown = true } };
            yield return new SelectTestCase() { name = "CreateVertexMode_HoveredVertex_Click_ActionKeyIsDown", expected = true, expectedAdditive = true, mode = SpriteMeshViewMode.CreateVertex, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0, isActionKeyDown = true } };
            yield return new SelectTestCase() { name = "CreateVertexMode_HoveredVertex_Click_AltIsDown", expected = false, mode = SpriteMeshViewMode.CreateVertex, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0, isAltDown = true } };
            yield return new SelectTestCase() { name = "CreateVertexMode_HoveredVertex_Click_HotControlSet", expected = false, mode = SpriteMeshViewMode.CreateVertex, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0, hotControl = 30 } };

            yield return new SelectTestCase() { name = "CreateEdgeMode_HoveredVertex_Click", expected = true, mode = SpriteMeshViewMode.CreateEdge, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new SelectTestCase() { name = "CreateEdgeMode_NoHoveredVertex_Click", expected = false, mode = SpriteMeshViewMode.CreateEdge, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new SelectTestCase() { name = "CreateEdgeMode_SelectedVertex_SameHoveredVertex_Click", expected = true, mode = SpriteMeshViewMode.CreateEdge, selectedVertex = 100, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new SelectTestCase() { name = "CreateEdgeMode_SelectedVertex_SameHoveredVertex_Click_ActionKeyIsDown", expected = true, expectedAdditive = true, mode = SpriteMeshViewMode.CreateEdge, selectedVertex = 100, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0, isActionKeyDown = true } };
            yield return new SelectTestCase() { name = "CreateEdgeMode_HoveredVertex_Click_ActionKeyIsDown", expected = true, expectedAdditive = true, mode = SpriteMeshViewMode.CreateEdge, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0, isActionKeyDown = true } };
            yield return new SelectTestCase() { name = "CreateEdgeMode_HoveredVertex_Click_AltIsDown", expected = false, mode = SpriteMeshViewMode.CreateEdge, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0, isAltDown = true } };
            yield return new SelectTestCase() { name = "CreateEdgeMode_HoveredVertex_Click_HotControlSet", expected = false, mode = SpriteMeshViewMode.CreateEdge, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0, hotControl = 30 } };

            yield return new SelectTestCase() { name = "SplitEdgeMode_HoveredVertex_Click", expected = true, mode = SpriteMeshViewMode.SplitEdge, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new SelectTestCase() { name = "SplitEdgeMode_NoHoveredVertex_Click", expected = false, mode = SpriteMeshViewMode.SplitEdge, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new SelectTestCase() { name = "SplitEdgeMode_SelectedVertex_SameHoveredVertex_Click", expected = true, mode = SpriteMeshViewMode.SplitEdge, selectedVertex = 100, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new SelectTestCase() { name = "SplitEdgeMode_SelectedVertex_SameHoveredVertex_Click_ActionKeyIsDown", expected = true, expectedAdditive = true, mode = SpriteMeshViewMode.SplitEdge, selectedVertex = 100, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0, isActionKeyDown = true } };
            yield return new SelectTestCase() { name = "SplitEdgeMode_HoveredVertex_Click_ActionKeyIsDown", expected = true, expectedAdditive = true, mode = SpriteMeshViewMode.SplitEdge, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0, isActionKeyDown = true } };
            yield return new SelectTestCase() { name = "SplitEdgeMode_HoveredVertex_Click_AltIsDown", expected = false, mode = SpriteMeshViewMode.SplitEdge, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0, isAltDown = true } };
            yield return new SelectTestCase() { name = "SplitEdgeMode_HoveredVertex_Click_HotControlSet", expected = false, mode = SpriteMeshViewMode.SplitEdge, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0, hotControl = 30 } };
        }

        [Test]
        public void SelectVertex([ValueSource("SelectVertexCases")] SelectTestCase testCase)
        {
            SetupBaseTestCase(testCase);

            Assert.AreEqual(testCase.expected, m_SpriteMeshView.IsActionTriggered(MeshEditorAction.SelectVertex));

            bool additive;
            Assert.AreEqual(testCase.expected, m_SpriteMeshView.DoSelectVertex(out additive));
            Assert.AreEqual(testCase.expectedAdditive, additive, "Additive state is wrong");
            if (testCase.expected)
                guiWrapper.Received().Repaint();
        }

        private static IEnumerable<ActionTestCase> MoveVertexCases()
        {
            yield return new ActionTestCase() { name = "SelectionMode_HoveredVertex_Click", expected = true, mode = SpriteMeshViewMode.EditGeometry, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new ActionTestCase() { name = "SelectionMode_NoVertexHovered_Click", expected = false, mode = SpriteMeshViewMode.EditGeometry, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new ActionTestCase() { name = "SelectionMode_HoveredVertex_Click_AltIsDown", expected = false, mode = SpriteMeshViewMode.EditGeometry, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0, isAltDown = true } };
            yield return new ActionTestCase() { name = "SelectionMode_HoveredVertex_Click_HotControlSet", expected = false, mode = SpriteMeshViewMode.EditGeometry, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0, hotControl = 30 } };

            yield return new ActionTestCase() { name = "CreateVertexMode_HoveredVertex_Click", expected = true, mode = SpriteMeshViewMode.CreateVertex, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new ActionTestCase() { name = "CreateEdgeMode_HoveredVertex_Click", expected = true, mode = SpriteMeshViewMode.CreateEdge, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new ActionTestCase() { name = "SplitEdgeMode_HoveredVertex_Click", expected = true, mode = SpriteMeshViewMode.SplitEdge, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0 } };
        }

        [Test]
        public void MoveVertex([ValueSource("MoveVertexCases")] ActionTestCase testCase)
        {
            SetupBaseTestCase(testCase);

            Assert.AreEqual(testCase.expected, m_SpriteMeshView.IsActionTriggered(MeshEditorAction.MoveVertex));

            Vector2 delta;
            Assert.AreEqual(testCase.expected, m_SpriteMeshView.DoMoveVertex(out delta));
        }

        private static IEnumerable<ActionTestCase> CreateEdgeCases()
        {
            yield return new ActionTestCase() { name = "SelectionMode_SelectedVertex_Shift_Click", expected = true, mode = SpriteMeshViewMode.EditGeometry, selectedVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0, isShiftDown = true } };
            yield return new ActionTestCase() { name = "SelectionMode_NoSelection_Shift_Click", expected = false, mode = SpriteMeshViewMode.EditGeometry, state = new GUIWrapperState() { mouseDownButton = 0, isShiftDown = true } };
            yield return new ActionTestCase() { name = "SelectionMode_SelectedVertex_Shift_Click_AltIsDown", expected = false, mode = SpriteMeshViewMode.EditGeometry, selectedVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0, isShiftDown = true, isAltDown = true } };
            yield return new ActionTestCase() { name = "SelectionMode_SelectedVertex_Shift_Click_HotControlSet", expected = false, mode = SpriteMeshViewMode.EditGeometry, selectedVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0, isShiftDown = true, hotControl = 30 } };
            yield return new ActionTestCase() { name = "SelectionMode_SelectedVertex_DifferentHoveredVertex_Shift_Click", expected = true, mode = SpriteMeshViewMode.EditGeometry, selectedVertex = 100, hoveredVertex = 0, state = new GUIWrapperState() { mouseDownButton = 0, isShiftDown = true } };
            yield return new ActionTestCase() { name = "SelectionMode_SelectedVertex_SameHoveredVertex_Shift_Click", expected = false, mode = SpriteMeshViewMode.EditGeometry, selectedVertex = 100, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0, isShiftDown = true } };
            yield return new ActionTestCase() { name = "SelectionMode_SelectedVertex_HoveredEdge_Shift_Click", expected = true, mode = SpriteMeshViewMode.EditGeometry, selectedVertex = 100, hoveredEdge = 0, state = new GUIWrapperState() { mouseDownButton = 0, isShiftDown = true } };

            yield return new ActionTestCase() { name = "CreateEdgeMode_SelectedVertex_Click", expected = true, mode = SpriteMeshViewMode.CreateEdge, selectedVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new ActionTestCase() { name = "CreateEdgeMode_NoSelection_Click", expected = false, mode = SpriteMeshViewMode.CreateEdge, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new ActionTestCase() { name = "CreateEdgeMode_SelectedVertex_Click_AltIsDown", expected = false, mode = SpriteMeshViewMode.CreateEdge, selectedVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0, isAltDown = true } };
            yield return new ActionTestCase() { name = "CreateEdgeMode_SelectedVertex_DifferentHoveredVertex_Click", expected = true, mode = SpriteMeshViewMode.CreateEdge, selectedVertex = 100, hoveredVertex = 0, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new ActionTestCase() { name = "CreateEdgeMode_SelectedVertex_SameHoveredVertex_Click", expected = false, mode = SpriteMeshViewMode.CreateEdge, selectedVertex = 100, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new ActionTestCase() { name = "CreateEdgeMode_SelectedVertex_HoveredEdge_Click", expected = true, mode = SpriteMeshViewMode.CreateEdge, selectedVertex = 100, hoveredEdge = 0, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new ActionTestCase() { name = "CreateEdgeMode_SelectedVertex_Click_HotControlSet", expected = false, mode = SpriteMeshViewMode.CreateEdge, selectedVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0, hotControl = 30 } };
        }

        [Test]
        public void CreateEdge([ValueSource("CreateEdgeCases")] ActionTestCase testCase)
        {
            SetupBaseTestCase(testCase);

            Assert.AreEqual(testCase.expected, m_SpriteMeshView.IsActionTriggered(MeshEditorAction.CreateEdge));

            Assert.AreEqual(testCase.expected, m_SpriteMeshView.DoCreateEdge());
            if (testCase.expected)
            {
                guiWrapper.Received(1).UseCurrentEvent();
                guiWrapper.Received(1).SetGuiChanged(true);
            }
            else
            {
                guiWrapper.DidNotReceive().UseCurrentEvent();
                guiWrapper.DidNotReceiveWithAnyArgs().SetGuiChanged(Arg.Any<bool>());
            }
        }

        private static IEnumerable<BaseTestCase> CreateEdgeConsumeMouseEventsCases()
        {
            yield return new BaseTestCase() { name = "CreateEdgeMode_MouseMove_UsesEvent", expected = true, mode = SpriteMeshViewMode.CreateEdge, selectedVertex = 100,  eventType = EventType.MouseMove, state = new GUIWrapperState() };
            yield return new BaseTestCase() { name = "CreateEdgeMode_MouseDrag_UsesEvent", expected = true, mode = SpriteMeshViewMode.CreateEdge, selectedVertex = 100, eventType = EventType.MouseDrag, state = new GUIWrapperState() { mouseButton = 0 } };
        }

        [Test]
        public void CreateEdgeConsumeMouseEvents([ValueSource("CreateEdgeConsumeMouseEventsCases")] BaseTestCase testCase)
        {
            SetupBaseTestCase(testCase);
            Assert.IsFalse(m_SpriteMeshView.DoCreateEdge());
            if (testCase.expected)
                guiWrapper.Received(1).UseCurrentEvent();
            else
                guiWrapper.DidNotReceive().UseCurrentEvent();
        }

        private static IEnumerable<SelectTestCase> SelectEdgeCases()
        {
            yield return new SelectTestCase() { name = "SelectionMode_HoveredEdge_Click", expected = true, mode = SpriteMeshViewMode.EditGeometry, hoveredEdge = 100, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new SelectTestCase() { name = "SelectionMode_NoHoveredEdge_Click", expected = false, mode = SpriteMeshViewMode.EditGeometry, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new SelectTestCase() { name = "SelectionMode_HoveredEdge_Click_ActionKeyIsDown", expected = true, expectedAdditive = true, mode = SpriteMeshViewMode.EditGeometry, hoveredEdge = 100, state = new GUIWrapperState() { mouseDownButton = 0, isActionKeyDown = true } };
            yield return new SelectTestCase() { name = "SelectionMode_HoveredEdge_Click_AltIsDown", expected = false, mode = SpriteMeshViewMode.EditGeometry, hoveredEdge = 100, state = new GUIWrapperState() { mouseDownButton = 0, isAltDown = true } };
            yield return new SelectTestCase() { name = "SelectionMode_HoveredEdge_Click_HotControlSet", expected = false, mode = SpriteMeshViewMode.EditGeometry, hoveredEdge = 100, state = new GUIWrapperState() { mouseDownButton = 0, hotControl = 30 } };

            yield return new SelectTestCase() { name = "CreateVertexMode_HoveredEdge_Click", expected = false, mode = SpriteMeshViewMode.CreateVertex, hoveredEdge = 100, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new SelectTestCase() { name = "CreateEdgeMode_HoveredEdge_Click", expected = false, mode = SpriteMeshViewMode.CreateEdge, hoveredEdge = 100, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new SelectTestCase() { name = "SplitEdgeMode_HoveredEdge_Click", expected = false, mode = SpriteMeshViewMode.SplitEdge, hoveredEdge = 100, state = new GUIWrapperState() { mouseDownButton = 0 } };
        }

        [Test]
        public void SelectEdge([ValueSource("SelectEdgeCases")] SelectTestCase testCase)
        {
            SetupBaseTestCase(testCase);

            Assert.AreEqual(testCase.expected, m_SpriteMeshView.IsActionTriggered(MeshEditorAction.SelectEdge));

            bool additive;
            Assert.AreEqual(testCase.expected, m_SpriteMeshView.DoSelectEdge(out additive));
            Assert.AreEqual(testCase.expectedAdditive, additive, "Additive state is wrong");
            if (testCase.expected)
                guiWrapper.Received().Repaint();
        }

        private static IEnumerable<ActionTestCase> SplitEdgeCases()
        {
            yield return new ActionTestCase() { name = "SelectionMode_HasClosestNonHoveredEdge_ShiftDown_Click", expected = true, mode = SpriteMeshViewMode.EditGeometry, nonHoveredEdge = 100, state = new GUIWrapperState() { mouseDownButton = 0, isShiftDown = true } };
            yield return new ActionTestCase() { name = "SelectionMode_HoveredEdge_ShiftDown_Click", expected = true, mode = SpriteMeshViewMode.EditGeometry, hoveredEdge = 100, state = new GUIWrapperState() { mouseDownButton = 0, isShiftDown = true } };
            yield return new ActionTestCase() { name = "SelectionMode_HasNoClosestEdge_ShiftDown_Click", expected = false, mode = SpriteMeshViewMode.EditGeometry, state = new GUIWrapperState() { mouseDownButton = 0, isShiftDown = true } };
            yield return new ActionTestCase() { name = "SelectionMode_HasClosestNonHoveredEdge_HoveredVertex_ShiftDown_Click", expected = false, mode = SpriteMeshViewMode.EditGeometry, nonHoveredEdge = 100, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0, isShiftDown = true } };
            yield return new ActionTestCase() { name = "SelectionMode_HasClosestNonHoveredEdge_SelectedVertex_ShiftDown_Click", expected = false, mode = SpriteMeshViewMode.EditGeometry, nonHoveredEdge = 100, selectedVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0, isShiftDown = true } };
            yield return new ActionTestCase() { name = "SelectionMode_HasClosestNonHoveredEdge_ShiftDown_Click_AltIsDown", expected = false, mode = SpriteMeshViewMode.EditGeometry, nonHoveredEdge = 100, state = new GUIWrapperState() { mouseDownButton = 0, isShiftDown = true, isAltDown = true } };
            yield return new ActionTestCase() { name = "SelectionMode_HasClosestNonHoveredEdge_ShiftDown_Click_HotControlSet", expected = false, mode = SpriteMeshViewMode.EditGeometry, nonHoveredEdge = 100, state = new GUIWrapperState() { mouseDownButton = 0, isShiftDown = true, hotControl = 30 } };

            yield return new ActionTestCase() { name = "SplitEdgeMode_HasClosestNonHoveredEdge_Click", expected = true, mode = SpriteMeshViewMode.SplitEdge, nonHoveredEdge = 100, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new ActionTestCase() { name = "SplitEdgeMode_HasNoClosestEdge_Click", expected = false, mode = SpriteMeshViewMode.SplitEdge, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new ActionTestCase() { name = "SplitEdgeMode_HasClosestNonHoveredEdge_HoveredVertex_Click", expected = false, mode = SpriteMeshViewMode.SplitEdge, nonHoveredEdge = 100, hoveredVertex = 100, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new ActionTestCase() { name = "SplitEdgeMode_HasClosestNonHoveredEdge_Click_AltIsDown", expected = false, mode = SpriteMeshViewMode.SplitEdge, nonHoveredEdge = 100, state = new GUIWrapperState() { mouseDownButton = 0, isAltDown = true } };
            yield return new ActionTestCase() { name = "SplitEdgeMode_HasClosestNonHoveredEdge_Click_HotControlSet", expected = false, mode = SpriteMeshViewMode.SplitEdge, nonHoveredEdge = 100, state = new GUIWrapperState() { mouseDownButton = 0, hotControl = 30 } };
        }

        [Test]
        public void SplitEdge([ValueSource("SplitEdgeCases")] ActionTestCase testCase)
        {
            SetupBaseTestCase(testCase);

            Assert.AreEqual(testCase.expected, m_SpriteMeshView.IsActionTriggered(MeshEditorAction.SplitEdge));

            Assert.AreEqual(testCase.expected, m_SpriteMeshView.DoSplitEdge());
            if (testCase.expected)
            {
                guiWrapper.Received(1).UseCurrentEvent();
                guiWrapper.Received(1).SetGuiChanged(true);
            }
            else
            {
                guiWrapper.DidNotReceive().UseCurrentEvent();
                guiWrapper.DidNotReceiveWithAnyArgs().SetGuiChanged(Arg.Any<bool>());
            }
        }

        private static IEnumerable<BaseTestCase> SplitEdgeConsumeMouseEventsCases()
        {
            yield return new BaseTestCase() { name = "SplitEdgeMode_MouseMove_UsesEvent", expected = true, mode = SpriteMeshViewMode.SplitEdge, nonHoveredEdge = 100, eventType = EventType.MouseMove, state = new GUIWrapperState() };
            yield return new BaseTestCase() { name = "SplitEdgeMode_MouseDrag_UsesEvent", expected = true, mode = SpriteMeshViewMode.SplitEdge, nonHoveredEdge = 100, eventType = EventType.MouseDrag, state = new GUIWrapperState() { mouseButton = 0 } };
        }

        [Test]
        public void SplitEdgeConsumeMouseEvents([ValueSource("SplitEdgeConsumeMouseEventsCases")] BaseTestCase testCase)
        {
            SetupBaseTestCase(testCase);
            Assert.IsFalse(m_SpriteMeshView.DoSplitEdge());
            if (testCase.expected)
                guiWrapper.Received(1).UseCurrentEvent();
            else
                guiWrapper.DidNotReceive().UseCurrentEvent();
        }

        private static IEnumerable<ActionTestCase> MoveEdgeCases()
        {
            yield return new ActionTestCase() { name = "SelectionMode_HoveredEdge_Click", expected = true, mode = SpriteMeshViewMode.EditGeometry, hoveredEdge = 100, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new ActionTestCase() { name = "SelectionMode_NoEdgeHovered_Click", expected = false, mode = SpriteMeshViewMode.EditGeometry, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new ActionTestCase() { name = "SelectionMode_HoveredEdge_Click_AltIsDown", expected = false, mode = SpriteMeshViewMode.EditGeometry, hoveredEdge = 100, state = new GUIWrapperState() { mouseDownButton = 0, isAltDown = true } };
            yield return new ActionTestCase() { name = "SelectionMode_HoveredEdge_Click_HotControlSet", expected = false, mode = SpriteMeshViewMode.EditGeometry, hoveredEdge = 100, state = new GUIWrapperState() { mouseDownButton = 0, hotControl = 30 } };

            yield return new ActionTestCase() { name = "CreateVertexMode_HoveredEdge_Click", expected = true, mode = SpriteMeshViewMode.CreateVertex, hoveredEdge = 100, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new ActionTestCase() { name = "CreateEdgeMode_HoveredEdge_Click", expected = true, mode = SpriteMeshViewMode.CreateEdge, hoveredEdge = 100, state = new GUIWrapperState() { mouseDownButton = 0 } };
            yield return new ActionTestCase() { name = "SplitEdgeMode_HoveredEdge_Click", expected = true, mode = SpriteMeshViewMode.SplitEdge, hoveredEdge = 100, state = new GUIWrapperState() { mouseDownButton = 0 } };
        }

        [Test]
        public void MoveEdge([ValueSource("MoveEdgeCases")] ActionTestCase testCase)
        {
            SetupBaseTestCase(testCase);

            Assert.AreEqual(testCase.expected, m_SpriteMeshView.IsActionTriggered(MeshEditorAction.MoveEdge));

            Vector2 delta;
            Assert.AreEqual(testCase.expected, m_SpriteMeshView.DoMoveEdge(out delta));
        }

        private static IEnumerable<CommandTestCase> RemoveCases()
        {
            yield return new CommandTestCase() { name = "SelectionMode_ValidateDeleteCommand", expected = true, mode = SpriteMeshViewMode.EditGeometry, commandEventType = CommandEventType.Validate, commandName = kDeleteCommandName, state = new GUIWrapperState() };
            yield return new CommandTestCase() { name = "SelectionMode_ExecuteDeleteCommand", expected = true, mode = SpriteMeshViewMode.EditGeometry, commandEventType = CommandEventType.Execute, commandName = kDeleteCommandName, state = new GUIWrapperState() };
            yield return new CommandTestCase() { name = "SelectionMode_OtherEvent", expected = false, mode = SpriteMeshViewMode.EditGeometry, commandEventType = CommandEventType.Other, state = new GUIWrapperState() };
            yield return new CommandTestCase() { name = "SelectionMode_ValidateDeleteCommand_AltIsDown", expected = false, mode = SpriteMeshViewMode.EditGeometry, commandEventType = CommandEventType.Validate, commandName = kDeleteCommandName, state = new GUIWrapperState() { isAltDown = true } };
            yield return new CommandTestCase() { name = "SelectionMode_ExecuteDeleteCommand_AltIsDown", expected = false, mode = SpriteMeshViewMode.EditGeometry, commandEventType = CommandEventType.Execute, commandName = kDeleteCommandName, state = new GUIWrapperState() { isAltDown = true } };
            yield return new CommandTestCase() { name = "SelectionMode_ValidateDeleteCommand_HotControlSet", expected = false, mode = SpriteMeshViewMode.EditGeometry, commandEventType = CommandEventType.Validate, commandName = kDeleteCommandName, state = new GUIWrapperState() { hotControl = 30 } };
            yield return new CommandTestCase() { name = "SelectionMode_ExecuteDeleteCommand_HotControlSet", expected = false, mode = SpriteMeshViewMode.EditGeometry, commandEventType = CommandEventType.Execute, commandName = kDeleteCommandName, state = new GUIWrapperState() { hotControl = 30 } };

            yield return new CommandTestCase() { name = "CreateVertexMode_ValidateDeleteCommand", expected = true, mode = SpriteMeshViewMode.CreateVertex, commandEventType = CommandEventType.Validate, commandName = kDeleteCommandName, state = new GUIWrapperState() };
            yield return new CommandTestCase() { name = "CreateVertexMode_ExecuteDeleteCommand", expected = true, mode = SpriteMeshViewMode.CreateVertex, commandEventType = CommandEventType.Execute, commandName = kDeleteCommandName, state = new GUIWrapperState() };
            yield return new CommandTestCase() { name = "CreateVertexMode_OtherEvent", expected = false, mode = SpriteMeshViewMode.CreateVertex, commandEventType = CommandEventType.Other, state = new GUIWrapperState() };
            yield return new CommandTestCase() { name = "CreateVertexMode_ValidateDeleteCommand_AltIsDown", expected = false, mode = SpriteMeshViewMode.CreateVertex, commandEventType = CommandEventType.Validate, commandName = kDeleteCommandName, state = new GUIWrapperState() { isAltDown = true } };
            yield return new CommandTestCase() { name = "CreateVertexMode_ExecuteDeleteCommand_AltIsDown", expected = false, mode = SpriteMeshViewMode.CreateVertex, commandEventType = CommandEventType.Execute, commandName = kDeleteCommandName, state = new GUIWrapperState() { isAltDown = true } };
            yield return new CommandTestCase() { name = "CreateVertexMode_ValidateDeleteCommand_HotControlSet", expected = false, mode = SpriteMeshViewMode.CreateVertex, commandEventType = CommandEventType.Validate, commandName = kDeleteCommandName, state = new GUIWrapperState() { hotControl = 30 } };
            yield return new CommandTestCase() { name = "CreateVertexMode_ExecuteDeleteCommand_HotControlSet", expected = false, mode = SpriteMeshViewMode.CreateVertex, commandEventType = CommandEventType.Execute, commandName = kDeleteCommandName, state = new GUIWrapperState() { hotControl = 30 } };

            yield return new CommandTestCase() { name = "CreateEdgeMode_ValidateDeleteCommand", expected = true, mode = SpriteMeshViewMode.CreateEdge, commandEventType = CommandEventType.Validate, commandName = kDeleteCommandName, state = new GUIWrapperState() };
            yield return new CommandTestCase() { name = "CreateEdgeMode_ExecuteDeleteCommand", expected = true, mode = SpriteMeshViewMode.CreateEdge, commandEventType = CommandEventType.Execute, commandName = kDeleteCommandName, state = new GUIWrapperState() };
            yield return new CommandTestCase() { name = "CreateEdgeMode_OtherEvent", expected = false, mode = SpriteMeshViewMode.CreateEdge, commandEventType = CommandEventType.Other, state = new GUIWrapperState() };
            yield return new CommandTestCase() { name = "CreateEdgeMode_ValidateDeleteCommand_AltIsDown", expected = false, mode = SpriteMeshViewMode.CreateEdge, commandEventType = CommandEventType.Validate, commandName = kDeleteCommandName, state = new GUIWrapperState() { isAltDown = true } };
            yield return new CommandTestCase() { name = "CreateEdgeMode_ExecuteDeleteCommand_AltIsDown", expected = false, mode = SpriteMeshViewMode.CreateEdge, commandEventType = CommandEventType.Execute, commandName = kDeleteCommandName, state = new GUIWrapperState() { isAltDown = true } };
            yield return new CommandTestCase() { name = "CreateEdgeMode_ValidateDeleteCommand_HotControlSet", expected = false, mode = SpriteMeshViewMode.CreateEdge, commandEventType = CommandEventType.Validate, commandName = kDeleteCommandName, state = new GUIWrapperState() { hotControl = 30 } };
            yield return new CommandTestCase() { name = "CreateEdgeMode_ExecuteDeleteCommand_HotControlSet", expected = false, mode = SpriteMeshViewMode.CreateEdge, commandEventType = CommandEventType.Execute, commandName = kDeleteCommandName, state = new GUIWrapperState() { hotControl = 30 } };

            yield return new CommandTestCase() { name = "SplitEdgeMode_ValidateDeleteCommand", expected = true, mode = SpriteMeshViewMode.SplitEdge, commandEventType = CommandEventType.Validate, commandName = kDeleteCommandName, state = new GUIWrapperState() };
            yield return new CommandTestCase() { name = "SplitEdgeMode_ExecuteDeleteCommand", expected = true, mode = SpriteMeshViewMode.SplitEdge, commandEventType = CommandEventType.Execute, commandName = kDeleteCommandName, state = new GUIWrapperState() };
            yield return new CommandTestCase() { name = "SplitEdgeMode_OtherEvent", expected = false, mode = SpriteMeshViewMode.SplitEdge, commandEventType = CommandEventType.Other, state = new GUIWrapperState() };
            yield return new CommandTestCase() { name = "SplitEdgeMode_ValidateDeleteCommand_AltIsDown", expected = false, mode = SpriteMeshViewMode.SplitEdge, commandEventType = CommandEventType.Validate, commandName = kDeleteCommandName, state = new GUIWrapperState() { isAltDown = true } };
            yield return new CommandTestCase() { name = "SplitEdgeMode_ExecuteDeleteCommand_AltIsDown", expected = false, mode = SpriteMeshViewMode.SplitEdge, commandEventType = CommandEventType.Execute, commandName = kDeleteCommandName, state = new GUIWrapperState() { isAltDown = true } };
            yield return new CommandTestCase() { name = "SplitEdgeMode_ValidateDeleteCommand_HotControlSet", expected = false, mode = SpriteMeshViewMode.SplitEdge, commandEventType = CommandEventType.Validate, commandName = kDeleteCommandName, state = new GUIWrapperState() { hotControl = 30 } };
            yield return new CommandTestCase() { name = "SplitEdgeMode_ExecuteDeleteCommand_HotControlSet", expected = false, mode = SpriteMeshViewMode.SplitEdge, commandEventType = CommandEventType.Execute, commandName = kDeleteCommandName, state = new GUIWrapperState() { hotControl = 30 } };
        }

        [Test]
        public void Remove([ValueSource("RemoveCases")] CommandTestCase testCase)
        {
            SetupBaseTestCase(testCase);
            SetupCommandTestCase(testCase);


            bool result = m_SpriteMeshView.DoRemove();

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
                Assert.AreEqual(testCase.expected, m_SpriteMeshView.IsActionTriggered(MeshEditorAction.Remove));
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

        private static IEnumerable<ActionTestCase> CancelModeCases()
        {
            yield return new ActionTestCase() { name = "CreateVertexMode_RightClick", expected = true, mode = SpriteMeshViewMode.CreateVertex, state = new GUIWrapperState() { mouseDownButton = 1 } };
            yield return new ActionTestCase() { name = "CreateVertexMode_EscapeKey", expected = true, mode = SpriteMeshViewMode.CreateVertex, state = new GUIWrapperState() { keyDown = KeyCode.Escape } };
            yield return new ActionTestCase() { name = "CreateEdgeMode_RightClick", expected = true, mode = SpriteMeshViewMode.CreateEdge, state = new GUIWrapperState() { mouseDownButton = 1 } };
            yield return new ActionTestCase() { name = "CreateEdgeMode_EscapeKey", expected = true, mode = SpriteMeshViewMode.CreateEdge, state = new GUIWrapperState() { keyDown = KeyCode.Escape } };
            yield return new ActionTestCase() { name = "SplitEdgeMode_RightClick", expected = true, mode = SpriteMeshViewMode.SplitEdge, state = new GUIWrapperState() { mouseDownButton = 1 } };
            yield return new ActionTestCase() { name = "SplitEdgeMode_EscapeKey", expected = true, mode = SpriteMeshViewMode.SplitEdge, state = new GUIWrapperState() { keyDown = KeyCode.Escape } };
            yield return new ActionTestCase() { name = "SelectionMode_RightClick", expected = false, mode = SpriteMeshViewMode.EditGeometry, state = new GUIWrapperState() { mouseDownButton = 1 } };
            yield return new ActionTestCase() { name = "SelectionMode_EscapeKey", expected = false, mode = SpriteMeshViewMode.EditGeometry, state = new GUIWrapperState() { keyDown = KeyCode.Escape } };
        }

        [Test]
        public void CancelMode([ValueSource("CancelModeCases")] ActionTestCase testCase)
        {
            SetupBaseTestCase(testCase);
            m_SpriteMeshView.CancelMode();
            if (testCase.expected)
            {
                Assert.AreEqual(SpriteMeshViewMode.EditGeometry, m_SpriteMeshView.mode, "Mode did not change to Selection");
                guiWrapper.Received(1).UseCurrentEvent();
            }
            else
            {
                Assert.AreEqual(testCase.mode, m_SpriteMeshView.mode, "Mode should not change");
                guiWrapper.DidNotReceive().UseCurrentEvent();
            }
        }
    }
}
