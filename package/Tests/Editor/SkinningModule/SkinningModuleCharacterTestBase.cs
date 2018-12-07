using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.U2D.Animation.Test.SkinningModuleTests
{
    public class SkinningModuleCharacterTestBase : SkinningModuleTestBase
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
                m_Character.bones = new SpriteBone[3];
                m_Character.bones[0] = new SpriteBone();
                m_Character.bones[0].name = "Bone 1";
                m_Character.bones[0].length = 10f;
                m_Character.bones[0].position = new Vector3(55f, 50f, 0);
                m_Character.bones[0].rotation = Quaternion.identity;
                m_Character.bones[0].parentId = -1;

                m_Character.bones[1] = new SpriteBone();
                m_Character.bones[1].name = "Bone 2";
                m_Character.bones[1].length = 30f;
                m_Character.bones[1].position = new Vector3(10f, 0, 0);
                m_Character.bones[1].rotation = Quaternion.Euler(0, 0, 315);
                m_Character.bones[1].parentId = 0;

                m_Character.bones[2] = new SpriteBone();
                m_Character.bones[2].name = "Bone 3";
                m_Character.bones[2].length = 40f;
                m_Character.bones[2].position = new Vector3(30f, 0, 0);
                m_Character.bones[2].rotation = Quaternion.Euler(0, 0, 45);
                m_Character.bones[2].parentId = 1;

                var spriteDataProvider = GetSpriteEditorDataProvider();

                m_Character.parts = new CharacterPart[2];
                m_Character.parts[0] = new CharacterPart();
                m_Character.parts[0].spritePosition = new RectInt(50, 0, 100, 100);
                m_Character.parts[0].spriteId = spriteDataProvider.GetSpriteRects()[0].spriteID.ToString();
                m_Character.parts[0].bones = new int[0];

                m_Character.parts[1] = new CharacterPart();
                m_Character.parts[1].spritePosition = new RectInt(75, 0, 100, 100);
                m_Character.parts[1].spriteId = spriteDataProvider.GetSpriteRects()[1].spriteID.ToString();
                m_Character.parts[1].bones = new int[0];
            }
        }

        public override void SetupWindow()
        {
            m_Window = EditorWindow.GetWindow<SpriteEditorWindowCharacterFake>();
            ((SpriteEditorWindowCharacterFake) m_Window).Populate();
        }
    }
}