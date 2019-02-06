using System;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.TestTools;

namespace UnityEditor.Experimental.U2D.Animation.Test.VisibilityToolTests
{
    class TestWindow : EditorWindow {}

    public class VisibilityToolWindowTests
    {
        VisibilityToolWindow m_VisibilityToolView;
        TestWindow m_Window;

        [SetUp]
        public void Setup()
        {
            m_VisibilityToolView = VisibilityToolWindow.CreateFromUXML();
            m_Window = EditorWindow.GetWindow<TestWindow>();
            m_Window.rootVisualElement.Add(m_VisibilityToolView);
        }

        [TearDown]
        public void TearDown()
        {
            m_Window.Close();
        }

        [Test]
        public void OnBoneOpacitySliderValueChange_TriggersEvent()
        {
            float boneOpacityValue = 0;
            bool callback = false;
            m_VisibilityToolView.onBoneOpacitySliderChange += x =>
            {
                boneOpacityValue = x;
                callback = true;
            };

            var slider = m_VisibilityToolView.Q<Slider>("BoneOpacitySlider");
            slider.value = 1.0f;

            Assert.IsTrue(callback);
            Assert.IsTrue(Mathf.Approximately(1.0f, boneOpacityValue));
        }

        [Test]
        public void SetBoneOpacity_SetsSliderValue()
        {
            m_VisibilityToolView.SetBoneOpacitySliderValue(1);
            var slider = m_VisibilityToolView.Q<Slider>("BoneOpacitySlider");
            Assert.IsTrue(Mathf.Approximately(1.0f, slider.value));
        }

        [Test]
        public void OnMeshOpacitySliderValueChange_TriggersEvent()
        {
            float boneOpacityValue = 0;
            bool callback = false;
            m_VisibilityToolView.onMeshOpacitySliderChange += x =>
            {
                boneOpacityValue = x;
                callback = true;
            };

            var slider = m_VisibilityToolView.Q<Slider>("MeshOpacitySlider");
            slider.value = 1.0f;

            Assert.IsTrue(callback);
            Assert.IsTrue(Mathf.Approximately(1.0f, boneOpacityValue));
        }

        [Test]
        public void SetMeshOpacity_SetsSliderValue()
        {
            m_VisibilityToolView.SetMeshOpacitySliderValue(1);
            var slider = m_VisibilityToolView.Q<Slider>("MeshOpacitySlider");
            Assert.IsTrue(Mathf.Approximately(1.0f, slider.value));
        }

