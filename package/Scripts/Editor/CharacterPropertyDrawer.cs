using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using UnityEditor;
using System.Collections;


namespace TMPro.EditorUtilities
{
    [CustomPropertyDrawer(typeof(TMP_Character))]
    public class CharacterPropertyDrawer : PropertyDrawer
    {
        [SerializeField]
        static Material s_InternalSDFMaterial;

        [SerializeField]
        static Material s_InternalBitmapMaterial;

        int m_GlyphSelectedForEditing = -1;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty prop_Unicode = property.FindPropertyRelative("unicode");
            SerializedProperty prop_Glyph = property.FindPropertyRelative("glyph");
            SerializedProperty prop_Scale = property.FindPropertyRelative("scale");

            SerializedProperty m_GlyphRect = prop_Glyph.FindPropertyRelative("m_GlyphRect");
            SerializedProperty m_GlyphMetrics = prop_Glyph.FindPropertyRelative("m_Metrics");

            // Get a reference to the sprite texture
            TMP_FontAsset fontAsset = property.serializedObject.targetObject as TMP_FontAsset;
            if (fontAsset != null)
            {
                if (s_InternalBitmapMaterial == null || s_InternalSDFMaterial == null)
                {
                    s_InternalSDFMaterial = new Material(Shader.Find("Hidden/TextMeshPro/Mobile/Distance Field SSD"));
                    s_InternalBitmapMaterial = new Material(Shader.Find("TextMeshPro/Mobile/Bitmap"));
                }

                Material mat;
                if (((GlyphRasterModes)fontAsset.atlasRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) == GlyphRasterModes.RASTER_MODE_BITMAP)
                    mat = s_InternalBitmapMaterial;
                else
                    mat = s_InternalSDFMaterial;

                Texture tex = fontAsset.atlasTexture;
                mat.mainTexture = tex;

                // Return if we don't have a texture assigned to the sprite asset.
                if (tex != null)
                {
                    Vector2 spriteTexPosition = new Vector2(position.x, position.y);
                    Vector2 spriteSize = new Vector2(32, 32);

                    int spriteImageX = m_GlyphRect.FindPropertyRelative("m_X").intValue;
                    int spriteImageY = m_GlyphRect.FindPropertyRelative("m_Y").intValue;
                    int spriteImageWidth = m_GlyphRect.FindPropertyRelative("m_Width").intValue;
                    int spriteImageHeight = m_GlyphRect.FindPropertyRelative("m_Height").intValue;

                    if (spriteImageWidth >= spriteImageHeight)
                    {
                        spriteSize.y = spriteImageHeight * spriteSize.x / spriteImageWidth;
                        spriteTexPosition.y += (spriteSize.x - spriteSize.y) / 2;
                    }
                    else
                    {
                        spriteSize.x = spriteImageWidth * spriteSize.y / spriteImageHeight;
                        spriteTexPosition.x += (spriteSize.y - spriteSize.x) / 2;
                    }

                    // Compute the normalized texture coordinates
                    Rect texCoords = new Rect((float)spriteImageX / tex.width, (float)spriteImageY / tex.height, (float)spriteImageWidth / tex.width, (float)spriteImageHeight / tex.height);

                    if (Event.current.type == EventType.Repaint)
                    {
                        // Should use the material assigned to the font asset.
                        Graphics.DrawTexture(new Rect(spriteTexPosition.x, spriteTexPosition.y + 10f, spriteSize.x, spriteSize.y), tex, texCoords, 0, 0, 0, 0, new Color(1f, 1f, 1f), mat);
                    }
                }
            }

            Rect rect = new Rect(position.x + 40, position.y, position.width, 49);

            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.richText = true;

            EditorGUIUtility.labelWidth = 40f;
            EditorGUIUtility.fieldWidth = 50;

            EditorGUI.LabelField(new Rect(rect.x, rect.y, 100f, 18), new GUIContent("Unicode: <color=#FFFF80>0x" + prop_Unicode.intValue.ToString("X") + "</color>"), style);
            EditorGUI.LabelField(new Rect(rect.x + 120f, rect.y, 80f, 18), new GUIContent("#: <color=#FFFF80>" + prop_Unicode.intValue + "</color>"), style);

            int glyphIndex = prop_Glyph.FindPropertyRelative("m_Index").intValue;

            // Reset glyph selection if new character has been selected.
            if (GUI.enabled && m_GlyphSelectedForEditing != glyphIndex)
                m_GlyphSelectedForEditing = -1;

            EditorGUI.LabelField(new Rect(rect.x, rect.y + 18, 120, 18), new GUIContent("Glyph ID: <color=#FFFF80>" + glyphIndex + "</color>"), style);

            if (GUI.Button(new Rect(rect.x + 120, rect.y + 18, 75, 18), new GUIContent("Edit Glyph")))
            {
                if (m_GlyphSelectedForEditing == -1)
                    m_GlyphSelectedForEditing = glyphIndex;
                else
                    m_GlyphSelectedForEditing = -1;
            }

            // Show the glyph property drawer if selected
            if (glyphIndex == m_GlyphSelectedForEditing && GUI.enabled)
            {
                Rect newRect = EditorGUILayout.GetControlRect(false, 115);
                newRect.x += 42;
                newRect.y -= 20;
                newRect.height -= 5;
                newRect.width -= 50;

                EditorGUI.DrawRect(newRect, new Color(0.3f, 0.3f, 0.3f, 0.45f));

                // Display GlyphRect
                newRect.x += 3;
                newRect.y += 2;
                EditorGUI.PropertyField(newRect, m_GlyphRect);

                // Display GlyphMetrics
                newRect.y += 45;
                EditorGUI.PropertyField(newRect, m_GlyphMetrics);

                rect.y += 120;
            }

            EditorGUI.PropertyField(new Rect(rect.x, rect.y + 36, 80, 18), prop_Scale);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 58;
        }

    }

}
