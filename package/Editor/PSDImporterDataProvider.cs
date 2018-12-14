using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.U2D;
using UnityEditor.Experimental.U2D.Common;
using UnityEditor.Experimental.U2D.Animation;
using System;

namespace UnityEditor.Experimental.U2D.PSD
{
    public abstract class PSDDataProvider
    {
        public PSDImporter dataProvider;
    }

    public class SpriteBoneDataProvider : PSDDataProvider, ISpriteBoneDataProvider
    {
        public List<SpriteBone> GetBones(GUID guid)
        {
            var sprite = ((SpriteMetaData)dataProvider.GetSpriteData(guid));
            Assert.IsNotNull(sprite, string.Format("Sprite not found for GUID:{0}", guid.ToString()));
            return sprite.spriteBone != null ? sprite.spriteBone.ToList() : new List<SpriteBone>();
        }

        public void SetBones(GUID guid, List<SpriteBone> bones)
        {
            var sprite = dataProvider.GetSpriteData(guid);
            if (sprite != null)
                ((SpriteMetaData)sprite).spriteBone = bones;
        }
    }

    public class TextureDataProvider : PSDDataProvider, ITextureDataProvider
    {
        Texture2D m_ReadableTexture;
        Texture2D m_OriginalTexture;

        PSDImporter textureImporter { get { return (PSDImporter)dataProvider.targetObject; } }

        public Texture2D texture
        {
            get
            {
                if (m_OriginalTexture == null)
                    m_OriginalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(textureImporter.assetPath);
                return m_OriginalTexture;
            }
        }

        public Texture2D previewTexture
        {
            get { return texture; }
        }

        public Texture2D GetReadableTexture2D()
        {
            if (m_ReadableTexture == null)
            {
                m_ReadableTexture = InternalEditorBridge.CreateTemporaryDuplicate(texture, texture.width, texture.height);
                if (m_ReadableTexture != null)
                    m_ReadableTexture.filterMode = FilterMode.Point;
            }
            return m_ReadableTexture;
        }

        public void GetTextureActualWidthAndHeight(out int width, out int height)
        {
            width = dataProvider.textureActualWidth;
            height = dataProvider.textureActualHeight;
        }
    }

    public class SpriteOutlineDataProvider : PSDDataProvider, ISpriteOutlineDataProvider
    {
        public List<Vector2[]> GetOutlines(GUID guid)
        {
            var sprite = ((SpriteMetaData)dataProvider.GetSpriteData(guid));
            Assert.IsNotNull(sprite, string.Format("Sprite not found for GUID:{0}", guid.ToString()));

            var outline = sprite.spriteOutline;
            if (outline != null)
                return outline.Select(x => x.outline).ToList();
            return new List<Vector2[]>();
        }

        public void SetOutlines(GUID guid, List<Vector2[]> data)
        {
            var sprite = dataProvider.GetSpriteData(guid);
            if (sprite != null)
                ((SpriteMetaData)sprite).spriteOutline = data.Select(x => new SpriteOutline() {outline = x}).ToList();
        }

        public float GetTessellationDetail(GUID guid)
        {
            return ((SpriteMetaData)dataProvider.GetSpriteData(guid)).tessellationDetail;
        }

        public void SetTessellationDetail(GUID guid, float value)
        {
            var sprite = dataProvider.GetSpriteData(guid);
            if (sprite != null)
                ((SpriteMetaData)sprite).tessellationDetail = value;
        }
    }

    public class SpritePhysicsOutlineProvider : PSDDataProvider, ISpritePhysicsOutlineDataProvider
    {
        public List<Vector2[]> GetOutlines(GUID guid)
        {
            var sprite = ((SpriteMetaData)dataProvider.GetSpriteData(guid));
            Assert.IsNotNull(sprite, string.Format("Sprite not found for GUID:{0}", guid.ToString()));
            var outline = sprite.spritePhysicsOutline;
            if (outline != null)
                return outline.Select(x => x.outline).ToList();

            return new List<Vector2[]>();
        }

        public void SetOutlines(GUID guid, List<Vector2[]> data)
        {
            var sprite = dataProvider.GetSpriteData(guid);
            if (sprite != null)
                ((SpriteMetaData)sprite).spritePhysicsOutline = data.Select(x => new SpriteOutline() { outline = x }).ToList();
        }

