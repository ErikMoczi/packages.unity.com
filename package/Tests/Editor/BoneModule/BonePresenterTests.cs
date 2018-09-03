using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using NUnit.Framework;
using NSubstitute;

namespace UnityEditor.Experimental.U2D.Animation.Test.Bone
{
    internal class BonePresenterTestBase
    {
        protected IBoneModel m_ModelMock;
        protected IBoneHierarchyView m_HierarchyViewMock;
        protected IBoneToolView m_ToolViewMock;
        protected IBoneInfoView m_InfoViewMock;

        protected BonePresenter m_BonePresenter;
        
        [SetUp]
        public void TestSetup()
        {
            m_ModelMock = Substitute.For<IBoneModel>();
            m_HierarchyViewMock = Substitute.For<IBoneHierarchyView>();
            m_ToolViewMock = Substitute.For<IBoneToolView>();
            m_InfoViewMock = Substitute.For<IBoneInfoView>();

            // Setup for snapping to work overall tests
            m_HierarchyViewMock.GetBoneRadius().Returns(0.5f);

            m_BonePresenter = new BonePresenter(m_ModelMock, m_HierarchyViewMock, m_ToolViewMock, m_InfoViewMock);
        }
        
        protected List<IBone> GenerateAttachedRootAndChild()
        {
            var root = Substitute.For<IBone>();
            IBone nullBone = null;
            root.name.Returns("root");
            root.parent.Returns(nullBone);
            root.isRoot.Returns(true);
            root.position.Returns(Vector3.zero);
            root.tip.Returns(Vector3.one);

            var child = Substitute.For<IBone>();
            child.name.Returns("child");
            child.parent.Returns(root);
            child.isRoot.Returns(false);
            child.position.Returns(Vector3.one);
            child.tip.Returns(Vector3.right);

            var bones = new List<IBone>();
            bones.Add(root);
            bones.Add(child);

            m_ModelMock.bones.Returns(bones.AsEnumerable());

            return bones;
        }
        
        protected List<IBone> GenerateDetachedRootAndChild()
        {
            var root = Substitute.For<IBone>();
            IBone nullBone = null;
            root.parent.Returns(nullBone);
            root.isRoot.Returns(true);
            root.position.Returns(Vector3.zero);
            root.tip.Returns(Vector3.one);

            var child = Substitute.For<IBone>();
            child.parent.Returns(root);
            child.position.Returns(Vector3.right);
            child.tip.Returns(Vector3.left);

            var bones = new List<IBone>();
            bones.Add(root);
            bones.Add(child);

            m_ModelMock.bones.Returns(bones.AsEnumerable());

            return bones;
        }
        
        protected List<IBone> GenerateSingleRoot()
        {
            var root = Substitute.For<IBone>();
            IBone nullBone = null;
            root.parent.Returns(nullBone);
            root.isRoot.Returns(true);
            root.position.Returns(Vector3.zero);
            root.tip.Returns(Vector3.one);
            
            var bones = new List<IBone>();
            bones.Add(root);

            m_ModelMock.bones.Returns(bones.AsEnumerable());

            return bones;
        }

        //
        //  R--C1--C2--
        //      |(C1 tip pointed to C2, C2 & C3 are children of C1)
        //     C3--C4--
        //
        protected List<IBone> GenerateComplexHierachy()
        {
            var root = Substitute.For<IBone>();
            IBone nullBone = null;
            root.name.Returns("root");
            root.parent.Returns(nullBone);
            root.isRoot.Returns(true);
            root.position.Returns(Vector3.zero);
            root.tip.Returns(Vector3.right);

            var child1 = Substitute.For<IBone>();
            child1.name.Returns("child1");
            child1.parent.Returns(root);
            child1.isRoot.Returns(false);
            child1.position.Returns(Vector3.right);
            child1.tip.Returns(Vector3.right * 2);

            var child2 = Substitute.For<IBone>();
            child2.name.Returns("child2");
            child2.parent.Returns(child1);
            child2.isRoot.Returns(false);
            child2.position.Returns(Vector3.right * 2);
            child2.tip.Returns(Vector3.right * 3);

            var child3 = Substitute.For<IBone>();
            child3.name.Returns("child3");
            child3.parent.Returns(child1);
            child3.isRoot.Returns(false);
            child3.position.Returns(Vector3.one);
            child3.tip.Returns(Vector3.one * 2);

            var child4 = Substitute.For<IBone>();
            child4.name.Returns("child4");
            child4.parent.Returns(child3);
            child4.isRoot.Returns(false);
            child4.position.Returns(Vector3.one * 2);
            child4.tip.Returns(Vector3.one * 3);

            var bones = new List<IBone>();
            bones.Add(root);
            bones.Add(child1);
            bones.Add(child2);
            bones.Add(child3);
            bones.Add(child4);

            m_ModelMock.bones.Returns(bones.AsEnumerable());

            return bones;
        }
    }

    [TestFixture]
    internal class PresenterDrawTest : BonePresenterTestBase
    {
        [Test]
        public void NoBonesAndIdling_NothingIsDrawn()
        {
            m_BonePresenter.DoBone(Rect.zero);

            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().DrawBone(null, false);
            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().DrawLinkToParent(null, false);
            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().DrawTip(null);
            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().DrawPreviewLinkFromBone(null);
            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().DrawPreviewTipFromTip(null);
        }

        [Test]
        public void HasTwoAttachedBoneAndIdling_DrawTwoBoneOnly()
        {
            var bones = GenerateAttachedRootAndChild();
            var root = bones[0];
            var child = bones[1];

            m_BonePresenter.DoBone(Rect.zero);

            var selected = true;
            var unselected = !selected;

            m_HierarchyViewMock.Received(1).DrawBone(root, unselected);
            m_HierarchyViewMock.Received(1).DrawBone(root, unselected);
            m_HierarchyViewMock.DidNotReceive().DrawBone(Arg.Any<IBone>(), selected);

            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().DrawLinkToParent(null, false);
            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().DrawTip(null);
            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().DrawPreviewLinkFromBone(null);
            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().DrawPreviewTipFromTip(null);
        }

        [Test]
        public void HasTwoDetachedBoneAndIdling_DrawTwoBoneAndOneLink()
        {
            var bones = GenerateDetachedRootAndChild();
            var root = bones[0];
            var child = bones[1];

            m_BonePresenter.DoBone(Rect.zero);

            m_HierarchyViewMock.Received(2).DrawBone(Arg.Any<IBone>(), false);
            m_HierarchyViewMock.DidNotReceive().DrawLinkToParent(root, Arg.Any<bool>());
            m_HierarchyViewMock.Received(1).DrawLinkToParent(child, false);

            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().DrawTip(null);
            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().DrawPreviewLinkFromBone(null);
            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().DrawPreviewTipFromTip(null);
        }

        [Test]
        public void HasTwoAttachedBoneAndSelectingChild_DrawNormalRootAndSelectedChildBoneAndLinkFromChildToRoot()
        {
            var bones = GenerateAttachedRootAndChild();
            var root = bones[0];
            var child = bones[1];

            var state = new BoneEditorState();
            state.selectedBones.Add(child);
            m_BonePresenter.state = state;

            m_BonePresenter.DoBone(Rect.zero);

            var selected = true;
            var unselected = !selected;
            m_HierarchyViewMock.Received(2).DrawBone(Arg.Any<IBone>(), Arg.Any<bool>());
            m_HierarchyViewMock.Received(1).DrawBone(root, unselected);
            m_HierarchyViewMock.Received(1).DrawBone(child, selected);
            
            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().DrawLinkToParent(Arg.Any<IBone>(), Arg.Any<bool>());
            
            m_HierarchyViewMock.DidNotReceive().DrawTip(root);
            m_HierarchyViewMock.Received(1).DrawTip(child);

            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().DrawPreviewLinkFromBone(null);
            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().DrawPreviewTipFromTip(null);
        }

