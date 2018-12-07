using System;
using NSubstitute;
using NUnit.Framework;
using UnityEngine.Events;

namespace UnityEditor.Experimental.U2D.Animation.Test.SpriteBoneInfluenceToolTests
{
    public class SpriteBoneInfluenceToolController
    {
        SkinningEvents m_Events;
        ISpriteBoneInfluenceToolModel m_Model;
        SpriteBoneInflueceToolController m_Controller;
        ISpriteBoneInfluenceWindow m_View;

        [SetUp]
        public void Setup()
        {
            m_Events = Substitute.For<SkinningEvents>();
            m_Events.selectedSpriteChanged.Returns(Substitute.For<SkinningEvents.SpriteEvent>());
            m_Events.boneSelectionChanged.Returns(Substitute.For<SkinningEvents.BoneSelectionEvent>());
            m_Events.boneNameChanged.Returns(Substitute.For<SkinningEvents.BoneEvent>());
            m_Events.skeletonTopologyChanged.Returns(Substitute.For<SkinningEvents.SkeletonEvent>());
            m_Events.characterPartChanged.Returns(Substitute.For<SkinningEvents.CharacterPartEvent>());
            m_Events.meshChanged.Returns(Substitute.For<SkinningEvents.MeshEvent>());

            m_Model = Substitute.For<ISpriteBoneInfluenceToolModel>();
            m_View = Substitute.For<ISpriteBoneInfluenceWindow>();
            m_View.visible.Returns(true);
            m_Model.view.Returns(m_View);
            m_Controller = new SpriteBoneInflueceToolController(m_Model, m_Events);
            m_Model.hasCharacter.Returns(true);
        }

        [Test]
        public void OnActivate_SetupsCorrectly()
        {
            m_Controller.Activate();
            m_Events.selectedSpriteChanged.Received(1).AddListener(Arg.Any<UnityAction<SpriteCache>>());
            m_Events.boneSelectionChanged.Received(1).AddListener(Arg.Any<UnityAction>());
            m_Events.boneNameChanged.Received(1).AddListener(Arg.Any<UnityAction<BoneCache>>());
            m_Events.skeletonTopologyChanged.Received(1).AddListener(Arg.Any<UnityAction<SkeletonCache>>());
            m_Events.meshChanged.Received(1).AddListener(Arg.Any<UnityAction<MeshCache>>());
            m_View.Received(1).SetHiddenFromLayout(false);
            m_View.Received(1).headerText = TextContent.noSpriteSelected;
        }

        [Test]
        public void OnDeactivate_TearDownsCorrectly()
        {
            m_Controller.Activate();
            m_Controller.Deactivate();
            m_View.Received(1).SetHiddenFromLayout(true);
        }

        [Test]
        public void OnMeshChanged_InformsViewOnChange()
        {
            m_Controller.Activate();
            m_Events.meshChanged.Invoke(null);
            m_View.Received(1).OnMeshChanged();
        }

        [Test]
        public void OnSpriteSelectionChanged_InformsViewOnChange()
        {
            m_Controller.Activate();
            var spriteCache = CacheObject.Create<SpriteCache>(null);
            spriteCache.name = "Test Sprite";
            m_Model.selectedSprite.Returns(spriteCache);
            m_View.ClearReceivedCalls();
            m_Events.selectedSpriteChanged.Invoke(spriteCache);
            m_View.Received(1).OnSpriteSelectionChanged();
            m_View.Received(1).headerText = "Test Sprite";
        }

        [Test]
        public void OnBoneSelectionChanged_InformsViewOnChange()
        {
            m_Controller.Activate();
            m_View.ClearReceivedCalls();
            m_Events.boneSelectionChanged.Invoke();
            m_View.Received(1).OnBoneSelectionChanged();
        }

        [Test]
        public void OnBoneNameChanged_InformsViewOnChange()
        {
            m_Controller.Activate();
            m_View.ClearReceivedCalls();
            m_Events.boneNameChanged.Invoke(null);
            m_View.Received(1).OnSkeletonChanged();
        }

        [Test]
        public void OnSkeletonTopologyChanged_InformsViewOnChange()
        {
            m_Controller.Activate();
            m_View.ClearReceivedCalls();
            m_Events.skeletonTopologyChanged.Invoke(null);
            m_View.Received(1).OnSkeletonChanged();
        }

        [Test]
        public void OnViewCreated_RegisterToViewCallback()
        {
            m_Controller.OnViewCreated();
            m_View.Received(1).onAddBone += Arg.Any<Action>();
            m_View.Received(1).onRemoveBone += Arg.Any<Action>();
            m_View.Received(1).onSelectionChanged += Arg.Any<Action<BoneCache[]>>();
            m_View.Received(1).SetController(Arg.Is(m_Controller));
        }

