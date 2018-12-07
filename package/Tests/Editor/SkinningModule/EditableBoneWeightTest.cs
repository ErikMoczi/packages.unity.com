using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEditor.U2D;
using UnityEditor.U2D.Interface;
using UnityEngine.U2D.Interface;

namespace UnityEditor.Experimental.U2D.Animation.Test.Weights
{
    [TestFixture]
    public class EditableBoneWeightTest
    {
        [Test]
        public void SortChannels_SortByWeightDescending()
        {
            EditableBoneWeight e = new EditableBoneWeight();
            e.AddChannel(0, 0.1f, true);
            e.AddChannel(0, 0.5f, true);
            e.AddChannel(0, 0.4f, true);
            e.AddChannel(0, 0.6f, true);

            e.Sort();

            Assert.AreEqual(0.6f, e[0].weight, "Channel has incorrect data.");
            Assert.AreEqual(0.5f, e[1].weight, "Channel has incorrect data.");
            Assert.AreEqual(0.4f, e[2].weight, "Channel has incorrect data.");
            Assert.AreEqual(0.1f, e[3].weight, "Channel has incorrect data.");
        }
    }

    [TestFixture]
    public class EditableBoneWeightUtilityTest
    {
        protected void AssertBoneWeightContainsChannels(BoneWeight expected, BoneWeight actual)
        {
            var m_BoneWeightDataList = new List<BoneWeightData>();

            for (var i = 0; i < 4; ++i)
                m_BoneWeightDataList.Add(new BoneWeightData()
                {
                    boneIndex = expected.GetBoneIndex(i),
                    weight = expected.GetWeight(i)
                });

            for (var i = 0; i < 4; ++i)
                Assert.IsTrue(m_BoneWeightDataList.Contains(new BoneWeightData() { boneIndex = actual.GetBoneIndex(i), weight = actual.GetWeight(i) }), "BoneWeight incorrect channel " + i + " with boneIndex " + actual.GetBoneIndex(i) + " and weight " + actual.GetWeight(i));
        }

        [Test]
        public void CreateFromBoneWeight_WithBoneIndicesDistinct_CreatesFourEnabledChannels()
        {
            var boneWeight = new BoneWeight()
            {
                boneIndex0 = 0,
                boneIndex1 = 1,
                boneIndex2 = 2,
                boneIndex3 = 3,
                weight0 = 0.1f,
                weight1 = 0.2f,
                weight2 = 0.3f,
                weight3 = 0.4f
            };
            var e = EditableBoneWeightUtility.CreateFromBoneWeight(boneWeight);

            Assert.AreEqual(4, e.Count(), "Incorrect number of channels.");
            Assert.True(e[0].enabled, "Channel should be enabled.");
            Assert.True(e[1].enabled, "Channel should be enabled.");
            Assert.True(e[2].enabled, "Channel should be enabled.");
            Assert.True(e[3].enabled, "Channel should be enabled.");
            Assert.AreEqual(0, e[0].boneIndex, "Channel has incorrect boneIndex.");
            Assert.AreEqual(1, e[1].boneIndex, "Channel has incorrect boneIndex.");
            Assert.AreEqual(2, e[2].boneIndex, "Channel has incorrect boneIndex.");
            Assert.AreEqual(3, e[3].boneIndex, "Channel has incorrect boneIndex.");
            Assert.AreEqual(0.1f, e[0].weight, "Channel has incorrect weight.");
            Assert.AreEqual(0.2f, e[1].weight, "Channel has incorrect weight.");
            Assert.AreEqual(0.3f, e[2].weight, "Channel has incorrect weight.");
            Assert.AreEqual(0.4f, e[3].weight, "Channel has incorrect weight.");
        }

