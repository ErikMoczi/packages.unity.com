using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using UnityEditor;
using System.Collections;


namespace TMPro.EditorUtilities
{

    [CustomPropertyDrawer(typeof(Glyph))]
    public class GlyphPropertyDrawer : PropertyDrawer
    {
        [SerializeField]
        static Material s_InternalSDFMaterial;

        [SerializeField]
        static Material s_InternalBitmapMaterial;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty prop_GlyphIndex = property.FindPropertyRelative("m_Index");
            SerializedProperty prop_GlyphMetrics = property.FindPropertyRelative("m_Metrics");
            SerializedProperty prop_GlyphRect = property.FindPropertyRelative("m_GlyphRect");
            //SerializedProperty prop_scale = property.FindPropertyRelative("m_Scale");
            //SerializedProperty prop_AtlasIndex = property.FindPropertyRelative("m_AtlasIndex");

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
                    Vector2 spriteSize = new Vector2(64, 64);

                    int spriteImageX = prop_GlyphRect.FindPropertyRelative("m_X").intValue;
                    int spriteImageY = prop_GlyphRect.FindPropertyRelative("m_Y").intValue;
                    int spriteImageWidth = prop_GlyphRect.FindPropertyRelative("m_Width").intValue;
                    int spriteImageHeight = prop_GlyphRect.FindPropertyRelative("m_Height").intValue;

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

            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.richText = true;

            // Glyph Index
            float labelWidth = GUI.skin.label.CalcSize(new GUIContent("ID: " + prop_GlyphIndex.intValue)).x;
            EditorGUI.LabelField(new Rect(position.x + (64 - labelWidth) / 2, position.y + 85, 64f, 18f), new GUIContent("ID: <color=#FFFF80>" + prop_GlyphIndex.intValue + "</color>"), style);

            // We get Rect since a valid position may not be provided by the caller.
            Rect rect = new Rect(position.x, position.y, position.width, 49);
            rect.x += 70;
            EditorGUI.PropertyField(rect, prop_GlyphRect);

            rect.y += 49;
            EditorGUI.PropertyField(rect, prop_GlyphMetrics);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 115f;
        }

    }
}
