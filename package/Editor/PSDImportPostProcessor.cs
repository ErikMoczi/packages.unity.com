using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Experimental.U2D.PSD
{
    public class PSDImportPostProcessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            List<string> assetPathModified = new List<string>();
            foreach (var importedAsset in importedAssets)
            {
                PSDImporter importer = AssetImporter.GetAtPath(importedAsset) as PSDImporter;
                if (importer != null)
                {
                    var texture = AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(importedAsset);
                    var sprites = AssetDatabase.LoadAllAssetsAtPath(importedAsset).OfType<Sprite>().ToArray();
                    if (texture == null || sprites.Length == 0)
                        continue;

                    importer.InitSpriteEditorDataProvider();
                    var physicsOutlineDataProvider = importer.GetDataProvider<ISpritePhysicsOutlineDataProvider>();
                    var textureDataProvider = importer.GetDataProvider<ITextureDataProvider>();
                    int actualWidth = 0, actualHeight = 0;
                    textureDataProvider.GetTextureActualWidthAndHeight(out actualWidth, out actualHeight);
                    float definitionScaleW = (float)texture.width / actualWidth;
                    float definitionScaleH = (float)texture.height / actualHeight;
                    float definitionScale = Mathf.Min(definitionScaleW, definitionScaleH);
                    var dataChanged = false;
                    foreach (var sprite in sprites)
                    {
                        var guid = sprite.GetSpriteID();
                        var outline = physicsOutlineDataProvider.GetOutlines(guid);
                        var outlineOffset = sprite.rect.size / 2;
                        if (outline != null && outline.Count > 0)
                        {
                            var convertedOutline = new Vector2[outline.Count][];
                            for (int i = 0; i < outline.Count; ++i)
                            {
                                convertedOutline[i] = new Vector2[outline[i].Length];
                                for (int j = 0; j < outline[i].Length; ++j)
                                {
                                    convertedOutline[i][j] = outline[i][j] * definitionScale + outlineOffset;
                                }
                            }
                            sprite.OverridePhysicsShape(convertedOutline);
                            dataChanged = true;
                        }
                    }

                    if (dataChanged)
                        assetPathModified.Add(importedAsset);
                }
            }

            if (assetPathModified.Count > 0)
            {
                var originalValue = EditorPrefs.GetBool("VerifySavingAssets", false);
                EditorPrefs.SetBool("VerifySavingAssets", false);
                AssetDatabase.ForceReserializeAssets(assetPathModified, ForceReserializeAssetsOptions.ReserializeMetadata);
                EditorPrefs.SetBool("VerifySavingAssets", originalValue);
            }
        }
    }
}
