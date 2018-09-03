using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEditor.U2D;
using UnityEditor.U2D.Interface;
using UnityEngine.U2D.Interface;

namespace UnityEditor.Experimental.U2D.Animation.Test.MeshModule.Weights
{
    [TestFixture]
    public class EditableBoneWeightTest
    {
        [Test]
        public void SetBoneWeightData_WithInvalidChannelIndex_TrowsException()
        {
            EditableBoneWeight e = new EditableBoneWeight();
            e.AddChannel(new BoneWeightData(), true);
            Assert.Throws<IndexOutOfRangeException>(() => e.SetBoneWeightData(-1, new BoneWeightData()));
            Assert.Throws<IndexOutOfRangeException>(() => e.SetBoneWeightData(1, new BoneWeightData()));
        }

        [Test]
        public void GetBoneWeightData_WithInvalidChannelIndex_TrowsException()
        {
            EditableBoneWeight e = new EditableBoneWeight();
            e.AddChannel(new BoneWeightData(), true);
            Assert.Throws<IndexOutOfRangeException>(() => e.GetBoneWeightData(-1));
            Assert.Throws<IndexOutOfRangeException>(() => e.GetBoneWeightData(1));
        }

        [Test]
        public void EnableChannel_WithInvalidChannelIndex_TrowsException()
        {
            EditableBoneWeight e = new EditableBoneWeight();
            e.AddChannel(new BoneWeightData(), true);
            Assert.Throws<IndexOutOfRangeException>(() => e.EnableChannel(-1, true));
            Assert.Throws<IndexOutOfRangeException>(() => e.EnableChannel(1, true));
        }

        [Test]
        public void IsChannelEnabled_WithInvalidChannelIndex_TrowsException()
        {
            EditableBoneWeight e = new EditableBoneWeight();
            e.AddChannel(new BoneWeightData(), true);
            Assert.Throws<IndexOutOfRangeException>(() => e.IsChannelEnabled(-1));
            Assert.Throws<IndexOutOfRangeException>(() => e.IsChannelEnabled(1));
        }

        [Test]
        public void SortChannels_SortByWeightDescending()
        {
            EditableBoneWeight e = new EditableBoneWeight();
            BoneWeightData d1 = new BoneWeightData(0, 0.1f);
            BoneWeightData d2 = new BoneWeightData(0, 0.5f);
            BoneWeightData d3 = new BoneWeightData(0, 0.4f);
            BoneWeightData d4 = new BoneWeightData(0, 0.6f);

            e.AddChannel(d1, true);
            e.AddChannel(d2, true);
            e.AddChannel(d3, true);
            e.AddChannel(d4, true);

            e.SortChannels();

            Assert.AreEqual(d4, e.GetBoneWeightData(0), "Channel has incorrect data.");
            Assert.AreEqual(d2, e.GetBoneWeightData(1), "Channel has incorrect data.");
            Assert.AreEqual(d3, e.GetBoneWeightData(2), "Channel has incorrect data.");
            Assert.AreEqual(d1, e.GetBoneWeightData(3), "Channel has incorrect data.");
        }
    }

