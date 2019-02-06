using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NSubstitute;
using UnityEditor.Experimental.U2D;
using UnityEditor.Experimental.U2D.Animation;
using UnityEditor.Experimental.U2D.PSD;
using UnityEngine.Experimental.U2D;

namespace UnityEditor.Experimental.U2D.PSD.Tests
{
    public class PSDImporterDataProviderTests
    {
        const string k_TestTempPath = "Assets/PSDTestTemp";
        const string k_TestFilePath = "Packages/com.unity.2d.psdimporter/Tests/EditorTests/TestAssets/TestPSB.psb";
        const string k_TestFileMetaPath = "Packages/com.unity.2d.psdimporter/Tests/EditorTests/TestAssets/TestPSB.psb.meta";
        PSDImporter m_Importer;

        [SetUp]
        public void Setup()
        {
            CopyTestFile(k_TestFilePath, k_TestTempPath);
            CopyTestFile(k_TestFileMetaPath, k_TestTempPath);
            AssetDatabase.Refresh();
            var copiedAsset = Path.Combine(k_TestTempPath, Path.GetFileName(k_TestFilePath));
            m_Importer = AssetImporter.GetAtPath(copiedAsset) as PSDImporter;
        }

        void CopyTestFile(string sourcePath, string destPath)
        {
            var copiedTestAssetPath = Path.Combine(destPath, Path.GetFileName(sourcePath));
            if (File.Exists(copiedTestAssetPath))
                File.Delete(copiedTestAssetPath);

            if (!Directory.Exists(destPath))
                Directory.CreateDirectory(destPath);

            File.Copy(sourcePath, copiedTestAssetPath);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(k_TestTempPath);
            AssetDatabase.Refresh();
        }

        [Test]
        [TestCase(SpriteImportMode.Single, false, 1)]
        [TestCase(SpriteImportMode.Single, true, 1)]
        [TestCase(SpriteImportMode.Multiple, false, 3)]
        [TestCase(SpriteImportMode.Multiple, true, 4)]
        public void SpriteRectDataProvider_HasCorrectNumberOfSpriteRect(SpriteImportMode mode, bool mosaicLayer, int expectedSprite)
        {
            var so = new SerializedObject(m_Importer);
            var textureImporterSettingsSP = so.FindProperty("m_TextureImporterSettings");
            textureImporterSettingsSP.FindPropertyRelative("m_SpriteMode").intValue = (int)mode;
            so.FindProperty("m_MosaicLayers").boolValue = mosaicLayer;
            so.ApplyModifiedProperties();
            m_Importer.SaveAndReimport();

            var spriteProvider = m_Importer.GetDataProvider<ISpriteEditorDataProvider>();
            Assert.AreEqual(expectedSprite, spriteProvider.GetSpriteRects().Length);
            Assert.AreEqual(expectedSprite, AssetDatabase.LoadAllAssetsAtPath(m_Importer.assetPath).Count(x => x is Sprite));
        }

        [Test]
        [TestCase(SpriteImportMode.Single, false, 1, false)]
        [TestCase(SpriteImportMode.Single, true, 1, false)]
        [TestCase(SpriteImportMode.Multiple, false, 2, true)]
        [TestCase(SpriteImportMode.Multiple, true, 3, true)]
        public void SpriteRectDataProvider_DeleteSpriteRectPersistAfterReimport(SpriteImportMode mode, bool mosaicLayer, int expectedSprite, bool expectDelete)
        {
            var so = new SerializedObject(m_Importer);
            var textureImporterSettingsSP = so.FindProperty("m_TextureImporterSettings");
            textureImporterSettingsSP.FindPropertyRelative("m_SpriteMode").intValue = (int)mode;
            so.FindProperty("m_MosaicLayers").boolValue = mosaicLayer;
            so.ApplyModifiedProperties();

            var spriteProvider = m_Importer.GetDataProvider<ISpriteEditorDataProvider>();
            var rects = spriteProvider.GetSpriteRects().ToList();
            var removedRect = rects[0];
            rects.RemoveAt(0);
            spriteProvider.SetSpriteRects(rects.ToArray());
            spriteProvider.Apply();
            m_Importer.SaveAndReimport();
            var importer = AssetImporter.GetAtPath(m_Importer.assetPath) as PSDImporter;
            spriteProvider = importer.GetDataProvider<ISpriteEditorDataProvider>();
            Assert.AreEqual(expectedSprite, spriteProvider.GetSpriteRects().Length);
            Assert.AreEqual(expectDelete, spriteProvider.GetSpriteRects().FirstOrDefault(x => x.spriteID == removedRect.spriteID) == null);
            Assert.AreEqual(expectedSprite, AssetDatabase.LoadAllAssetsAtPath(m_Importer.assetPath).Count(x => x is Sprite));
        }

