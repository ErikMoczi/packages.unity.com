using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore;

namespace TMPro
{
    /// <summary>
    /// A basic element of text.
    /// </summary>
    [Serializable]
    public class TMP_Character : TMP_TextElement
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public TMP_Character()
        {
            m_ElementType = TextElementType.Character;
            this.scale = 1.0f;
        }

        /// <summary>
        /// Constructor font new character
        /// </summary>
        /// <param name="unicode"></param>
        /// <param name="glyph"></param>
        public TMP_Character(uint unicode, Glyph glyph)
        {
            m_ElementType = TextElementType.Character;

            this.unicode = unicode;
            this.glyph = glyph;
            this.scale = 1.0f;
        }
    }
}
