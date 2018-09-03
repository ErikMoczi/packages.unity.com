using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEngine.Experimental.U2D;

using NUnit.Framework;
using NSubstitute;

namespace UnityEditor.Experimental.U2D.Animation.Test.Bone
{
    internal class BoneSpriteEditorModuleTests
    {
        private ISpriteEditor m_SpriteEditor;
        private ISpriteBoneDataProvider m_BoneDataProvider;
        private SpriteRect m_SR1;
        private SpriteRect m_SR2;

        private BoneSpriteEditorModule m_Module;

        [SetUp]
        public void TestSetup()
        {
            m_BoneDataProvider = Substitute.For<ISpriteBoneDataProvider>();
            m_SpriteEditor = Substitute.For<ISpriteEditor>();
            m_SR1 = new SpriteRect();
            m_SR2 = new SpriteRect();

            m_SR1.spriteID = new GUID() {  };

            List<SpriteBone> bones1 = new List<SpriteBone>();
            bones1.Add(new SpriteBone() { name = "root1", parentId = -1, rotation = Quaternion.identity });
            m_BoneDataProvider.GetBones(m_SR1.spriteID).Returns(bones1);

            List<SpriteBone> bones2 = new List<SpriteBone>();
            bones2.Add(new SpriteBone() { name = "root2", parentId = -1, rotation = Quaternion.identity });
            m_BoneDataProvider.GetBones(m_SR2.spriteID).Returns(bones2);

            m_SpriteEditor.GetDataProvider<ISpriteBoneDataProvider>().Returns(m_BoneDataProvider);

            m_Module = new BoneSpriteEditorModule();
            var prop = m_Module.GetType().GetProperty("spriteEditor");
            prop.SetValue(m_Module, m_SpriteEditor, null);
        }

        [Test]
        public void ActivateModule_NoSelectedSpriteRect_NoGettingOfBoneData()
        {
            m_SpriteEditor.selectedSpriteRect.Returns(null as SpriteRect);

            m_Module.OnModuleActivate();

            m_BoneDataProvider.DidNotReceiveWithAnyArgs().GetBones(Arg.Any<GUID>());
        }

        [Test]
        public void ActivateModule_WithSelectedSpriteRect_GetBoneDataRelatedIt()
        {
            m_SpriteEditor.selectedSpriteRect.Returns(m_SR1);

            m_Module.OnModuleActivate();

            m_BoneDataProvider.ReceivedWithAnyArgs(1).GetBones(Arg.Any<GUID>());
            m_BoneDataProvider.Received(1).GetBones(m_SR1.spriteID);
        }

        [Test]
        public void ActivateModule_StartedWithNoSelectedSpriteRect_TriggerASelection_GetBoneDataForNewSelection()
        {
            m_SpriteEditor.selectedSpriteRect.Returns(null as SpriteRect);

            m_Module.OnModuleActivate();

            var moduleViewMock = Substitute.For<IBoneSpriteEditorModuleView>();
            moduleViewMock.HandleSpriteSelection().Returns(true);
            m_Module.spriteEditorModuleView = moduleViewMock;

            m_SpriteEditor.selectedSpriteRect.Returns(m_SR2);

            m_Module.DoMainGUI();

            m_BoneDataProvider.ReceivedWithAnyArgs(1).GetBones(Arg.Any<GUID>());
            m_BoneDataProvider.Received(1).GetBones(m_SR2.spriteID);
        }

        [Test]
        public void ActivateModule_WithSelectedSpriteRect_SelectedAnotherRect_GotBoneDataForBoth()
        {
            m_SpriteEditor.selectedSpriteRect.Returns(m_SR1);

            m_Module.OnModuleActivate();

            var moduleViewMock = Substitute.For<IBoneSpriteEditorModuleView>();
            moduleViewMock.HandleSpriteSelection().Returns(true);
            m_Module.spriteEditorModuleView = moduleViewMock;

            m_SpriteEditor.selectedSpriteRect.Returns(m_SR2);

            m_Module.DoMainGUI();

            m_BoneDataProvider.ReceivedWithAnyArgs(2).GetBones(Arg.Any<GUID>());
            m_BoneDataProvider.Received(1).GetBones(m_SR1.spriteID);
            m_BoneDataProvider.Received(1).GetBones(m_SR2.spriteID);
        }

        [Test]
        public void NoBoneDataEverRequested_ApplyChanges_NoSetBoneDataWillBeCall()
        {
            m_SpriteEditor.selectedSpriteRect.Returns(null as SpriteRect);

            m_Module.OnModuleActivate();
            m_Module.ApplyRevert(true);

            m_BoneDataProvider.DidNotReceiveWithAnyArgs().SetBones(Arg.Any<GUID>(), Arg.Any<List<SpriteBone>>());
        }

        [Test]
        public void GotTwiceBoneData_ApplyChanges_SetTwiceBoneData()
        {
            m_SpriteEditor.selectedSpriteRect.Returns(m_SR1);

            m_Module.OnModuleActivate();

            var moduleViewMock = Substitute.For<IBoneSpriteEditorModuleView>();
            moduleViewMock.HandleSpriteSelection().Returns(true);
            m_Module.spriteEditorModuleView = moduleViewMock;

            m_SpriteEditor.selectedSpriteRect.Returns(m_SR2);

            m_Module.DoMainGUI();
            m_Module.ApplyRevert(true);

            m_BoneDataProvider.ReceivedWithAnyArgs(2).SetBones(Arg.Any<GUID>(), Arg.Any<List<SpriteBone>>());
            m_BoneDataProvider.Received(1).SetBones(m_SR1.spriteID, Arg.Any<List<SpriteBone>>());
            m_BoneDataProvider.Received(1).SetBones(m_SR2.spriteID, Arg.Any<List<SpriteBone>>());
        }

        [Test]
        public void GotBoneData_RevertChanges_NoSetBoneDataCalled()
        {
            m_SpriteEditor.selectedSpriteRect.Returns(m_SR1);

            m_Module.OnModuleActivate();
            m_Module.ApplyRevert(false);

            m_BoneDataProvider.DidNotReceiveWithAnyArgs().SetBones(Arg.Any<GUID>(), Arg.Any<List<SpriteBone>>());
        }
    }
}