        [Test]
        public void HasTwoAttachedBoneAndSelectingRoot_DrawNormalChildAndSelectedRoot()
        {
            var bones = GenerateAttachedRootAndChild();
            var root = bones[0];
            var child = bones[1];

            var state = new BoneEditorState();
            state.selectedBones.Add(root);
            m_BonePresenter.state = state;

            m_BonePresenter.DoBone(Rect.zero);

            var selected = true;
            var unselected = !selected;
            m_HierarchyViewMock.Received(2).DrawBone(Arg.Any<IBone>(), Arg.Any<bool>());
            m_HierarchyViewMock.Received(1).DrawBone(root, selected);
            m_HierarchyViewMock.Received(1).DrawBone(child, unselected);

            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().DrawLinkToParent(null, false);

            m_HierarchyViewMock.Received(1).DrawTip(root);
            m_HierarchyViewMock.DidNotReceive().DrawTip(child);

            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().DrawPreviewLinkFromBone(null);
            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().DrawPreviewTipFromTip(null);
        }
        
        private void VerifyDrawCreationCursor()
        {
            m_HierarchyViewMock.Received(1).ShowCreationCursor();

            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().DrawBone(null, false);
            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().DrawLinkToParent(null, false);
            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().DrawTip(null);
            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().DrawPreviewLinkFromBone(null);
            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().DrawPreviewTipFromTip(null);
        }

        [Test]
        public void NoBone_InNormalCreationState_DrawCreationCursor()
        {
            var state = new BoneEditorState();
            state.normalCreating = true;
            state.freeCreatingBone = true;
            m_BonePresenter.state = state;

            m_BonePresenter.DoBone(Rect.zero);

            VerifyDrawCreationCursor();
        }

        [Test]
        public void NoBone_InFreeCreationState_DrawCreationCursor()
        {
            var state = new BoneEditorState();
            state.freeCreating = true;
            state.freeCreatingBone = true;
            m_BonePresenter.state = state;

            m_BonePresenter.DoBone(Rect.zero);

            VerifyDrawCreationCursor();
        }

        private void VerifyDrawPreviewTip(IBone bone)
        {
            m_HierarchyViewMock.Received(1).ShowCreationCursor();
            m_HierarchyViewMock.Received(1).DrawBone(bone, true);
            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().DrawLinkToParent(null, false);
            m_HierarchyViewMock.Received(1).DrawTip(bone);
            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().DrawPreviewLinkFromBone(null);
            m_HierarchyViewMock.Received(1).DrawPreviewTipFromTip(bone);
        }

        [Test]
        public void InNormalCreationState_CreatedRootBone_DrawPreviewTip()
        {
            var root = GenerateSingleRoot().First();

            var state = new BoneEditorState();
            state.selectedBones.Add(root);
            state.normalCreating = true;
            state.freeCreatingBone = false;
            m_BonePresenter.state = state;

            m_BonePresenter.DoBone(Rect.zero);

            VerifyDrawPreviewTip(root);
        }

        [Test]
        public void SelectingRoot_EnterNormalCreationState_DrawPreviewTip()
        {
            var root = GenerateSingleRoot().First();

            var state = new BoneEditorState();
            state.selectedBones.Add(root);
            state.normalCreating = true;
            state.freeCreatingBone = true;
            m_BonePresenter.state = state;

            m_BonePresenter.DoBone(Rect.zero);

            VerifyDrawPreviewTip(root);
        }

        [Test]
        public void InFreeCreationState_CreatedRootBone_DrawPreviewTip()
        {
            var root = GenerateSingleRoot().First();

            var state = new BoneEditorState();
            state.selectedBones.Add(root);
            state.freeCreating = true;
            state.freeCreatingBone = false;
            m_BonePresenter.state = state;

            m_BonePresenter.DoBone(Rect.zero);

            VerifyDrawPreviewTip(root);
        }
        
        [Test]
        public void InFreeCreationState_CreatedRootBoneAndTheTip_DrawPreviewLink()
        {
            var root = GenerateSingleRoot().First();

            var state = new BoneEditorState();
            state.selectedBones.Add(root);
            state.freeCreating = true;
            state.freeCreatingBone = true;
            m_BonePresenter.state = state;

            m_BonePresenter.DoBone(Rect.zero);

            m_HierarchyViewMock.Received(1).ShowCreationCursor();
            m_HierarchyViewMock.Received(1).DrawBone(root, true);
            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().DrawLinkToParent(null, false);
            m_HierarchyViewMock.Received(1).DrawTip(root);
            m_HierarchyViewMock.Received(1).DrawPreviewLinkFromBone(root);
            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().DrawPreviewTipFromTip(null);
        }
    }

    [TestFixture]
    internal class PresenterToolTest : BonePresenterTestBase
    {
        private void VerifyAllHandlingCalledOnce()
        {
            m_ToolViewMock.ReceivedWithAnyArgs(1).HandleCreate(false, false);
            m_ToolViewMock.ReceivedWithAnyArgs(1).HandleFreeCreate(false, false);
            m_ToolViewMock.ReceivedWithAnyArgs(1).HandleMove(false, false);
            m_ToolViewMock.ReceivedWithAnyArgs(1).HandleParent(false, false);
            m_ToolViewMock.ReceivedWithAnyArgs(1).HandleSplit(false);
            m_ToolViewMock.ReceivedWithAnyArgs(1).HandleDelete(false);
        }

        [Test]
        public void NoBoneSelected_OnlyEnableCreationTools()
        {
            GenerateAttachedRootAndChild();

            m_BonePresenter.DoTool(new Rect());

            VerifyAllHandlingCalledOnce();
            
            m_ToolViewMock.Received(1).HandleCreate(false, true);
            m_ToolViewMock.Received(1).HandleFreeCreate(false, true);
            m_ToolViewMock.Received(1).HandleMove(false, false);
            m_ToolViewMock.Received(1).HandleParent(false, false);
            m_ToolViewMock.Received(1).HandleSplit(false);
            m_ToolViewMock.Received(1).HandleDelete(false);
        }

        [Test]
        public void SelectingChildBone_EnableAllTools()
        {
            var bones = GenerateAttachedRootAndChild();
            var child = bones[1];

            var state = new BoneEditorState();
            state.selectedBones.Add(child);
            m_BonePresenter.state = state;

            m_BonePresenter.DoTool(new Rect());

            VerifyAllHandlingCalledOnce();

            m_ToolViewMock.Received(1).HandleCreate(false, true);
            m_ToolViewMock.Received(1).HandleFreeCreate(false, true);
            m_ToolViewMock.Received(1).HandleMove(false, true);
            m_ToolViewMock.Received(1).HandleParent(false, true);
            m_ToolViewMock.Received(1).HandleSplit(true);
            m_ToolViewMock.Received(1).HandleDelete(true);
        }

        [Test]
        public void SelectingMultipleBone_DisableCreationTool()
        {
            var bones = GenerateAttachedRootAndChild();
            var root = bones[0];
            var child = bones[1];

            var state = new BoneEditorState();
            state.selectedBones.Add(root);
            state.selectedBones.Add(child);
            m_BonePresenter.state = state;

            m_BonePresenter.DoTool(new Rect());

            VerifyAllHandlingCalledOnce();

            m_ToolViewMock.Received(1).HandleCreate(false, false);
            m_ToolViewMock.Received(1).HandleFreeCreate(false, false);
            m_ToolViewMock.Received(1).HandleMove(false, true);
            m_ToolViewMock.Received(1).HandleParent(false, true);
            m_ToolViewMock.Received(1).HandleSplit(true);
            m_ToolViewMock.Received(1).HandleDelete(true);
        }

        [Test]
        public void NoBoneCreated_EnterNormaCreationMode_StateIsNormalCreationAndCreatingRoot()
        {
            m_ToolViewMock.HandleCreate(Arg.Any<bool>(), Arg.Any<bool>()).Returns(true);

            m_BonePresenter.DoTool(new Rect());

            Assert.IsTrue(m_BonePresenter.state.normalCreating);
            Assert.IsFalse(m_BonePresenter.state.freeCreating);
            Assert.IsFalse(m_BonePresenter.state.parenting);
            Assert.IsFalse(m_BonePresenter.state.freeMoving);
            Assert.IsFalse(m_BonePresenter.state.multiselecting);
            Assert.IsFalse(m_BonePresenter.state.freeCreatingBone);
            Assert.IsTrue(m_BonePresenter.state.normalCreatingRoot);
        }

