using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Experimental.U2D;

namespace UnityEditor.Experimental.U2D.Animation
{
    public struct UniqueSpriteBone
    {
        public float length { get; set; }
        public string name { get; set; }
        public int parentId { get; set; }
        public Vector3 position { get; set; }
        public Quaternion rotation { get; set; }
        public GUID id { get; set; }

        public UniqueSpriteBone(SpriteBone spriteBone)
        {
            this.length = spriteBone.length;
            this.name = spriteBone.name;
            this.parentId = spriteBone.parentId;
            this.position = spriteBone.position;
            this.rotation = spriteBone.rotation;
            this.id = new GUID();
        }
        
        public SpriteBone spriteBone
        {
            get
            {
                return new SpriteBone()
                {
                    length = this.length,
                    name = this.name,
                    parentId = this.parentId,
                    position = this.position,
                    rotation = this.rotation
                };
            }
        }
    }

    internal class BoneCacheManager
    {
        private ISpriteBoneDataProvider m_BoneDP;
        private ISpriteMeshDataProvider m_MeshDP;

        private Dictionary<GUID, List<UniqueSpriteBone>> m_SpriteBoneCache;
        private Dictionary<GUID, List<GUID>> m_BoneUniqueIdInOrder;

        public BoneCacheManager(ISpriteBoneDataProvider boneDP, ISpriteMeshDataProvider meshDP)
        {
            m_BoneDP = boneDP;
            m_MeshDP = meshDP;

            m_SpriteBoneCache = new Dictionary<GUID, List<UniqueSpriteBone>>();
            m_BoneUniqueIdInOrder = new Dictionary<GUID, List<GUID>>();
        }

        public void Apply()
        {
            foreach (var p in m_SpriteBoneCache)
            {
                FixAndApplyWeight(p.Key, p.Value);
                ApplyBone(p.Key, p.Value);
            }
        }

        public void CleanUp()
        {
            m_SpriteBoneCache.Clear();
            m_BoneUniqueIdInOrder.Clear();
        }

        public List<UniqueSpriteBone> GetSpriteBoneRawData(GUID spriteId)
        {
            List<UniqueSpriteBone> uniqueBones;
            if (!m_SpriteBoneCache.TryGetValue(spriteId, out uniqueBones))
            {
                var normalBones = LoadBoneFromDataProvider(spriteId);
                uniqueBones = NormalToUnique(normalBones);

                m_SpriteBoneCache.Add(spriteId, uniqueBones);
                m_BoneUniqueIdInOrder.Add(spriteId, GenerateBoneIdList(uniqueBones));
            }

            return uniqueBones;
        }

        public void SetSpriteBoneRawData(GUID spriteId, List<UniqueSpriteBone> uniqueSpriteBones)
        {
            m_SpriteBoneCache[spriteId] = uniqueSpriteBones;
        }
        
        private void ApplyBone(GUID spriteId, List<UniqueSpriteBone> uniqueBones)
        {
            m_BoneDP.SetBones(spriteId, UniqueToNormal(uniqueBones));
        }

        private void FixAndApplyWeight(GUID spriteId, List<UniqueSpriteBone> uniqueBones)
        {
            var originalWeight = m_MeshDP.GetVertices(spriteId);
            if (originalWeight != null && originalWeight.Length > 0)
            {
                var originalBoneIdList = m_BoneUniqueIdInOrder[spriteId];
                var latestBoneIdList = GenerateBoneIdList(uniqueBones);

                var indexMap = new int[originalBoneIdList.Count];
                for (var i = 0; i < originalBoneIdList.Count; ++i)
                    indexMap[i] = latestBoneIdList.IndexOf(originalBoneIdList[i]);

                if (FixWeight(indexMap, originalWeight))
                {
                    m_MeshDP.SetVertices(spriteId, originalWeight);

                    // SetEdges and SetIndices need to call with original data, else there is an error in ISpriteDataProvider and bail this operation
                    m_MeshDP.SetEdges(spriteId, m_MeshDP.GetEdges(spriteId));
                    m_MeshDP.SetIndices(spriteId, m_MeshDP.GetIndices(spriteId));
                }
            }
        }

