using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.U2D.Interface;
using UnityEngine.Experimental.U2D;
using UnityEditor.U2D.Interface;
using UnityEditor.Experimental.U2D;
using System;
using System.Collections.Generic;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal enum WeightEditorMode
    {
        AddAndSubtract,
        GrowAndShrink,
        Smooth
    }

    internal class WeightEditor
    {
        public SpriteMeshData spriteMeshData
        {
            get { return m_SpriteMeshData; }
            set
            {
                if(m_SpriteMeshData != value)
                {
                    m_SpriteMeshData = value;
                    m_CachedBoneNames = null;
                    
                    if(m_SpriteMeshData != null)
                        m_CachedBoneNames = MeshModuleUtility.GetBoneNameList(m_SpriteMeshData);
                }
            }
        }

        public GUIContent[] boneNames { get { return m_CachedBoneNames; } }
        public IUndoObject undoObject { get; set; }
        public WeightEditorMode mode { get; set; }
        public int boneIndex { get; set; }
        public ISelection selection { get; set; }
        public WeightEditorMode currentMode { get; private set; }
        public bool useRelativeValues { get; private set; }
        public bool emptySelectionEditsAll { get; set; }
        public bool autoNormalize { get; set; }

        private SpriteMeshData m_SpriteMeshData;
        private GUIContent[] m_CachedBoneNames;
        private const int maxSmoothIterations = 8;
        private float[] m_SmoothValues;
        private readonly List<BoneWeight[]> m_SmoothedBoneWeights = new List<BoneWeight[]>();
        private readonly List<BoneWeight> m_StoredBoneWeights = new List<BoneWeight>();
        
        public WeightEditor()
        {
            autoNormalize = true;
        }

        public void OnEditStart(bool relative)
        {
            RegisterUndo();
            currentMode = mode;
            useRelativeValues = relative;

            if (!useRelativeValues)
                StoreBoneWeights();

            if (mode == WeightEditorMode.Smooth)
                PrepareSmoothingBuffers();
        }

        public void OnEditEnd()
        {
            if (currentMode == WeightEditorMode.AddAndSubtract)
            {
                for (int i = 0; i < spriteMeshData.vertices.Count; ++i)
                    spriteMeshData.vertices[i].editableBoneWeight.ClampChannels(4);
            }

            if (autoNormalize)
                spriteMeshData.NormalizeWeights(null);
        }

        public void DoEdit(float value)
        {
            if (!useRelativeValues)
                RestoreBoneWeights();

            if (currentMode == WeightEditorMode.AddAndSubtract)
                SetWeight(value);
            else if (currentMode == WeightEditorMode.GrowAndShrink)
                SetWeight(value, false);
            else if (currentMode == WeightEditorMode.Smooth)
                SmoothWeights(value);
        }

        private void RegisterUndo()
        {
            Debug.Assert(undoObject != null);

            undoObject.RegisterCompleteObjectUndo("Edit Weights");
        }

        private void SetWeight(float value, bool createNewChannel = true)
        {
            if (boneIndex == -1)
                return;

            Debug.Assert(selection != null);

            for (int i = 0; i < spriteMeshData.vertices.Count; ++i)
            {
                if (selection.Count == 0 && emptySelectionEditsAll ||
                    selection.Count > 0 && selection.IsSelected(i))
                {
                    EditableBoneWeight editableBoneWeight = spriteMeshData.vertices[i].editableBoneWeight;

                    int channel = editableBoneWeight.GetChannelFromBoneIndex(boneIndex);

                    if (channel == -1)
                    {
                        if (createNewChannel)
                        {
                            editableBoneWeight.AddChannel(new BoneWeightData(boneIndex, 0f), true);
                            channel = editableBoneWeight.GetChannelFromBoneIndex(boneIndex);
                        }
                        else
                        {
                            continue;
                        }
                    }

                    BoneWeightData data = editableBoneWeight.GetBoneWeightData(channel);
                    data.weight += value;

                    editableBoneWeight.SetBoneWeightData(channel, data);

                    if (editableBoneWeight.GetWeightSum() > 1f)
                        editableBoneWeight.CompensateOtherChannels(channel);

                    editableBoneWeight.FilterChannels(0f);
                }
            }
        }

        private void SmoothWeights(float value)
        {
            Debug.Assert(selection != null);

            for (int i = 0; i < spriteMeshData.vertices.Count; ++i)
            {
                if (selection.Count == 0 && emptySelectionEditsAll ||
                    selection.Count > 0 && selection.IsSelected(i))
                {
                    m_SmoothValues[i] += value;
                    m_SmoothValues[i] = Mathf.Clamp(m_SmoothValues[i], 0f, float.MaxValue);

                    float lerpValue = GetLerpValue(m_SmoothValues[i]);
                    int lerpIndex = GetLerpIndex(m_SmoothValues[i]);
                    BoneWeight[] smoothedBoneWeightsFloor = GetSmoothedBoneWeights(lerpIndex - 1);
                    BoneWeight[] smoothedBoneWeightsCeil = GetSmoothedBoneWeights(lerpIndex);

                    BoneWeight boneWeight = EditableBoneWeightUtility.Lerp(smoothedBoneWeightsFloor[i], smoothedBoneWeightsCeil[i], lerpValue);
                    spriteMeshData.vertices[i].editableBoneWeight.SetFromBoneWeight(boneWeight);
                    spriteMeshData.vertices[i].editableBoneWeight.UnifyChannelsWithSameBoneIndex();
                }
            }
        }

        protected void PrepareSmoothingBuffers()
        {
            if (m_SmoothValues == null || m_SmoothValues.Length != spriteMeshData.vertices.Count)
                m_SmoothValues = new float[spriteMeshData.vertices.Count];

            Array.Clear(m_SmoothValues, 0, m_SmoothValues.Length);

            m_SmoothedBoneWeights.Clear();

            BoneWeight[] boneWeights = new BoneWeight[spriteMeshData.vertices.Count];

            for (int i = 0; i < spriteMeshData.vertices.Count; i++)
            {
                EditableBoneWeight editableBoneWeight = spriteMeshData.vertices[i].editableBoneWeight;
                boneWeights[i] = editableBoneWeight.ToBoneWeight(false);
            }

            m_SmoothedBoneWeights.Add(boneWeights);
        }

        private BoneWeight[] GetSmoothedBoneWeights(int lerpIndex)
        {
            Debug.Assert(lerpIndex >= 0);

            while (lerpIndex >= m_SmoothedBoneWeights.Count && lerpIndex <= maxSmoothIterations)
            {
                BoneWeight[] boneWeights = new BoneWeight[spriteMeshData.vertices.Count];
                SmoothingUtility.SmoothWeights(m_SmoothedBoneWeights[m_SmoothedBoneWeights.Count - 1], spriteMeshData.indices, spriteMeshData.bones.Count, boneWeights);
                m_SmoothedBoneWeights.Add(boneWeights);
            }

            return m_SmoothedBoneWeights[Mathf.Min(lerpIndex, maxSmoothIterations)];
        }

        private float GetLerpValue(float smoothValue)
        {
            Debug.Assert(smoothValue >= 0f);
            return smoothValue - Mathf.Floor(smoothValue);
        }

        private int GetLerpIndex(float smoothValue)
        {
            Debug.Assert(smoothValue >= 0f);
            return Mathf.RoundToInt(Mathf.Floor(smoothValue) + 1);
        }

        private void StoreBoneWeights()
        {
            Debug.Assert(selection != null);

            m_StoredBoneWeights.Clear();

            for (int i = 0; i < spriteMeshData.vertices.Count; i++)
            {
                EditableBoneWeight editableBoneWeight = spriteMeshData.vertices[i].editableBoneWeight;
                m_StoredBoneWeights.Add(editableBoneWeight.ToBoneWeight(false));
            }
        }

        private void RestoreBoneWeights()
        {
            Debug.Assert(selection != null);

            for (int i = 0; i < spriteMeshData.vertices.Count; i++)
            {
                EditableBoneWeight editableBoneWeight = spriteMeshData.vertices[i].editableBoneWeight;
                editableBoneWeight.SetFromBoneWeight(m_StoredBoneWeights[i]);
            }

            if (m_SmoothValues != null)
                Array.Clear(m_SmoothValues, 0, m_SmoothValues.Length);
        }
    }
}
