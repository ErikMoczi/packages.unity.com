using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.U2D.Animation.Test.SkinningModuleTests
{
    public class SpriteEditorWindowFullFake
            : SpriteEditorWindowFake
                , ISpriteEditorDataProvider
                , ISpriteBoneDataProvider
                , ISpriteMeshDataProvider
                , ICharacterDataProvider
                , ITextureDataProvider
    {
        private class SpriteData
        {
            public List<SpriteBone> bones;
            public Vertex2DMetaData[] vertices;
            public int[] indices;
            public Vector2Int[] edges;
        }

        private SpriteRect[] m_SpriteRects;
        private SpriteData[] m_SpriteData;
        private CharacterData m_Character;

        private bool m_HasCharacter;

        public void Populate(bool character)
        {
            m_HasCharacter = character;

            m_SpriteRects = new SpriteRect[2];
            m_SpriteRects[0] = new SpriteRect();
            m_SpriteRects[0].rect = new Rect(0, 0, 100, 100);
            m_SpriteRects[0].alignment = SpriteAlignment.Center;
            m_SpriteRects[0].border = Vector4.zero;
            m_SpriteRects[0].name = "Sprite1";
            m_SpriteRects[0].pivot = Vector2.zero;
            m_SpriteRects[0].spriteID = GUID.Generate();

            m_SpriteRects[1] = new SpriteRect();
            m_SpriteRects[1].rect = new Rect(100, 0, 100, 100);
            m_SpriteRects[1].alignment = SpriteAlignment.Center;
            m_SpriteRects[1].border = Vector4.zero;
            m_SpriteRects[1].name = "Sprite2";
            m_SpriteRects[1].pivot = Vector2.zero;
            m_SpriteRects[1].spriteID = GUID.Generate();

            pixelsPerUnit = 100f;

            m_SpriteData = new SpriteData[2];
            m_SpriteData[0] = new SpriteData();
            m_SpriteData[0].bones = new List<SpriteBone>();

            var bone = new SpriteBone()
            {
                name = "Bone-1-1",
                length = 20f,
                position = Vector3.zero,
                rotation = Quaternion.identity,
                parentId = -1
            };
            m_SpriteData[0].bones.Add(bone);

            bone = new SpriteBone()
            {
                name = "Bone-1-2",
                length = 50f,
                position = new Vector3(20, 0, 0),
                rotation = Quaternion.Euler(0, 0, 270),
                parentId = 0
            };
            m_SpriteData[0].bones.Add(bone);

            m_SpriteData[0].vertices = new Vertex2DMetaData[4];
            m_SpriteData[0].vertices[0] = new Vertex2DMetaData()
            {
                position =  new Vector2(0, 0),
                boneWeight = new BoneWeight()
            };
            m_SpriteData[0].vertices[1] = new Vertex2DMetaData()
            {
                position =  new Vector2(0, 100),
                boneWeight = new BoneWeight()
            };
            m_SpriteData[0].vertices[2] = new Vertex2DMetaData()
            {
                position =  new Vector2(100, 100),
                boneWeight = new BoneWeight()
            };
            m_SpriteData[0].vertices[3] = new Vertex2DMetaData()
            {
                position =  new Vector2(100, 0),
                boneWeight = new BoneWeight()
            };
            m_SpriteData[0].indices = new int[6] { 0, 1, 2, 0, 2, 3 };
            m_SpriteData[0].edges = new Vector2Int[4]
            {
                new Vector2Int(0, 1),
                new Vector2Int(1, 2),
                new Vector2Int(2, 3),
                new Vector2Int(3, 0)
            };

            m_SpriteData[1] = new SpriteData();
            m_SpriteData[1].bones = new List<SpriteBone>();

            bone = new SpriteBone()
            {
                name = "Bone-2-1",
                length = 20f,
                position = new Vector3(100, 0, 0),
                rotation = Quaternion.identity,
                parentId = -1
            };
            m_SpriteData[1].bones.Add(bone);

            bone = new SpriteBone()
            {
                name = "Bone-2-2",
                length = 50f,
                position = new Vector3(120, 0, 0),
                rotation = Quaternion.Euler(0, 0, 270),
                parentId = 0
            };
            m_SpriteData[1].bones.Add(bone);

            m_SpriteData[1].vertices = new Vertex2DMetaData[3];
            m_SpriteData[1].vertices[0] = new Vertex2DMetaData()
            {
                position =  new Vector2(100, 0),
                boneWeight = new BoneWeight()
                {
                    boneIndex0 = 0,
                    weight0 = 1.5f
                }
            };
            m_SpriteData[1].vertices[1] = new Vertex2DMetaData()
            {
                position =  new Vector2(100, 100),
                boneWeight = new BoneWeight()
                {
                    boneIndex0 = 1,
                    weight0 = 0.5f
                }
            };
            m_SpriteData[1].vertices[2] = new Vertex2DMetaData()
            {
                position =  new Vector2(190, 50),
                boneWeight = new BoneWeight()
                {
                    boneIndex0 = 0,
                    weight0 = 0.5f,
                    boneIndex1 = 1,
                    weight1 = 0.5f
                }
            };
            m_SpriteData[1].indices = new int[3] { 0, 1, 2 };
            m_SpriteData[1].edges = new Vector2Int[3]
            {
                new Vector2Int(0, 1),
                new Vector2Int(1, 2),
                new Vector2Int(2, 0)
            };

            m_Character.bones = new SpriteBone[3];
            m_Character.bones[0] = new SpriteBone();
            m_Character.bones[0].name = "Bone 1";
            m_Character.bones[0].length = 10f;
            m_Character.bones[0].position = new Vector3(55, 0, 0);
            m_Character.bones[0].rotation = Quaternion.identity;
            m_Character.bones[0].parentId = -1;

            m_Character.bones[1] = new SpriteBone();
            m_Character.bones[1].name = "Bone 2";
            m_Character.bones[1].length = 30f;
            m_Character.bones[1].position = Vector3.zero;
            m_Character.bones[1].rotation = Quaternion.Euler(0, 0, 315);
            m_Character.bones[1].parentId = 0;

            m_Character.bones[2] = new SpriteBone();
            m_Character.bones[2].name = "Bone 3";
            m_Character.bones[2].length = 20f;
            m_Character.bones[2].position = Vector3.zero;
            m_Character.bones[2].rotation = Quaternion.Euler(0, 0, 45);
            m_Character.bones[2].parentId = 1;

            m_Character.parts = new CharacterPart[2];
            m_Character.parts[0] = new CharacterPart();
            m_Character.parts[0].spritePosition = new RectInt(50, 0, 100, 100);
            m_Character.parts[0].spriteId = m_SpriteRects[0].spriteID.ToString();
            m_Character.parts[0].bones = new int[0];

            m_Character.parts[1] = new CharacterPart();
            m_Character.parts[1].spritePosition = new RectInt(75, 0, 100, 100);
            m_Character.parts[1].spriteId = m_SpriteRects[1].spriteID.ToString();
            m_Character.parts[1].bones = new int[0];
        }

        protected override ISpriteEditorDataProvider GetSpriteEditorDataProvider()
        {
            return this;
        }

        public override SpriteRect[] GetSpriteRects()
        {
            return m_SpriteRects;
        }

        public void SetSpriteRects(SpriteRect[] spriteRects)
        {
            m_SpriteRects = spriteRects;
        }

        public void Apply()
        {
        }

        public void InitSpriteEditorDataProvider()
        {
        }

        public override T GetDataProvider<T>()
        {
            if (typeof(T) == typeof(ICharacterDataProvider))
            {
                if (m_HasCharacter == false)
                    return null;
            }
            return this as T;
        }

        public bool HasDataProvider(Type type)
        {
            if (type == typeof(ICharacterDataProvider))
            {
                return m_HasCharacter;
            }
            return type.IsInstanceOfType(this);
        }

        public SpriteImportMode spriteImportMode { get; private set; }
        public float pixelsPerUnit { get; private set; }
        public Object targetObject { get; private set; }

        public List<SpriteBone> GetBones(GUID guid)
        {
            for (int i = 0; i < m_SpriteRects.Length; ++i)
            {
                if (guid == m_SpriteRects[i].spriteID)
                    return m_SpriteData[i].bones;
            }

            return null;
        }

        public void SetBones(GUID guid, List<SpriteBone> bones)
        {
            for (int i = 0; i < m_SpriteRects.Length; ++i)
            {
                if (guid == m_SpriteRects[i].spriteID)
                    m_SpriteData[i].bones = bones;
            }
        }

        public Vertex2DMetaData[] GetVertices(GUID guid)
        {
            for (int i = 0; i < m_SpriteRects.Length; ++i)
            {
                if (guid == m_SpriteRects[i].spriteID)
                    return m_SpriteData[i].vertices;
            }

            return null;
        }

        public void SetVertices(GUID guid, Vertex2DMetaData[] vertices)
        {
            for (int i = 0; i < m_SpriteRects.Length; ++i)
            {
                if (guid == m_SpriteRects[i].spriteID)
                    m_SpriteData[i].vertices = vertices;
            }
        }

        public int[] GetIndices(GUID guid)
        {
            for (int i = 0; i < m_SpriteRects.Length; ++i)
            {
                if (guid == m_SpriteRects[i].spriteID)
                    return m_SpriteData[i].indices;
            }

            return null;
            ;
        }

        public void SetIndices(GUID guid, int[] indices)
        {
            for (int i = 0; i < m_SpriteRects.Length; ++i)
            {
                if (guid == m_SpriteRects[i].spriteID)
                    m_SpriteData[i].indices = indices;
            }
        }

        public Vector2Int[] GetEdges(GUID guid)
        {
            for (int i = 0; i < m_SpriteRects.Length; ++i)
            {
                if (guid == m_SpriteRects[i].spriteID)
                    return m_SpriteData[i].edges;
            }

            return null;
        }

        public void SetEdges(GUID guid, Vector2Int[] edges)
        {
            for (int i = 0; i < m_SpriteRects.Length; ++i)
            {
                if (guid == m_SpriteRects[i].spriteID)
                    m_SpriteData[i].edges = edges;
            }
        }

        public CharacterData GetCharacterData()
        {
            return m_Character;
        }

        public void SetCharacterData(CharacterData characterData)
        {
            m_Character = characterData;
        }

        public void GetTextureActualWidthAndHeight(out int width, out int height)
        {
            width = 200;
            height = 100;
        }

        public Texture2D GetReadableTexture2D()
        {
            throw new System.NotImplementedException();
        }

        public Texture2D texture { get; private set; }
        public Texture2D previewTexture { get; private set; }
    }

    public class SkinningModuleFullFakeTestBase : SkinningModuleTestBase
    {
        protected override SpriteEditorWindowFake GetWindowFake()
        {
            var window = EditorWindow.GetWindow<SpriteEditorWindowFullFake>();
            window.Populate(false);
            return window;
        }
    }

    public class SkinningModuleFullFakeCharacterTestBase : SkinningModuleTestBase
    {
        protected override SpriteEditorWindowFake GetWindowFake()
        {
            var window = EditorWindow.GetWindow<SpriteEditorWindowFullFake>();
            window.Populate(true);
            return window;
        }
    }
}