        [Test]
        public void CreateFromBoneWeight_WithRepetedBoneIndices_CreatesFourChannels_UnifyingTheRepeatedIndices()
        {
            var boneWeight = new BoneWeight()
            {
                boneIndex0 = 0,
                boneIndex1 = 1,
                boneIndex2 = 0,
                boneIndex3 = 1,
                weight0 = 0.1f,
                weight1 = 0.2f,
                weight2 = 0.3f,
                weight3 = 0.4f
            };

            var e = EditableBoneWeightUtility.CreateFromBoneWeight(boneWeight);

            Assert.AreEqual(4, e.Count(), "Incorrect number of channels.");
            Assert.True(e[0].enabled, "Channel should be enabled.");
            Assert.True(e[1].enabled, "Channel should be enabled.");
            Assert.False(e[2].enabled, "Channel should be disabled.");
            Assert.False(e[3].enabled, "Channel should be disabled.");
            Assert.AreEqual(0, e[0].boneIndex, "Channel has incorrect boneIndex.");
            Assert.AreEqual(1, e[1].boneIndex, "Channel has incorrect boneIndex.");
            Assert.AreEqual(0, e[2].boneIndex, "Channel has incorrect boneIndex.");
            Assert.AreEqual(1, e[3].boneIndex, "Channel has incorrect boneIndex.");
            Assert.AreEqual(0.4f, e[0].weight, 0.001f, "Channel has incorrect weight.");
            Assert.AreEqual(0.6f, e[1].weight, 0.001f, "Channel has incorrect weight.");
            Assert.AreEqual(0f, e[2].weight, 0.001f, "Channel has incorrect weight.");
            Assert.AreEqual(0f, e[3].weight, 0.001f, "Channel has incorrect weight.");
        }

        [Test]
        public void SetFromBoneWeight_CreatesFourChannelsInSameOrder()
        {
            var boneWeight = new BoneWeight()
            {
                boneIndex0 = 0,
                boneIndex1 = 1,
                boneIndex2 = 2,
                boneIndex3 = 3,
                weight0 = 0.1f,
                weight1 = 0.2f,
                weight2 = 0.3f,
                weight3 = 0.4f
            };
            var e = new EditableBoneWeight();
            e.SetFromBoneWeight(boneWeight);
            Assert.AreEqual(4, e.Count, "Incorrect Channel Count");
            Assert.AreEqual(e[0].weight, boneWeight.weight0, "Incorrect weight");
            Assert.AreEqual(e[1].weight, boneWeight.weight1, "Incorrect weight");
            Assert.AreEqual(e[2].weight, boneWeight.weight2, "Incorrect weight");
            Assert.AreEqual(e[3].weight, boneWeight.weight3, "Incorrect weight");
            Assert.AreEqual(e[0].weight, boneWeight.weight0, "Incorrect boneIndex");
            Assert.AreEqual(e[1].weight, boneWeight.weight1, "Incorrect boneIndex");
            Assert.AreEqual(e[2].weight, boneWeight.weight2, "Incorrect boneIndex");
            Assert.AreEqual(e[3].weight, boneWeight.weight3, "Incorrect boneIndex");
            Assert.IsTrue(e[0].enabled, "Incorrect channel enabled");
            Assert.IsTrue(e[1].enabled, "Incorrect channel enabled");
            Assert.IsTrue(e[2].enabled, "Incorrect channel enabled");
            Assert.IsTrue(e[3].enabled, "Incorrect channel enabled");
        }

        [Test]
        public void SetFromBoneWeight_DisablesChannelsWithZeroWeight()
        {
            var e = new EditableBoneWeight();
            e.SetFromBoneWeight(new BoneWeight());
            Assert.False(e[0].enabled, "Channel should be disabled.");
            Assert.False(e[1].enabled, "Channel should be disabled.");
            Assert.False(e[2].enabled, "Channel should be disabled.");
            Assert.False(e[3].enabled, "Channel should be disabled.");
        }

        [Test]
        public void ToBoneWeight_WithNoChannels_CreatesDefaultBoneWeight()
        {
            BoneWeight boneWeight = EditableBoneWeightUtility.ToBoneWeight(new EditableBoneWeight(), false);

            Assert.AreEqual(new BoneWeight(), boneWeight, "Empty EditableBoneWeight should generate a default BoneWeight");
        }