        [Test]
        public void NoBoneCreated_EnterFreeCreationMode_StateIsFreeCreationAndCreatingBone()
        {
            m_ToolViewMock.HandleFreeCreate(Arg.Any<bool>(), Arg.Any<bool>()).Returns(true);

            m_BonePresenter.DoTool(new Rect());

            Assert.IsFalse(m_BonePresenter.state.normalCreating);
            Assert.IsTrue(m_BonePresenter.state.freeCreating);
            Assert.IsFalse(m_BonePresenter.state.parenting);
            Assert.IsFalse(m_BonePresenter.state.freeMoving);
            Assert.IsFalse(m_BonePresenter.state.multiselecting);
            Assert.IsTrue(m_BonePresenter.state.freeCreatingBone);
            Assert.IsFalse(m_BonePresenter.state.normalCreatingRoot);
        }

        [Test]
        public void SelectedChildBone_EnterNormalCreationMode_StateIsNormalCreation()
        {
            m_ToolViewMock.HandleCreate(Arg.Any<bool>(), Arg.Any<bool>()).Returns(true);

            var bones = GenerateAttachedRootAndChild();
            var child = bones[1];

            var state = new BoneEditorState();
            state.selectedBones.Add(child);
            m_BonePresenter.state = state;

            m_BonePresenter.DoTool(new Rect());

            Assert.IsTrue(m_BonePresenter.state.normalCreating);
            Assert.IsFalse(m_BonePresenter.state.freeCreating);
            Assert.IsFalse(m_BonePresenter.state.parenting);
            Assert.IsFalse(m_BonePresenter.state.freeMoving);
            Assert.IsFalse(m_BonePresenter.state.multiselecting);
            Assert.IsFalse(m_BonePresenter.state.freeCreatingBone);
            Assert.IsFalse(m_BonePresenter.state.normalCreatingRoot);
        }

        [Test]
        public void SelectedChildBone_EnterFreeCreationMode_StateIsFreeCreationAndCreatingBone()
        {
            m_ToolViewMock.HandleFreeCreate(Arg.Any<bool>(), Arg.Any<bool>()).Returns(true);

            var bones = GenerateAttachedRootAndChild();
            var child = bones[1];

            var state = new BoneEditorState();
            state.selectedBones.Add(child);
            m_BonePresenter.state = state;

            m_BonePresenter.DoTool(new Rect());

            Assert.IsFalse(m_BonePresenter.state.normalCreating);
            Assert.IsTrue(m_BonePresenter.state.freeCreating);
            Assert.IsFalse(m_BonePresenter.state.parenting);
            Assert.IsFalse(m_BonePresenter.state.freeMoving);
            Assert.IsFalse(m_BonePresenter.state.multiselecting);
            Assert.IsTrue(m_BonePresenter.state.freeCreatingBone);
            Assert.IsFalse(m_BonePresenter.state.normalCreatingRoot);
        }

        [Test]
        public void EnterFreeMove_StateIsFreeMove()
        {
            m_ToolViewMock.HandleMove(Arg.Any<bool>(), Arg.Any<bool>()).Returns(true);

            var bones = GenerateAttachedRootAndChild();
            var child = bones[1];

            var state = new BoneEditorState();
            state.selectedBones.Add(child);
            m_BonePresenter.state = state;

            m_BonePresenter.DoTool(new Rect());

            Assert.IsFalse(m_BonePresenter.state.normalCreating);
            Assert.IsFalse(m_BonePresenter.state.freeCreating);
            Assert.IsFalse(m_BonePresenter.state.parenting);
            Assert.IsTrue(m_BonePresenter.state.freeMoving);
            Assert.IsFalse(m_BonePresenter.state.multiselecting);
            Assert.IsFalse(m_BonePresenter.state.freeCreatingBone);
            Assert.IsFalse(m_BonePresenter.state.normalCreatingRoot);
        }

        [Test]
        public void EnterParenting_StateIsParenting()
        {
            m_ToolViewMock.HandleParent(Arg.Any<bool>(), Arg.Any<bool>()).Returns(true);

            var bones = GenerateAttachedRootAndChild();
            var child = bones[1];

            var state = new BoneEditorState();
            state.selectedBones.Add(child);
            m_BonePresenter.state = state;

            m_BonePresenter.DoTool(new Rect());

            Assert.IsFalse(m_BonePresenter.state.normalCreating);
            Assert.IsFalse(m_BonePresenter.state.freeCreating);
            Assert.IsTrue(m_BonePresenter.state.parenting);
            Assert.IsFalse(m_BonePresenter.state.freeMoving);
            Assert.IsFalse(m_BonePresenter.state.multiselecting);
            Assert.IsFalse(m_BonePresenter.state.freeCreatingBone);
            Assert.IsFalse(m_BonePresenter.state.normalCreatingRoot);
        }

        public enum ToolToggleMode
        {
            NormalCreation,
            FreeCreation,
            FreeMove,
            Parenting
        }

        private static IEnumerable<TestCaseData> ToggleSwappingTestCase()
        {
            // From normal creation to other states
            var original = new BoneEditorState();
            original.normalCreating = true;

            var expecting = new BoneEditorState();
            expecting.freeCreating = true;
            expecting.freeCreatingBone = true;

            var testCaseData = new TestCaseData(original.Clone(), ToolToggleMode.FreeCreation, expecting.Clone());
            testCaseData.SetName("Normal Creation entering Free Creation");
            yield return testCaseData;

            expecting.Reset();
            expecting.freeMoving = true;
            testCaseData = new TestCaseData(original.Clone(), ToolToggleMode.FreeMove, expecting.Clone());
            testCaseData.SetName("Normal Creation entering Free Move");
            yield return testCaseData;

            expecting.Reset();
            expecting.parenting = true;
            testCaseData = new TestCaseData(original.Clone(), ToolToggleMode.Parenting, expecting.Clone());
            testCaseData.SetName("Normal Creation entering Parenting");
            yield return testCaseData;

            // From free creation to other states
            original.Reset();
            original.freeCreating = true;

            expecting.Reset();
            expecting.normalCreating = true;
            expecting.normalCreatingRoot = true;

            testCaseData = new TestCaseData(original.Clone(), ToolToggleMode.NormalCreation, expecting.Clone());
            testCaseData.SetName("Free Creation entering Normal Creation");
            yield return testCaseData;

            expecting.Reset();
            expecting.freeMoving = true;
            testCaseData = new TestCaseData(original.Clone(), ToolToggleMode.FreeMove, expecting.Clone());
            testCaseData.SetName("Free Creation entering Free Move");
            yield return testCaseData;

            expecting.Reset();
            expecting.parenting = true;
            testCaseData = new TestCaseData(original.Clone(), ToolToggleMode.Parenting, expecting.Clone());
            testCaseData.SetName("Free Creation entering Parenting");
            yield return testCaseData;

            // From free move to other states
            original.Reset();
            original.freeMoving = true;

            expecting.Reset();
            expecting.normalCreating = true;
            expecting.normalCreatingRoot = true;

            testCaseData = new TestCaseData(original.Clone(), ToolToggleMode.NormalCreation, expecting.Clone());
            testCaseData.SetName("Free Moving entering Normal Creation");
            yield return testCaseData;

            expecting.Reset();
            expecting.freeCreating = true;
            expecting.freeCreatingBone = true;
            testCaseData = new TestCaseData(original.Clone(), ToolToggleMode.FreeCreation, expecting.Clone());
            testCaseData.SetName("Free Moving entering Free Creation");
            yield return testCaseData;

            expecting.Reset();
            expecting.parenting = true;
            testCaseData = new TestCaseData(original.Clone(), ToolToggleMode.Parenting, expecting.Clone());
            testCaseData.SetName("Free Moving entering Parenting");
            yield return testCaseData;

            // From parenting to other states
            original.Reset();
            original.parenting = true;

            expecting.Reset();
            expecting.normalCreating = true;
            expecting.normalCreatingRoot = true;

            testCaseData = new TestCaseData(original.Clone(), ToolToggleMode.NormalCreation, expecting.Clone());
            testCaseData.SetName("Parenting entering Normal Creation");
            yield return testCaseData;

            expecting.Reset();
            expecting.freeCreating = true;
            expecting.freeCreatingBone = true;
            testCaseData = new TestCaseData(original.Clone(), ToolToggleMode.FreeCreation, expecting.Clone());
            testCaseData.SetName("Parenting entering Free Creation");
            yield return testCaseData;

            expecting.Reset();
            expecting.freeMoving = true;
            testCaseData = new TestCaseData(original.Clone(), ToolToggleMode.FreeMove, expecting.Clone());
            testCaseData.SetName("Parenting entering Free Moving");
            yield return testCaseData;
        }

