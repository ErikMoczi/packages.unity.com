using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using NSubstitute;

namespace UnityEditor.Experimental.U2D.Animation.Test.AnalyticTests
{
    internal class AnalyticTests
    {
        AnimationAnalytics m_Analytics;
        SkinningEvents m_Events;
        IAnimationAnalyticsModel m_Model;
        IAnalyticsStorage m_Storage;
        const int k_TestInstanceId = 1001;
        Dictionary<Tools, ITool> m_Tools;

        void MockTools()
        {
            var enumValues = Enum.GetValues(typeof(Tools));
            m_Tools = new Dictionary<Tools, ITool>();
            foreach (var toolEnum in enumValues)
            {
                m_Tools.Add((Tools)toolEnum, Substitute.For<ITool>());
            }
        }

        [TearDown]
        public void TearDown()
        {
            m_Analytics.Dispose();
        }

        [SetUp]
        public void Setup()
        {
            MockTools();
            m_Events = new SkinningEvents();
            m_Model = Substitute.For<IAnimationAnalyticsModel>();
            m_Model.selectedTool.Returns(m_Tools[Tools.EditPose]);
            m_Model.GetTool(Arg.Any<Tools>()).Returns(x => m_Tools[x.Arg<Tools>()]);
            m_Model.applicationElapseTime.Returns(0);
            m_Storage = Substitute.For<IAnalyticsStorage>();
            m_Analytics = new AnimationAnalytics(m_Storage, m_Events, m_Model, 1001);
        }

        [Test]
        public void FirstEventIsDefaultSelectedTool()
        {
            m_Analytics.FlushEvent();
            m_Storage.Received(1).SendUsageEvent(
                Arg.Is<AnimationToolUsageEvent>(
                    evt =>
                        evt.animation_tool == AnimationToolType.PreviewPose && evt.animation_events.Count == 0)
            );
            m_Storage.DidNotReceive().SendApplyEvent(Arg.Any<AnimationToolApplyEvent>());
        }

        internal static IEnumerable<TestCaseData> ToolEnumCaseData()
        {
            var toolMapping = new Dictionary<Tools, AnimationToolType>()
            {
                {Tools.EditJoints, AnimationToolType.EditPose},
                {Tools.Visibility, AnimationToolType.Visibilility },
                {Tools.EditPose, AnimationToolType.PreviewPose },
                {Tools.CreateBone, AnimationToolType.CreateBone },
                {Tools.SplitBone, AnimationToolType.SplitBone },
                {Tools.ReparentBone, AnimationToolType.ReparentBone },
                {Tools.EditGeometry, AnimationToolType.EditGeometry },
                {Tools.CreateVertex, AnimationToolType.CreateVertex },
                {Tools.CreateEdge, AnimationToolType.CreateEdge },
                {Tools.SplitEdge, AnimationToolType.SplitEdge },
                {Tools.GenerateGeometry, AnimationToolType.GenerateGeometry },
                {Tools.WeightSlider, AnimationToolType.WeightSlider },
                {Tools.WeightBrush, AnimationToolType.WeightBrush },
                {Tools.BoneInfluence, AnimationToolType.BoneInfluence },
                {Tools.GenerateWeights, AnimationToolType.GenerateWeights }
            };
            foreach (var toolEnum in toolMapping)
            {
                yield return new TestCaseData(toolEnum.Key, toolEnum.Value);
            }
        }

        [Test, TestCaseSource("ToolEnumCaseData")]
        public void ToolChangeRegisterNewEvent(Tools toolEnums, AnimationToolType animationToolEnum)
        {
            m_Model.ClearReceivedCalls();
            m_Events.toolChanged.Invoke(m_Tools[toolEnums]);
            var nextTool = Tools.EditPose;
            if (toolEnums == nextTool)
                nextTool = Tools.GenerateGeometry;


            m_Events.toolChanged.Invoke(m_Tools[nextTool]);

            // Check storage received calls
            var calls = m_Storage.ReceivedCalls();
            Assert.AreEqual(2, calls.Count());
            var firstCallArg = (AnimationToolUsageEvent)calls.ElementAt(0).GetArguments()[0];
            Assert.IsTrue(firstCallArg.animation_tool == AnimationToolType.PreviewPose && firstCallArg.animation_events.Count == 0);
            firstCallArg = (AnimationToolUsageEvent)calls.ElementAt(1).GetArguments()[0];
            Assert.IsTrue(firstCallArg.animation_tool == animationToolEnum && firstCallArg.animation_events.Count == 0);
            m_Storage.DidNotReceive().SendApplyEvent(Arg.Any<AnimationToolApplyEvent>());
        }

