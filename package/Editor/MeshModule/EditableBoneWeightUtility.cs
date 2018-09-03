using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal static class EditableBoneWeightUtility
    {
        static List<BoneWeightData> s_BoneWeightDataList = new List<BoneWeightData>();

        public static EditableBoneWeight CreateFromBoneWeight(BoneWeight boneWeight)
        {
            EditableBoneWeight editableBoneWeight = new EditableBoneWeight();

            editableBoneWeight.SetFromBoneWeight(boneWeight);
            editableBoneWeight.UnifyChannelsWithSameBoneIndex();

            return editableBoneWeight;
        }

        public static void SetFromBoneWeight(this EditableBoneWeight editableBoneWeight, BoneWeight boneWeight)
        {
            editableBoneWeight.Clear();

            editableBoneWeight.AddChannel(new BoneWeightData(boneWeight.boneIndex0, boneWeight.weight0), boneWeight.weight0 > 0f);
            editableBoneWeight.AddChannel(new BoneWeightData(boneWeight.boneIndex1, boneWeight.weight1), boneWeight.weight1 > 0f);
            editableBoneWeight.AddChannel(new BoneWeightData(boneWeight.boneIndex2, boneWeight.weight2), boneWeight.weight2 > 0f);
            editableBoneWeight.AddChannel(new BoneWeightData(boneWeight.boneIndex3, boneWeight.weight3), boneWeight.weight3 > 0f);
        }

        public static BoneWeight ToBoneWeight(this EditableBoneWeight editableBoneWeight, bool sortByWeight)
        {
            BoneWeight boneWeight = new BoneWeight();

            int channelCount = editableBoneWeight.GetChannelCount();

            if (channelCount > 0)
            {
                s_BoneWeightDataList.Clear();
                s_BoneWeightDataList.Capacity = editableBoneWeight.GetChannelCount();

                for (int i = 0; i < channelCount; ++i)
                {
                    s_BoneWeightDataList.Add(editableBoneWeight.GetBoneWeightData(i));
                }

                if (sortByWeight)
                    s_BoneWeightDataList.Sort();

                if (s_BoneWeightDataList.Count >= 1)
                {
                    boneWeight.boneIndex0 = s_BoneWeightDataList[0].boneIndex;
                    boneWeight.weight0 = s_BoneWeightDataList[0].weight;
                }

                if (s_BoneWeightDataList.Count >= 2)
                {
                    boneWeight.boneIndex1 = s_BoneWeightDataList[1].boneIndex;
                    boneWeight.weight1 = s_BoneWeightDataList[1].weight;
                }

                if (s_BoneWeightDataList.Count >= 3)
                {
                    boneWeight.boneIndex2 = s_BoneWeightDataList[2].boneIndex;
                    boneWeight.weight2 = s_BoneWeightDataList[2].weight;
                }

                if (s_BoneWeightDataList.Count >= 4)
                {
                    boneWeight.boneIndex3 = s_BoneWeightDataList[3].boneIndex;
                    boneWeight.weight3 = s_BoneWeightDataList[3].weight;
                }
            }

            return boneWeight;
        }

        public static bool HasBoneIndex(this EditableBoneWeight editableBoneWeight, int boneIndex)
        {
            return GetChannelFromBoneIndex(editableBoneWeight, boneIndex) > -1;
        }

        public static int GetChannelFromBoneIndex(this EditableBoneWeight editableBoneWeight, int boneIndex)
        {
            for (int i = 0; i < editableBoneWeight.GetChannelCount(); ++i)
            {
                if (editableBoneWeight.IsChannelEnabled(i))
                {
                    BoneWeightData data = editableBoneWeight.GetBoneWeightData(i);

                    if (data.boneIndex == boneIndex)
                        return i;
                }
            }

            return -1;
        }

        public static void ClampChannels(this EditableBoneWeight editableBoneWeight, int numChannels, bool sortChannels = true)
        {
            if (sortChannels)
                editableBoneWeight.SortChannels();

            while (editableBoneWeight.GetChannelCount() > numChannels)
                editableBoneWeight.RemoveChannel(numChannels);
        }

        public static void RemoveBone(this EditableBoneWeight editableBoneWeight, int boneIndex)
        {
            int channelCount = editableBoneWeight.GetChannelCount();

            for (int i = 0; i < channelCount; ++i)
            {
                BoneWeightData data = editableBoneWeight.GetBoneWeightData(i);

                if (data.boneIndex > boneIndex)
                {
                    data.boneIndex -= 1;
                }
                else if (data.boneIndex == boneIndex)
                {
                    data.boneIndex = 0;
                    data.weight = 0f;
                    editableBoneWeight.EnableChannel(i, false);
                }

                editableBoneWeight.SetBoneWeightData(i, data);
            }

            editableBoneWeight.NormalizeChannels();
            editableBoneWeight.SortChannels();
        }

        public static void ValidateChannels(this EditableBoneWeight editableBoneWeight)
        {
            for (int channelIndex = 0; channelIndex < editableBoneWeight.GetChannelCount(); ++channelIndex)
            {
                BoneWeightData data = editableBoneWeight.GetBoneWeightData(channelIndex);

                if (!editableBoneWeight.IsChannelEnabled(channelIndex))
                    data.weight = 0f;

                data.weight = Mathf.Clamp01(data.weight);
                editableBoneWeight.SetBoneWeightData(channelIndex, data);
            }
        }

        public static float GetWeightSum(this EditableBoneWeight editableBoneWeight)
        {
            float sum = 0f;

            for (int i = 0; i < editableBoneWeight.GetChannelCount(); ++i)
            {
                if (editableBoneWeight.IsChannelEnabled(i))
                {
                    BoneWeightData data = editableBoneWeight.GetBoneWeightData(i);

                    sum += data.weight;
                }
            }

            return sum;
        }

        public static void NormalizeChannels(this EditableBoneWeight editableBoneWeight)
        {
            ValidateChannels(editableBoneWeight);

            float sum = editableBoneWeight.GetWeightSum();

            if (sum == 0f || sum == 1f)
                return;

            for (int i = 0; i < editableBoneWeight.GetChannelCount(); ++i)
            {
                if (editableBoneWeight.IsChannelEnabled(i))
                {
                    BoneWeightData data = editableBoneWeight.GetBoneWeightData(i);

                    data.weight = data.weight / sum;

                    editableBoneWeight.SetBoneWeightData(i, data);
                }
            }
        }

        public static void CompensateOtherChannels(this EditableBoneWeight editableBoneWeight, int masterChannelIndex)
        {
            ValidateChannels(editableBoneWeight);

            int validChannelCount = 0;
            float sum = 0f;

            for (int i = 0; i < editableBoneWeight.GetChannelCount(); ++i)
            {
                if (i != masterChannelIndex && editableBoneWeight.IsChannelEnabled(i))
                {
                    BoneWeightData data = editableBoneWeight.GetBoneWeightData(i);

                    sum += data.weight;
                    ++validChannelCount;
                }
            }

            if (validChannelCount == 0)
                return;

            BoneWeightData channelData = editableBoneWeight.GetBoneWeightData(masterChannelIndex);

            float targetSum = 1f - channelData.weight;

            for (int i = 0; i < editableBoneWeight.GetChannelCount(); ++i)
            {
                if (i != masterChannelIndex && editableBoneWeight.IsChannelEnabled(i))
                {
                    BoneWeightData data = editableBoneWeight.GetBoneWeightData(i);

                    if (sum == 0f)
                        data.weight = targetSum / validChannelCount;
                    else
                        data.weight = data.weight * targetSum / sum;

                    editableBoneWeight.SetBoneWeightData(i, data);
                }
            }
        }

        public static void UnifyChannelsWithSameBoneIndex(this EditableBoneWeight editableBoneWeight)
        {
            int channelCount = editableBoneWeight.GetChannelCount();

            for (int i = 0; i < channelCount; ++i)
            {
                BoneWeightData data = editableBoneWeight.GetBoneWeightData(i);

                if (!editableBoneWeight.IsChannelEnabled(i))
                    continue;

                bool weightChanged = false;

                for (int j = i + 1; j < channelCount; ++j)
                {
                    BoneWeightData otherData = editableBoneWeight.GetBoneWeightData(j);

                    if (otherData.boneIndex == data.boneIndex)
                    {
                        weightChanged = true;
                        data.weight += otherData.weight;
                        editableBoneWeight.EnableChannel(j, false);
                    }
                }

                if (weightChanged)
                {
                    editableBoneWeight.SetBoneWeightData(i, data);
                    editableBoneWeight.CompensateOtherChannels(i);
                }
            }
        }

        public static void FilterChannels(this EditableBoneWeight editableBoneWeight, float weightTolerance)
        {
            int channelCount = editableBoneWeight.GetChannelCount();

            for (int i = 0; i < channelCount; ++i)
            {
                BoneWeightData data = editableBoneWeight.GetBoneWeightData(i);

                if (data.weight <= weightTolerance)
                {
                    data.boneIndex = 0;
                    data.weight = 0f;

                    editableBoneWeight.SetBoneWeightData(i, data);
                    editableBoneWeight.EnableChannel(i, false);
                }
            }
        }

        public static BoneWeight Lerp(BoneWeight first, BoneWeight second, float t)
        {
            EditableBoneWeight firstEditable = CreateFromBoneWeight(first);
            EditableBoneWeight secondEditable = CreateFromBoneWeight(second);

            return Lerp(firstEditable, secondEditable, t).ToBoneWeight(true);
        }

        private static EditableBoneWeight Lerp(EditableBoneWeight first, EditableBoneWeight second, float t)
        {
            EditableBoneWeight result = new EditableBoneWeight();

            foreach (BoneWeightChannel channel in first)
            {
                if (!channel.enabled)
                    continue;

                BoneWeightData data = channel.boneWeightData;
                data.weight *= (1f - t);

                if (data.weight > 0f)
                    result.AddChannel(data, true);
            }

            foreach (BoneWeightChannel channel in second)
            {
                if (!channel.enabled)
                    continue;

                BoneWeightData data = channel.boneWeightData;
                data.weight *= t;

                if (data.weight > 0f)
                    result.AddChannel(data, true);
            }

            result.UnifyChannelsWithSameBoneIndex();

            if (result.GetWeightSum() > 1f)
                result.NormalizeChannels();

            result.FilterChannels(0f);
            result.ClampChannels(4, true);

            return result;
        }
    }
}