        [Test, TestCaseSource("ToggleSwappingTestCase")]
        public void EnterAnotherToggleMode_DeactivatePreviousToggledState(BoneEditorState originalState, ToolToggleMode enteringMode, BoneEditorState expectingState)
        {
            // These setup to mimic how toggle button works, when a state was "true" the toggle button will return "true"
            m_ToolViewMock.HandleCreate(originalState.normalCreating, true).Returns(originalState.normalCreating);
            m_ToolViewMock.HandleFreeCreate(originalState.freeCreating, true).Returns(originalState.freeCreating);
            m_ToolViewMock.HandleMove(originalState.freeMoving, false).Returns(originalState.freeMoving);
            m_ToolViewMock.HandleParent(originalState.parenting, false).Returns(originalState.parenting);

            // These setup to mimic a click on a deactivated toggle button.
            switch (enteringMode)
            {
                case ToolToggleMode.NormalCreation:
                    m_ToolViewMock.HandleCreate(Arg.Any<bool>(), Arg.Any<bool>()).Returns(true);
                    break;
                case ToolToggleMode.FreeCreation:
                    m_ToolViewMock.HandleFreeCreate(Arg.Any<bool>(), Arg.Any<bool>()).Returns(true);
                    break;
                case ToolToggleMode.FreeMove:
                    m_ToolViewMock.HandleMove(Arg.Any<bool>(), Arg.Any<bool>()).Returns(true);
                    break;
                case ToolToggleMode.Parenting:
                    m_ToolViewMock.HandleParent(Arg.Any<bool>(), Arg.Any<bool>()).Returns(true);
                    break;
            }

            m_BonePresenter.state = originalState;

            m_BonePresenter.DoTool(new Rect());

            Assert.AreEqual(expectingState, m_BonePresenter.state);
        }

        private static IEnumerable<TestCaseData> ToggleModeTestCase()
        {
            var state = new BoneEditorState();
            state.normalCreating = true;
            var testCaseData = new TestCaseData(state);
            testCaseData.SetName("Normal Creation");
            yield return testCaseData;

            state = new BoneEditorState();
            state.freeCreating = true;
            testCaseData = new TestCaseData(state);
            testCaseData.SetName("Free Creation");
            yield return testCaseData;

            state = new BoneEditorState();
            state.freeMoving = true;
            testCaseData = new TestCaseData(state);
            testCaseData.SetName("Free Move");
            yield return testCaseData;

            state = new BoneEditorState();
            state.parenting = true;
            testCaseData = new TestCaseData(state);
            testCaseData.SetName("Parenting");
            yield return testCaseData;
        }

        [Test, TestCaseSource("ToggleModeTestCase")]
        public void GlobalCancelWillDeactivateAllToggleMode(BoneEditorState state)
        {
            m_ToolViewMock.HandleGlobalCancel().Returns(true);
            m_BonePresenter.state = state;

            m_BonePresenter.DoTool(new Rect());

            Assert.IsFalse(m_BonePresenter.state.normalCreating);
            Assert.IsFalse(m_BonePresenter.state.freeCreating);
            Assert.IsFalse(m_BonePresenter.state.parenting);
            Assert.IsFalse(m_BonePresenter.state.freeMoving);
            Assert.IsFalse(m_BonePresenter.state.multiselecting);
            Assert.IsFalse(m_BonePresenter.state.freeCreatingBone);
            Assert.IsFalse(m_BonePresenter.state.normalCreatingRoot);
        }
    }

    [TestFixture]
    internal class PresenterInfoViewTest : BonePresenterTestBase
    {
        [Test]
        public void WithNoBoneSelected_SkipShowingInfo()
        {
            m_BonePresenter.DoInfoPanel(Rect.zero);

            string dummyName = "";
            m_InfoViewMock.DidNotReceiveWithAnyArgs().HandleName(ref dummyName);
            m_InfoViewMock.DidNotReceiveWithAnyArgs().HandleNextSelection();
        }

        [Test]
        public void WithMultipleBoneSelected_SkipShowingInfo()
        {
            var bones = GenerateAttachedRootAndChild();
            var root = bones[0];
            var child = bones[1];

            var state = new BoneEditorState();
            state.selectedBones.Add(root);
            state.selectedBones.Add(child);
            m_BonePresenter.state = state;

            m_BonePresenter.DoInfoPanel(Rect.zero);

            string dummyName = "";
            m_InfoViewMock.DidNotReceiveWithAnyArgs().HandleName(ref dummyName);
            m_InfoViewMock.DidNotReceiveWithAnyArgs().HandleNextSelection();
        }

        [Test]
        public void WithRootBoneSelected_ShowRootInfo()
        {
            var bones = GenerateAttachedRootAndChild();
            var root = bones[0];

            var state = new BoneEditorState();
            state.selectedBones.Add(root);
            m_BonePresenter.state = state;

            m_BonePresenter.DoInfoPanel(Rect.zero);

            string dummyName = "root";
            m_InfoViewMock.ReceivedWithAnyArgs(1).HandleName(ref dummyName);
            m_InfoViewMock.Received(1).HandleName(ref dummyName);
        }

        [Test]
        public void ChangingNameWillUpdateModel()
        {
            var bones = GenerateAttachedRootAndChild();
            var root = bones[0];
            
            var state = new BoneEditorState();
            state.selectedBones.Add(root);
            m_BonePresenter.state = state;

            string dummyName = "root";
            m_InfoViewMock.HandleName(ref dummyName).Returns(x => 
                {
                    x[0] = "root_changed";
                    return true;
                });

            m_BonePresenter.DoInfoPanel(Rect.zero);

            m_ModelMock.Received(1).SetBoneName(root, "root_changed");
        }

        [Test]
        public void DuplicateBoneName_DisplayWarningWhenHandlingRename()
        {
            var root = Substitute.For<IBone>();
            IBone nullBone = null;
            root.name.Returns("root");
            root.parent.Returns(nullBone);
            root.isRoot.Returns(true);

            var child1 = Substitute.For<IBone>();
            child1.name.Returns("child");
            child1.parent.Returns(root);
            child1.isRoot.Returns(false);

            var child2 = Substitute.For<IBone>();
            child2.name.Returns("child");
            child2.parent.Returns(root);
            child2.isRoot.Returns(false);

            var bones = new List<IBone>();
            bones.Add(root);
            bones.Add(child1);
            bones.Add(child2);

            m_ModelMock.bones.Returns(bones.AsEnumerable());

            var state = new BoneEditorState();
            state.selectedBones.Add(child1);
            m_BonePresenter.state = state;

            m_BonePresenter.DoInfoPanel(Rect.zero);

            m_InfoViewMock.Received(1).DisplayDuplicateBoneNameWarning();
        }

        private static IEnumerable<TestCaseData> TabSelectionsTestCases()
        {
            yield return new TestCaseData(1, "child1");
            yield return new TestCaseData(2, "child2");
            yield return new TestCaseData(3, "child3");
            yield return new TestCaseData(4, "child4");
            yield return new TestCaseData(5, "root");
        }

        [Test, TestCaseSource("TabSelectionsTestCases")]
        public void TabOnInfoView_SelectTheCorrectNextBone(int tabCount, string selectedBoneName)
        {
            var bones = GenerateComplexHierachy();
            var root = bones[0];

            var state = new BoneEditorState();
            state.selectedBones.Add(root);
            m_BonePresenter.state = state;

            m_InfoViewMock.HandleNextSelection().Returns(true);

            for (var i = 0; i < tabCount; ++i)
                m_BonePresenter.DoInfoPanel(Rect.zero);

            Assert.AreEqual(selectedBoneName, m_BonePresenter.state.selectedBones[0].name);
        }

