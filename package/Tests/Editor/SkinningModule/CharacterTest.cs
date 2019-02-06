using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using NUnit.Framework;
using UnityEditor.Experimental.U2D.Animation.Test.SkinningModuleTests;

namespace UnityEditor.Experimental.U2D.Animation.Test.Character
{
    public class CharacterSkeleton : SkinningModuleCharacterTestBase
    {
        protected override SpriteEditorWindowFake GetWindowFake()
        {
            var window = EditorWindow.GetWindow<SpriteEditorWindowCharacterFake>();
            window.Populate();
            return window;
        }

        private void SwapCharacterData(CharacterData data)
        {
            var window = m_Window as SpriteEditorWindowCharacterFake;
            window.SetCharacterData(data);
            window.Deactivate();
            window.Activate();
        }

        private CharacterData SharedBones()
        {
            var spriteRects = m_Window.GetSpriteRects();
            var data = new CharacterData();

            data.bones = new SpriteBone[3];
            data.bones[0] = new SpriteBone();
            data.bones[0].name = "Bone 1";
            data.bones[0].length = 10f;
            data.bones[0].position = new Vector3(55f, 50f, 0);
            data.bones[0].rotation = Quaternion.identity;
            data.bones[0].parentId = -1;

            data.bones[1] = new SpriteBone();
            data.bones[1].name = "Bone 2";
            data.bones[1].length = 30f;
            data.bones[1].position = new Vector3(10f, 0, 0);
            data.bones[1].rotation = Quaternion.Euler(0, 0, 315);
            data.bones[1].parentId = 0;

            data.bones[2] = new SpriteBone();
            data.bones[2].name = "Bone 3";
            data.bones[2].length = 40f;
            data.bones[2].position = new Vector3(30f, 0, 0);
            data.bones[2].rotation = Quaternion.Euler(0, 0, 45);
            data.bones[2].parentId = 1;

            data.parts = new CharacterPart[2];
            data.parts[0] = new CharacterPart();
            data.parts[0].spritePosition = new RectInt(50, 0, 100, 100);
            data.parts[0].spriteId = spriteRects[0].spriteID.ToString();
            data.parts[0].bones = new int[] { 0, 1, 2 };

            data.parts[1] = new CharacterPart();
            data.parts[1].spritePosition = new RectInt(75, 0, 100, 100);
            data.parts[1].spriteId = spriteRects[1].spriteID.ToString();
            data.parts[1].bones = new int[] { 0, 1, 2 };

            return data;
        }

        [Test]
        public void RemoveSharedBone_RemoveInvalidBoneFromCharacterPartBoneList()
        {
            SwapCharacterData(SharedBones());

            var skeleton = skinningCache.character.skeleton;
            var sharedBone = skeleton.bones[0];

            var sprites = skinningCache.GetSprites();

            Assert.AreEqual(2, sprites.Length, "Incorrect number of sprites");

            foreach (var sprite in sprites)
                Assert.IsTrue(sprite.GetCharacterPart().Contains(sharedBone), "CharacterPart should contain bone");

            skeleton.DestroyBone(sharedBone);
            skinningCache.events.skeletonTopologyChanged.Invoke(skeleton);

            foreach (var sprite in sprites)
            {
                var characterPart = sprite.GetCharacterPart();
                Assert.AreEqual(2, characterPart.BoneCount, "Incorrect number of bones after destroy shared bone");
                Assert.IsFalse(characterPart.Contains(sharedBone), "Character part should not contain the shared bone anymore");
            }
        }
    }
}