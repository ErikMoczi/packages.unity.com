using NUnit.Framework;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.U2D.Animation.Test.SkinningModuleTests
{
    public class GenerateGeometrySpriteSheetTest : SkinningModuleTestBase
    {
        private GenerateGeometryPanel m_GenerateGeometryPanel;

        public override void DoOtherSetup()
        {
            var sprite = skinningCache.GetSprites()[0];
            skinningCache.events.selectedSpriteChanged.Invoke(sprite);

            m_GenerateGeometryPanel = m_Window.GetMainVisualContainer().Q<GenerateGeometryPanel>("GenerateGeometryPanel");
            m_GenerateGeometryPanel.generateWeights = false;
        }

        [Test]
        public void GenerateGeometry_GeneratesGeometryForSelectedSprite()
        {
            m_GenerateGeometryPanel.GenerateGeometry();

            var sprite = skinningCache.selectedSprite;
            var meshCache = sprite.GetMesh();

            Assert.AreEqual(169, meshCache.vertexCount);
            Assert.AreEqual(798, meshCache.indices.Count);
            Assert.AreEqual(70, meshCache.edges.Count);

            for (int i = 0; i < meshCache.vertexCount; ++i)
            {
                var weight = meshCache.GetWeight(i);
                Assert.AreEqual(0, weight.Sum(), 0.00001f);
            }

            sprite = skinningCache.GetSprites()[1];
            meshCache = sprite.GetMesh();
            
            Assert.AreEqual(0, meshCache.vertexCount);
            Assert.AreEqual(0, meshCache.indices.Count);
            Assert.AreEqual(0, meshCache.edges.Count);
        }

        [Test]
        public void GenerateGeometryWithNoSelectedSprite_DoesNotGenerateGeometry()
        {
            skinningCache.events.selectedSpriteChanged.Invoke(null);

            m_GenerateGeometryPanel.GenerateGeometry();

            var sprite = skinningCache.GetSprites()[0];
            var meshCache = sprite.GetMesh();

            Assert.AreEqual(0, meshCache.vertexCount);
            Assert.AreEqual(0, meshCache.indices.Count);
            Assert.AreEqual(0, meshCache.edges.Count);

            sprite = skinningCache.GetSprites()[1];
            meshCache = sprite.GetMesh();
            
            Assert.AreEqual(0, meshCache.vertexCount);
            Assert.AreEqual(0, meshCache.indices.Count);
            Assert.AreEqual(0, meshCache.edges.Count);
        }

        [Test]
        public void GenerateGeometryAll_GeneratesGeometryForAll()
        {
            m_GenerateGeometryPanel.GenerateGeometryAll();

            var sprite = skinningCache.GetSprites()[0];
            skinningCache.events.selectedSpriteChanged.Invoke(sprite);
            var meshCache = sprite.GetMesh();

            Assert.AreEqual(169, meshCache.vertexCount);
            Assert.AreEqual(798, meshCache.indices.Count);
            Assert.AreEqual(70, meshCache.edges.Count);

            for (int i = 0; i < meshCache.vertexCount; ++i)
            {
                var weight = meshCache.GetWeight(i);
                Assert.AreEqual(0, weight.Sum(), 0.00001f);
            }

            sprite = skinningCache.GetSprites()[1];
            skinningCache.events.selectedSpriteChanged.Invoke(sprite);
            meshCache = sprite.GetMesh();

            Assert.AreEqual(81, meshCache.vertexCount);
            Assert.AreEqual(390, meshCache.indices.Count);
            Assert.AreEqual(30, meshCache.edges.Count);

            for (int i = 0; i < meshCache.vertexCount; ++i)
            {
                var weight = meshCache.GetWeight(i);
                Assert.AreEqual(0, weight.Sum(), 0.00001f);
            }
        }

        [Test]
        public void GenerateGeometryWithWeights_GeneratesGeometryAndWeightsForSelectedSprite()
        {
            m_GenerateGeometryPanel.generateWeights = true;

            m_GenerateGeometryPanel.GenerateGeometry();

            var sprite = skinningCache.selectedSprite;
            var meshCache = sprite.GetMesh();

            Assert.AreEqual(169, meshCache.vertexCount);
            Assert.AreEqual(798, meshCache.indices.Count);
            Assert.AreEqual(70, meshCache.edges.Count);

            for (int i = 0; i < meshCache.vertexCount; ++i)
            {
                var weight = meshCache.GetWeight(i);
                Assert.Greater(weight.Sum(), 0);
            }

            sprite = skinningCache.GetSprites()[1];
            meshCache = sprite.GetMesh();
            
            Assert.AreEqual(0, meshCache.vertexCount);
            Assert.AreEqual(0, meshCache.indices.Count);
            Assert.AreEqual(0, meshCache.edges.Count);
        }

        [Test]
        public void GenerateGeometryWithWeightsAll_GeneratesGeometryAndWeightsForAll()
        {
            m_GenerateGeometryPanel.generateWeights = true;

            m_GenerateGeometryPanel.GenerateGeometryAll();

            var sprite = skinningCache.GetSprites()[0];
            skinningCache.events.selectedSpriteChanged.Invoke(sprite);
            var meshCache = sprite.GetMesh();

            Assert.AreEqual(169, meshCache.vertexCount);
            Assert.AreEqual(798, meshCache.indices.Count);
            Assert.AreEqual(70, meshCache.edges.Count);

            for (int i = 0; i < meshCache.vertexCount; ++i)
            {
                var weight = meshCache.GetWeight(i);
                Assert.Greater(weight.Sum(), 0);
            }

            sprite = skinningCache.GetSprites()[1];
            skinningCache.events.selectedSpriteChanged.Invoke(sprite);
            meshCache = sprite.GetMesh();

            Assert.AreEqual(81, meshCache.vertexCount);
            Assert.AreEqual(390, meshCache.indices.Count);
            Assert.AreEqual(30, meshCache.edges.Count);

            // No Bones in Sprite 2
            for (int i = 0; i < meshCache.vertexCount; ++i)
            {
                var weight = meshCache.GetWeight(i);
                Assert.AreEqual(1f, weight.Sum(), 0.00001f);
            }
        }
    }

    public class GenerateGeometryCharacterTest : SkinningModuleCharacterTestBase
    {
        private GenerateGeometryPanel m_GenerateGeometryPanel;

        public override void DoOtherSetup()
        {
            var sprite = skinningCache.GetSprites()[0];
            skinningCache.events.selectedSpriteChanged.Invoke(sprite);

            m_GenerateGeometryPanel = m_Window.GetMainVisualContainer().Q<GenerateGeometryPanel>("GenerateGeometryPanel");
            m_GenerateGeometryPanel.generateWeights = false;
        }

        [Test]
        public void GenerateGeometry_GeneratesGeometryForSelectedSprite()
        {
            m_GenerateGeometryPanel.GenerateGeometry();

            var sprite = skinningCache.selectedSprite;
            var meshCache = sprite.GetMesh();

            Assert.AreEqual(169, meshCache.vertexCount);
            Assert.AreEqual(798, meshCache.indices.Count);
            Assert.AreEqual(70, meshCache.edges.Count);
            Assert.AreEqual(0, meshCache.bones.Length);

            for (int i = 0; i < meshCache.vertexCount; ++i)
            {
                var weight = meshCache.GetWeight(i);
                Assert.AreEqual(0, weight.Sum(), 0.00001f);
            }

            sprite = skinningCache.GetSprites()[1];
            meshCache = sprite.GetMesh();

            Assert.AreEqual(0, meshCache.vertexCount);
            Assert.AreEqual(0, meshCache.indices.Count);
            Assert.AreEqual(0, meshCache.edges.Count);
            Assert.AreEqual(0, meshCache.bones.Length);
        }

        [Test]
        public void GenerateGeometryWithWeights_GeneratesGeometryAndWeightsForSelectedSprite()
        {
            m_GenerateGeometryPanel.generateWeights = true;

            m_GenerateGeometryPanel.GenerateGeometry();

            var sprite = skinningCache.selectedSprite;
            var meshCache = sprite.GetMesh();

            Assert.AreEqual(169, meshCache.vertexCount);
            Assert.AreEqual(798, meshCache.indices.Count);
            Assert.AreEqual(70, meshCache.edges.Count);
            Assert.AreEqual(2, meshCache.bones.Length);

            for (int i = 0; i < meshCache.vertexCount; ++i)
            {
                var weight = meshCache.GetWeight(i);
                Assert.Greater(weight.Sum(), 0);
            }

            sprite = skinningCache.GetSprites()[1];
            meshCache = sprite.GetMesh();

            Assert.AreEqual(0, meshCache.vertexCount);
            Assert.AreEqual(0, meshCache.indices.Count);
            Assert.AreEqual(0, meshCache.edges.Count);
            Assert.AreEqual(0, meshCache.bones.Length);
        }

        [Test]
        public void GenerateGeometryWithWeightsForAll_GeneratesGeometryAndWeightsForAllSprites()
        {
            m_GenerateGeometryPanel.generateWeights = true;

            m_GenerateGeometryPanel.GenerateGeometryAll();

            var sprite = skinningCache.selectedSprite;
            var meshCache = sprite.GetMesh();

            Assert.AreEqual(169, meshCache.vertexCount);
            Assert.AreEqual(798, meshCache.indices.Count);
            Assert.AreEqual(70, meshCache.edges.Count);
            Assert.AreEqual(2, meshCache.bones.Length);

            for (int i = 0; i < meshCache.vertexCount; ++i)
            {
                var weight = meshCache.GetWeight(i);
                Assert.Greater(weight.Sum(), 0);
            }

            sprite = skinningCache.GetSprites()[1];
            meshCache = sprite.GetMesh();

            Assert.AreEqual(81, meshCache.vertexCount);
            Assert.AreEqual(390, meshCache.indices.Count);
            Assert.AreEqual(30, meshCache.edges.Count);
            Assert.AreEqual(1, meshCache.bones.Length);

            for (int i = 0; i < meshCache.vertexCount; ++i)
            {
                var weight = meshCache.GetWeight(i);
                Assert.Greater(weight.Sum(), 0);
            }
        }
    }
}
