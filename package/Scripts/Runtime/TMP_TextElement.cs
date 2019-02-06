using System;
using UnityEngine;
using UnityEngine.TextCore;

namespace TMPro
{
    public enum TextElementType
    {
        Character   = 0x1,
        Sprite      = 0x2,
    }

    /// <summary>
    /// Base class for all text elements like Character and SpriteCharacter.
    /// </summary>
    [Serializable]
    public class TMP_TextElement
    {
        public TextElementType elementType
        {
            get { return m_ElementType; }
        }
        [SerializeField]
        protected TextElementType m_ElementType;

        /// <summary>
        /// The unicode value (code point) of the character.
        /// </summary>
        public uint unicode;
        
        /// <summary>
        /// The glyph used by this text element.
        /// </summary>
        public Glyph glyph;

        /// <summary>
        /// The relative scale of the character.
        /// </summary>
        public float scale;
    }
}