        internal class AnimationUsageEventTestCaseData
        {
            public Action<SkinningEvents> eventCall;
            public AnimationToolUsageEvent expected;
            public string testName = "AnimationUsageEventTestCaseData";
            public bool IsEventSame(AnimationToolUsageEvent evt)
            {
                var passed = expected.animation_tool == evt.animation_tool;
                passed |= expected.animation_events.Count == evt.animation_events.Count;
                passed |= expected.instance_id == evt.instance_id;
                for (int i = 0; i < expected.animation_events.Count; ++i)
                {
                    var evt1 = expected.animation_events[i];
                    var evt2 = evt.animation_events[i];
                    passed |= evt1.data == evt2.data && evt1.repeated_event == evt2.repeated_event && evt1.repeated_event == evt2.repeated_event;
                }
                return passed;
            }

            public override string ToString()
            {
                return testName;
            }
        }

        internal static IEnumerable<AnimationUsageEventTestCaseData> EventSystemCases()
        {
            yield return new AnimationUsageEventTestCaseData()
            {
                eventCall = (eventSystem) => eventSystem.shortcut.Invoke("1"),
                expected = new AnimationToolUsageEvent()
                {
                    instance_id = k_TestInstanceId,
                    animation_tool = AnimationToolType.PreviewPose,
                    animation_events = new List<AnimationEvent>()
                    {
                        new AnimationEvent()
                        {
                            sub_type =  AnimationEventType.Shortcut,
                            repeated_event = 0
                        }
                    }
                },
                testName = "Shortcut Event"
            };

            yield return new AnimationUsageEventTestCaseData()
            {
                eventCall = (eventSystem) => eventSystem.paste.Invoke(true, true, true, true),
                expected = new AnimationToolUsageEvent()
                {
                    instance_id = k_TestInstanceId,
                    animation_tool = AnimationToolType.PreviewPose,
                    animation_events = new List<AnimationEvent>()
                    {
                        new AnimationEvent()
                        {
                            sub_type =  AnimationEventType.Paste,
                            repeated_event = 0,
                            data = "b:true m:true x:true y:true"
                        }
                    }
                },
                testName = "Paste Event"
            };

            yield return new AnimationUsageEventTestCaseData()
            {
                eventCall = (eventSystem) =>
                {
                    eventSystem.selectedSpriteChanged.Invoke(null);
                    eventSystem.selectedSpriteChanged.Invoke(null);
                },
                expected = new AnimationToolUsageEvent()
                {
                    instance_id = k_TestInstanceId,
                    animation_tool = AnimationToolType.PreviewPose,
                    animation_events = new List<AnimationEvent>()
                    {
                        new AnimationEvent()
                        {
                            sub_type =  AnimationEventType.Paste,
                            repeated_event = 0,
                            data = "b:true m:true x:true y:true"
                        }
                    }
                },
                testName = "Select Sprite Event Twice"
            };

            yield return new AnimationUsageEventTestCaseData()
            {
                eventCall = (eventSystem) =>
                {
                    eventSystem.skeletonPreviewPoseChanged.Invoke(null);
                },
                expected = new AnimationToolUsageEvent()
                {
                    instance_id = k_TestInstanceId,
                    animation_tool = AnimationToolType.PreviewPose,
                    animation_events = new List<AnimationEvent>()
                    {
                        new AnimationEvent()
                        {
                            sub_type =  AnimationEventType.Paste,
                            repeated_event = 0,
                            data = ""
                        }
                    }
                },
                testName = "Skeleton Preview Pose Event"
            };

            yield return new AnimationUsageEventTestCaseData()
            {
                eventCall = (eventSystem) =>
                {
                    eventSystem.skeletonBindPoseChanged.Invoke(null);
                },
                expected = new AnimationToolUsageEvent()
                {
                    instance_id = k_TestInstanceId,
                    animation_tool = AnimationToolType.PreviewPose,
                    animation_events = new List<AnimationEvent>()
                    {
                        new AnimationEvent()
                        {
                            sub_type =  AnimationEventType.SkeletonBindPoseChanged,
                            repeated_event = 0,
                            data = ""
                        }
                    }
                },
                testName = "Skeleton Bind Pose Event"
            };

            yield return new AnimationUsageEventTestCaseData()
            {
                eventCall = (eventSystem) =>
                {
                    eventSystem.skeletonTopologyChanged.Invoke(null);
                },
                expected = new AnimationToolUsageEvent()
                {
                    instance_id = k_TestInstanceId,
                    animation_tool = AnimationToolType.PreviewPose,
                    animation_events = new List<AnimationEvent>()
                    {
                        new AnimationEvent()
                        {
                            sub_type =  AnimationEventType.SkeletonTopologyChanged,
                            repeated_event = 0,
                            data = ""
                        }
                    }
                },
                testName = "Skeleton Topology Event"
            };


            yield return new AnimationUsageEventTestCaseData()
            {
                eventCall = (eventSystem) =>
                {
                    eventSystem.meshChanged.Invoke(null);
                },
                expected = new AnimationToolUsageEvent()
                {
                    instance_id = k_TestInstanceId,
                    animation_tool = AnimationToolType.PreviewPose,
                    animation_events = new List<AnimationEvent>()
                    {
                        new AnimationEvent()
                        {
                            sub_type =  AnimationEventType.MeshChanged,
                            repeated_event = 0,
                            data = ""
                        }
                    }
                },
                testName = "Mesh Changed Event"
            };

            yield return new AnimationUsageEventTestCaseData()
            {
                eventCall = (eventSystem) =>
                {
                    eventSystem.meshPreviewChanged.Invoke(null);
                },
                expected = new AnimationToolUsageEvent()
                {
                    instance_id = k_TestInstanceId,
                    animation_tool = AnimationToolType.PreviewPose,
                    animation_events = new List<AnimationEvent>()
                    {}
                },
                testName = "Mesh Preview Event"
            };

            yield return new AnimationUsageEventTestCaseData()
            {
                eventCall = (eventSystem) =>
                {
                    eventSystem.skinningModeChanged.Invoke(SkinningMode.Character);
                },
                expected = new AnimationToolUsageEvent()
                {
                    instance_id = k_TestInstanceId,
                    animation_tool = AnimationToolType.PreviewPose,
                    animation_events = new List<AnimationEvent>()
                    {
                        new AnimationEvent()
                        {
                            sub_type =  AnimationEventType.SkinningModuleModeChanged,
                            repeated_event = 0,
                            data = SkinningMode.Character.ToString()
                        }
                    }
                },
                testName = "Skinning Mode Event"
            };

            yield return new AnimationUsageEventTestCaseData()
            {
                eventCall = (eventSystem) =>
                {
                    eventSystem.boneSelectionChanged.Invoke();
                },
                expected = new AnimationToolUsageEvent()
                {
                    instance_id = k_TestInstanceId,
                    animation_tool = AnimationToolType.PreviewPose,
                    animation_events = new List<AnimationEvent>()
                    {
                        new AnimationEvent()
                        {
                            sub_type =  AnimationEventType.BoneSelectionChanged,
                            repeated_event = 0,
                            data = "0"
                        }
                    }
                },
                testName = "Bone Selection Changed Event"
            };

            yield return new AnimationUsageEventTestCaseData()
            {
                eventCall = (eventSystem) =>
                {
                    eventSystem.boneNameChanged.Invoke(null);
                    eventSystem.boneDepthChanged.Invoke(null);
                },
                expected = new AnimationToolUsageEvent()
                {
                    instance_id = k_TestInstanceId,
                    animation_tool = AnimationToolType.PreviewPose,
                    animation_events = new List<AnimationEvent>()
                    {
                        new AnimationEvent()
                        {
                            sub_type =  AnimationEventType.BoneNameChanged,
                            repeated_event = 0,
                            data = ""
                        },
                        new AnimationEvent()
                        {
                            sub_type =  AnimationEventType.BoneDepthChanged,
                            repeated_event = 0,
                            data = ""
                        }
                    }
                },
                testName = "Bone Name And Depth Event"
            };

            yield return new AnimationUsageEventTestCaseData()
            {
                eventCall = (eventSystem) =>
                {
                    eventSystem.characterPartChanged.Invoke(null);
                },
                expected = new AnimationToolUsageEvent()
                {
                    instance_id = k_TestInstanceId,
                    animation_tool = AnimationToolType.PreviewPose,
                    animation_events = new List<AnimationEvent>()
                    {
                        new AnimationEvent()
                        {
                            sub_type =  AnimationEventType.CharacterPartChanged,
                            repeated_event = 0,
                            data = ""
                        }
                    }
                },
                testName = "Bone Selection Changed Event"
            };

            yield return new AnimationUsageEventTestCaseData()
            {
                eventCall = (eventSystem) =>
                {
                    eventSystem.restoreBindPose.Invoke();
                },
                expected = new AnimationToolUsageEvent()
                {
                    instance_id = k_TestInstanceId,
                    animation_tool = AnimationToolType.PreviewPose,
                    animation_events = new List<AnimationEvent>()
                    {
                        new AnimationEvent()
                        {
                            sub_type =  AnimationEventType.RestoreBindPose,
                            repeated_event = 0,
                            data = ""
                        }
                    }
                },
                testName = "Restore Bind Pose Event"
            };

            yield return new AnimationUsageEventTestCaseData()
            {
                eventCall = (eventSystem) =>
                {
                    eventSystem.copy.Invoke();
                },
                expected = new AnimationToolUsageEvent()
                {
                    instance_id = k_TestInstanceId,
                    animation_tool = AnimationToolType.PreviewPose,
                    animation_events = new List<AnimationEvent>()
                    {
                        new AnimationEvent()
                        {
                            sub_type =  AnimationEventType.Copy,
                            repeated_event = 0,
                            data = ""
                        }
                    }
                },
                testName = "Copy Event"
            };

            yield return new AnimationUsageEventTestCaseData()
            {
                eventCall = (eventSystem) =>
                {
                    eventSystem.boneVisibility.Invoke("rename");
                },
                expected = new AnimationToolUsageEvent()
                {
                    instance_id = k_TestInstanceId,
                    animation_tool = AnimationToolType.PreviewPose,
                    animation_events = new List<AnimationEvent>()
                    {
                        new AnimationEvent()
                        {
                            sub_type =  AnimationEventType.Visibility,
                            repeated_event = 0,
                            data = "rename"
                        }
                    }
                },
                testName = "Bone Visibility Event"
            };
        }

