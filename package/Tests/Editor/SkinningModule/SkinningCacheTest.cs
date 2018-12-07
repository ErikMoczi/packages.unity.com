using NUnit.Framework;

namespace UnityEditor.Experimental.U2D.Animation.Test.SkinningModuleTests
{
    public class SkinningCacheSpriteSheetTest : SkinningModuleTestBase
    {
        [Test]
        public void LoadsCorrectSpritesFromAsset()
        {
            Assert.IsTrue(skinningCache.GetSprites().Length == 2);
        }

        [Test]
        public void LoadsCorrectBonesFromAssetInSpriteSheetMode()
        {
            var sprite = skinningCache.GetSprites()[0];
            Assert.IsTrue(sprite.GetSkeleton().bones.Length == 3);
        }

        [Test]
        public void InvokeSelectedSpriteChangedWithSprite_SelectsSprite()
        {
            var sprite = skinningCache.GetSprites()[0];
            skinningCache.events.selectedSpriteChanged.Invoke(sprite);

            Assert.AreEqual(sprite, skinningCache.selectedSprite);
        }

        [Test]
        public void InvokeSelectedSpriteChangedWithNull_SelectsNull()
        {
            skinningCache.events.selectedSpriteChanged.Invoke(null);

            Assert.AreEqual(null, skinningCache.selectedSprite);
        }
    }

    public class SkinningCacheCharacterTest : SkinningModuleCharacterTestBase
    {
        [Test]
        public void LoadsCorrectBonesFromAssetInSpriteSheetMode()
        {
            var sprite = skinningCache.GetSprites()[0];
            Assert.AreEqual(0, sprite.GetSkeleton().bones.Length);
        }

        [Test]
        public void LoadsCorrectBonesFromAssetInCharacterMode()
        {
            var sprite = skinningCache.GetSprites()[0];
            Assert.AreEqual(0, sprite.GetCharacterPart().bones.Length);
        }

        [Test]
        public void LoadsCorrectSkeletonBonesFromAssetInCharacterMode()
        {
            Assert.AreEqual(3, skinningCache.character.skeleton.BoneCount);
        }
    }
}
