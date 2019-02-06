using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace  UnityEditor.Experimental.U2D.Animation.Test.BoneVisibilityTests
{
    internal class BoneTreeWidgetControllerTests
    {
        IBoneTreeViewModel m_Model;
        SkinningEvents m_EventSystem;
        BoneTreeWidgetController m_Controller;
        IBoneVisibilityToolView m_View;

        [SetUp]
        public void Setup()
        {
            m_View = Substitute.For<IBoneVisibilityToolView>();
            m_Model = Substitute.For<IBoneTreeViewModel>();
            m_Model.view.Returns(m_View);
            m_EventSystem = Substitute.For<SkinningEvents>();
            m_EventSystem.selectedSpriteChanged.Returns(Substitute.For<SkinningEvents.SpriteEvent>());
            m_EventSystem.skinningModeChanged.Returns(Substitute.For<SkinningEvents.SkinningModuleModeEvent>());
            m_EventSystem.boneSelectionChanged.Returns(Substitute.For<SkinningEvents.BoneSelectionEvent>());
            m_EventSystem.boneNameChanged.Returns(Substitute.For<SkinningEvents.BoneEvent>());
            m_EventSystem.skeletonTopologyChanged.Returns(Substitute.For<SkinningEvents.SkeletonEvent>());
            m_EventSystem.boneVisibility.Returns(Substitute.For<SkinningEvents.BoneVisibilityEvent>());
            m_Controller = new BoneTreeWidgetController(m_Model, m_EventSystem);
        }

        [Test]
        public void ContollerActivateSetupCorrectly()
        {
            m_Controller.Activate();
            m_View.Received(1).OnSelectionChange(Arg.Any<SkeletonCache>());
            m_View.Received(1).OnBoneSelectionChange(Arg.Any<SkeletonSelection>());
            m_EventSystem.selectedSpriteChanged.Received(1).AddListener(Arg.Any<UnityAction<SpriteCache>>());
            m_EventSystem.skinningModeChanged.Received(1).AddListener(Arg.Any<UnityAction<SkinningMode>>());
            m_EventSystem.boneSelectionChanged.Received(1).AddListener(Arg.Any<UnityAction>());
            m_EventSystem.boneNameChanged.Received(1).AddListener(Arg.Any<UnityAction<BoneCache>>());
            m_EventSystem.skeletonTopologyChanged.Received(1).AddListener(Arg.Any<UnityAction<SkeletonCache>>());
        }

        [Test]
        public void ContollerActivateTearDownCorrectly()
        {
            m_Controller.Activate();
            m_Controller.Deactivate();
            LogAssert.NoUnexpectedReceived();

            // Unable to test these. Need to dig in more
            //m_EventSystem.selectedSpriteChanged.Received(1).RemoveListener(mm);
            //m_EventSystem.skinningModeChanged.Received(1).RemoveListener(Arg.Any<UnityAction<SkinningMode>>());
            //m_EventSystem.boneSelectionChanged.Received(1).RemoveListener(Arg.Any<UnityAction>());
            //m_EventSystem.boneNameChanged.Received(1).RemoveListener(Arg.Any<UnityAction<BoneCache>>());
            //m_EventSystem.skeletonTopologyChanged.Received(1).RemoveListener(Arg.Any<UnityAction<SkeletonCache>>());
        }

        [Test]
        public void OnSelectionChange_InformsViewOnChange()
        {
            m_Controller.Activate();
            SpriteCache sprite = Substitute.For<SpriteCache>();

            m_Model.GetAllVisibility().Returns(false);
            var skeletonCache = Substitute.For<SkeletonCache>();
            m_Model.GetSelectedSkeleton().Returns(skeletonCache);
            m_Model.ClearReceivedCalls();
            m_View.ClearReceivedCalls();
            m_EventSystem.selectedSpriteChanged.Invoke(sprite);
            m_Model.Received(1).GetSelectedSkeleton();
            m_Model.Received(1).GetBoneSeletion();
            m_View.Received(1).OnSelectionChange(Arg.Is<SkeletonCache>(x => x == skeletonCache));
            m_View.Received(1).OnBoneSelectionChange(Arg.Any<SkeletonSelection>());
        }

        [Test]
        public void OnSkinningModuleModeChanged_InformViewOnChange()
        {
            m_Controller.Activate();

            m_Model.GetAllVisibility().Returns(false);
            var skeletonCache = Substitute.For<SkeletonCache>();
            m_Model.GetSelectedSkeleton().Returns(skeletonCache);
            m_Model.ClearReceivedCalls();
            m_View.ClearReceivedCalls();
            m_EventSystem.skinningModeChanged.Invoke(SkinningMode.Character);
            m_Model.Received(1).GetSelectedSkeleton();
            m_Model.Received(1).GetBoneSeletion();
            m_View.Received(1).OnSelectionChange(Arg.Is<SkeletonCache>(x => x == skeletonCache));
            m_View.Received(1).OnBoneSelectionChange(Arg.Any<SkeletonSelection>());
        }

        [Test]
        public void OnBoneSelectionChanged_InformViewOnChange()
        {
            m_Controller.Activate();
            m_Model.ClearReceivedCalls();
            m_View.ClearReceivedCalls();
            m_EventSystem.boneSelectionChanged.Invoke();
            m_Model.Received(1).GetBoneSeletion();
            m_View.Received(1).OnBoneSelectionChange(Arg.Any<SkeletonSelection>());
        }

        [Test]
        public void OnBoneNameChanged_InformViewOnChange()
        {
            m_Controller.Activate();
            m_Model.ClearReceivedCalls();
            m_View.ClearReceivedCalls();
            var bone = Substitute.For<BoneCache>();
            m_EventSystem.boneNameChanged.Invoke(bone);
            m_View.Received(1).OnBoneNameChanged(Arg.Any<BoneCache>());
        }

        [Test]
        public void SkeletonTopologyChanged_InformViewOnChange()
        {
            m_Controller.Activate();
            m_Model.ClearReceivedCalls();
            m_View.ClearReceivedCalls();
            var skeleton = new SkeletonCache();
            m_EventSystem.skeletonTopologyChanged.Invoke(skeleton);
            m_View.Received(1).OnSelectionChange(Arg.Any<SkeletonCache>());
        }

        [Test]
        public void BuildTreeView_CreatesCorrectTreeView()
        {
            m_Controller.Activate();
            var skeleton = Substitute.For<SkeletonCache>();
            var bones = new[]
            {
                Substitute.For<BoneCache>(),
                Substitute.For<BoneCache>(),
                Substitute.For<BoneCache>(),
                Substitute.For<BoneCache>(),
            };
            bones[0].name = "Bone_1";
            bones[1].name = "Bone_1_1";
            bones[2].name = "Bone_1_1_1";
            bones[3].name = "Bone_2";

            var b = bones[2];
            b.parentBone.Returns(bones[1]);
            b = bones[1];
            b.parentBone.Returns(bones[0]);
            bones[0].siblingIndex.Returns(0);
            bones[1].siblingIndex.Returns(0);
            bones[2].siblingIndex.Returns(0);
            bones[3].siblingIndex.Returns(1);

            skeleton.bones.Returns(bones);
            m_Model.GetSelectedSkeleton().Returns(skeleton);
            var treeViewItem = m_Controller.BuildTreeView();
            Assert.AreEqual(bones.Length, treeViewItem.Count);
            for (int i = 0; i < bones.Length; ++i)
            {
                Assert.AreEqual(bones[i].name, treeViewItem[i].displayName);
                int expectedDepth = 0;
                var parentBone = bones[i].parentBone;
                while (parentBone != null)
                {
                    ++expectedDepth;
                    parentBone = parentBone.parentBone;
                }
                Assert.AreEqual(expectedDepth, treeViewItem[i].depth);
            }
        }

        [Test]
        public void GetIDsToExpand_ReturnsCorrectIdsForTreeView()
        {
            m_Controller.Activate();
            var bones = new[]
            {
                Substitute.For<BoneCache>(),
                Substitute.For<BoneCache>(),
                Substitute.For<BoneCache>(),
            };
            var b = bones[2];
            b.parentBone.Returns(bones[1]);
            b = bones[1];
            b.parentBone.Returns(bones[0]);

            var ids = m_Controller.GetIDsToExpand(new[] { bones[2] });
            // child bone doesn't need to be expanded
            Assert.AreEqual(bones.Length - 1 , ids.Count);
            for (int i = 0; i < bones.Length - 1; ++i)
            {
                Assert.IsTrue(ids.Contains(bones[i].GetInstanceID()));
            }
        }

        [Test]
        public void GetIDsToSelect_ReturnsCorrectIdsForTreeView()
        {
            m_Controller.Activate();
            var bones = new[]
            {
                Substitute.For<BoneCache>(),
                Substitute.For<BoneCache>(),
                Substitute.For<BoneCache>(),
            };
            var b = bones[2];
            b.parentBone.Returns(bones[1]);
            b = bones[1];
            b.parentBone.Returns(bones[0]);

            var ids = m_Controller.GetIDsToSelect(bones);
            // child bone doesn't need to be expanded
            Assert.AreEqual(bones.Length, ids.Length);
            for (int i = 0; i < bones.Length; ++i)
            {
                Assert.IsTrue(ids.Contains(bones[i].GetInstanceID()));
            }
        }

        [Test]
        public void SelectBones_InvokeEventOnBoneSelection()
        {
            var bone1 = Substitute.For<BoneCache>();
            var bone2 = Substitute.For<BoneCache>();
            bone1.name = "test_name";
            var treeViewList = new List<TreeViewItem>()
            {
                new TreeViewItemBase<BoneCache>(bone1.GetInstanceID(), 0, "test", bone1),
                new TreeViewItemBase<BoneCache>(bone2.GetInstanceID(), 0, "test", bone2)
            };
            treeViewList[0].AddChild(treeViewList[1]);
            bool eventCalled = false;
            UnityAction callback = () =>
            {
                eventCalled = true;
            };
            m_EventSystem.boneSelectionChanged.AddListener(callback);
            BoneCache[] bones = null;
            m_Model.SelectBones(Arg.Do<BoneCache[]>(x => bones = x));
            m_Controller.SelectBones(new[] { bone1.GetInstanceID() }, treeViewList);
            m_EventSystem.boneSelectionChanged.RemoveListener(callback);

            Assert.IsTrue(eventCalled);
            Assert.AreEqual(1, bones.Length);
        }

        [Test]
        public void GetTreeItemVisibility_GetsValiueFromModel()
        {
            m_Controller.Activate();
            var boneCache = CacheObject.Create<BoneCache>(null);
            var treeViewItem = new TreeViewItemBase<BoneCache>(0 , 0, "test", boneCache);
            treeViewItem.customData.isVisible = false;
            m_Model.GetVisibility(Arg.Is<BoneCache>(x => x == boneCache)).Returns(true);
            Assert.AreEqual(true, m_Controller.GetTreeItemVisibility(treeViewItem));

            treeViewItem.customData.isVisible = true;
            m_Model.GetVisibility(Arg.Is<BoneCache>(x => x == boneCache)).Returns(false);
            Assert.AreEqual(false, m_Controller.GetTreeItemVisibility(treeViewItem));
        }

        [Test]
        public void SetTreeItemVisibility_DoesNotIncludeChildren()
        {
            var bone1 = Substitute.For<BoneCache>();
            var bone2 = Substitute.For<BoneCache>();
            var treeViewItem1 = new TreeViewItemBase<BoneCache>(0 , 0, "test", bone1);
            var treeViewItem2 = new TreeViewItemBase<BoneCache>(0 , 0, "test", bone2);
            treeViewItem1.AddChild(treeViewItem2);
            m_Controller.SetTreeItemVisibility(treeViewItem1, false, false);
            m_Model.Received(1).SetVisibility(Arg.Any<BoneCache>(), false);
            m_Model.Received(1).SetVisibility(Arg.Is<BoneCache>(x => x == bone1), false);
        }

        [Test]
        public void SetTreeItemVisibility_IncludeChildren()
        {
            var bone1 = Substitute.For<BoneCache>();
            var bone2 = Substitute.For<BoneCache>();
            var treeViewItem1 = new TreeViewItemBase<BoneCache>(0, 0, "test", bone1);
            var treeViewItem2 = new TreeViewItemBase<BoneCache>(0, 0, "test", bone2);
            treeViewItem1.AddChild(treeViewItem2);
            m_Controller.SetTreeItemVisibility(treeViewItem1, false, true);
            m_Model.Received(2).SetVisibility(Arg.Any<BoneCache>(), false);
            m_Model.Received(1).SetVisibility(Arg.Is<BoneCache>(x => x == bone1), false);
            m_Model.Received(1).SetVisibility(Arg.Is<BoneCache>(x => x == bone2), false);
        }

        [Test]
        public void GetTreeItemDepthValue()
        {
            m_Controller.Activate();
            var boneCache = CacheObject.Create<BoneCache>(null);
            var treeViewItem = new TreeViewItemBase<BoneCache>(0, 0, "test", boneCache);
            treeViewItem.customData.depth = 5;
            m_Model.GetDepth(Arg.Is<BoneCache>(x => x == boneCache)).Returns(10);
            Assert.AreEqual(10, m_Controller.GetTreeItemDepthValue(treeViewItem));
        }

        [Test]
        public void SetTreeItemDepthValue()
        {
            m_Controller.Activate();
            var boneCache = CacheObject.Create<BoneCache>(null);
            var treeViewItem = new TreeViewItemBase<BoneCache>(0, 0, "test", boneCache);
            treeViewItem.customData.depth = 5;
            m_Controller.SetTreeItemDepthValue(treeViewItem, 100);
            m_Model.Received(1).SetDepth(Arg.Any<BoneCache>(), Arg.Any<int>());
            m_Model.Received(1).SetDepth(Arg.Is<BoneCache>(x => x == boneCache), 100);
        }

        [Test]
        public void SetTreeViewBoneName()
        {
            var bone1 = Substitute.For<BoneCache>();
            var bone2 = Substitute.For<BoneCache>();
            bone1.name = "test_name";
            var treeViewList = new List<TreeViewItem>()
            {
                new TreeViewItemBase<BoneCache>(0, 0, "test", bone1),
                new TreeViewItemBase<BoneCache>(0, 0, "test", bone2)
            };
            treeViewList[0].AddChild(treeViewList[1]);
            m_Controller.SetTreeViewBoneName(treeViewList, bone1);
            Assert.AreEqual("test_name", treeViewList[0].displayName);
            Assert.AreEqual("test", treeViewList[1].displayName);
        }

        [Test]
        public void TreeViewItemRename()
        {
            var bone1 = Substitute.For<BoneCache>();
            var bone2 = Substitute.For<BoneCache>();
            var treeViewList = new List<TreeViewItem>()
            {
                new TreeViewItemBase<BoneCache>(bone1.GetInstanceID(), 0, "test", bone1),
                new TreeViewItemBase<BoneCache>(bone2.GetInstanceID(), 0, "test", bone2)
            };

            var nameEventChangeSent = false;
            UnityAction<BoneCache> callback = x =>
            {
                nameEventChangeSent = x == bone1;
            };
            m_EventSystem.boneNameChanged.AddListener(callback);
            m_Controller.TreeViewItemRename(treeViewList, bone1.GetInstanceID(), "test_name");
            m_EventSystem.boneNameChanged.RemoveListener(callback);
            Assert.AreEqual("test_name", treeViewList[0].displayName);
            Assert.AreEqual("test", treeViewList[1].displayName);
            Assert.IsTrue(nameEventChangeSent);
            m_Model.Received(1).SetName(Arg.Any<BoneCache>(), Arg.Any<string>());
            m_Model.Received(1).SetName(Arg.Is<BoneCache>(x => x == bone1), "test_name");
        }

        [Test]
        [TestCase(0, new[] {0}, false, TestName = "Reparent root to root")]
        [TestCase(0, new[] {1, 2}, true, TestName = "Reparent item already parented to root")]
        [TestCase(0, new[] {2, 3}, true, TestName = "Reparent items does not same parent")]
        [TestCase(3, new[] {1, 2}, true, TestName = "Reparent child to root")]
        [TestCase(1, new[] {3}, true, TestName = "Reparent root to non root")]
        [TestCase(1, new[] {2}, true, TestName = "Reparent item already parented")]
        [TestCase(1, new[] {0}, false, TestName = "Reparent parent to children")]
        public void CanReparent(int parentIndex, int[] draggedItemIndex, bool expected)
        {
            var skeleton = Substitute.For<SkeletonCache>();
            var bones = new[]
            {
                Substitute.For<BoneCache>(),
                Substitute.For<BoneCache>(),
                Substitute.For<BoneCache>(),
                Substitute.For<BoneCache>(),
            };
            bones[0].name = "Bone_1";
            bones[1].name = "Bone_1_1";
            bones[2].name = "Bone_1_1_1";
            bones[3].name = "Bone_2";

            var b = bones[2];
            b.parentBone.Returns(bones[1]);
            b = bones[1];
            b.parentBone.Returns(bones[0]);
            bones[0].siblingIndex.Returns(0);
            bones[1].siblingIndex.Returns(0);
            bones[2].siblingIndex.Returns(0);
            bones[3].siblingIndex.Returns(1);

            skeleton.bones.Returns(bones);
            m_Model.GetSelectedSkeleton().Returns(skeleton);
            var treeViewItem = m_Controller.BuildTreeView();
            treeViewItem[0].AddChild(treeViewItem[1]);
            treeViewItem[1].AddChild(treeViewItem[2]);

            var parentItem = treeViewItem[parentIndex] as TreeViewItemBase<BoneCache>;
            var draggedItem = new List<TreeViewItem>();
            foreach (var i in draggedItemIndex)
                draggedItem.Add(treeViewItem[i]);

            Assert.AreEqual(expected, m_Controller.CanReparent(parentItem, draggedItem));
        }

        [Test]
        [TestCase(0, new[] { 2, 3 }, false, SkinningMode.Character, false, TestName = "No Character in SkinningMode.Chracter")]
        [TestCase(0, new[] { 2, 3 }, false, SkinningMode.SpriteSheet, true, TestName = "No Character in SkinningMode.SpriteSheet")]
        [TestCase(0, new[] { 2, 3 }, true, SkinningMode.SpriteSheet, false, TestName = "Has Character in SkinningMode.SpriteSheet")]
        [TestCase(0, new[] { 2, 3 }, true, SkinningMode.Character, true, TestName = "Can Reparent item not parented")]
        [TestCase(3, new[] { 1, 2 }, true, SkinningMode.Character, true, TestName = "Reparent item to another root")]
        [TestCase(1, new[] { 3 }, true, SkinningMode.Character, true, TestName = "Reparent root to root")]
        public void ReparentItems(int parentIndex, int[] draggedItemIndex, bool hasCharacter, SkinningMode mode, bool shouldReparent)
        {
            m_Model.hasCharacter.Returns(hasCharacter);
            m_Model.mode.Returns(mode);
            var skeleton = Substitute.For<SkeletonCache>();
            var bones = new[]
            {
                Substitute.For<BoneCache>(),
                Substitute.For<BoneCache>(),
                Substitute.For<BoneCache>(),
                Substitute.For<BoneCache>(),
            };
            bones[0].name = "Bone_1";
            bones[1].name = "Bone_1_1";
            bones[2].name = "Bone_1_1_1";
            bones[3].name = "Bone_2";

            var b = bones[2];
            b.parentBone.Returns(bones[1]);
            b = bones[1];
            b.parentBone.Returns(bones[0]);
            bones[0].siblingIndex.Returns(0);
            bones[1].siblingIndex.Returns(0);
            bones[2].siblingIndex.Returns(0);
            bones[3].siblingIndex.Returns(1);

            skeleton.bones.Returns(bones);
            m_Model.GetSelectedSkeleton().Returns(skeleton);
            var treeViewItem = m_Controller.BuildTreeView();
            treeViewItem[0].AddChild(treeViewItem[1]);
            treeViewItem[1].AddChild(treeViewItem[2]);

            var parentItem = treeViewItem[parentIndex] as TreeViewItemBase<BoneCache>;
            var draggedItem = new List<TreeViewItem>();
            foreach (var i in draggedItemIndex)
                draggedItem.Add(treeViewItem[i]);

            var eventCalled = 0;
            UnityAction<SkeletonCache> callback = x =>
            {
                ++eventCalled;
            };
            m_EventSystem.skeletonTopologyChanged.AddListener(callback);
            m_Controller.ReparentItems(parentItem, draggedItem, 0);
            m_EventSystem.skeletonTopologyChanged.RemoveListener(callback);
            if (shouldReparent)
            {
                Assert.AreEqual(draggedItem.Count, eventCalled);
                m_Model.Received(draggedItem.Count).SetBoneParent(Arg.Any<BoneCache>(), Arg.Any<BoneCache>(), 0);
                for (int i = 0; i < draggedItem.Count; ++i)
                {
                    m_Model.Received(1).SetBoneParent(Arg.Is<BoneCache>(x => x == parentItem.customData), Arg.Is<BoneCache>(y => y == ((TreeViewItemBase<BoneCache>)draggedItem[i]).customData), 0);
                }
            }
            else
            {
                m_Model.DidNotReceiveWithAnyArgs().SetBoneParent(Arg.Any<BoneCache>(), Arg.Any<BoneCache>(), 0);
                Assert.AreEqual(0, eventCalled);
            }
        }

        [Test]
        public void CanDrag()
        {
            var eventString = "";
            UnityAction<string> callback = x =>
            {
                eventString = x;
            };
            m_EventSystem.boneVisibility.AddListener(callback);
            Assert.IsFalse(m_Controller.CanDrag());
            m_EventSystem.boneVisibility.RemoveListener(callback);
            Assert.AreEqual("drag", eventString);
        }

        [Test]
        public void CanRename()
        {
            var eventString = "";
            UnityAction<string> callback = x =>
            {
                eventString = x;
            };
            m_EventSystem.boneVisibility.AddListener(callback);
            Assert.IsFalse(m_Controller.CanRename());
            m_EventSystem.boneVisibility.RemoveListener(callback);
            Assert.AreEqual("rename", eventString);
        }
    }

    internal class BoneReparentToolControllerTests
    {
        SkinningEvents m_EventSystem;
        IBoneTreeViewModel m_Model;
        BoneReparentToolController m_Controller;

        [SetUp]
        public void Setup()
        {
            m_Model = Substitute.For<IBoneTreeViewModel>();
            m_EventSystem = Substitute.For<SkinningEvents>();
            m_EventSystem.selectedSpriteChanged.Returns(Substitute.For<SkinningEvents.SpriteEvent>());
            m_EventSystem.skinningModeChanged.Returns(Substitute.For<SkinningEvents.SkinningModuleModeEvent>());
            m_EventSystem.boneSelectionChanged.Returns(Substitute.For<SkinningEvents.BoneSelectionEvent>());
            m_EventSystem.boneNameChanged.Returns(Substitute.For<SkinningEvents.BoneEvent>());
            m_EventSystem.skeletonTopologyChanged.Returns(Substitute.For<SkinningEvents.SkeletonEvent>());
            m_EventSystem.boneVisibility.Returns(Substitute.For<SkinningEvents.BoneVisibilityEvent>());
            m_Controller = new BoneReparentToolController(m_Model, m_EventSystem);
        }

        [Test]
        [TestCase(false, SkinningMode.Character, false)]
        [TestCase(false, SkinningMode.SpriteSheet, true)]
        [TestCase(true, SkinningMode.Character, true)]
        [TestCase(true, SkinningMode.SpriteSheet, false)]
        public void CanDrag(bool hasCharacter, SkinningMode mode, bool canDrag)
        {
            m_Model.hasCharacter.Returns(hasCharacter);
            m_Model.mode.Returns(mode);
            var eventString = "";
            UnityAction<string> callback = x =>
            {
                eventString = x;
            };
            m_EventSystem.boneVisibility.AddListener(callback);
            Assert.AreEqual(canDrag, m_Controller.CanDrag());
            m_EventSystem.boneVisibility.RemoveListener(callback);
            Assert.AreEqual("drag", eventString);
        }

        [Test]
        public void CanRename()
        {
            var eventString = "";
            UnityAction<string> callback = x =>
            {
                eventString = x;
            };
            m_EventSystem.boneVisibility.AddListener(callback);
            Assert.IsTrue(m_Controller.CanRename());
            m_EventSystem.boneVisibility.RemoveListener(callback);
            Assert.AreEqual("rename", eventString);
        }
    }
}