    [TestFixture]
    public class EditableBoneWeightUtilityTest
    {
        [Test]
        public void CreateFromBoneWeight_WithBoneIndicesDistinct_CreatesFourEnabledChannels()
        {
            BoneWeight boneWeight = new BoneWeight();
            boneWeight.boneIndex0 = 0;
            boneWeight.boneIndex1 = 1;
            boneWeight.boneIndex2 = 2;
            boneWeight.boneIndex3 = 3;
            boneWeight.weight0 = 0.1f;
            boneWeight.weight1 = 0.2f;
            boneWeight.weight2 = 0.3f;
            boneWeight.weight3 = 0.4f;
            EditableBoneWeight editableBoneWeight = EditableBoneWeightUtility.CreateFromBoneWeight(boneWeight);

            Assert.AreEqual(4, editableBoneWeight.GetChannelCount(), "Incorrect number of channels.");
            Assert.True(editableBoneWeight.IsChannelEnabled(0), "Channel should be enabled.");
            Assert.True(editableBoneWeight.IsChannelEnabled(1), "Channel should be enabled.");
            Assert.True(editableBoneWeight.IsChannelEnabled(2), "Channel should be enabled.");
            Assert.True(editableBoneWeight.IsChannelEnabled(3), "Channel should be enabled.");
            Assert.AreEqual(new BoneWeightData(0, 0.1f), editableBoneWeight.GetBoneWeightData(0), "Channel has incorrect data.");
            Assert.AreEqual(new BoneWeightData(1, 0.2f), editableBoneWeight.GetBoneWeightData(1), "Channel has incorrect data.");
            Assert.AreEqual(new BoneWeightData(2, 0.3f), editableBoneWeight.GetBoneWeightData(2), "Channel has incorrect data.");
            Assert.AreEqual(new BoneWeightData(3, 0.4f), editableBoneWeight.GetBoneWeightData(3), "Channel has incorrect data.");
        }

        [Test]
        public void CreateFromBoneWeight_WithRepetedBoneIndices_CreatesFourChannels_UnifyingTheRepeatedIndices()
        {
            BoneWeight boneWeight = new BoneWeight();
            boneWeight.boneIndex0 = 0;
            boneWeight.boneIndex1 = 1;
            boneWeight.boneIndex2 = 0;
            boneWeight.boneIndex3 = 1;
            boneWeight.weight0 = 0.1f;
            boneWeight.weight1 = 0.2f;
            boneWeight.weight2 = 0.3f;
            boneWeight.weight3 = 0.4f;
            EditableBoneWeight editableBoneWeight = EditableBoneWeightUtility.CreateFromBoneWeight(boneWeight);

            Assert.AreEqual(4, editableBoneWeight.GetChannelCount(), "Incorrect number of channels.");
            Assert.True(editableBoneWeight.IsChannelEnabled(0), "Channel should be enabled.");
            Assert.True(editableBoneWeight.IsChannelEnabled(1), "Channel should be enabled.");
            Assert.False(editableBoneWeight.IsChannelEnabled(2), "Channel should be disabled.");
            Assert.False(editableBoneWeight.IsChannelEnabled(3), "Channel should be disabled.");
            Assert.AreEqual(0, editableBoneWeight.GetBoneWeightData(0).boneIndex, "Incorrect bone index");
            Assert.AreEqual(1, editableBoneWeight.GetBoneWeightData(1).boneIndex, "Incorrect bone index");
            Assert.AreEqual(0, editableBoneWeight.GetBoneWeightData(2).boneIndex, "Incorrect bone index");
            Assert.AreEqual(1, editableBoneWeight.GetBoneWeightData(3).boneIndex, "Incorrect bone index");
            Assert.AreEqual(0.4f, editableBoneWeight.GetBoneWeightData(0).weight, 0.00001f, "Incorrect weight");
            Assert.AreEqual(0.6f, editableBoneWeight.GetBoneWeightData(1).weight, "Incorrect weight");
            Assert.AreEqual(0f, editableBoneWeight.GetBoneWeightData(2).weight, "Incorrect weight");
            Assert.AreEqual(0f, editableBoneWeight.GetBoneWeightData(3).weight, "Incorrect weight");
        }

