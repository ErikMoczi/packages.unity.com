using UnityEngine;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System;
using UnityEngine.Experimental.U2D.Animation;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.U2D.PSD.Tests
{
    public class PSDImporterTests
    {
        const string k_TestTempPath = "Assets/PSDTestTemp";
        const string k_TestFilePath = "Packages/com.unity.2d.psdimporter/Tests/EditorTests/TestAssets/TestPSB.psb";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(k_TestTempPath);
            AssetDatabase.Refresh();
        }

        string CopyTestAssetFile(bool copyMetaFile = true)
        {
            var copiedTestAssetPath = Path.Combine(k_TestTempPath, Path.GetFileName(k_TestFilePath));
            if (File.Exists(copiedTestAssetPath))
                File.Delete(copiedTestAssetPath);

            if (!Directory.Exists(k_TestTempPath))
                Directory.CreateDirectory(k_TestTempPath);
            File.Copy(k_TestFilePath, copiedTestAssetPath);
            if (copyMetaFile)
                File.Copy(k_TestFilePath + ".meta", copiedTestAssetPath + ".meta");
            AssetDatabase.Refresh();
            return copiedTestAssetPath;
        }

        [TestCase(EditorBehaviorMode.Mode2D, true)]
        [TestCase(EditorBehaviorMode.Mode3D, true)]
        public void EditorBehaviourModeImportTest(EditorBehaviorMode mode, bool expectSprite)
        {
            EditorSettings.defaultBehaviorMode = mode;
            var testAssetPath = CopyTestAssetFile(false);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(testAssetPath);
            Assert.AreEqual(expectSprite, (sprite != null));
        }

        [TestCase(true, true, SpriteImportMode.Multiple, true)]
        [TestCase(true, false, SpriteImportMode.Multiple, false)]
        [TestCase(false, true, SpriteImportMode.Multiple, false)]
        [TestCase(false, false, SpriteImportMode.Multiple, false)]
        [TestCase(true, true, SpriteImportMode.Single, false)]
        [TestCase(true, false, SpriteImportMode.Single, false)]
        [TestCase(false, true, SpriteImportMode.Single, false)]
        [TestCase(false, false, SpriteImportMode.Single, false)]
        [Test]
        public void PSBImporterProduceGameObject(bool mosaicLayer, bool charcterMode, SpriteImportMode importMode, bool shouldHaveGameObject)
        {
            var testAssetPath = CopyTestAssetFile();
            var importer = AssetImporter.GetAtPath(testAssetPath);
            var so = new SerializedObject(importer);

            so.FindProperty("m_MosaicLayers").boolValue = mosaicLayer;
            so.FindProperty("m_CharacterMode").boolValue = charcterMode;
            var textureImporterSettingsSP = so.FindProperty("m_TextureImporterSettings");
            textureImporterSettingsSP.FindPropertyRelative("m_TextureType").intValue = (int)TextureImporterType.Sprite;
            textureImporterSettingsSP.FindPropertyRelative("m_SpriteMode").intValue = (int)importMode;
            so.ApplyModifiedPropertiesWithoutUndo();
            importer.SaveAndReimport();
            Assert.AreEqual(shouldHaveGameObject, AssetDatabase.LoadAssetAtPath<GameObject>(testAssetPath) != null);
        }

        struct GameObjectHierarchyNode
        {
            public string name;
            public Type[] componentType;
            public GameObjectHierarchyNode[] children;
        }

        [Test]
        public void PSBImportProduceGameObjectWithCorrectHierarchy()
        {
            var testAssetPath = CopyTestAssetFile();
            var importer = AssetImporter.GetAtPath(testAssetPath);
            var so = new SerializedObject(importer);

            so.FindProperty("m_MosaicLayers").boolValue = true;
            so.FindProperty("m_CharacterMode").boolValue = true;
            var textureImporterSettingsSP = so.FindProperty("m_TextureImporterSettings");
            textureImporterSettingsSP.FindPropertyRelative("m_TextureType").intValue = (int)TextureImporterType.Sprite;
            textureImporterSettingsSP.FindPropertyRelative("m_SpriteMode").intValue = (int)SpriteImportMode.Multiple;
            so.ApplyModifiedPropertiesWithoutUndo();
            importer.SaveAndReimport();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(testAssetPath);
            Assert.NotNull(prefab);

            var expectedGOHierarchy = new GameObjectHierarchyNode
            {
                name = "TestPSB",
                componentType = new[] { typeof(Transform) },
                children = new[]
                {
                    new GameObjectHierarchyNode()
                    {
                        name = "Green",
                        componentType = new[] { typeof(Transform), typeof(SpriteRenderer) },
                        children = new GameObjectHierarchyNode[0]
                    },
                    new GameObjectHierarchyNode()
                    {
                        name = "Blue",
                        componentType = new[] { typeof(Transform), typeof(SpriteRenderer) },
                        children = new GameObjectHierarchyNode[0]
                    },
                    new GameObjectHierarchyNode()
                    {
                        name = "Pink",
                        componentType = new[] { typeof(Transform), typeof(SpriteRenderer) },
                        children = new GameObjectHierarchyNode[0]
                    },
                    new GameObjectHierarchyNode()
                    {
                        name = "Black",
                        componentType = new[] { typeof(Transform), typeof(SpriteRenderer), typeof(SpriteSkin)},
                        children = new GameObjectHierarchyNode[0]
                    },

                    new GameObjectHierarchyNode()
                    {
                        name = "bone_1",
                        componentType = new[] { typeof(Transform) },
                        children = new[]
                        {
                            new GameObjectHierarchyNode()
                            {
                                name = "bone_2",
                                componentType = new[] { typeof(Transform) },
                                children = new[]
                                {
                                    new GameObjectHierarchyNode()
                                    {
                                        name = "bone_3",
                                        componentType = new[] { typeof(Transform) },
                                        children = new GameObjectHierarchyNode[0]
                                    }
                                }
                            }
                        }
                    },
                }
            };

            VerifyGameObjectHierarchy(prefab, expectedGOHierarchy);
        }

        private void VerifyGameObjectHierarchy(GameObject go, GameObjectHierarchyNode hierarchy)
        {
            Assert.AreEqual(hierarchy.name, go.name);
            Assert.AreEqual(hierarchy.children.Length, go.transform.childCount);
            var components = go.GetComponents(typeof(Component));
            Assert.LessOrEqual(hierarchy.componentType.Length, components.Length);
            foreach (var component in hierarchy.componentType)
            {
                Assert.NotNull(components.SingleOrDefault(x => x.GetType() == component));
            }
            for (int i = 0; i < go.transform.childCount; ++i)
            {
                var child = go.transform.GetChild(i).gameObject;
                var childHierarchy = hierarchy.children.FirstOrDefault(x => x.name == child.name);
                VerifyGameObjectHierarchy(child, childHierarchy);
            }
        }

        [Test]
        public void PSBImportWithReslice_RecreateSpritesFromLayer()
        {
            var testAssetPath = CopyTestAssetFile();
            var importer = AssetImporter.GetAtPath(testAssetPath) as PSDImporter;
            var so = new SerializedObject(importer);
            so.FindProperty("m_MosaicLayers").boolValue = true;
            var textureImporterSettingsSP = so.FindProperty("m_TextureImporterSettings");
            textureImporterSettingsSP.FindPropertyRelative("m_TextureType").intValue = (int)TextureImporterType.Sprite;
            textureImporterSettingsSP.FindPropertyRelative("m_SpriteMode").intValue = (int)SpriteImportMode.Multiple;
            so.ApplyModifiedPropertiesWithoutUndo();
            importer.SaveAndReimport();

            // now we have 4 sprites. Modify it to 1
            importer = AssetImporter.GetAtPath(testAssetPath) as PSDImporter;
            importer.SetSpriteRects(new[]
            {
                new SpriteRect()
                {
                    name = "TestSprite",
                    rect = new Rect(0, 0, 32, 32)
                }
            });
            importer.Apply();
            importer.SaveAndReimport();

            importer = AssetImporter.GetAtPath(testAssetPath) as PSDImporter;
            Assert.AreEqual(1, importer.GetSpriteRects().Length);
            var assets = AssetDatabase.LoadAllAssetsAtPath(testAssetPath);
            Assert.AreEqual(1, assets.Count(x => x is Sprite));

            so = new SerializedObject(importer);
            so.FindProperty("m_ResliceFromLayer").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            importer.Apply();
            importer.SaveAndReimport();

            importer = AssetImporter.GetAtPath(testAssetPath) as PSDImporter;
            Assert.AreEqual(4, importer.GetSpriteRects().Length);
            assets = AssetDatabase.LoadAllAssetsAtPath(testAssetPath);
            Assert.AreEqual(4, assets.Count(x => x is Sprite));
            so = new SerializedObject(importer);
            Assert.IsFalse(so.FindProperty("m_ResliceFromLayer").boolValue);
        }
    }
}
