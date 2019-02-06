
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using Unity.Properties;

namespace Unity.Tiny
{
    internal abstract class InspectorBackend : IInspectorBackend
    {
        #region Fields
        protected readonly TinyInspector m_Inspector;
        protected bool m_Locked;

        private List<IPropertyContainer> m_Targets = new List<IPropertyContainer>();

        
        private Dictionary<Type, int> m_TypeToCount = new Dictionary<Type, int>();
        #endregion

        #region Properties
        public InspectorMode Mode { get; set; } = InspectorMode.Normal;

        public List<IPropertyContainer> Targets
        {
            get
            {
                return m_Targets;
            }
            set
            {
                if (m_Locked)
                {
                    return;
                }
                m_Targets = value;
                m_Targets.RemoveAll(t => null == t);
                m_TypeToCount = Targets.GroupBy(t => t.GetType()).ToDictionary(g => g.Key, g => g.Count());
            }
        }

        public bool Locked
        {
            get { return m_Locked; }
            set { m_Locked = value; }
        }

        public bool ShowFamilies { get; set; }

        #endregion
        protected InspectorBackend(TinyInspector inspector)
        {
            m_Inspector = inspector;
            Targets = new List<IPropertyContainer>();
        }

        #region API
        public void OnGUI()
        {
            Targets.RemoveAll(container => null == container);
 

            if (IsInspectingDifferentTypes())
            {
                ShowDifferentTypes(m_TypeToCount);
                return;
            }

            ValidateTargets();
            if (m_Targets == null || m_Targets.Count == 0)
            {
                m_Locked = false;
                return;
            }

            Inspect();
        }
        #endregion

        #region Implementation
        protected virtual void ValidateTargets() { }
        public virtual void Build() { }
        protected virtual void Inspect() { }
        protected virtual void ShowDifferentTypes(Dictionary<Type, int> types) { }

        private bool IsInspectingDifferentTypes()
        {
            return m_TypeToCount.Count > 1;
        }

        protected void RestrictToType(Type type)
        {
            Targets.RemoveAll(t => t.GetType() != type);
            Build();
        }

        protected void FlipLocked()
        {
            m_Locked = !m_Locked;
        }
        #endregion
    }
}