        [Test]
        public void SetActiveTabWhenNoTabAddedDoesNotThrowException()
        {
            m_VisibilityToolView.SetActiveTab(0);
            LogAssert.NoUnexpectedReceived();
            m_VisibilityToolView.SetActiveTab(-1);
            LogAssert.NoUnexpectedReceived();
            m_VisibilityToolView.SetActiveTab(1);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SetToolAvailableWhenNoTabAddedDoesNotThrowException()
        {
            m_VisibilityToolView.SetActiveTab(0);
            LogAssert.NoUnexpectedReceived();
            m_VisibilityToolView.SetActiveTab(-1);
            LogAssert.NoUnexpectedReceived();
            m_VisibilityToolView.SetActiveTab(1);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SetActiveTabWithOneTabAddedDoesNotThrowException()
        {
            m_VisibilityToolView.AddToolTab("Test tab", () => {});
            m_VisibilityToolView.SetActiveTab(0);
            LogAssert.NoUnexpectedReceived();
            m_VisibilityToolView.SetActiveTab(-1);
            LogAssert.NoUnexpectedReceived();
            m_VisibilityToolView.SetActiveTab(1);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SetToolAvailableWithOneTabAddedDoesNotThrowException()
        {
            m_VisibilityToolView.AddToolTab("Test tab", () => {});
            m_VisibilityToolView.SetActiveTab(0);
            LogAssert.NoUnexpectedReceived();
            m_VisibilityToolView.SetActiveTab(-1);
            LogAssert.NoUnexpectedReceived();
            m_VisibilityToolView.SetActiveTab(1);
            LogAssert.NoUnexpectedReceived();
        }
    }

    public class VisibilityToolControllerTests
    {
        VisibilityToolController m_Controller;
        IVisibilityToolModel m_Model;
        IVisibilityToolWindow m_View;
        IVisibilityTool[] m_Tools;

        [SetUp]
        public void Setup()
        {
            m_Model = Substitute.For<IVisibilityToolModel>();
            m_View = Substitute.For<IVisibilityToolWindow>();
            m_Tools = new IVisibilityTool[]
            {
                Substitute.For<IVisibilityTool>(),
                Substitute.For<IVisibilityTool>(),
                Substitute.For<IVisibilityTool>()
            };
            m_Tools[0].isAvailable.Returns(false);
            m_Tools[1].isAvailable.Returns(true);
            m_Tools[2].isAvailable.Returns(true);
            int index = 0;
            foreach (var tool in m_Tools)
            {
                tool.name.Returns(string.Format("Tool {0}", index++));
            }
            m_Model.skinningCache.Returns((SkinningCache)null);
            m_Model.view.Returns(m_View);
            m_Model.meshOpacityValue.Returns(0.66f);
            m_Model.boneOpacityValue.Returns(0.66f);
        }

        [Test]
        public void ControllerInitializesToolCorrectly()
        {
            m_Controller = new VisibilityToolController(m_Model, m_Tools);
            foreach (var tool in m_Tools)
            {
                tool.Received(1).SetAvailabilityChangeCallback(Arg.Any<Action>());
                tool.Received(1).Setup();
            }

            m_Model.view.Received(m_Tools.Length).AddToolTab(Arg.Any<string>(), Arg.Any<Action>());
            m_Model.view.Received(m_Tools.Length).SetToolAvailable(Arg.Any<int>(), Arg.Any<bool>());
            for (int i = 0; i < m_Tools.Length; ++i)
            {
                var t = m_Tools[i];
                m_Model.view.Received().AddToolTab(Arg.Is<string>(x => x == t.name), Arg.Any<Action>());
                m_Model.view.Received().SetToolAvailable(Arg.Is<int>(i), Arg.Is<bool>(t.isAvailable));
            }
        }

        [Test]
        public void CurrentToolIndexIsSetToFirstAvailableTool()
        {
            m_Controller = new VisibilityToolController(m_Model, m_Tools);
            m_Model.Received(1).currentToolIndex = 1;
        }

        [Test]
        public void ControllerActivateWithCorrectSetup()
        {
            m_Model.skinningCache.Returns((SkinningCache)null);

            m_Controller = new VisibilityToolController(m_Model, m_Tools);
            m_Controller.Activate();
            m_Model.view.Received(1).Show();
            m_Model.view.DidNotReceive().Hide();
            m_Model.view.Received(1).SetBoneOpacitySliderValue(Arg.Is(m_Model.boneOpacityValue));
            m_Model.view.Received(1).SetMeshOpacitySliderValue(Arg.Is(m_Model.meshOpacityValue));

            m_Tools[2].DidNotReceive().Activate();
            m_Tools[0].DidNotReceive().Activate();
            m_Tools[1].Received(1).Activate();

            m_Model.view.Received(1).SetActiveTab(Arg.Is(m_Model.currentToolIndex));

            // Check if action is setup properly to route data from view to model
            m_Model.view.onMeshOpacitySliderChange += Raise.Event<Action<float>>(0.33f);
            m_Model.view.onBoneOpacitySliderChange += Raise.Event<Action<float>>(0.33f);
            m_Model.Received(1).boneOpacityValue = 0.33f;
            m_Model.Received(1).meshOpacityValue = 0.33f;
        }

        [Test]
        public void ControllerDeactivateWithCorrectTeardown()
        {
            m_Model.skinningCache.Returns((SkinningCache)null);
            m_Controller = new VisibilityToolController(m_Model, m_Tools);
            m_Controller.Deactivate();
            m_Model.view.Received(1).Hide();
            m_Model.view.DidNotReceive().Show();
            m_Tools[2].DidNotReceive().Deactivate();
            m_Tools[0].DidNotReceive().Deactivate();
            m_Tools[1].Received(1).Deactivate();

            // Check if action is teardown properly to route data from view to model
            m_Model.view.onMeshOpacitySliderChange += Raise.Event<Action<float>>(0.33f);
            m_Model.view.onBoneOpacitySliderChange += Raise.Event<Action<float>>(0.33f);
            m_Model.DidNotReceive().boneOpacityValue = 0.33f;
            m_Model.DidNotReceive().meshOpacityValue = 0.33f;
        }

        [Test]
        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(2, true)]
        public void OnToolChangeCorrectToolIsActivated(int toolIndex, bool shouldChange)
        {
            Action callback = null;
            m_Model.skinningCache.Returns((SkinningCache)null);
            m_Model.view.AddToolTab(Arg.Is(string.Format("Tool {0}", toolIndex)), Arg.Do<Action>(x => callback = x));
            m_Controller = new VisibilityToolController(m_Model, m_Tools);
            m_Controller.Activate();

            m_View.ClearReceivedCalls();
            m_Model.ClearReceivedCalls();
            m_Tools[toolIndex].ClearReceivedCalls();
            callback();
            if (shouldChange)
            {
                m_Tools[toolIndex].Received(1).Activate();
                m_View.Received(1).SetActiveTab(Arg.Is(toolIndex));
                m_Model.Received(1).currentToolIndex = toolIndex;
            }
            else
            {
                m_Tools[toolIndex].DidNotReceive().Activate();
                m_View.DidNotReceive().SetActiveTab(Arg.Any<int>());
                m_Model.DidNotReceive().currentToolIndex = toolIndex;
            }
        }

        [Test, TestCase(new[] {false, true, true}, 0, 1)]
        [TestCase(new[] {false, true, true}, 1, 2)]
        [TestCase(new[] {true, true, true}, 1, 0)]
        public void ToolAvailabilityChangeActivatesCorrectTool(bool[] startAvailability, int toolChange, int expectedNewTool)
        {
            Action[] callback = new Action[m_Tools.Length];
            for (int i = 0; i < m_Tools.Length; ++i)
            {
                var index = i;
                m_Tools[i].isAvailable.Returns(startAvailability[i]);
                m_Tools[i].SetAvailabilityChangeCallback(Arg.Do<Action>(x => callback[index] = x));
            }

            m_Controller = new VisibilityToolController(m_Model, m_Tools);
            m_View.ClearReceivedCalls();
            m_Model.ClearReceivedCalls();
            m_Controller.Activate();
            m_Tools[toolChange].isAvailable.Returns(!startAvailability[toolChange]);
            callback[toolChange]();

            m_View.Received(1).SetToolAvailable(Arg.Is(toolChange), Arg.Is(!startAvailability[toolChange]));
            if (expectedNewTool >= 0)
            {
                m_View.Received(1).SetActiveTab(Arg.Is(expectedNewTool));
                m_Model.Received(1).currentToolIndex = expectedNewTool;
            }
        }
    }
}
