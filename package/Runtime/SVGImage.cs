﻿using System;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_2018_3_OR_NEWER
using UnityEngine.Rendering;
#else
using UnityEngine.Experimental.Rendering;
#endif
using UnityEngine.Experimental.U2D;
using Unity.Collections;

[AddComponentMenu("UI/SVG Image", 11)]
public class SVGImage : MaskableGraphic
{
    [SerializeField] private Sprite m_Sprite = null;

    /// <summary>
    /// The sprite that is used to render this image.
    /// </summary>
    /// <remarks>
    /// This returns the source Sprite of an Image. This Sprite can also be viewed and changed in the Inspector as part of an Image component. This can also be used to change the Sprite using a script.
    /// </remarks>
    public Sprite sprite
    {
        get { return m_Sprite; }
        set
        {
            if (m_Sprite !=  value)
            {
                m_Sprite = value;
                SetAllDirty();
            }
        }
    }

    public override Texture mainTexture
    {
        get
        {
            if (sprite == null)
            {
                if (material != null && material.mainTexture != null)
                {
                    return material.mainTexture;
                }
                return s_WhiteTexture;
            }

            return sprite.texture != null ? sprite.texture : s_WhiteTexture;
        }
    }

    static NativeSlice<Color32> s_SpriteColor = new NativeSlice<Color32>();
    static NativeSlice<Vector2> s_TextCord2 = new NativeSlice<Vector2>();
    static UIVertex s_TempVertex = new UIVertex();

    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        if (sprite == null)
        {
            base.OnPopulateMesh(toFill);
            return;
        }

        GenerateSprite(toFill);
    }

    void GenerateSprite(VertexHelper vh)
    {
        var spriteSize = new Vector2(sprite.rect.width, sprite.rect.height);

        // Covert sprite pivot into normalized space.
        var spritePivot = sprite.pivot / spriteSize;
        var rectPivot = rectTransform.pivot;
        Rect r = GetPixelAdjustedRect();
        var drawingSize = new Vector2(r.width, r.height);
        var spriteBoundSize = sprite.bounds.size;

        // Calculate the drawing offset based on the difference between the two pivots.
        var drawOffset = (rectPivot - spritePivot) * drawingSize;

        bool hasColorAttribute = sprite.HasVertexAttribute(VertexAttribute.Color);
        if (hasColorAttribute)
            s_SpriteColor = sprite.GetVertexAttribute<Color32>(VertexAttribute.Color);

        bool hasTextCord2Attribute = sprite.HasVertexAttribute(VertexAttribute.TexCoord2);
        if (hasTextCord2Attribute)
            s_TextCord2 = sprite.GetVertexAttribute<Vector2>(VertexAttribute.TexCoord2);

        var color32 = color;
        vh.Clear();

        Vector2[] vertices = sprite.vertices;
        Vector2[] uvs = sprite.uv;
        for (int i = 0; i < vertices.Length; ++i)
        {
            vh.AddVert(new Vector3((vertices[i].x / spriteBoundSize.x) * drawingSize.x - drawOffset.x, (vertices[i].y / spriteBoundSize.y) * drawingSize.y - drawOffset.y), 
                hasColorAttribute ? color32 * s_SpriteColor[i] : color32, uvs[i]);

            // VertexHelper access to uv2 isn't great work around the API for now. Copy current vert out and then back with the proper uv2 if we have it.
            if (hasTextCord2Attribute)
            {
                vh.PopulateUIVertex(ref s_TempVertex, vh.currentVertCount - 1);
                s_TempVertex.uv2 = s_TextCord2[i];
                vh.SetUIVertex(s_TempVertex, vh.currentVertCount - 1);
            }
        }

        UInt16[] triangles = sprite.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            vh.AddTriangle(triangles[i + 0], triangles[i + 1], triangles[i + 2]);
        }
    }
}