        [Test]
        public void ClickSelectABone_TriggerSelectionChangedOnInfoView()
        {
            var bones = GenerateAttachedRootAndChild();
            var root = bones[0];
            var child = bones[1];

            m_HierarchyViewMock.HandleBoneSelect(child).Returns(true);

            m_BonePresenter.DoBone(Rect.zero);

            m_InfoViewMock.Received(1).SelectionChanged();
        }

    }

    [TestFixture]
    internal class PresenterSelectionTest : BonePresenterTestBase
    {
        private void VerifyNeverHandleSelectAnyBone()
        {
            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().HandleBoneSelect(null);
            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().HandleTipSelect(null);
        }

        private void VerifyNeverHandleDragAnyBone()
        {
            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().HandleBoneDrag(null);
            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().HandleBoneTipDrag(null);
            m_HierarchyViewMock.DidNotReceiveWithAnyArgs().HandleBoneDrag(null);
        }

        [Test]
        public void NoBonesAndIdle_NoHandling()
        {
            m_BonePresenter.DoBone(Rect.zero);

            VerifyNeverHandleSelectAnyBone();
            VerifyNeverHandleDragAnyBone();
        }

        [Test]
        public void NotSelectingAnyBone_IdleState_HandleSelectionOnBoneAndTip()
        {
            var bones = GenerateAttachedRootAndChild();
            var root = bones[0];
            var child = bones[1];

            m_BonePresenter.DoBone(Rect.zero);

            m_HierarchyViewMock.ReceivedWithAnyArgs(2).HandleBoneSelect(null);
            m_HierarchyViewMock.Received(1).HandleBoneSelect(root);
            m_HierarchyViewMock.Received(1).HandleBoneSelect(child);
            m_HierarchyViewMock.ReceivedWithAnyArgs(2).HandleTipSelect(null);
            m_HierarchyViewMock.Received(1).HandleTipSelect(root);
            m_HierarchyViewMock.Received(1).HandleTipSelect(child);
            
            VerifyNeverHandleDragAnyBone();
        }

        [Test]
        public void NotSelectingAnyBone_CreationState_NoHandling()
        {
            var state = new BoneEditorState();
            state.normalCreating = true;
            m_BonePresenter.state = state;

            m_BonePresenter.DoBone(Rect.zero);

            VerifyNeverHandleSelectAnyBone();
            VerifyNeverHandleDragAnyBone();
        }

        [Test]
        public void SelectedChildBone_IdleState_HandleSelectionOnAllBone_HandleMovementOnChildBone()
        {
            var bones = GenerateAttachedRootAndChild();
            var root = bones[0];
            var child = bones[1];

            var state = new BoneEditorState();
            state.selectedBones.Add(child);
            m_BonePresenter.state = state;

            m_BonePresenter.DoBone(Rect.zero);

            m_HierarchyViewMock.ReceivedWithAnyArgs(2).HandleBoneSelect(null);
            m_HierarchyViewMock.ReceivedWithAnyArgs(2).HandleTipSelect(null);

            m_HierarchyViewMock.ReceivedWithAnyArgs(1).HandleBoneDrag(null);
            m_HierarchyViewMock.Received(1).HandleBoneDrag(child);
            m_HierarchyViewMock.ReceivedWithAnyArgs(1).HandleBoneTipDrag(null);
            m_HierarchyViewMock.Received(1).HandleBoneTipDrag(child);
            m_HierarchyViewMock.ReceivedWithAnyArgs(1).HandleBoneDrag(null);
            m_HierarchyViewMock.Received(1).HandleBoneDrag(child);
        }

        [Test]
        public void SelectedChildBone_CreationState_NoHandling()
        {
            var bones = GenerateAttachedRootAndChild();
            var root = bones[0];
            var child = bones[1];

            var state = new BoneEditorState();
            state.selectedBones.Add(child);
            state.normalCreating = true;
            m_BonePresenter.state = state;

            m_BonePresenter.DoBone(Rect.zero);

            VerifyNeverHandleSelectAnyBone();
            VerifyNeverHandleDragAnyBone();
        }

        [Test]
        public void SelectedChildBone_ParentingState_HandleSelectionOnly()
        {
            var bones = GenerateAttachedRootAndChild();
            var root = bones[0];
            var child = bones[1];

            var state = new BoneEditorState();
            state.selectedBones.Add(child);
            state.parenting = true;
            m_BonePresenter.state = state;

            m_BonePresenter.DoBone(Rect.zero);

            m_HierarchyViewMock.ReceivedWithAnyArgs(2).HandleBoneSelect(null);
            m_HierarchyViewMock.ReceivedWithAnyArgs(2).HandleTipSelect(null);

            VerifyNeverHandleDragAnyBone();
        }

        [Test]
        public void SelectedAllBone_HandleSelectionAndMovementOnAllBone()
        {
            var bones = GenerateAttachedRootAndChild();
            var root = bones[0];
            var child = bones[1];

            var state = new BoneEditorState();
            state.selectedBones.Add(root);
            state.selectedBones.Add(child);
            m_BonePresenter.state = state;

            m_BonePresenter.DoBone(Rect.zero);

            m_HierarchyViewMock.ReceivedWithAnyArgs(2).HandleBoneSelect(null);
            m_HierarchyViewMock.ReceivedWithAnyArgs(2).HandleTipSelect(null);
            m_HierarchyViewMock.ReceivedWithAnyArgs(2).HandleBoneDrag(null);
            m_HierarchyViewMock.ReceivedWithAnyArgs(2).HandleBoneTipDrag(null);
            m_HierarchyViewMock.ReceivedWithAnyArgs(2).HandleBoneDrag(null);
        }
    }

    [TestFixture]
    internal class PresenterMoveTest : BonePresenterTestBase
    {
        [Test]
        public void NoBone_CreateState_Click_ModelCreateNewBone()
        {
            Vector3 mousePosition = Vector3.zero;
            m_HierarchyViewMock.HandleFullViewCursor(ref mousePosition).Returns(true);

            var state = new BoneEditorState();
            state.normalCreating = true;
            state.normalCreatingRoot = true;
            m_BonePresenter.state = state;

            m_BonePresenter.DoBone(Rect.zero);
            
            m_ModelMock.Received(1).CreateNewRoot(mousePosition);
            m_ModelMock.DidNotReceiveWithAnyArgs().CreateNewChildBone(null, mousePosition);
        }

        [Test]
        public void NoBone_FreeCreateState_Click_ModelCreateNewBone()
        {
            Vector3 mousePosition = Vector3.zero;
            m_HierarchyViewMock.HandleFullViewCursor(ref mousePosition).Returns(true);

            var state = new BoneEditorState();
            state.freeCreating = true;
            state.freeCreatingBone = true;
            m_BonePresenter.state = state;

            m_BonePresenter.DoBone(Rect.zero);

            m_ModelMock.Received(1).CreateNewRoot(mousePosition);
            m_ModelMock.DidNotReceiveWithAnyArgs().CreateNewChildBone(null, mousePosition);
        }

        [Test]
        public void CreatedABone_StillInCreateState_ClickAgain_ModelUpdateTipForCreatedBone()
        {
            Vector3 mousePosition = Vector3.zero;
            m_HierarchyViewMock.HandleFullViewCursor(ref mousePosition).Returns(true);

            var root = GenerateSingleRoot().First();

            var state = new BoneEditorState();
            state.selectedBones.Add(root);
            state.normalCreating = true;
            state.normalCreatingRoot = true;
            m_BonePresenter.state = state;

            m_BonePresenter.DoBone(Rect.zero);

            m_ModelMock.DidNotReceiveWithAnyArgs().CreateNewRoot(mousePosition);
            m_ModelMock.DidNotReceiveWithAnyArgs().CreateNewChildBone(null, mousePosition);
            m_ModelMock.Received(1).MoveTip(root, mousePosition);
        }

