using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.U2D.Animation
{
    [Serializable]
    internal class SpriteMeshCache : ScriptableObject, IEnumerable<SpriteMeshData>
    {
        public SerializableSelection selection
        {
            get { return m_Selection; }
        }

        public SerializableSelection boneSelection
        {
            get { return m_BoneSelection; }
        }

        public WeightTool selectedWeightTool
        {
            get { return m_SelectedWeightTool; }
            set { m_SelectedWeightTool = value; }
        }

        public Mode mode
        {
            get { return m_Mode; }
            set { m_Mode = value; }
        }

        public void AddSpriteMeshData(SpriteMeshData spriteMeshData)
        {
            m_SpriteMeshData.Add(spriteMeshData);
        }

        public SpriteMeshData GetSpriteMeshData(GUID spriteID)
        {
            for (int i = 0; i < m_SpriteMeshData.Count; i++)
            {
                SpriteMeshData spriteMeshData = m_SpriteMeshData[i];
                if (spriteMeshData.spriteID == spriteID)
                    return spriteMeshData;
            }

            return null;
        }

        IEnumerator<SpriteMeshData> IEnumerable<SpriteMeshData>.GetEnumerator()
        {
            return m_SpriteMeshData.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)m_SpriteMeshData.GetEnumerator();
        }

        [SerializeField]
        List<SpriteMeshData> m_SpriteMeshData = new List<SpriteMeshData>();

        [SerializeField]
        SerializableSelection m_Selection = new SerializableSelection();

        [SerializeField]
        SerializableSelection m_BoneSelection = new SerializableSelection();

        [SerializeField]
        WeightTool m_SelectedWeightTool;

        [SerializeField]
        Mode m_Mode;
    }
}
