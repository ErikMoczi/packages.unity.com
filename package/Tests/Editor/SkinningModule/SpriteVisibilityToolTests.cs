using NSubstitute;
using NUnit.Framework;
using UnityEngine.Events;

namespace  UnityEditor.Experimental.U2D.Animation.Test.SpriteVisibilityTests
{
    internal class SpriteVisibilityToolControllerTests
    {
        SpriteVisibilityToolController m_Controller;
        ISpriteVisibilityToolModel m_Model;
        SkinningEvents m_Events;
        ISpriteVisibilityToolView m_View;

        [SetUp]
        public void Setup()
        {
            m_Model = Substitute.For<ISpriteVisibilityToolModel>();
            m_Events = Substitute.For<SkinningEvents>();
            m_Events.skinningModeChanged.Returns(Substitute.For<SkinningEvents.SkinningModuleModeEvent>());
            m_Events.selectedSpriteChanged.Returns(Substitute.For<SkinningEvents.SpriteEvent>());
            m_View = Substitute.For<ISpriteVisibilityToolView>();
            m_Model.view.Returns(m_View);
            m_Controller = new SpriteVisibilityToolController(m_Model, m_Events);
        }

        [Test]
        public void OnActivate_HaveCorrectSetup()
        {
            var spriteCache = CacheObject.Create<SpriteCache>(null);
            m_Model.selectedSprite.Returns(spriteCache);
            m_Model.previousVisibility.Returns(true);
            m_Model.allVisibility.Returns(true);

            m_Events.skinningModeChanged.Received(1).AddListener(Arg.Any<UnityAction<SkinningMode>>());
            m_Controller.Activate();
            m_Events.selectedSpriteChanged.Received(1).AddListener(Arg.Any<UnityAction<SpriteCache>>());
            m_View.Received(1).Setup();
            m_Model.DidNotReceiveWithAnyArgs().previousVisibility = true;
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void OnActivateWithPreviousVisibilityDifferent_ResetsSpriteVisibility(bool hasCharacter)
        {
            var character = Substitute.For<CharacterCache>();
            var parts = new[]
            {
                Substitute.For<CharacterPartCache>(),
                Substitute.For<CharacterPartCache>()
            };
            character.parts.Returns(parts);
            m_Model.character.Returns(character);
            m_Model.hasCharacter.Returns(hasCharacter);
            m_Model.previousVisibility.Returns(true);
            m_Model.allVisibility.Returns(false);
            m_Controller.Activate();


            m_Model.Received(1).previousVisibility = false;
            var recieved = hasCharacter ? 1 : 0;
            foreach (var p in parts)
                p.Received(recieved).isVisible = false;
        }

        [Test]
        [TestCase(SkinningMode.SpriteSheet, 0)]
        [TestCase(SkinningMode.Character, 1)]
        public void OnViewModeChanged_UpdatesToolAvailability(SkinningMode mode, int hasCharacterRecievedCall)
        {
            bool availabilityChangedCalled = false;
            m_Controller.OnAvailabilityChangeListeners += () => availabilityChangedCalled = true;
            m_Controller.Activate();
            m_Model.mode.Returns(mode);
            m_Model.previousVisibility.Returns(false);
            m_Model.allVisibility.Returns(true);
            m_Events.skinningModeChanged.Invoke(mode);
            var temp = m_Model.Received(hasCharacterRecievedCall).hasCharacter;
            Assert.IsTrue(availabilityChangedCalled);
        }

        [Test]
        public void OnSpriteSelectedChanged_UpdatesViewOnSelection()
        {
            m_Controller.Activate();
            m_View.ClearReceivedCalls();
            m_Events.selectedSpriteChanged.Invoke(null);
            m_View.Received(1).SetSelection(Arg.Any<SpriteCache>());
        }

        static void SetupCharacter(out CharacterCache character, out CharacterPartCache[] parts, out SpriteCache[] sprites)
        {
            character = Substitute.For<CharacterCache>();
            parts = new[]
            {
                Substitute.For<CharacterPartCache>(),
                Substitute.For<CharacterPartCache>()
            };
            sprites = new[]
            {
                Substitute.For<SpriteCache>(),
                Substitute.For<SpriteCache>()
            };
            parts[0].sprite.Returns(sprites[0]);
            parts[1].sprite.Returns(sprites[1]);
            sprites[0].name = "Sprite1";
            sprites[1].name = "Sprite2";
            character.parts.Returns(parts);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void GetAllVisibility_RetrivesValueFromModel(bool value)
        {
            m_Model.allVisibility.Returns(value);
            Assert.AreEqual(value, m_Controller.GetAllVisibility());
        }

        [Test]
        public void BuildTreeView_CreatesTreeViewDataBsedOnCharacterparts()
        {
            CharacterCache character;
            CharacterPartCache[] parts;
            SpriteCache[] sprites;
            SetupCharacter(out character, out parts, out sprites);
            m_Model.character.Returns(character);
            var treeView = m_Controller.BuildTreeView();
            Assert.AreEqual(parts.Length, treeView.Count);
            for (int i = 0; i < parts.Length; ++i)
            {
                Assert.AreEqual(parts[i].GetInstanceID(), treeView[i].id);
                Assert.AreEqual(sprites[i].name, treeView[i].displayName);
            }
        }

        [Test]
        [TestCase(true, true)]
        [TestCase(true, false)]
        public void SetCharacterPartVisibility_UpdatesVisibilityValue(bool visible, bool isolate)
        {
            CharacterCache character;
            CharacterPartCache[] parts;
            SpriteCache[] sprites;
            SetupCharacter(out character, out parts, out sprites);
            m_Model.character.Returns(character);
            var treeView = m_Controller.BuildTreeView();
            m_Controller.SetCharacterPartVisibility(treeView[0], visible, isolate);

            if (isolate)
            {
                parts[0].Received(1).isVisible = !visible;
                parts[1].Received(1).isVisible = visible;
            }
            else
            {
                parts[0].Received(1).isVisible = visible;
                parts[1].DidNotReceive().isVisible = Arg.Any<bool>();
            }
        }

        [Test]
        public void SetSelectedSprite_InvokesEventOnSelectionChanged()
        {
            CharacterCache character;
            CharacterPartCache[] parts;
            SpriteCache[] sprites;
            SetupCharacter(out character, out parts, out sprites);
            m_Model.character.Returns(character);
            var treeView = m_Controller.BuildTreeView();

            var selectedId = 0;
            UnityAction<SpriteCache> selectedCallback = sprite => selectedId = sprite.GetInstanceID();
            m_Events.selectedSpriteChanged.AddListener(selectedCallback);
            m_Controller.SetSelectedSprite(treeView, new[] {parts[0].GetInstanceID()});
            m_Events.selectedSpriteChanged.RemoveListener(selectedCallback);
            Assert.AreEqual(sprites[0].GetInstanceID(), selectedId);
        }

        [Test]
        public void GetTreeViewSelectionID_ReturnsCorrectIDForTreeView()
        {
            CharacterCache character;
            CharacterPartCache[] parts;
            SpriteCache[] sprites;
            SetupCharacter(out character, out parts, out sprites);
            m_Model.character.Returns(character);
            var treeView = m_Controller.BuildTreeView();

            var selectedId = m_Controller.GetTreeViewSelectionID(treeView, sprites[0]);
            Assert.AreEqual(parts[0].GetInstanceID(), selectedId);
        }

        [Test]
        public void SetAllVisibility_SetsAllPartsVisibility()
        {
            CharacterCache character;
            CharacterPartCache[] parts;
            SpriteCache[] sprites;
            SetupCharacter(out character, out parts, out sprites);
            m_Model.hasCharacter.Returns(true);
            m_Model.character.Returns(character);
            m_Model.allVisibility.Returns(false);
            m_Controller.SetAllVisibility(true);
            m_Model.Received(1).allVisibility = true;
            foreach (var part in parts)
                part.Received(1).isVisible = true;
        }

        [Test]
        public void GetCharacterPartVisibility_ReturnsCorrectVisibilitValue()
        {
            var part = CacheObject.Create<CharacterPartCache>(null);
            part.isVisible = false;
            var treeViewItem = new TreeViewItemBase<CharacterPartCache>(0, 0, "", part);
            Assert.IsFalse(m_Controller.GetCharacterPartVisibility(treeViewItem));
        }
    }
}