        public float GetTessellationDetail(GUID guid)
        {
            return ((SpriteMetaData)dataProvider.GetSpriteData(guid)).tessellationDetail;
        }

        public void SetTessellationDetail(GUID guid, float value)
        {
            var sprite = dataProvider.GetSpriteData(guid);
            if (sprite != null)
                ((SpriteMetaData)sprite).tessellationDetail = value;
        }
    }

    public class SpriteMeshDataProvider : PSDDataProvider, ISpriteMeshDataProvider
    {
        public Vertex2DMetaData[] GetVertices(GUID guid)
        {
            var sprite = ((SpriteMetaData)dataProvider.GetSpriteData(guid));
            Assert.IsNotNull(sprite, string.Format("Sprite not found for GUID:{0}", guid.ToString()));
            var v = sprite.vertices;
            if (v != null)
                return v.ToArray();

            return new Vertex2DMetaData[0];
        }

        public void SetVertices(GUID guid, Vertex2DMetaData[] vertices)
        {
            var sprite = dataProvider.GetSpriteData(guid);
            if (sprite != null)
                ((SpriteMetaData)sprite).vertices = vertices.ToList();
        }

        public int[] GetIndices(GUID guid)
        {
            var sprite = ((SpriteMetaData)dataProvider.GetSpriteData(guid));
            Assert.IsNotNull(sprite, string.Format("Sprite not found for GUID:{0}", guid.ToString()));
            var v = sprite.indices;
            if (v != null)
                return v;

            return new int[0];
        }

        public void SetIndices(GUID guid, int[] indices)
        {
            var sprite = dataProvider.GetSpriteData(guid);
            if (sprite != null)
                ((SpriteMetaData)sprite).indices = indices;
        }

        public Vector2Int[] GetEdges(GUID guid)
        {
            var sprite = ((SpriteMetaData)dataProvider.GetSpriteData(guid));
            Assert.IsNotNull(sprite, string.Format("Sprite not found for GUID:{0}", guid.ToString()));
            var v = sprite.edges;
            if (v != null)
                return v;

            return new Vector2Int[0];
        }

        public void SetEdges(GUID guid, Vector2Int[] edges)
        {
            var sprite = dataProvider.GetSpriteData(guid);
            if (sprite != null)
                ((SpriteMetaData)sprite).edges = edges;
        }
    }


    public class CharacterDataProvider : PSDDataProvider, ICharacterDataProvider
    {
        public CharacterData GetCharacterData()
        {
            var cd = dataProvider.characterData;
            var parts = cd.parts == null ? new List<CharacterPart>() : cd.parts.ToList();
            var spriteRects = dataProvider.GetSpriteMetaData();
            parts.RemoveAll(x => Array.FindIndex(spriteRects, y => y.spriteID == new GUID(x.spriteId)) == -1);
            foreach (var spriteMetaData in spriteRects)
            {
                var srIndex = parts.FindIndex(x => new GUID(x.spriteId) == spriteMetaData.spriteID);
                CharacterPart cp = srIndex == -1 ? new CharacterPart() : parts[srIndex];
                cp.spriteId = spriteMetaData.spriteID.ToString();
                cp.spritePosition = new RectInt();
                var uvTransform = spriteMetaData.uvTransform;
                var outlineOffset = new Vector2(spriteMetaData.rect.x - uvTransform.x, spriteMetaData.rect.y - uvTransform.y);
                cp.spritePosition.position = new Vector2Int((int)outlineOffset.x, (int)outlineOffset.y);
                cp.spritePosition.size = new Vector2Int((int)spriteMetaData.rect.width, (int)spriteMetaData.rect.height);
                if (srIndex == -1)
                    parts.Add(cp);
                else
                    parts[srIndex] = cp;
            }

            var layers = dataProvider.GetPSDLayers();
            parts.Sort((x, y) =>
            {
                var xIndex = layers.FindIndex(l => l.spriteID == new GUID(x.spriteId));
                var yIndex = layers.FindIndex(l => l.spriteID == new GUID(y.spriteId));
                return xIndex.CompareTo(yIndex);
            });

            parts.Reverse();
            cd.parts = parts.ToArray();
            cd.dimension = dataProvider.documentSize;
            return cd;
        }

        public void SetCharacterData(CharacterData characterData)
        {
            characterData.parts = characterData.parts.Reverse().ToArray();
            dataProvider.characterData = characterData;
        }
    }
}
