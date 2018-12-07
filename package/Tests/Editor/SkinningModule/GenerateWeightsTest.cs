using NUnit.Framework;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.U2D.Animation.Test.SkinningModuleTests
{
    public class GenerateWeightsSpriteSheetTest : SkinningModuleFullFakeTestBase
    {
        private GenerateWeightsPanel m_GenerateWeightsPanel;

        public override void DoOtherSetup()
        {
            var sprite = skinningCache.GetSprites()[0];
            skinningCache.events.selectedSpriteChanged.Invoke(sprite);

            m_GenerateWeightsPanel = m_Window.GetMainVisualContainer().Q<GenerateWeightsPanel>("GenerateWeightsPanel");
            m_GenerateWeightsPanel.associateBones = false;
        }

        [Test]
        public void DefaultSelectedSprite_HasNoWeights()
        {
            // Sprite 1 does not have weights set up
            var sprite = skinningCache.selectedSprite;
            var meshCache = sprite.GetMesh();

            for (int i = 0; i < meshCache.vertexCount; ++i)
            {
                var weight = meshCache.GetWeight(i);
                Assert.AreEqual(0, weight.Sum(), 0.00001f);
            }
        }

        [Test]
        public void GenerateWeights_GeneratesWeightsForSelectedSprite()
        {
            m_GenerateWeightsPanel.OnGenerateWeights();

            var sprite = skinningCache.selectedSprite;
            var meshCache = sprite.GetMesh();

            for (int i = 0; i < meshCache.vertexCount; ++i)
            {
                var weight = meshCache.GetWeight(i);
                Assert.Greater(weight.Sum(), 0);
            }
        }

        [Test]
        public void GenerateWeightsAll_GeneratesWeightsForAllSprites()
        {
            skinningCache.events.selectedSpriteChanged.Invoke(null);

            m_GenerateWeightsPanel.OnGenerateWeights();

            foreach (var sprite in skinningCache.GetSprites())
            {
                var meshCache = sprite.GetMesh();
                for (int i = 0; i < meshCache.vertexCount; ++i)
                {
                    var weight = meshCache.GetWeight(i);
                    Assert.Greater(weight.Sum(), 0);
                }
            }
        }

        [Test]
        public void NormalizeWeights_NormalizeWeightsForSelectedSprite()
        {
            m_GenerateWeightsPanel.OnGenerateWeights();
            m_GenerateWeightsPanel.OnNormalizeWeights();

            var sprite = skinningCache.selectedSprite;
            var meshCache = sprite.GetMesh();

            for (int i = 0; i < meshCache.vertexCount; ++i)
            {
                var weight = meshCache.GetWeight(i);
                Assert.AreEqual(1f, weight.Sum(), 0.0001f);
            }
        }

        [Test]
        public void NormalizeWeightsAll_NormalizeWeightsForAllSprites()
        {
            // Sprite 1 does not have weights set up
            m_GenerateWeightsPanel.OnGenerateWeights();

            skinningCache.events.selectedSpriteChanged.Invoke(null);

            m_GenerateWeightsPanel.OnNormalizeWeights();

            foreach (var sprite in skinningCache.GetSprites())
            {
                var meshCache = sprite.GetMesh();
                for (int i = 0; i < meshCache.vertexCount; ++i)
                {
                    var weight = meshCache.GetWeight(i);
                    Assert.AreEqual(1f, weight.Sum(), 0.0001f);
                }
            }
        }

        [Test]
        public void ClearWeights_ClearsWeightsForSelectedSprite()
        {
            m_GenerateWeightsPanel.OnGenerateWeights();
            m_GenerateWeightsPanel.OnClearWeights();

            var sprite = skinningCache.selectedSprite;
            var meshCache = sprite.GetMesh();

            for (int i = 0; i < meshCache.vertexCount; ++i)
            {
                var weight = meshCache.GetWeight(i);
                Assert.AreEqual(0, weight.Sum(), 0.00001f);
            }
        }

        [Test]
        public void ClearWeightsAll_ClearsWeightsForAllSprites()
        {
            // Sprite 1 does not have weights set up
            m_GenerateWeightsPanel.OnGenerateWeights();

            skinningCache.events.selectedSpriteChanged.Invoke(null);

            m_GenerateWeightsPanel.OnClearWeights();

            foreach (var sprite in skinningCache.GetSprites())
            {
                var meshCache = sprite.GetMesh();
                for (int i = 0; i < meshCache.vertexCount; ++i)
                {
                    var weight = meshCache.GetWeight(i);
                    Assert.AreEqual(0, weight.Sum(), 0.00001f);
                }
            }
        }
    }
}
