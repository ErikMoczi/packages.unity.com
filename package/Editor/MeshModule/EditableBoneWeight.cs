using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.Experimental.U2D.Animation
{
    [Serializable]
    internal struct BoneWeightData : IComparable<BoneWeightData>
    {
        [SerializeField]
        int m_BoneIndex;

        [SerializeField]
        float m_Weight;

        public int boneIndex
        {
            get { return m_BoneIndex; }
            set { m_BoneIndex = value; }
        }

        public float weight
        {
            get { return m_Weight; }
            set { m_Weight = value; }
        }

        public BoneWeightData(int boneIndex, float weight)
        {
            m_BoneIndex = boneIndex;
            m_Weight = weight;
        }

        public int CompareTo(BoneWeightData other)
        {
            return other.weight.CompareTo(weight);
        }
    }

    [Serializable]
    internal class BoneWeightChannel : IComparable<BoneWeightChannel>
    {
        [SerializeField]
        bool m_Enabled;

        [SerializeField]
        BoneWeightData m_BoneWeightData;

        public bool enabled
        {
            get { return m_Enabled; }
            set { m_Enabled = value; }
        }

        public BoneWeightData boneWeightData
        {
            get { return m_BoneWeightData; }
            set { m_BoneWeightData = value; }
        }

        public BoneWeightChannel() : this(new BoneWeightData(0, 0f), true)
        {
        }

        public BoneWeightChannel(int boneIndex, float weight, bool enabled) : this(new BoneWeightData(boneIndex, weight), enabled)
        {
        }

        public BoneWeightChannel(BoneWeightData data, bool enabled)
        {
            m_Enabled = enabled;
            boneWeightData = data;
        }

        public int CompareTo(BoneWeightChannel other)
        {
            int result = other.enabled.CompareTo(enabled);

            if (result == 0)
                result = boneWeightData.CompareTo(other.boneWeightData);

            return result;
        }
    }

    [Serializable]
    internal class EditableBoneWeight : IEnumerable
    {
        [SerializeField]
        List<BoneWeightChannel> m_Channels = new List<BoneWeightChannel>();

        public EditableBoneWeight() {}

        public IEnumerator GetEnumerator()
        {
            return m_Channels.GetEnumerator();
        }

        public void Clear()
        {
            m_Channels.Clear();
        }

        public void AddChannel(BoneWeightData boneWeightData, bool enabled)
        {
            m_Channels.Add(new BoneWeightChannel(boneWeightData, enabled));
        }

        public void RemoveChannel(int channelIndex)
        {
            Debug.Assert(channelIndex < GetChannelCount());

            m_Channels.RemoveAt(channelIndex);
        }

        public int GetChannelCount()
        {
            return m_Channels.Count;
        }

        public void SetBoneWeightData(int channelIndex, BoneWeightData boneWeightData)
        {
            if (channelIndex < 0 || channelIndex >= GetChannelCount())
                throw new IndexOutOfRangeException("channel index out of range");

            m_Channels[channelIndex].boneWeightData = boneWeightData;
        }

        public BoneWeightData GetBoneWeightData(int channelIndex)
        {
            if (channelIndex < 0 || channelIndex >= GetChannelCount())
                throw new IndexOutOfRangeException("channel index out of range");

            return m_Channels[channelIndex].boneWeightData;
        }

        public void EnableChannel(int channelIndex, bool enabled)
        {
            if (channelIndex < 0 || channelIndex >= GetChannelCount())
                throw new IndexOutOfRangeException("channel index out of range");

            m_Channels[channelIndex].enabled = enabled;
        }

        public bool IsChannelEnabled(int channelIndex)
        {
            if (channelIndex < 0 || channelIndex >= GetChannelCount())
                throw new IndexOutOfRangeException("channel index out of range");

            return m_Channels[channelIndex].enabled;
        }

        public void SortChannels()
        {
            m_Channels.Sort();
        }
    }
}