        [Test]
        [TestCase(SpriteImportMode.Single, false, 1, false)]
        [TestCase(SpriteImportMode.Single, true, 1, false)]
        [TestCase(SpriteImportMode.Multiple, false, 4, true)]
        [TestCase(SpriteImportMode.Multiple, true, 5, true)]
        public void SpriteRectDataProvider_AddSpriteRectPersistAfterReimport(SpriteImportMode mode, bool mosaicLayer, int expectedSprite, bool expectAdd)
        {
            var so = new SerializedObject(m_Importer);
            var textureImporterSettingsSP = so.FindProperty("m_TextureImporterSettings");
            textureImporterSettingsSP.FindPropertyRelative("m_SpriteMode").intValue = (int)mode;
            so.FindProperty("m_MosaicLayers").boolValue = mosaicLayer;
            so.ApplyModifiedProperties();

            var spriteProvider = m_Importer.GetDataProvider<ISpriteEditorDataProvider>();
            var rects = spriteProvider.GetSpriteRects().ToList();
            var newRect = new SpriteRect();
            newRect.name = "Test";
            newRect.rect = new Rect(0, 0, 32, 32);
            rects.Add(newRect);
            spriteProvider.SetSpriteRects(rects.ToArray());
            spriteProvider.Apply();
            m_Importer.SaveAndReimport();
            var importer = AssetImporter.GetAtPath(m_Importer.assetPath) as PSDImporter;
            spriteProvider = importer.GetDataProvider<ISpriteEditorDataProvider>();
            Assert.AreEqual(expectedSprite, spriteProvider.GetSpriteRects().Length);
            Assert.AreEqual(expectAdd, spriteProvider.GetSpriteRects().FirstOrDefault(x => x.spriteID == newRect.spriteID) != null);
            Assert.AreEqual(expectedSprite, AssetDatabase.LoadAllAssetsAtPath(m_Importer.assetPath).Count(x => x is Sprite));
        }

        [Test]
        public void SpriteBoneDataProvider_AddBonePersistAfterReimport()
        {
            var boneProvider = m_Importer.GetDataProvider<ISpriteBoneDataProvider>();
            var spriteProvider = m_Importer.GetDataProvider<ISpriteEditorDataProvider>();
            var spriteRect = spriteProvider.GetSpriteRects().FirstOrDefault(x => x.name == "Green");
            boneProvider.SetBones(spriteRect.spriteID, new List<SpriteBone>()
            {
                new SpriteBone() {
                    name = "TestBone",
                    length = 1,
                    position = Vector2.zero,
                    rotation = Quaternion.identity,
                    parentId = -1
                }
            });

            spriteProvider.Apply();
            m_Importer.SaveAndReimport();
            var importer = AssetImporter.GetAtPath(m_Importer.assetPath) as PSDImporter;
            boneProvider = importer.GetDataProvider<ISpriteBoneDataProvider>();
            var bones = boneProvider.GetBones(spriteRect.spriteID);
            Assert.AreEqual("TestBone", bones[0].name);
        }

        [Test]
        public void SpriteBoneDataProvider_HasCorretBoneData()
        {
            var boneProvider = m_Importer.GetDataProvider<ISpriteBoneDataProvider>();
            var spriteProvider = m_Importer.GetDataProvider<ISpriteEditorDataProvider>();
            var spriteRect = spriteProvider.GetSpriteRects().FirstOrDefault(x => x.name == "Black");
            var bones = boneProvider.GetBones(spriteRect.spriteID);
            Assert.AreEqual(3, bones.Count);
        }

        [Test]
        public void SpriteBoneDataProvider_DeleteBonePersistAfterReimport()
        {
            var boneProvider = m_Importer.GetDataProvider<ISpriteBoneDataProvider>();
            var spriteProvider = m_Importer.GetDataProvider<ISpriteEditorDataProvider>();
            var spriteRect = spriteProvider.GetSpriteRects().FirstOrDefault(x => x.name == "Black");
            var bones = boneProvider.GetBones(spriteRect.spriteID);
            Assert.IsTrue(0 < bones.Count, "Bones should exist for test setup");
            bones.Clear();
            boneProvider.SetBones(spriteRect.spriteID, bones);
            spriteProvider.Apply();
            m_Importer.SaveAndReimport();
            var importer = AssetImporter.GetAtPath(m_Importer.assetPath) as PSDImporter;
            boneProvider = importer.GetDataProvider<ISpriteBoneDataProvider>();
            bones = boneProvider.GetBones(spriteRect.spriteID);
            Assert.AreEqual(0, bones.Count);
        }

        [Test]
        public void SpriteOutlineDataProvider_HasCorrectOutline()
        {
            var provider = m_Importer.GetDataProvider<ISpriteOutlineDataProvider>();
            var spriteProvider = m_Importer.GetDataProvider<ISpriteEditorDataProvider>();
            foreach (var sprite in spriteProvider.GetSpriteRects())
            {
                var outline = provider.GetOutlines(sprite.spriteID);
                Assert.IsTrue(outline.Count > 0);
                Assert.IsTrue(outline[0].Length > 0);
            }
        }