        [Test]
        public void GetSelectedSpriteBoneInfluence_GetsCorrectDataFromModel()
        {
            var spriteCache = CacheObject.Create<SpriteCache>(null);
            var characterpart = CacheObject.Create<CharacterPartCache>(null);
            var bones = new[]
            {
                CacheObject.Create<BoneCache>(null),
                CacheObject.Create<BoneCache>(null),
            };
            characterpart.bones = bones;
            m_Model.selectedSprite.Returns(spriteCache);
            m_Model.GetSpriteCharacterPart(Arg.Is(spriteCache)).Returns(characterpart);
            var bonesResult = m_Controller.GetSelectedSpriteBoneInfluence();
            Assert.AreEqual(bones.Length, bonesResult.Length);
            for (int i = 0; i < bones.Length; ++i)
                Assert.AreEqual(bones[i], bonesResult[1]);
        }

        [Test]
        public void ShouldEnableAddButton_ReturnsCorrectButtonState()
        {
            var spriteCache = CacheObject.Create<SpriteCache>(null);
            m_Model.selectedSprite.Returns(spriteCache);

            var bones = new[]
            {
                CacheObject.Create<BoneCache>(null),
                CacheObject.Create<BoneCache>(null),
            };
            m_Model.selectedBones.Returns(bones);
            Assert.IsTrue(m_Controller.ShouldEnableAddButton(new BoneCache[0]));
        }

        [Test]
        public void OnViewSelectionChanged_UpdatesModelOnSelectionChange()
        {
            m_Controller.OnViewCreated();
            var bones = new[]
            {
                CacheObject.Create<BoneCache>(null),
                CacheObject.Create<BoneCache>(null),
            };
            m_View.onSelectionChanged += Raise.Event<Action<BoneCache[]>>(new[] {bones});
            m_Model.Received(1).selectedBones = Arg.Any<BoneCache[]>();
            BoneCache[] boneResult = m_Model.selectedBones;
            Assert.AreEqual(bones.Length, boneResult.Length);
        }

        [Test]
        public void OnAddBone_AddsNewBoneToModel()
        {
            m_Controller.OnViewCreated();
            SkeletonCache skeleton = CacheObject.Create<SkeletonCache>(null);
            m_Model.characterSkeleton.Returns(skeleton);

            var spriteCache = CacheObject.Create<SpriteCache>(null);
            var characterpart = CacheObject.Create<CharacterPartCache>(null);
            var bones = new[]
            {
                CacheObject.Create<BoneCache>(null),
                CacheObject.Create<BoneCache>(null),
            };
            characterpart.bones = new BoneCache[0];
            m_Model.selectedSprite.Returns(spriteCache);
            m_Model.GetSpriteCharacterPart(Arg.Is(spriteCache)).Returns(characterpart);
            m_Model.selectedBones.Returns(bones);
            m_View.onAddBone += Raise.Event<Action>(new object[0]);
            Assert.AreEqual(bones.Length, characterpart.bones.Length);
        }

        [Test]
        public void OnRemoveBone_RemovesBoneFromModel()
        {
            m_Controller.OnViewCreated();
            SkeletonCache skeleton = CacheObject.Create<SkeletonCache>(null);
            m_Model.characterSkeleton.Returns(skeleton);

            var spriteCache = CacheObject.Create<SpriteCache>(null);
            var characterpart = CacheObject.Create<CharacterPartCache>(null);
            var bones = new[]
            {
                CacheObject.Create<BoneCache>(null),
                CacheObject.Create<BoneCache>(null),
            };
            characterpart.bones = bones;
            m_Model.selectedSprite.Returns(spriteCache);
            m_Model.GetSpriteCharacterPart(Arg.Is(spriteCache)).Returns(characterpart);
            m_Model.selectedBones.Returns(bones);
            m_View.onRemoveBone += Raise.Event<Action>(new object[0]);
            Assert.AreEqual(0, characterpart.bones.Length);
        }

        [Test]
        public void GetSelectedBoneForList_RetunsCorrectValue()
        {
            var spriteCache = CacheObject.Create<SpriteCache>(null);
            var characterpart = CacheObject.Create<CharacterPartCache>(null);
            var bones = new[]
            {
                CacheObject.Create<BoneCache>(null),
                CacheObject.Create<BoneCache>(null),
            };
            characterpart.bones = bones;
            m_Model.selectedSprite.Returns(spriteCache);
            m_Model.selectedBones.Returns(bones);
            m_Model.GetSpriteCharacterPart(Arg.Is(spriteCache)).Returns(characterpart);
            var bonesResult = m_Controller.GetSelectedBoneForList(bones);
            Assert.AreEqual(bones.Length, bonesResult.Length);
            for (int i = 0; i < bones.Length; ++i)
                Assert.AreEqual(i, bonesResult[i]);
        }
    }
}
