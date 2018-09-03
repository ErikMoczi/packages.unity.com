using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.Experimental.U2D.Animation
{
    public class SmoothingUtility
    {
        public static void SmoothWeights(BoneWeight[] boneWeightIn, IList<int> indices, int boneCount, BoneWeight[] boneWeightOut)
        {
            Debug.Assert(boneWeightIn != null);
            Debug.Assert(boneWeightOut != null);
            Debug.Assert(boneWeightIn != boneWeightOut);
            Debug.Assert(boneWeightIn.Length == boneWeightOut.Length);

            PrepareTempBuffers(boneWeightIn.Length, boneCount);

            EditableBoneWeight editableBoneWeight = new EditableBoneWeight();

            for (int i = 0; i < boneWeightIn.Length; ++i)
            {
                editableBoneWeight.SetFromBoneWeight(boneWeightIn[i]);
                for (int channelIndex = 0; channelIndex < editableBoneWeight.GetChannelCount(); ++channelIndex)
                {
                    if (editableBoneWeight.IsChannelEnabled(channelIndex))
                    {
                        BoneWeightData boneWeightData = editableBoneWeight.GetBoneWeightData(channelIndex);
                        m_DataInTemp[i, boneWeightData.boneIndex] = boneWeightData.weight;
                    }
                }
            }

            SmoothPerVertexData(indices, m_DataInTemp, m_DataOutTemp);

            for (int i = 0; i < boneWeightIn.Length; ++i)
            {
                editableBoneWeight.Clear();

                for (int boneIndex = 0; boneIndex < boneCount; ++boneIndex)
                {
                    float weight = m_DataOutTemp[i, boneIndex];
                    int boneIndex2 = weight > 0f ? boneIndex : 0;
                    editableBoneWeight.AddChannel(new BoneWeightData(boneIndex2, weight), weight > 0);
                }

                editableBoneWeight.ClampChannels(4);
                editableBoneWeight.NormalizeChannels();

                boneWeightOut[i] = editableBoneWeight.ToBoneWeight(false);
            }
        }

        public static void SmoothPerVertexData(IList<int> indices, float[,] dataIn, float[,] dataOut)
        {
            Debug.Assert(dataIn != null);
            Debug.Assert(dataOut != null);
            Debug.Assert(dataIn != dataOut);
            Debug.Assert(dataIn.Length == dataOut.Length);

            int rowLength = dataIn.GetLength(0);
            int colLength = dataIn.GetLength(1);

            PrepareDenominatorBuffer(rowLength);

            for (int i = 0; i < indices.Count / 3; ++i)
            {
                for (int j = 0; j < 3; ++j)
                {
                    int j1 = (j + 1) % 3;
                    int j2 = (j + 2) % 3;

                    for (int k = 0; k < colLength; ++k)
                        dataOut[indices[i * 3 + j], k] += dataIn[indices[i * 3 + j1], k] + dataIn[indices[i * 3 + j2], k];

                    m_DenominatorTemp[indices[i * 3 + j]] += 2;
                }
            }

            for (int i = 0; i < rowLength; ++i)
                for (int j = 0; j < colLength; ++j)
                    dataOut[i, j] /= m_DenominatorTemp[i];
        }

        private static void PrepareDenominatorBuffer(int rowLength)
        {
            if (m_DenominatorTemp == null || m_DenominatorTemp.Length != rowLength)
                m_DenominatorTemp = new float[rowLength];
            else
                Array.Clear(m_DenominatorTemp, 0, m_DenominatorTemp.Length);
        }

        private static void PrepareTempBuffers(int rowLength, int colLength)
        {
            if (m_DataInTemp == null || m_DataInTemp.GetLength(0) != rowLength || m_DataInTemp.GetLength(1) != colLength)
                m_DataInTemp = new float[rowLength, colLength];
            else
                Array.Clear(m_DataInTemp, 0, m_DataInTemp.Length);

            if (m_DataOutTemp == null || m_DataOutTemp.GetLength(0) != rowLength || m_DataOutTemp.GetLength(1) != colLength)
                m_DataOutTemp = new float[rowLength, colLength];
            else
                Array.Clear(m_DataOutTemp, 0, m_DataOutTemp.Length);
        }

        private static float[,] m_DataInTemp;
        private static float[,] m_DataOutTemp;
        private static float[] m_DenominatorTemp;
    }
}
