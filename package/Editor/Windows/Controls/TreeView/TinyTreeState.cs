

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Tiny
{
    [Serializable]
    internal class TinyTreeState : ISerializationCallbackReceiver
    {
        public TreeViewState TreeViewState;
        public MultiColumnHeaderState MultiColumnHeaderState;
        public string SearchString = string.Empty;
        
        [SerializeField] private List<string> m_InstanceIdKeys = new List<string>();
        [SerializeField] private List<int> m_InstanceIdValues = new List<int>();
        private readonly Dictionary<TinyId, int> m_InstanceIds = new Dictionary<TinyId, int>();
        private int m_NextInstanceId = 1;
        
        public void OnBeforeSerialize()
        {
            m_InstanceIdKeys.Clear();    
            m_InstanceIdValues.Clear();

            foreach (var kvp in m_InstanceIds)
            {
                m_InstanceIdKeys.Add(kvp.Key.ToString());
                m_InstanceIdValues.Add(kvp.Value);
            }
            
            m_InstanceIds.Clear();
        }

        public void OnAfterDeserialize()
        {
            m_InstanceIds.Clear();
            
            Assert.IsTrue(m_InstanceIdKeys.Count == m_InstanceIdValues.Count);

            for (var i = 0; i < m_InstanceIdKeys.Count; i++)
            {
                m_InstanceIds.Add(new TinyId(m_InstanceIdKeys[i]), m_InstanceIdValues[i]);
            }
            
            m_InstanceIdKeys.Clear();
            m_InstanceIdValues.Clear();

            ComputeNextInstanceId();
        }
        
        public int GetInstanceId(TinyId id)
        {
            int instanceId;
            
            if (m_InstanceIds.TryGetValue(id, out instanceId))
            {
                return instanceId;
            }
            
            instanceId = m_NextInstanceId++;
            m_InstanceIds.Add(id, instanceId);

            return instanceId;
        }

        public void ClearUnusedInstanceIds(IRegistry registry)
        {
            ComputeNextInstanceId();
        }

        private void ComputeNextInstanceId()
        {
            if (m_InstanceIds.Count > 0)
            {
                m_NextInstanceId = m_InstanceIds.Values.Max() + 1;
            }
            else
            {
                m_NextInstanceId = 1;
            }
        }
        
        /// <summary>
        /// Sets up the base state and overrides any serialized values
        /// </summary>
        public void Init(MultiColumnHeaderState multiColumnHeaderState)
        {
            if (null == TreeViewState)
            {
                TreeViewState = new TreeViewState();
            }

            if (null != MultiColumnHeaderState)
            {
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(MultiColumnHeaderState, multiColumnHeaderState))
                {
                    MultiColumnHeaderState.OverwriteSerializedFields(MultiColumnHeaderState, multiColumnHeaderState);
                }
            }

            MultiColumnHeaderState = multiColumnHeaderState;
        }
    }
}

