using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.InteractiveTutorials
{
    [Serializable]
    class MaskingSettings
    {
        public bool enabled { get { return m_Enabled; } set { m_Enabled = value; } }
        [SerializeField]
        private bool m_Enabled;

        public IEnumerable<UnmaskedView> unmaskedViews { get { return m_UnmaskedViews; } }
        [SerializeField]
        private List<UnmaskedView> m_UnmaskedViews = new List<UnmaskedView>();

        public void SetUnmaskedViews(IEnumerable<UnmaskedView> unmaskedViews)
        {
            m_UnmaskedViews.Clear();
            if (unmaskedViews != null)
                m_UnmaskedViews.AddRange(unmaskedViews);
        }
    }
}