        [Test]
        public void SetBoneWeightData_DisablesChannelsWithZeroWeight()
        {
            EditableBoneWeight e = new EditableBoneWeight();
            e.SetFromBoneWeight(new BoneWeight());
            Assert.IsFalse(e.IsChannelEnabled(0), "Channel should be disabled");
            Assert.IsFalse(e.IsChannelEnabled(1), "Channel should be disabled");
            Assert.IsFalse(e.IsChannelEnabled(2), "Channel should be disabled");
            Assert.IsFalse(e.IsChannelEnabled(3), "Channel should be disabled");
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
            EditableBoneWeight editableBoneWeight = new EditableBoneWeight();

            editableBoneWeight.AddChannel(new BoneWeightData(0, 0.1f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(1, 0.4f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(2, 0.2f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(3, 0.3f), true);

            BoneWeight boneWeight = new BoneWeight();
            boneWeight.boneIndex0 = 1;
            boneWeight.boneIndex1 = 3;
            boneWeight.boneIndex2 = 2;
            boneWeight.boneIndex3 = 0;
            boneWeight.weight0 = 0.4f;
            boneWeight.weight1 = 0.3f;
            boneWeight.weight2 = 0.2f;
            boneWeight.weight3 = 0.1f;

            Assert.AreEqual(boneWeight, editableBoneWeight.ToBoneWeight(true), "ToBoneWeight should output a sorted BoneWeight");
        }

        [Test]
        public void ClampChannels_ReducesChannelCount()
        {
            EditableBoneWeight editableBoneWeight = new EditableBoneWeight();

            editableBoneWeight.AddChannel(new BoneWeightData(0, 0.4f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(1, 0.3f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(2, 0.2f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(3, 0.1f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(4, 0.7f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(5, 0.6f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(6, 0.5f), true);

            editableBoneWeight.ClampChannels(4, false);

            Assert.AreEqual(4, editableBoneWeight.GetChannelCount(), "Should contain four channels after clamp");
            Assert.AreEqual(0, editableBoneWeight.GetBoneWeightData(0).boneIndex, "Channel does not contain the right bone index");
            Assert.AreEqual(1, editableBoneWeight.GetBoneWeightData(1).boneIndex, "Channel does not contain the right bone index");
            Assert.AreEqual(2, editableBoneWeight.GetBoneWeightData(2).boneIndex, "Channel does not contain the right bone index");
            Assert.AreEqual(3, editableBoneWeight.GetBoneWeightData(3).boneIndex, "Channel does not contain the right bone index");
        }

        [Test]
        public void ClampChannels_WithSortWeights_ReducesChannelCount_GraterWeightsFirst()
        {
            EditableBoneWeight editableBoneWeight = new EditableBoneWeight();

            editableBoneWeight.AddChannel(new BoneWeightData(0, 0.4f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(1, 0.3f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(2, 0.2f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(3, 0.1f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(4, 0.7f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(5, 0.6f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(6, 0.5f), true);

            editableBoneWeight.ClampChannels(4, true);

            Assert.AreEqual(4, editableBoneWeight.GetChannelCount(), "Should contain four channels after clamp");
            Assert.AreEqual(4, editableBoneWeight.GetBoneWeightData(0).boneIndex, "Channel does not contain the right bone index");
            Assert.AreEqual(5, editableBoneWeight.GetBoneWeightData(1).boneIndex, "Channel does not contain the right bone index");
            Assert.AreEqual(6, editableBoneWeight.GetBoneWeightData(2).boneIndex, "Channel does not contain the right bone index");
            Assert.AreEqual(0, editableBoneWeight.GetBoneWeightData(3).boneIndex, "Channel does not contain the right bone index");
        }

        [Test]
        public void ValidateChannels_ClampsWeight01_SetWeightToZeroIfDisabled()
        {
            EditableBoneWeight editableBoneWeight = new EditableBoneWeight();

            editableBoneWeight.AddChannel(new BoneWeightData(0, 2f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(1, 1f), false);

            editableBoneWeight.ValidateChannels();

            Assert.AreEqual(1f, editableBoneWeight.GetBoneWeightData(0).weight, "Weight sould be clamped");
            Assert.AreEqual(0f, editableBoneWeight.GetBoneWeightData(1).weight, "Weight sould be 0f if channel disabled");
        }

        [Test]
        public void NormalizeChannels_OutputsNormalizedWeights()
        {
            EditableBoneWeight editableBoneWeight = new EditableBoneWeight();

            editableBoneWeight.AddChannel(new BoneWeightData(0, 1f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(1, 1f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(2, 1f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(3, 1f), true);

            editableBoneWeight.NormalizeChannels();

            Assert.AreEqual(0.25f, editableBoneWeight.GetBoneWeightData(0).weight, "Weight should be normalized");
            Assert.AreEqual(0.25f, editableBoneWeight.GetBoneWeightData(1).weight, "Weight should be normalized");
            Assert.AreEqual(0.25f, editableBoneWeight.GetBoneWeightData(2).weight, "Weight should be normalized");
            Assert.AreEqual(0.25f, editableBoneWeight.GetBoneWeightData(3).weight, "Weight should be normalized");
        }

        [Test]
        public void CompensateOtherChannels_KeepsMasterChannelWeight_NormalizeUsingOthers()
        {
            EditableBoneWeight editableBoneWeight = new EditableBoneWeight();

            editableBoneWeight.AddChannel(new BoneWeightData(0, 0.25f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(1, 0f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(2, 0f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(3, 0f), true);

            editableBoneWeight.CompensateOtherChannels(0);

            Assert.AreEqual(0.25f, editableBoneWeight.GetBoneWeightData(1).weight, "Weight should be normalized");
            Assert.AreEqual(0.25f, editableBoneWeight.GetBoneWeightData(2).weight, "Weight should be normalized");
            Assert.AreEqual(0.25f, editableBoneWeight.GetBoneWeightData(3).weight, "Weight should be normalized");
        }

        [Test]
        public void UnifyChannelsWithSameBoneIndex_AddsWeightsInSameChannel_DisablesRepeatedChannels()
        {
            EditableBoneWeight editableBoneWeight = new EditableBoneWeight();

            editableBoneWeight.AddChannel(new BoneWeightData(0, 0.1f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(1, 0.2f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(0, 0.3f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(3, 0.4f), true);

            editableBoneWeight.UnifyChannelsWithSameBoneIndex();

            Assert.AreEqual(0.4f, editableBoneWeight.GetBoneWeightData(0).weight, "Incorrect boneWeight");
            Assert.AreEqual(0.2f, editableBoneWeight.GetBoneWeightData(1).weight, "Incorrect boneWeight");
            Assert.AreEqual(0f, editableBoneWeight.GetBoneWeightData(2).weight, "Incorrect boneWeight");
            Assert.False(editableBoneWeight.IsChannelEnabled(2), "Channel should be disabled");
            Assert.AreEqual(0.4f, editableBoneWeight.GetBoneWeightData(3).weight, "Incorrect boneWeight");
        }

        [Test]
        public void LerpBoneWeight_AllIndicesInCommon()
        {
            BoneWeight first = new BoneWeight();
            first.boneIndex0 = 0;
            first.boneIndex1 = 1;
            first.boneIndex2 = 2;
            first.boneIndex3 = 3;
            first.weight0 = 0.1f;
            first.weight1 = 0.2f;
            first.weight2 = 0.3f;
            first.weight3 = 0.4f;

            BoneWeight second = new BoneWeight();
            second.boneIndex0 = 0;
            second.boneIndex1 = 1;
            second.boneIndex2 = 2;
            second.boneIndex3 = 3;
            second.weight0 = 0.4f;
            second.weight1 = 0.3f;
            second.weight2 = 0.2f;
            second.weight3 = 0.1f;

            BoneWeight result = EditableBoneWeightUtility.Lerp(first, second, 0f);

            Assert.AreEqual(3, result.boneIndex0, "Incorrect boneIndex");
            Assert.AreEqual(2, result.boneIndex1, "Incorrect boneIndex");
            Assert.AreEqual(1, result.boneIndex2, "Incorrect boneIndex");
            Assert.AreEqual(0, result.boneIndex3, "Incorrect boneIndex");
            Assert.AreEqual(0.4f, result.weight0, "Incorrect boneWeight");
            Assert.AreEqual(0.3f, result.weight1, "Incorrect boneWeight");
            Assert.AreEqual(0.2f, result.weight2, "Incorrect boneWeight");
            Assert.AreEqual(0.1f, result.weight3, "Incorrect boneWeight");

            result = EditableBoneWeightUtility.Lerp(first, second, 0.2f);

            Assert.AreEqual(3, result.boneIndex0, "Incorrect boneIndex");
            Assert.AreEqual(2, result.boneIndex1, "Incorrect boneIndex");
            Assert.AreEqual(1, result.boneIndex2, "Incorrect boneIndex");
            Assert.AreEqual(0, result.boneIndex3, "Incorrect boneIndex");
            Assert.AreEqual(0.340000033f, result.weight0, "Incorrect boneWeight");
            Assert.AreEqual(0.279999942f, result.weight1, "Incorrect boneWeight");
            Assert.AreEqual(0.219999984f, result.weight2, "Incorrect boneWeight");
            Assert.AreEqual(0.159999982f, result.weight3, "Incorrect boneWeight");

            result = EditableBoneWeightUtility.Lerp(first, second, 0.5f);

            Assert.AreEqual(0, result.boneIndex0, "Incorrect boneIndex");
            Assert.AreEqual(2, result.boneIndex1, "Incorrect boneIndex");
            Assert.AreEqual(3, result.boneIndex2, "Incorrect boneIndex");
            Assert.AreEqual(1, result.boneIndex3, "Incorrect boneIndex");
            Assert.AreEqual(0.25000003f, result.weight0, "Incorrect boneWeight");
            Assert.AreEqual(0.25f, result.weight1, "Incorrect boneWeight");
            Assert.AreEqual(0.25f, result.weight2, "Incorrect boneWeight");
            Assert.AreEqual(0.249999985f, result.weight3, "Incorrect boneWeight");

            result = EditableBoneWeightUtility.Lerp(first, second, 0.8f);

            Assert.AreEqual(0, result.boneIndex0, "Incorrect boneIndex");
            Assert.AreEqual(1, result.boneIndex1, "Incorrect boneIndex");
            Assert.AreEqual(2, result.boneIndex2, "Incorrect boneIndex");
            Assert.AreEqual(3, result.boneIndex3, "Incorrect boneIndex");
            Assert.AreEqual(0.340000033f, result.weight0, "Incorrect boneWeight");
            Assert.AreEqual(0.279999971f, result.weight1, "Incorrect boneWeight");
            Assert.AreEqual(0.220000014f, result.weight2, "Incorrect boneWeight");
            Assert.AreEqual(0.159999996f, result.weight3, "Incorrect boneWeight");

            result = EditableBoneWeightUtility.Lerp(first, second, 1f);

            Assert.AreEqual(0, result.boneIndex0, "Incorrect boneIndex");
            Assert.AreEqual(1, result.boneIndex1, "Incorrect boneIndex");
            Assert.AreEqual(2, result.boneIndex2, "Incorrect boneIndex");
            Assert.AreEqual(3, result.boneIndex3, "Incorrect boneIndex");
            Assert.AreEqual(0.4f, result.weight0, "Incorrect boneWeight");
            Assert.AreEqual(0.3f, result.weight1, "Incorrect boneWeight");
            Assert.AreEqual(0.2f, result.weight2, "Incorrect boneWeight");
            Assert.AreEqual(0.1f, result.weight3, "Incorrect boneWeight");
        }

        [Test]
        public void LerpBoneWeight_SomeIndicesInCommon()
        {
            BoneWeight first = new BoneWeight();
            first.boneIndex0 = 0;
            first.boneIndex1 = 1;
            first.boneIndex2 = 2;
            first.boneIndex3 = 3;
            first.weight0 = 0.1f;
            first.weight1 = 0.2f;
            first.weight2 = 0.3f;
            first.weight3 = 0.4f;

            BoneWeight second = new BoneWeight();
            second.boneIndex0 = 0;
            second.boneIndex1 = 4;
            second.boneIndex2 = 2;
            second.boneIndex3 = 5;
            second.weight0 = 0.4f;
            second.weight1 = 0.3f;
            second.weight2 = 0.2f;
            second.weight3 = 0.1f;

            BoneWeight result = EditableBoneWeightUtility.Lerp(first, second, 0f);

            Assert.AreEqual(3, result.boneIndex0, "Incorrect boneIndex");
            Assert.AreEqual(2, result.boneIndex1, "Incorrect boneIndex");
            Assert.AreEqual(1, result.boneIndex2, "Incorrect boneIndex");
            Assert.AreEqual(0, result.boneIndex3, "Incorrect boneIndex");
            Assert.AreEqual(0.4f, result.weight0, "Incorrect boneWeight");
            Assert.AreEqual(0.3f, result.weight1, "Incorrect boneWeight");
            Assert.AreEqual(0.2f, result.weight2, "Incorrect boneWeight");
            Assert.AreEqual(0.1f, result.weight3, "Incorrect boneWeight");

            result = EditableBoneWeightUtility.Lerp(first, second, 0.2f);

            Assert.AreEqual(3, result.boneIndex0, "Incorrect boneIndex");
            Assert.AreEqual(2, result.boneIndex1, "Incorrect boneIndex");
            Assert.AreEqual(1, result.boneIndex2, "Incorrect boneIndex");
            Assert.AreEqual(0, result.boneIndex3, "Incorrect boneIndex");
            Assert.AreEqual(0.319999993f, result.weight0, "Incorrect boneWeight");
            Assert.AreEqual(0.279999971f, result.weight1, "Incorrect boneWeight");
            Assert.AreEqual(0.159999996f, result.weight2, "Incorrect boneWeight");
            Assert.AreEqual(0.159999996f, result.weight3, "Incorrect boneWeight");

            result = EditableBoneWeightUtility.Lerp(first, second, 0.5f);

            Assert.AreEqual(0, result.boneIndex0, "Incorrect boneIndex");
            Assert.AreEqual(2, result.boneIndex1, "Incorrect boneIndex");
            Assert.AreEqual(3, result.boneIndex2, "Incorrect boneIndex");
            Assert.AreEqual(4, result.boneIndex3, "Incorrect boneIndex");
            Assert.AreEqual(0.25000003f, result.weight0, "Incorrect boneWeight");
            Assert.AreEqual(0.249999985f, result.weight1, "Incorrect boneWeight");
            Assert.AreEqual(0.200000003f, result.weight2, "Incorrect boneWeight");
            Assert.AreEqual(0.150000006f, result.weight3, "Incorrect boneWeight");

            result = EditableBoneWeightUtility.Lerp(first, second, 0.8f);

            Assert.AreEqual(0, result.boneIndex0, "Incorrect boneIndex");
            Assert.AreEqual(4, result.boneIndex1, "Incorrect boneIndex");
            Assert.AreEqual(2, result.boneIndex2, "Incorrect boneIndex");
            Assert.AreEqual(5, result.boneIndex3, "Incorrect boneIndex");
            Assert.AreEqual(0.340000033f, result.weight0, "Incorrect boneWeight");
            Assert.AreEqual(0.239999995f, result.weight1, "Incorrect boneWeight");
            Assert.AreEqual(0.219999999f, result.weight2, "Incorrect boneWeight");
            Assert.AreEqual(0.0799999982f, result.weight3, "Incorrect boneWeight");

            result = EditableBoneWeightUtility.Lerp(first, second, 1f);

            Assert.AreEqual(0, result.boneIndex0, "Incorrect boneIndex");
            Assert.AreEqual(4, result.boneIndex1, "Incorrect boneIndex");
            Assert.AreEqual(2, result.boneIndex2, "Incorrect boneIndex");
            Assert.AreEqual(5, result.boneIndex3, "Incorrect boneIndex");
            Assert.AreEqual(0.4f, result.weight0, "Incorrect boneWeight");
            Assert.AreEqual(0.3f, result.weight1, "Incorrect boneWeight");
            Assert.AreEqual(0.2f, result.weight2, "Incorrect boneWeight");
            Assert.AreEqual(0.1f, result.weight3, "Incorrect boneWeight");
        }

        [Test]
        public void LerpBoneWeight_AllIndicesDifferent()
        {
            BoneWeight first = new BoneWeight();
            first.boneIndex0 = 0;
            first.boneIndex1 = 1;
            first.boneIndex2 = 2;
            first.boneIndex3 = 3;
            first.weight0 = 0.1f;
            first.weight1 = 0.2f;
            first.weight2 = 0.3f;
            first.weight3 = 0.4f;

            BoneWeight second = new BoneWeight();
            second.boneIndex0 = 4;
            second.boneIndex1 = 5;
            second.boneIndex2 = 6;
            second.boneIndex3 = 7;
            second.weight0 = 0.4f;
            second.weight1 = 0.3f;
            second.weight2 = 0.2f;
            second.weight3 = 0.1f;

            BoneWeight result = EditableBoneWeightUtility.Lerp(first, second, 0f);

            Assert.AreEqual(3, result.boneIndex0, "Incorrect boneIndex");
            Assert.AreEqual(2, result.boneIndex1, "Incorrect boneIndex");
            Assert.AreEqual(1, result.boneIndex2, "Incorrect boneIndex");
            Assert.AreEqual(0, result.boneIndex3, "Incorrect boneIndex");
            Assert.AreEqual(0.4f, result.weight0, "Incorrect boneWeight");
            Assert.AreEqual(0.3f, result.weight1, "Incorrect boneWeight");
            Assert.AreEqual(0.2f, result.weight2, "Incorrect boneWeight");
            Assert.AreEqual(0.1f, result.weight3, "Incorrect boneWeight");

            result = EditableBoneWeightUtility.Lerp(first, second, 0.2f);

            Assert.AreEqual(3, result.boneIndex0, "Incorrect boneIndex");
            Assert.AreEqual(2, result.boneIndex1, "Incorrect boneIndex");
            Assert.AreEqual(1, result.boneIndex2, "Incorrect boneIndex");
            Assert.AreEqual(4, result.boneIndex3, "Incorrect boneIndex");
            Assert.AreEqual(0.319999993f, result.weight0, "Incorrect boneWeight");
            Assert.AreEqual(0.24000001f, result.weight1, "Incorrect boneWeight");
            Assert.AreEqual(0.159999996f, result.weight2, "Incorrect boneWeight");
            Assert.AreEqual(0.0800000057f, result.weight3, "Incorrect boneWeight");

            result = EditableBoneWeightUtility.Lerp(first, second, 0.5f);

            Assert.AreEqual(4, result.boneIndex0, "Incorrect boneIndex");
            Assert.AreEqual(3, result.boneIndex1, "Incorrect boneIndex");
            Assert.AreEqual(2, result.boneIndex2, "Incorrect boneIndex");
            Assert.AreEqual(5, result.boneIndex3, "Incorrect boneIndex");
            Assert.AreEqual(0.200000003f, result.weight0, "Incorrect boneWeight");
            Assert.AreEqual(0.200000003f, result.weight1, "Incorrect boneWeight");
            Assert.AreEqual(0.150000006f, result.weight2, "Incorrect boneWeight");
            Assert.AreEqual(0.150000006f, result.weight3, "Incorrect boneWeight");

            result = EditableBoneWeightUtility.Lerp(first, second, 0.8f);

            Assert.AreEqual(4, result.boneIndex0, "Incorrect boneIndex");
            Assert.AreEqual(5, result.boneIndex1, "Incorrect boneIndex");
            Assert.AreEqual(6, result.boneIndex2, "Incorrect boneIndex");
            Assert.AreEqual(7, result.boneIndex3, "Incorrect boneIndex");
            Assert.AreEqual(0.320000023f, result.weight0, "Incorrect boneWeight");
            Assert.AreEqual(0.24000001f, result.weight1, "Incorrect boneWeight");
            Assert.AreEqual(0.160000011f, result.weight2, "Incorrect boneWeight");
            Assert.AreEqual(0.0800000057f, result.weight3, "Incorrect boneWeight");

            result = EditableBoneWeightUtility.Lerp(first, second, 1f);

            Assert.AreEqual(4, result.boneIndex0, "Incorrect boneIndex");
            Assert.AreEqual(5, result.boneIndex1, "Incorrect boneIndex");
            Assert.AreEqual(6, result.boneIndex2, "Incorrect boneIndex");
            Assert.AreEqual(7, result.boneIndex3, "Incorrect boneIndex");
            Assert.AreEqual(0.4f, result.weight0, "Incorrect boneWeight");
            Assert.AreEqual(0.3f, result.weight1, "Incorrect boneWeight");
            Assert.AreEqual(0.2f, result.weight2, "Incorrect boneWeight");
            Assert.AreEqual(0.1f, result.weight3, "Incorrect boneWeight");
        }

        [Test]
        public void FilterChannels_DisablesChannelsWithWeightBelowTolerance()
        {
            EditableBoneWeight editableBoneWeight = new EditableBoneWeight();

            editableBoneWeight.AddChannel(new BoneWeightData(0, 0.2f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(1, 0.2f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(2, 0.3f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(3, 0.3f), true);

            editableBoneWeight.FilterChannels(0.25f);

            Assert.False(editableBoneWeight.IsChannelEnabled(0), "Channel should be disabled");
            Assert.False(editableBoneWeight.IsChannelEnabled(1), "Channel should be disabled");
            Assert.True(editableBoneWeight.IsChannelEnabled(2), "Channel should be enabled");
            Assert.True(editableBoneWeight.IsChannelEnabled(3), "Channel should be enabled");
            Assert.AreEqual(0f, editableBoneWeight.GetBoneWeightData(0).weight, "Incorrect boneWeight");
            Assert.AreEqual(0f, editableBoneWeight.GetBoneWeightData(1).weight, "Incorrect boneWeight");
            Assert.AreEqual(0.3f, editableBoneWeight.GetBoneWeightData(2).weight, "Incorrect boneWeight");
            Assert.AreEqual(0.3f, editableBoneWeight.GetBoneWeightData(3).weight, "Incorrect boneWeight");
        }

        [Test]
        public void RemoveBone_DisablesChannelsWithSameBoneIndex_DecrementsIndicesHigherThanBoneIndex_NormalizeBoneWeight()
        {
            EditableBoneWeight editableBoneWeight = new EditableBoneWeight();

            editableBoneWeight.AddChannel(new BoneWeightData(0, 0.4f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(1, 0.3f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(2, 0.2f), true);
            editableBoneWeight.AddChannel(new BoneWeightData(3, 0.1f), true);

            editableBoneWeight.RemoveBone(1);

            Assert.True(editableBoneWeight.IsChannelEnabled(0), "Channel should be enabled");
            Assert.True(editableBoneWeight.IsChannelEnabled(1), "Channel should be enabled");
            Assert.True(editableBoneWeight.IsChannelEnabled(2), "Channel should be enabled");
            Assert.False(editableBoneWeight.IsChannelEnabled(3), "Channel should be disabled");
            Assert.AreEqual(0, editableBoneWeight.GetBoneWeightData(0).boneIndex, "Incorrect boneIndex");
            Assert.AreEqual(1, editableBoneWeight.GetBoneWeightData(1).boneIndex, "Incorrect boneIndex");
            Assert.AreEqual(2, editableBoneWeight.GetBoneWeightData(2).boneIndex, "Incorrect boneIndex");
            Assert.AreEqual(0, editableBoneWeight.GetBoneWeightData(3).boneIndex, "Incorrect boneIndex");
            Assert.AreEqual(0.571428537f, editableBoneWeight.GetBoneWeightData(0).weight, "Incorrect boneWeight");
            Assert.AreEqual(0.285714269f, editableBoneWeight.GetBoneWeightData(1).weight, "Incorrect boneWeight");
            Assert.AreEqual(0.142857134f, editableBoneWeight.GetBoneWeightData(2).weight, "Incorrect boneWeight");
            Assert.AreEqual(0f, editableBoneWeight.GetBoneWeightData(3).weight, "Incorrect boneWeight");
            Assert.AreEqual(0.99999994f, editableBoneWeight.GetBoneWeightData(0).weight +
                editableBoneWeight.GetBoneWeightData(1).weight +
                editableBoneWeight.GetBoneWeightData(2).weight +
                editableBoneWeight.GetBoneWeightData(3).weight, "BoneWeight is not normalized");
        }
    }
}