        [Test, TestCaseSource("EventSystemCases")]
        public void AnimationUsageEventCapturedInCurrentToolEvent(AnimationUsageEventTestCaseData testCase)
        {
            testCase.eventCall(m_Events);
            m_Events.toolChanged.Invoke(m_Tools[Tools.ReparentBone]);

            m_Storage.Received(1).SendUsageEvent(
                Arg.Is<AnimationToolUsageEvent>(
                    evt => testCase.IsEventSame(evt)
                )
            );
        }

        [Test]
        public void AnimationUsageEventTruncatedWhenHitElementLimit()
        {
            for (int i = 0; i < AnalyticConstant.k_MaxNumberOfElements; ++i)
                m_Events.shortcut.Invoke(i.ToString());
            m_Events.toolChanged.Invoke(m_Tools[Tools.ReparentBone]);
            m_Storage.Received(1).SendUsageEvent(
                Arg.Is<AnimationToolUsageEvent>(
                    evt =>
                        evt.animation_tool == AnimationToolType.PreviewPose &&
                        evt.animation_events.Count < AnalyticConstant.k_MaxNumberOfElements &&
                        evt.animation_events[evt.animation_events.Count - 1].sub_type == AnimationEventType.Truncated
                )
            );
        }

        [Test]
        public void AnimationApplyEventHasCorrectBoneStatistics()
        {
            var bones = new[]
            {
                Substitute.For<BoneCache>(),
                Substitute.For<BoneCache>(),
                Substitute.For<BoneCache>(),
                Substitute.For<BoneCache>(),
                Substitute.For<BoneCache>(),
                Substitute.For<BoneCache>(),
                Substitute.For<BoneCache>(),
            };
            // Creating the following structure
            // | |
            //   | \  \
            //   |  \

            bones[1].parentBone.Returns(bones[0]);
            bones[0].chainedChild.Returns(bones[1]);
            bones[2].parentBone.Returns(bones[1]);
            bones[1].chainedChild.Returns(bones[2]);

            bones[3].parentBone.Returns(bones[0]);
            bones[4].parentBone.Returns(bones[3]);
            bones[3].chainedChild.Returns(bones[4]);

            bones[5].parentBone.Returns(bones[0]);

            m_Analytics.SendApplyEvent(5, new[] {1, 2, 3, 4, 5}, bones);

            m_Storage.Received(1).SendApplyEvent(Arg.Is<AnimationToolApplyEvent>(x =>
                x.bone_chain_count.Length == 2 &&
                x.bone_chain_count[0] == 2 &&
                x.bone_chain_count[1] == 0 &&
                x.bone_sprite_count.Length == 5 &&
                x.bone_root_count == 2 &&
                x.bone_depth.Length == 2 &&
                x.bone_depth[0] == 3 &&
                x.bone_depth[1] == 1 &&
                x.bone_count.Length == 2 &&
                x.bone_count[0] == 6 &&
                x.bone_count[1] == 1
            ));
        }
    }
}