        [Test]
        public void SpriteOutlineDataProvider_SetOutlinePersistAfterReimport()
        {
            var provider = m_Importer.GetDataProvider<ISpriteOutlineDataProvider>();
            var spriteProvider = m_Importer.GetDataProvider<ISpriteEditorDataProvider>();
            var spriteRect = spriteProvider.GetSpriteRects()[0];
            var outline = provider.GetOutlines(spriteRect.spriteID);
            var newOutline = new[]
            {
                new Vector2(0 , 0),
                new Vector2(0 , spriteRect.rect.height),
                new Vector2(spriteRect.rect.width , spriteRect.rect.height),
                new Vector2(spriteRect.rect.width , 0),
            };
            outline[0] = newOutline;
            provider.SetOutlines(spriteRect.spriteID, outline);
            spriteProvider.Apply();
            m_Importer.SaveAndReimport();
            var importer = AssetImporter.GetAtPath(m_Importer.assetPath) as PSDImporter;
            provider = importer.GetDataProvider<ISpriteOutlineDataProvider>();
            outline = provider.GetOutlines(spriteRect.spriteID);
            for (int i = 0; i < newOutline.Length; ++i)
            {
                Assert.AreEqual(newOutline[i], outline[0][i]);
            }
        }

        [Test]
        public void SpriteMeshDataProvider_SetDataPersistAfterReimport()
        {
            var provider = m_Importer.GetDataProvider<ISpriteMeshDataProvider>();
            var spriteProvider = m_Importer.GetDataProvider<ISpriteEditorDataProvider>();
            var spriteRect = spriteProvider.GetSpriteRects()[0];
            var vertices = new[]
            {
                new Vertex2DMetaData()
                {
                    boneWeight = new BoneWeight(),
                    position = Vector2.down
                },

                new Vertex2DMetaData()
                {
                    boneWeight = new BoneWeight(),
                    position = Vector2.up
                },

                new Vertex2DMetaData()
                {
                    boneWeight = new BoneWeight(),
                    position = Vector2.left
                },
            };
            var indices = new[] { 0, 1, 2 };
            var edges = new[] { Vector2Int.zero, Vector2Int.down };
            provider.SetVertices(spriteRect.spriteID, vertices);
            provider.SetIndices(spriteRect.spriteID, indices);
            provider.SetEdges(spriteRect.spriteID, edges);

            spriteProvider.Apply();
            m_Importer.SaveAndReimport();
            var importer = AssetImporter.GetAtPath(m_Importer.assetPath) as PSDImporter;
            provider = importer.GetDataProvider<ISpriteMeshDataProvider>();

            var testVertices = provider.GetVertices(spriteRect.spriteID);
            for (int i = 0; i < vertices.Length; ++i)
            {
                Assert.AreEqual(vertices[i], testVertices[i]);
            }

            var testIndices = provider.GetIndices(spriteRect.spriteID);
            for (int i = 0; i < indices.Length; ++i)
            {
                Assert.AreEqual(indices[i], testIndices[i]);
            }

            var testEdges = provider.GetEdges(spriteRect.spriteID);
            for (int i = 0; i < testEdges.Length; ++i)
            {
                Assert.AreEqual(edges[i], testEdges[i]);
            }
        }

        [Test]
        public void UserCreatedSpriteRect_AppearsBottomLeftInCharacterMode()
        {
            var so = new SerializedObject(m_Importer);
            var textureImporterSettingsSP = so.FindProperty("m_TextureImporterSettings");
            textureImporterSettingsSP.FindPropertyRelative("m_SpriteMode").intValue = (int)SpriteImportMode.Multiple;
            so.FindProperty("m_MosaicLayers").boolValue = true;
            so.FindProperty("m_CharacterMode").boolValue = true;
            so.ApplyModifiedProperties();

            var spriteProvider = m_Importer.GetDataProvider<ISpriteEditorDataProvider>();
            var textureDataProvider = spriteProvider.GetDataProvider<ITextureDataProvider>();
            int width, height;
            textureDataProvider.GetTextureActualWidthAndHeight(out width, out height);
            var spriteRect = spriteProvider.GetSpriteRects().ToList();

            spriteRect.Add(new SpriteRect()
            {
                border = Vector4.zero,
                alignment = SpriteAlignment.Center,
                name = "InsertedRect",
                pivot = Vector2.zero,
                rect = new Rect(width - width * 0.5f, height - height * 0.5f, width * 0.5f, height * 0.5f),
            });
            var newSpriteID = spriteRect[spriteRect.Count - 1].spriteID;
            spriteProvider.SetSpriteRects(spriteRect.ToArray());
            spriteProvider.Apply();

            m_Importer.SaveAndReimport();
            var importer = AssetImporter.GetAtPath(m_Importer.assetPath) as PSDImporter;
            var characterProvider = importer.GetDataProvider<ICharacterDataProvider>();
            var characterData = characterProvider.GetCharacterData();
            Assert.AreEqual(spriteRect.Count, characterData.parts.Length);
            var insertedSpriteCharacterPart = characterData.parts.FirstOrDefault(x => x.spriteId == newSpriteID.ToString());
            Assert.AreEqual(new RectInt(0, 0, (int)(width * 0.5f), (int)(height * 0.5f)), insertedSpriteCharacterPart.spritePosition);
        }
    }
}