        private bool FixWeight(int[] indices, Vertex2DMetaData[] weights)
        {
            var weightFixed = false;
            for (var i = 0; i < weights.Length; ++i)
            {
                var boneWeight = weights[i].boneWeight;
                var workingIndex = 0;
                var workingWeight = 0.0f;

                if (FixWeight(boneWeight.boneIndex0, boneWeight.weight0, GetNewIndex(boneWeight.boneIndex0, indices), out workingIndex, out workingWeight))
                {
                    boneWeight.boneIndex0 = workingIndex;
                    boneWeight.weight0 = workingWeight;
                }
                if (FixWeight(boneWeight.boneIndex1, boneWeight.weight1, GetNewIndex(boneWeight.boneIndex1, indices), out workingIndex, out workingWeight))
                {
                    boneWeight.boneIndex1 = workingIndex;
                    boneWeight.weight1 = workingWeight;
                }
                if (FixWeight(boneWeight.boneIndex2, boneWeight.weight2, GetNewIndex(boneWeight.boneIndex2, indices), out workingIndex, out workingWeight))
                {
                    boneWeight.boneIndex2 = workingIndex;
                    boneWeight.weight2 = workingWeight;
                }
                if (FixWeight(boneWeight.boneIndex3, boneWeight.weight3, GetNewIndex(boneWeight.boneIndex3, indices), out workingIndex, out workingWeight))
                {
                    boneWeight.boneIndex3 = workingIndex;
                    boneWeight.weight3 = workingWeight;
                }

                if (weights[i].boneWeight != boneWeight)
                {
                    weights[i].boneWeight = boneWeight;
                    weightFixed = true;
                }
            }
            return weightFixed;
        }

        private bool FixWeight(int originalIndex, float originalWeight, int newIndex, out int boneWeightIndex, out float boneWeightValue)
        {
            if (newIndex == -1)
            {
                boneWeightIndex = 0;
                boneWeightValue = 0.0f;
                return true;
            }
            else if (originalIndex != newIndex)
            {
                boneWeightIndex = newIndex;
                boneWeightValue = originalWeight;
                return true;
            }

            boneWeightIndex = originalIndex;
            boneWeightValue = originalWeight;
            return false;
        }
        
        private int GetNewIndex(int currentIndex, int[] indexMap)
        {
            int newIndex = -1;

            if(currentIndex >= 0 && currentIndex < indexMap.Length)
                newIndex = indexMap[currentIndex];

            return newIndex;
        }

        private List<SpriteBone> LoadBoneFromDataProvider(GUID spriteID)
        {
            return m_BoneDP.GetBones(spriteID);
        }

        private List<SpriteBone> UniqueToNormal(List<UniqueSpriteBone> uniqueSpriteBones)
        {
            List<SpriteBone> normalBones = new List<SpriteBone>();
            foreach (var u in uniqueSpriteBones)
                normalBones.Add(u.spriteBone);

            return normalBones;
        }

        private List<UniqueSpriteBone> NormalToUnique(List<SpriteBone> spriteBones)
        {
            List<UniqueSpriteBone> uniqueBones = new List<UniqueSpriteBone>();
            foreach (var n in spriteBones)
            {
                var u = new UniqueSpriteBone(n);
                u.id = GUID.Generate();
                uniqueBones.Add(u);
            }

            return uniqueBones;
        }

        private List<GUID> GenerateBoneIdList(List<UniqueSpriteBone> uniqueBones)
        {
            List<GUID> boneIds = new List<GUID>();
            foreach (var u in uniqueBones)
                boneIds.Add(u.id);

            return boneIds;
        }
    }
}