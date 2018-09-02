using System;
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    internal class DelayedActionManager : MonoBehaviour
    {
        struct DelegateInfo
        {
            Delegate m_delegate;
            object m_target;
            public DelegateInfo(Delegate d, object p)
            {
                m_delegate = d;
                m_target = p;
            }
            public void Invoke()
            {
                try
                {
                    if (m_target == null)
                        m_delegate.DynamicInvoke();
                    else
                        m_delegate.DynamicInvoke(m_target);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
        List<DelegateInfo>[] m_actions = new List<DelegateInfo>[] { new List<DelegateInfo>(), new List<DelegateInfo>() };
        int m_collectionIndex = 0;
        static DelayedActionManager m_instance;

        public static void AddAction(Delegate action, object parameters)
        {
            if (m_instance == null)
                m_instance = new GameObject("DelayedActionManager", typeof(DelayedActionManager)).GetComponent<DelayedActionManager>();

            m_instance.AddActionInternal(action, parameters);
        }

        void AddActionInternal(Delegate action, object parameters)
        {
            m_actions[m_collectionIndex].Add(new DelegateInfo(action, parameters));
        }

        void Awake()
        {
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            int iterationCount = 0;
            do
            {
                int invokeIndex = m_collectionIndex;
                m_collectionIndex = (m_collectionIndex + 1) % 2;
                var list = m_actions[invokeIndex];
                if (list.Count > 0)
                {
                    for (int i = 0; i < list.Count; i++)
                        list[i].Invoke();
                    list.Clear();
                }
                iterationCount++;
            } while (m_actions[m_collectionIndex].Count > 0);
        }
    }

}
