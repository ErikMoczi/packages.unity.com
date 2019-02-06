using System;
using UnityEngine;
using UnityEngine.TextCore;

namespace TMPro
{
    /// <summary>
    /// The visual representation of the sprite character using this glyph.
    /// </summary>
    [Serializable]
    public class TMP_SpriteGlyph : Glyph
    {
        /// <summary>
        /// An optional reference to the underlying sprite used to create this glyph. 
        /// </summary>
        public Sprite sprite;


        // ********************
        // CONSTRUCTORS
        // ********************

        public TMP_SpriteGlyph() { }

        public TMP_SpriteGlyph(uint index, GlyphMetrics metrics, GlyphRect glyphRect, float scale, int atlasIndex)
        {
            this.index = index;
            this.metrics = metrics;
            this.glyphRect = glyphRect;
            this.scale = scale;
            this.atlasIndex = atlasIndex;
        }


        public TMP_SpriteGlyph(uint index, GlyphMetrics metrics, GlyphRect glyphRect, float scale, int atlasIndex, Sprite sprite)
        {
            this.index = index;
            this.metrics = metrics;
            this.glyphRect = glyphRect;
            this.scale = scale;
            this.atlasIndex = atlasIndex;
            this.sprite = sprite;
        }
    }
}