        [Test]
        public void CreatedABone_StillInFreeCreateState_ClickAgain_ModelUpdateTipForCreatedBone()
        {
            Vector3 mousePosition = Vector3.zero;
            m_HierarchyViewMock.HandleFullViewCursor(ref mousePosition).Returns(true);

            var root = GenerateSingleRoot().First();

            var state = new BoneEditorState();
            state.selectedBones.Add(root);
            state.freeCreating = true;
            state.freeCreatingBone = false;
            m_BonePresenter.state = state;

            m_BonePresenter.DoBone(Rect.zero);

            m_ModelMock.DidNotReceiveWithAnyArgs().CreateNewRoot(mousePosition);
            m_ModelMock.DidNotReceiveWithAnyArgs().CreateNewChildBone(null, mousePosition);
            m_ModelMock.Received(1).MoveTip(root, mousePosition);
        }

        [Test]
        public void DragRootBoneNode_ModelMoveTheBoneNodePosition_AdjustTheTip()
        {
            var bones = GenerateAttachedRootAndChild();
            var root = bones[0];
            var child = bones[1];

            Vector3 draggedPosition = Vector3.one;
            m_HierarchyViewMock.HandleBoneNodeDrag(root).Returns(draggedPosition);

            var state = new BoneEditorState();
            state.selectedBones.Add(root);
            m_BonePresenter.state = state;

            m_BonePresenter.ResetRelationship();
            m_BonePresenter.DoBone(Rect.zero);

            m_ModelMock.ReceivedWithAnyArgs(1).MoveBone(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveBone(root, draggedPosition);
            m_ModelMock.ReceivedWithAnyArgs(1).MoveTip(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveTip(root, draggedPosition);
        }

        [Test]
        public void DragChildBoneNode_ModelMoveTheChildBoneNodePositionAndRootTip_AdjustChildTip()
        {
            var bones = GenerateAttachedRootAndChild();
            var root = bones[0];
            var child = bones[1];

            Vector3 draggedPosition = Vector3.one;
            m_HierarchyViewMock.HandleBoneNodeDrag(child).Returns(draggedPosition);

            var state = new BoneEditorState();
            state.selectedBones.Add(child);
            m_BonePresenter.state = state;

            m_BonePresenter.ResetRelationship();
            m_BonePresenter.DoBone(Rect.zero);
            
            m_ModelMock.ReceivedWithAnyArgs(1).MoveBone(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveBone(child, draggedPosition);
            m_ModelMock.Received(1).MoveTip(root, draggedPosition);
            m_ModelMock.Received(1).MoveTip(child, child.tip);
        }

        [Test]
        public void DragChildTip_ModelMoveTheChildTip()
        {
            var bones = GenerateAttachedRootAndChild();
            var root = bones[0];
            var child = bones[1];

            Vector3 draggedPosition = Vector3.up;
            m_HierarchyViewMock.HandleBoneTipDrag(child).Returns(draggedPosition);

            var state = new BoneEditorState();
            state.selectedBones.Add(child);
            m_BonePresenter.state = state;

            m_BonePresenter.ResetRelationship();
            m_BonePresenter.DoBone(Rect.zero);

            m_ModelMock.DidNotReceiveWithAnyArgs().MoveBone(null, Arg.Any<Vector3>());
            m_ModelMock.ReceivedWithAnyArgs(1).MoveTip(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveTip(child, draggedPosition);
        }

        [Test]
        public void DragBoneTip_NearChildNode_ModelMoveTheChildTipSnapped()
        {
            var bones = GenerateAttachedRootAndChild();
            var root = bones[0];
            var child = bones[1];

            Vector3 draggedPosition = Vector3.one + new Vector3(0.1f, 0.1f, 0.0f);
            m_HierarchyViewMock.HandleBoneTipDrag(root).Returns(draggedPosition);

            var state = new BoneEditorState();
            state.selectedBones.Add(root);
            m_BonePresenter.state = state;

            m_BonePresenter.ResetRelationship();
            m_BonePresenter.DoBone(Rect.zero);

            m_ModelMock.DidNotReceiveWithAnyArgs().MoveBone(null, Arg.Any<Vector3>());
            m_ModelMock.ReceivedWithAnyArgs(1).MoveTip(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveTip(root, child.position);
        }

        [Test]
        public void DetachedBone_DragChildBoneNode_ModelMoveTheChildBoneNodeOnly_AdjustChildTip()
        {
            var bones = GenerateDetachedRootAndChild();
            var root = bones[0];
            var child = bones[1];

            Vector3 draggedPosition = Vector3.one;
            m_HierarchyViewMock.HandleBoneNodeDrag(child).Returns(draggedPosition);
            
            var state = new BoneEditorState();
            state.selectedBones.Add(child);
            m_BonePresenter.state = state;

            m_BonePresenter.ResetRelationship();
            m_BonePresenter.DoBone(Rect.zero);
            
            m_ModelMock.ReceivedWithAnyArgs(1).MoveBone(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveBone(child, draggedPosition);
            m_ModelMock.ReceivedWithAnyArgs(1).MoveTip(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveTip(child, child.tip);
        }

        [Test]
        public void FreeMoveState_DragRootBoneNode_ModelMoveTheBoneNodePosition_AdjustTheTip()
        {
            var bones = GenerateAttachedRootAndChild();
            var root = bones[0];
            var child = bones[1];

            Vector3 draggedPosition = Vector3.one;
            m_HierarchyViewMock.HandleBoneNodeDrag(root).Returns(draggedPosition);

            var state = new BoneEditorState();
            state.selectedBones.Add(root);
            state.freeMoving = true;
            m_BonePresenter.state = state;

            m_BonePresenter.ResetRelationship();
            m_BonePresenter.DoBone(Rect.zero);
            
            m_ModelMock.ReceivedWithAnyArgs(1).MoveBone(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveBone(root, draggedPosition);
            m_ModelMock.ReceivedWithAnyArgs(1).MoveTip(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveTip(root, draggedPosition);
        }

        [Test]
        public void FreeMoveState_DragChildBoneNode_ModelMoveTheChildBoneNodeOnly_AdjustChildTip()
        {
            var bones = GenerateAttachedRootAndChild();
            var root = bones[0];
            var child = bones[1];

            Vector3 draggedPosition = Vector3.one;
            m_HierarchyViewMock.HandleBoneNodeDrag(child).Returns(draggedPosition);

            var state = new BoneEditorState();
            state.selectedBones.Add(child);
            state.freeMoving = true;
            m_BonePresenter.state = state;

            m_BonePresenter.ResetRelationship();
            m_BonePresenter.DoBone(Rect.zero);
            
            m_ModelMock.ReceivedWithAnyArgs(1).MoveBone(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveBone(child, draggedPosition);
            m_ModelMock.ReceivedWithAnyArgs(1).MoveTip(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveTip(child, child.tip);
        }

        [Test]
        public void FreeMoveState_DragChildTip_ModelMoveTheChildTip()
        {
            var bones = GenerateAttachedRootAndChild();
            var root = bones[0];
            var child = bones[1];

            Vector3 draggedPosition = Vector3.one;
            m_HierarchyViewMock.HandleBoneTipDrag(child).Returns(draggedPosition);

            var state = new BoneEditorState();
            state.selectedBones.Add(child);
            state.freeMoving = true;
            m_BonePresenter.state = state;

            m_BonePresenter.ResetRelationship();
            m_BonePresenter.DoBone(Rect.zero);
            
            m_ModelMock.DidNotReceiveWithAnyArgs().MoveBone(null, Arg.Any<Vector3>());
            m_ModelMock.ReceivedWithAnyArgs(1).MoveTip(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveTip(child, draggedPosition);
        }

        [Test]
        public void DragRootBody_ModelMoveWholeHierachy()
        {
            var bones = GenerateAttachedRootAndChild();
            var root = bones[0];
            var child = bones[1];

            Vector3 draggedPosition = Vector3.one;
            m_HierarchyViewMock.HandleBoneDrag(root).Returns(draggedPosition);

            var state = new BoneEditorState();  
            state.selectedBones.Add(root);
            m_BonePresenter.state = state;

            m_BonePresenter.ResetRelationship();
            m_BonePresenter.DoBone(Rect.zero);

            m_ModelMock.ReceivedWithAnyArgs(1).MoveBone(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveBone(root, draggedPosition, false);
            m_ModelMock.DidNotReceiveWithAnyArgs().MoveTip(null, Arg.Any<Vector3>());
        }

        [Test]
        public void DragChildBody_ModelMoveChildBone_AdjustRootTip()
        {
            var bones = GenerateAttachedRootAndChild();
            var root = bones[0];
            var child = bones[1];

            Vector3 draggedPosition = Vector3.one;
            m_HierarchyViewMock.HandleBoneDrag(child).Returns(draggedPosition);

            var state = new BoneEditorState();
            state.selectedBones.Add(child);
            m_BonePresenter.state = state;

            m_BonePresenter.ResetRelationship();
            m_BonePresenter.DoBone(Rect.zero);
            
            m_ModelMock.ReceivedWithAnyArgs(1).MoveBone(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveBone(child, draggedPosition, false);
            m_ModelMock.ReceivedWithAnyArgs(1).MoveTip(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveTip(root, draggedPosition);
        }

        [Test]
        public void FreeMoveState_DragRootBody_ModelMoveRootBoneOnly()
        {
            var bones = GenerateAttachedRootAndChild();
            var root = bones[0];
            var child = bones[1];

            Vector3 draggedPosition = Vector3.one;
            m_HierarchyViewMock.HandleBoneDrag(root).Returns(draggedPosition);

            var state = new BoneEditorState();
            state.selectedBones.Add(root);
            state.freeMoving = true;
            m_BonePresenter.state = state;

            m_BonePresenter.ResetRelationship();
            m_BonePresenter.DoBone(Rect.zero);

            m_ModelMock.ReceivedWithAnyArgs(1).MoveBone(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveBone(root, draggedPosition);
            m_ModelMock.DidNotReceiveWithAnyArgs().MoveTip(null, Arg.Any<Vector3>());
        }

        [Test]
        public void FreeMoveState_DragChildBody_ModelMoveChildBoneOnly()
        {
            var bones = GenerateAttachedRootAndChild();
            var root = bones[0];
            var child = bones[1];

            Vector3 draggedPosition = Vector3.one;
            m_HierarchyViewMock.HandleBoneDrag(child).Returns(draggedPosition);

            var state = new BoneEditorState();
            state.selectedBones.Add(child);
            state.freeMoving = true;
            m_BonePresenter.state = state;

            m_BonePresenter.ResetRelationship();
            m_BonePresenter.DoBone(Rect.zero);

            m_ModelMock.ReceivedWithAnyArgs(1).MoveBone(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveBone(child, draggedPosition);
            m_ModelMock.DidNotReceiveWithAnyArgs().MoveTip(null, Arg.Any<Vector3>());
        }

        [Test]
        public void DetachedBone_DragRootBody_ModelMoveWholeHierachy()
        {
            var bones = GenerateDetachedRootAndChild();
            var root = bones[0];
            var child = bones[1];

            Vector3 draggedPosition = Vector3.one;
            m_HierarchyViewMock.HandleBoneDrag(root).Returns(draggedPosition);

            var state = new BoneEditorState();
            state.selectedBones.Add(root);
            m_BonePresenter.state = state;

            m_BonePresenter.ResetRelationship();
            m_BonePresenter.DoBone(Rect.zero);
            
            m_ModelMock.ReceivedWithAnyArgs(1).MoveBone(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveBone(root, draggedPosition, false);
            m_ModelMock.DidNotReceiveWithAnyArgs().MoveTip(null, Arg.Any<Vector3>());
        }

        [Test]
        public void DetachedBone_DragChildBody_ModelMoveChildBoneOnly()
        {
            var bones = GenerateDetachedRootAndChild();
            var root = bones[0];
            var child = bones[1];

            Vector3 draggedPosition = Vector3.one;
            m_HierarchyViewMock.HandleBoneDrag(child).Returns(draggedPosition);

            var state = new BoneEditorState();
            state.selectedBones.Add(child);
            m_BonePresenter.state = state;

            m_BonePresenter.ResetRelationship();
            m_BonePresenter.DoBone(Rect.zero);

            m_ModelMock.ReceivedWithAnyArgs(1).MoveBone(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveBone(child, draggedPosition, false);
            m_ModelMock.DidNotReceiveWithAnyArgs().MoveTip(null, Arg.Any<Vector3>());
        }

        [Test]
        public void FreeMoveState_DetachedBone_DragRootBody_ModelMoveRootBoneOnly()
        {
            var bones = GenerateDetachedRootAndChild();
            var root = bones[0];
            var child = bones[1];

            Vector3 draggedPosition = Vector3.one;
            m_HierarchyViewMock.HandleBoneDrag(root).Returns(draggedPosition);

            var state = new BoneEditorState();
            state.selectedBones.Add(root);
            state.freeMoving = true;
            m_BonePresenter.state = state;

            m_BonePresenter.ResetRelationship();
            m_BonePresenter.DoBone(Rect.zero);

            m_ModelMock.ReceivedWithAnyArgs(1).MoveBone(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveBone(root, draggedPosition);
            m_ModelMock.DidNotReceiveWithAnyArgs().MoveTip(null, Arg.Any<Vector3>());
        }
    }

    [TestFixture]
    internal class PresenterComplexHierachyTest : BonePresenterTestBase
    {
        [Test]
        public void SelectedMultipleBone_DragIndividualNode_OnlyMoveThatNode()
        {
            var bones = GenerateComplexHierachy();
            var child1 = bones[1];
            var child2 = bones[2];
            var child4 = bones[4];

            Vector3 draggedPosition = Vector3.one;
            m_HierarchyViewMock.HandleBoneNodeDrag(child2).Returns(draggedPosition);

            var state = new BoneEditorState();
            state.selectedBones.Add(child2);
            state.selectedBones.Add(child4);
            m_BonePresenter.state = state;

            m_BonePresenter.ResetRelationship();
            m_BonePresenter.DoBone(Rect.zero);
            
            m_ModelMock.ReceivedWithAnyArgs(1).MoveBone(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveBone(child2, draggedPosition);
            m_ModelMock.ReceivedWithAnyArgs(2).MoveTip(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveTip(child1, draggedPosition);
            m_ModelMock.Received(1).MoveTip(child2, child2.tip);
        }

        [Test]
        public void SelectedMultipleBone_DragIndividualTip_OnlyMoveThatTip()
        {
            var bones = GenerateComplexHierachy();
            var child2 = bones[2];
            var child4 = bones[4];

            Vector3 draggedPosition = Vector3.one;
            m_HierarchyViewMock.HandleBoneTipDrag(child4).Returns(draggedPosition);
            
            var state = new BoneEditorState();
            state.selectedBones.Add(child2);
            state.selectedBones.Add(child4);
            m_BonePresenter.state = state;

            m_BonePresenter.ResetRelationship();
            m_BonePresenter.DoBone(Rect.zero);

            m_ModelMock.DidNotReceiveWithAnyArgs().MoveBone(null, Arg.Any<Vector3>());
            m_ModelMock.ReceivedWithAnyArgs(1).MoveTip(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveTip(child4, draggedPosition);
        }

        [Test]
        public void SelectedMultipleBone_DragIndividualBody_MoveAllSelectedBoneAndHierachy()
        {
            var bones = GenerateComplexHierachy();
            var child1 = bones[1];
            var child2 = bones[2];
            var child3 = bones[3];
            var child4 = bones[4];

            Vector3 draggedPosition = Vector3.one;
            m_HierarchyViewMock.HandleBoneDrag(child2).Returns(draggedPosition);
            
            var state = new BoneEditorState();
            state.selectedBones.Add(child2);
            state.selectedBones.Add(child3);
            m_BonePresenter.state = state;

            m_BonePresenter.ResetRelationship();
            m_BonePresenter.DoBone(Rect.zero);

            var delta = draggedPosition - child2.position;

            m_ModelMock.ReceivedWithAnyArgs(2).MoveBone(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveBone(child2, draggedPosition, false);
            m_ModelMock.Received(1).MoveBone(child3, child3.position + delta, false);
            m_ModelMock.ReceivedWithAnyArgs(1).MoveTip(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveTip(child1, draggedPosition);
        }

        [Test]
        public void SelectedMultipleBone_DragParentOfAll_MoveOnlyTheParentHierachy()
        {
            var bones = GenerateComplexHierachy();
            var root = bones[0];
            var child1 = bones[1];
            var child2 = bones[2];
            var child3 = bones[3];
            var child4 = bones[4];

            Vector3 draggedPosition = Vector3.one;
            m_HierarchyViewMock.HandleBoneDrag(child1).Returns(draggedPosition);
            
            var state = new BoneEditorState();
            state.selectedBones.Add(child1);
            state.selectedBones.Add(child2);
            state.selectedBones.Add(child3);
            state.selectedBones.Add(child4);
            m_BonePresenter.state = state;

            m_BonePresenter.ResetRelationship();
            m_BonePresenter.DoBone(Rect.zero);
            
            m_ModelMock.ReceivedWithAnyArgs(1).MoveBone(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveBone(child1, draggedPosition, false);
            m_ModelMock.ReceivedWithAnyArgs(1).MoveTip(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveTip(root, draggedPosition);
        }

        [Test]
        public void FreeMoveState_SelectedMultipleBone_DragIndividualNode_OnlyMoveThatNode()
        {
            var bones = GenerateComplexHierachy();
            var child1 = bones[1];
            var child2 = bones[2];
            var child4 = bones[4];

            Vector3 draggedPosition = Vector3.one;
            m_HierarchyViewMock.HandleBoneNodeDrag(child2).Returns(draggedPosition);
            
            var state = new BoneEditorState();
            state.selectedBones.Add(child2);
            state.selectedBones.Add(child4);
            state.freeMoving = true;
            m_BonePresenter.state = state;

            m_BonePresenter.ResetRelationship();
            m_BonePresenter.DoBone(Rect.zero);
            
            m_ModelMock.ReceivedWithAnyArgs(1).MoveBone(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveBone(child2, draggedPosition);
            m_ModelMock.ReceivedWithAnyArgs(1).MoveTip(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveTip(child2, child2.tip);
        }

        [Test]
        public void FreeMoveState_SelectedMultipleBone_DragIndividualTip_OnlyMoveThatTip()
        {
            var bones = GenerateComplexHierachy();
            var child2 = bones[2];
            var child4 = bones[4];

            Vector3 draggedPosition = Vector3.one;
            m_HierarchyViewMock.HandleBoneTipDrag(child4).Returns(draggedPosition);

            var state = new BoneEditorState();
            state.selectedBones.Add(child2);
            state.selectedBones.Add(child4);
            state.freeMoving = true;
            m_BonePresenter.state = state;

            m_BonePresenter.ResetRelationship();
            m_BonePresenter.DoBone(Rect.zero);
            
            m_ModelMock.DidNotReceiveWithAnyArgs().MoveBone(null, Arg.Any<Vector3>());
            m_ModelMock.ReceivedWithAnyArgs(1).MoveTip(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveTip(child4, draggedPosition);
        }

        [Test]
        public void FreeMoveState_SelectedMultipleBone_DragIndividualBody_MoveAllSelectedBoneOnly()
        {
            var bones = GenerateComplexHierachy();
            var child2 = bones[2];
            var child3 = bones[3];

            Vector3 draggedPosition = Vector3.one;
            m_HierarchyViewMock.HandleBoneDrag(child2).Returns(draggedPosition);
            
            var state = new BoneEditorState();
            state.selectedBones.Add(child2);
            state.selectedBones.Add(child3);
            state.freeMoving = true;
            m_BonePresenter.state = state;

            m_BonePresenter.ResetRelationship();
            m_BonePresenter.DoBone(Rect.zero);

            var delta = draggedPosition - child2.position;
            
            m_ModelMock.ReceivedWithAnyArgs(2).MoveBone(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveBone(child2, draggedPosition);
            m_ModelMock.Received(1).MoveBone(child3, child3.position + delta);
            m_ModelMock.DidNotReceiveWithAnyArgs().MoveTip(null, Arg.Any<Vector3>());

        }

        [Test]
        public void SelectedOneBone_ParentingState_ClickOnValidNewParent_ModelUpdateParent()
        {
            var bones = GenerateComplexHierachy();
            var child2 = bones[2];
            var child4 = bones[4];
            
            m_HierarchyViewMock.HandleBoneSelect(child2).Returns(true);
            
            var state = new BoneEditorState();
            state.selectedBones.Add(child4);
            state.parenting = true;
            m_BonePresenter.state = state;

            m_BonePresenter.DoBone(Rect.zero);
            
            m_ModelMock.ReceivedWithAnyArgs(1).Parent(null, null);
            m_ModelMock.Received(1).Parent(child4, child2);
        }

        [Test]
        public void SelectedMultipleBone_ParentingState_ClickOnValidNewParent_ModelUpdateParent()
        {
            var bones = GenerateComplexHierachy();
            var child2 = bones[2];
            var child3 = bones[3];
            var child4 = bones[4];

            m_HierarchyViewMock.HandleBoneSelect(child2).Returns(true);

            var state = new BoneEditorState();
            state.selectedBones.Add(child3);
            state.selectedBones.Add(child4);
            state.parenting = true;
            m_BonePresenter.state = state;

            m_BonePresenter.DoBone(Rect.zero);
            
            m_ModelMock.ReceivedWithAnyArgs(2).Parent(null, null);
            m_ModelMock.Received(1).Parent(child3, child2);
            m_ModelMock.Received(1).Parent(child4, child2);
        }

        [Test]
        public void TryToParentAParentToAChild_PreventItAndDisplayError()
        {
            var bones = GenerateComplexHierachy();
            var child1 = bones[1];
            var child3 = bones[3];
            var child4 = bones[4];
            
            m_HierarchyViewMock.HandleBoneSelect(child4).Returns(true);

            var state = new BoneEditorState();
            state.selectedBones.Add(child1);
            state.selectedBones.Add(child3);
            state.parenting = true;
            m_BonePresenter.state = state;
            
            Assert.Throws<InvalidOperationException>(
                () => { m_BonePresenter.DoBone(Rect.zero); },
                "Cannot parent {0} to {1}. This will create a loop.", child1.name, child3.name);

            m_ModelMock.DidNotReceiveWithAnyArgs().Parent(null, null);
        }

        [Test]
        public void SplitBoneWithMultipleChildren_CreateNewBoneAtMiddleAndReparentAllChildren()
        {
            m_ToolViewMock.HandleSplit(true).Returns(true);

            var bones = GenerateComplexHierachy();
            var child1 = bones[1];
            var child2 = bones[2];
            var child3 = bones[3];

            var state = new BoneEditorState();
            state.selectedBones.Add(child1);
            m_BonePresenter.state = state;

            m_BonePresenter.DoTool(new Rect());

            var middlePoint = Vector3.Lerp(child1.position, child1.tip, 0.5f);
            m_ModelMock.ReceivedWithAnyArgs(1).CreateNewChildBone(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).CreateNewChildBone(child1, middlePoint);
            m_ModelMock.ReceivedWithAnyArgs(2).MoveTip(null, Arg.Any<Vector3>());
            m_ModelMock.Received(1).MoveTip(child1, middlePoint);
            m_ModelMock.Received(1).MoveTip(Arg.Any<IBone>(), child1.tip);
            m_ModelMock.ReceivedWithAnyArgs(2).Parent(null, null);
            m_ModelMock.Received(1).Parent(child2, Arg.Any<IBone>());
            m_ModelMock.Received(1).Parent(child3, Arg.Any<IBone>());
        }

        [Test]
        public void DeleteBoneWithMultipleChildren_DeleteTheBoneAndReparentAllChildren()
        {
            m_ToolViewMock.HandleDelete(true).Returns(true);

            var bones = GenerateComplexHierachy();
            var root = bones[0];
            var child1 = bones[1];
            var child2 = bones[2];
            var child3 = bones[3];

            var state = new BoneEditorState();
            state.selectedBones.Add(child1);
            m_BonePresenter.state = state;

            m_BonePresenter.DoTool(new Rect());

            m_ModelMock.ReceivedWithAnyArgs(1).DeleteBone(null);
            m_ModelMock.Received(1).DeleteBone(child1);
            m_ModelMock.ReceivedWithAnyArgs(2).Parent(null, null);
            m_ModelMock.Received(1).Parent(child2, root);
            m_ModelMock.Received(1).Parent(child3, root);
        }
    }
}