        [Test]
        public void ToBoneWeight_WithSortWeights_OutputsSortedBoneWeight()
        {
            var e = new EditableBoneWeight();
            e.AddChannel(0, 0.1f, true);
            e.AddChannel(1, 0.4f, true);
            e.AddChannel(2, 0.2f, true);
            e.AddChannel(3, 0.3f, true);

            var boneWeight = new BoneWeight()
            {
                boneIndex0 = 1,
                boneIndex1 = 3,
                boneIndex2 = 2,
                boneIndex3 = 0,
                weight0 = 0.4f,
                weight1 = 0.3f,
                weight2 = 0.2f,
                weight3 = 0.1f
            };

            Assert.AreEqual(boneWeight, e.ToBoneWeight(true), "ToBoneWeight should output a sorted BoneWeight");
        }

        [Test]
        public void ClampChannels_ReducesChannelCount()
        {
            var e = new EditableBoneWeight();

            e.AddChannel(0, 0.4f, true);
            e.AddChannel(1, 0.3f, true);
            e.AddChannel(2, 0.2f, true);
            e.AddChannel(3, 0.1f, true);
            e.AddChannel(4, 0.7f, true);
            e.AddChannel(5, 0.6f, true);
            e.AddChannel(6, 0.5f, true);

            e.Clamp(4, false);

            Assert.AreEqual(4, e.Count(), "Should contain four channels after clamp");
            Assert.AreEqual(0, e[0].boneIndex, "Channel does not contain the right bone index");
            Assert.AreEqual(1, e[1].boneIndex, "Channel does not contain the right bone index");
            Assert.AreEqual(2, e[2].boneIndex, "Channel does not contain the right bone index");
            Assert.AreEqual(3, e[3].boneIndex, "Channel does not contain the right bone index");
        }

        [Test]
        public void ClampChannels_WithSortWeights_ReducesChannelCount_GraterWeightsFirst()
        {
            var e = new EditableBoneWeight();

            e.AddChannel(0, 0.4f, true);
            e.AddChannel(1, 0.3f, true);
            e.AddChannel(2, 0.2f, true);
            e.AddChannel(3, 0.1f, true);
            e.AddChannel(4, 0.7f, true);
            e.AddChannel(5, 0.6f, true);
            e.AddChannel(6, 0.5f, true);

            e.Clamp(4, true);

            Assert.AreEqual(4, e.Count(), "Should contain four channels after clamp");
            Assert.AreEqual(4, e[0].boneIndex, "Channel does not contain the right bone index");
            Assert.AreEqual(5, e[1].boneIndex, "Channel does not contain the right bone index");
            Assert.AreEqual(6, e[2].boneIndex, "Channel does not contain the right bone index");
            Assert.AreEqual(0, e[3].boneIndex, "Channel does not contain the right bone index");
        }

        [Test]
        public void ValidateChannels_ClampsWeight01_SetWeightToZeroIfDisabled()
        {
            var e = new EditableBoneWeight();

            e.AddChannel(0, 2f, true);
            e.AddChannel(1, 1f, false);

            e.ValidateChannels();

            Assert.AreEqual(1f, e[0].weight, "Weight sould be clamped");
            Assert.AreEqual(0f, e[1].weight, "Weight sould be 0f if channel disabled");
        }

        [Test]
        public void NormalizeChannels_OutputsNormalizedWeights()
        {
            var e = new EditableBoneWeight();

            e.AddChannel(0, 1f, true);
            e.AddChannel(1, 1f, true);
            e.AddChannel(2, 1f, true);
            e.AddChannel(3, 1f, true);

            e.Normalize();

            Assert.AreEqual(0.25f, e[0].weight, "Weight should be normalized");
            Assert.AreEqual(0.25f, e[2].weight, "Weight should be normalized");
            Assert.AreEqual(0.25f, e[1].weight, "Weight should be normalized");
            Assert.AreEqual(0.25f, e[3].weight, "Weight should be normalized");
        }

