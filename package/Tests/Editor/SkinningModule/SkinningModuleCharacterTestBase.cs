using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.U2D.Animation.Test.SkinningModuleTests
{
    public class SpriteEditorWindowCharacterFake : SpriteEditorWindowFake, ICharacterDataProvider
    {
        private CharacterData m_Character;

        public override T GetDataProvider<T>()
        {
            if (typeof(T) == typeof(ICharacterDataProvider))
            {
                return this as T;
            }

            return base.GetDataProvider<T>();
        }

        public CharacterData GetCharacterData()
        {
            return m_Character;
        }

        public void SetCharacterData(CharacterData characterData)
        {
            m_Character = characterData;
        }

        public void Populate()
        {
            m_Character = DefaultCharacterData();
        }

        private CharacterData DefaultCharacterData()
        {
            CharacterData data = new CharacterData();

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
            data.parts[0].spriteId = GetSpriteRects()[0].spriteID.ToString();
            data.parts[0].bones = new int[0];

            data.parts[1] = new CharacterPart();
            data.parts[1].spritePosition = new RectInt(75, 0, 100, 100);
            data.parts[1].spriteId = GetSpriteRects()[1].spriteID.ToString();
            data.parts[1].bones = new int[0];

            return data;
        }
    }

    public class SkinningModuleCharacterTestBase : SkinningModuleTestBase
    {
        protected override SpriteEditorWindowFake GetWindowFake()
        {
            var window = EditorWindow.GetWindow<SpriteEditorWindowCharacterFake>();
            window.Populate();
            return window;
        }
    }
}