        [Test]
        public void CompensateOtherChannels_KeepsMasterChannelWeight_NormalizeUsingOthers()
        {
            var e = new EditableBoneWeight();

            e.AddChannel(0, 0.25f, true);
            e.AddChannel(1, 0f, true);
            e.AddChannel(2, 0f, true);
            e.AddChannel(3, 0f, true);

            e.CompensateOtherChannels(0);

            Assert.AreEqual(0.25f, e[0].weight, "Weight should be normalized");
            Assert.AreEqual(0.25f, e[1].weight, "Weight should be normalized");
            Assert.AreEqual(0.25f, e[2].weight, "Weight should be normalized");
            Assert.AreEqual(0.25f, e[3].weight, "Weight should be normalized");
        }

        [Test]
        public void UnifyChannelsWithSameBoneIndex_AddsWeightsInSameChannel_DisablesRepeatedChannels()
        {
            var e = new EditableBoneWeight();

            e.AddChannel(0, 0.1f, true);
            e.AddChannel(1, 0.2f, true);
            e.AddChannel(0, 0.3f, true);
            e.AddChannel(3, 0.4f, true);

            e.UnifyChannelsWithSameBoneIndex();

            Assert.AreEqual(0.4f, e[0].weight, "Incorrect boneWeight");
            Assert.AreEqual(0.2f, e[1].weight, "Incorrect boneWeight");
            Assert.AreEqual(0f, e[2].weight, "Incorrect boneWeight");
            Assert.False(e[2].enabled, "Channel should be disabled");
            Assert.AreEqual(0.4f, e[3].weight, "Incorrect boneWeight");
        }

        [Test]
        public void LerpBoneWeight_AllIndicesInCommon()
        {
            var first = new BoneWeight()
            {
                boneIndex0 = 0,
                boneIndex1 = 1,
                boneIndex2 = 2,
                boneIndex3 = 3,
                weight0 = 0.1f,
                weight1 = 0.2f,
                weight2 = 0.3f,
                weight3 = 0.4f
            };

            var second = new BoneWeight()
            {
                boneIndex0 = 0,
                boneIndex1 = 1,
                boneIndex2 = 2,
                boneIndex3 = 3,
                weight0 = 0.4f,
                weight1 = 0.3f,
                weight2 = 0.2f,
                weight3 = 0.1f
            };

            var result = EditableBoneWeightUtility.Lerp(first, second, 0f);

            var expected = new BoneWeight()
            {
                boneIndex0 = 3,
                boneIndex1 = 2,
                boneIndex2 = 1,
                boneIndex3 = 0,
                weight0 = 0.4f,
                weight1 = 0.3f,
                weight2 = 0.2f,
                weight3 = 0.1f
            };

            AssertBoneWeightContainsChannels(expected, result);

            result = EditableBoneWeightUtility.Lerp(first, second, 0.2f);

            expected = new BoneWeight()
            {
                boneIndex0 = 3,
                boneIndex1 = 2,
                boneIndex2 = 1,
                boneIndex3 = 0,
                weight0 = 0.340000033f,
                weight1 = 0.279999942f,
                weight2 = 0.219999984f,
                weight3 = 0.159999982f
            };

            AssertBoneWeightContainsChannels(expected, result);

            result = EditableBoneWeightUtility.Lerp(first, second, 0.5f);

            expected = new BoneWeight()
            {
                boneIndex0 = 0,
                boneIndex1 = 2,
                boneIndex2 = 3,
                boneIndex3 = 1,
                weight0 = 0.25000003f,
                weight1 = 0.25f,
                weight2 = 0.25f,
                weight3 = 0.249999985f
            };

            AssertBoneWeightContainsChannels(expected, result);

            result = EditableBoneWeightUtility.Lerp(first, second, 0.8f);

            expected = new BoneWeight()
            {
                boneIndex0 = 0,
                boneIndex1 = 1,
                boneIndex2 = 2,
                boneIndex3 = 3,
                weight0 = 0.340000033f,
                weight1 = 0.279999971f,
                weight2 = 0.220000014f,
                weight3 = 0.159999996f
            };

            AssertBoneWeightContainsChannels(expected, result);

            result = EditableBoneWeightUtility.Lerp(first, second, 1f);

            expected = new BoneWeight()
            {
                boneIndex0 = 0,
                boneIndex1 = 1,
                boneIndex2 = 2,
                boneIndex3 = 3,
                weight0 = 0.4f,
                weight1 = 0.3f,
                weight2 = 0.2f,
                weight3 = 0.1f
            };

            AssertBoneWeightContainsChannels(expected, result);
        }

        [Test]
        public void LerpBoneWeight_SomeIndicesInCommon()
        {
            var first = new BoneWeight()
            {
                boneIndex0 = 0,
                boneIndex1 = 1,
                boneIndex2 = 2,
                boneIndex3 = 3,
                weight0 = 0.1f,
                weight1 = 0.2f,
                weight2 = 0.3f,
                weight3 = 0.4f
            };

            var second = new BoneWeight()
            {
                boneIndex0 = 0,
                boneIndex1 = 4,
                boneIndex2 = 2,
                boneIndex3 = 5,
                weight0 = 0.4f,
                weight1 = 0.3f,
                weight2 = 0.2f,
                weight3 = 0.1f
            };

            var result = EditableBoneWeightUtility.Lerp(first, second, 0f);

            var expected = new BoneWeight()
            {
                boneIndex0 = 3,
                boneIndex1 = 2,
                boneIndex2 = 1,
                boneIndex3 = 0,
                weight0 = 0.4f,
                weight1 = 0.3f,
                weight2 = 0.2f,
                weight3 = 0.1f
            };

            AssertBoneWeightContainsChannels(expected, result);

            result = EditableBoneWeightUtility.Lerp(first, second, 0.2f);

            expected = new BoneWeight()
            {
                boneIndex0 = 3,
                boneIndex1 = 2,
                boneIndex2 = 1,
                boneIndex3 = 0,
                weight0 = 0.320000023f,
                weight1 = 0.280000001f,
                weight2 = 0.160000011f,
                weight3 = 0.160000011f
            };

            AssertBoneWeightContainsChannels(expected, result);

            result = EditableBoneWeightUtility.Lerp(first, second, 0.5f);

            expected = new BoneWeight()
            {
                boneIndex0 = 0,
                boneIndex1 = 2,
                boneIndex2 = 3,
                boneIndex3 = 4,
                weight0 = 0.25000003f,
                weight1 = 0.249999985f,
                weight2 = 0.200000003f,
                weight3 = 0.150000006f
            };

            AssertBoneWeightContainsChannels(expected, result);

            result = EditableBoneWeightUtility.Lerp(first, second, 0.8f);

            expected = new BoneWeight()
            {
                boneIndex0 = 0,
                boneIndex1 = 4,
                boneIndex2 = 2,
                boneIndex3 = 5,
                weight0 = 0.340000033f,
                weight1 = 0.239999995f,
                weight2 = 0.219999999f,
                weight3 = 0.0799999982f
            };

            AssertBoneWeightContainsChannels(expected, result);

            result = EditableBoneWeightUtility.Lerp(first, second, 1f);

            Assert.AreEqual(0, result.boneIndex0, "Incorrect boneIndex");
            Assert.AreEqual(4, result.boneIndex1, "Incorrect boneIndex");
            Assert.AreEqual(2, result.boneIndex2, "Incorrect boneIndex");
            Assert.AreEqual(5, result.boneIndex3, "Incorrect boneIndex");
            Assert.AreEqual(0.4f, result.weight0, "Incorrect boneWeight");
            Assert.AreEqual(0.3f, result.weight1, "Incorrect boneWeight");
            Assert.AreEqual(0.2f, result.weight2, "Incorrect boneWeight");
            Assert.AreEqual(0.1f, result.weight3, "Incorrect boneWeight");

            expected = new BoneWeight()
            {
                boneIndex0 = 0,
                boneIndex1 = 4,
                boneIndex2 = 2,
                boneIndex3 = 5,
                weight0 = 0.4f,
                weight1 = 0.3f,
                weight2 = 0.2f,
                weight3 = 0.1f
            };

            AssertBoneWeightContainsChannels(expected, result);
        }

        [Test]
        public void LerpBoneWeight_AllIndicesDifferent()
        {
            var first = new BoneWeight()
            {
                boneIndex0 = 0,
                boneIndex1 = 1,
                boneIndex2 = 2,
                boneIndex3 = 3,
                weight0 = 0.1f,
                weight1 = 0.2f,
                weight2 = 0.3f,
                weight3 = 0.4f
            };

            var second = new BoneWeight()
            {
                boneIndex0 = 4,
                boneIndex1 = 5,
                boneIndex2 = 6,
                boneIndex3 = 7,
                weight0 = 0.4f,
                weight1 = 0.3f,
                weight2 = 0.2f,
                weight3 = 0.1f
            };

            var result = EditableBoneWeightUtility.Lerp(first, second, 0f);

            var expected = new BoneWeight()
            {
                boneIndex0 = 3,
                boneIndex1 = 2,
                boneIndex2 = 1,
                boneIndex3 = 0,
                weight0 = 0.4f,
                weight1 = 0.3f,
                weight2 = 0.2f,
                weight3 = 0.1f
            };

            AssertBoneWeightContainsChannels(expected, result);

            result = EditableBoneWeightUtility.Lerp(first, second, 0.2f);

            expected = new BoneWeight()
            {
                boneIndex0 = 3,
                boneIndex1 = 2,
                boneIndex2 = 1,
                boneIndex3 = 4,
                weight0 = 0.319999993f,
                weight1 = 0.24000001f,
                weight2 = 0.159999996f,
                weight3 = 0.0800000057f
            };

            AssertBoneWeightContainsChannels(expected, result);

            result = EditableBoneWeightUtility.Lerp(first, second, 0.5f);

            expected = new BoneWeight()
            {
                boneIndex0 = 4,
                boneIndex1 = 3,
                boneIndex2 = 2,
                boneIndex3 = 5,
                weight0 = 0.200000003f,
                weight1 = 0.200000003f,
                weight2 = 0.150000006f,
                weight3 = 0.150000006f
            };

            AssertBoneWeightContainsChannels(expected, result);

            result = EditableBoneWeightUtility.Lerp(first, second, 0.8f);

            expected = new BoneWeight()
            {
                boneIndex0 = 4,
                boneIndex1 = 5,
                boneIndex2 = 6,
                boneIndex3 = 7,
                weight0 = 0.320000023f,
                weight1 = 0.24000001f,
                weight2 = 0.160000011f,
                weight3 = 0.0800000057f
            };

            AssertBoneWeightContainsChannels(expected, result);

            result = EditableBoneWeightUtility.Lerp(first, second, 1f);

            expected = new BoneWeight()
            {
                boneIndex0 = 4,
                boneIndex1 = 5,
                boneIndex2 = 6,
                boneIndex3 = 7,
                weight0 = 0.4f,
                weight1 = 0.3f,
                weight2 = 0.2f,
                weight3 = 0.1f
            };

            AssertBoneWeightContainsChannels(expected, result);
        }

        [Test]
        public void FilterChannels_DisablesChannelsWithWeightBelowTolerance()
        {
            var e = new EditableBoneWeight();

            e.AddChannel(0, 0.2f, true);
            e.AddChannel(1, 0.2f, true);
            e.AddChannel(2, 0.3f, true);
            e.AddChannel(3, 0.3f, true);

            e.FilterChannels(0.25f);

            Assert.False(e[0].enabled, "Channel should be disabled");
            Assert.False(e[1].enabled, "Channel should be disabled");
            Assert.True(e[2].enabled, "Channel should be enabled");
            Assert.True(e[3].enabled, "Channel should be enabled");
            Assert.AreEqual(0f, e[0].weight, "Incorrect boneWeight");
            Assert.AreEqual(0f, e[1].weight, "Incorrect boneWeight");
            Assert.AreEqual(0.3f, e[2].weight, "Incorrect boneWeight");
            Assert.AreEqual(0.3f, e[3].weight, "Incorrect boneWeight");
        }
    }
}
