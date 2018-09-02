using System;
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// This object allows for scheduling the invocation of Actions until a specified time has elapsed.  It is used to delay callbacks until the end of the current frame in the case that the callback is assigned when an operation is already complete.
    /// </summary>
    public class DelayedActionManager : MonoBehaviour
    {

        struct DelegateInfo
        {
            static int s_id = 0;
            int m_id;
            Delegate m_delegate;
            float m_invocationTime;
            object[] m_target;
            public DelegateInfo(Delegate d, float invocationTime, params object[] p)
            {
                m_delegate = d;
                m_id = s_id++;
                m_target = p;
                m_invocationTime = invocationTime;
                if (m_delegate.Method == null)
                    Debug.LogErrorFormat("NULL delegate.Method");
            }

            public float InvocationTime { get { return m_invocationTime; } }
            public override string ToString()
            {
                if (m_delegate == null)
                    return "Null m_delegate for " + m_id;
                var n = m_id + " (target=" + m_delegate.Target + ") " + m_delegate.Method.DeclaringType.Name + "." + (m_delegate.Method == null ? "null" : m_delegate.Method.Name) + "(";
                var sep = "";
                foreach (var o in m_target)
                {
                    n += sep + o;
                    sep = ", ";
                }
                return n + ") @" + m_invocationTime;
            }

            public void Invoke()
            {
                try
                {
                    m_delegate.DynamicInvoke(m_target);
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat("Exception thrown in DynamicInvoke: {0} {1}", e, this);
                }
            }
        }
        List<DelegateInfo>[] m_actions = new List<DelegateInfo>[] { new List<DelegateInfo>(), new List<DelegateInfo>() };
        LinkedList<DelegateInfo> m_delayedActions = new LinkedList<DelegateInfo>();
        Stack<LinkedListNode<DelegateInfo>> m_nodeCache = new Stack<LinkedListNode<DelegateInfo>>(10);
        int m_collectionIndex = 0;
        static DelayedActionManager m_instance;
        bool m_destroyOnCompletion = false;
        LinkedListNode<DelegateInfo> GetNode(ref DelegateInfo del)
        {
            if (m_nodeCache.Count > 0)
            {
                var node = m_nodeCache.Pop();
                node.Value = del;
                return node;
            }
            return new LinkedListNode<DelegateInfo>(del);
        }
        /// <summary>
        /// Destroy the current object
        /// </summary>
        public static void Clear()
        {
            if (m_instance != null)
                m_instance.DestroyWhenComplete();
            m_instance = null;
        }

        private void DestroyWhenComplete()
        {
            m_destroyOnCompletion = true;
        }

        /// <summary>
        /// Schedule an action to execute after a time delay.  If delay is less than or equal 0, the action will wait until the LateUpdate of this object to be invoked.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        /// <param name="delay">Time delay to wait until invocation.  If less than or equal to 0, the action will be invoked during the LateUpdate call of the current frame.</param>
        /// <param name="parameters">The parameters to be passed to the action when invoked.</param>
        public static void AddAction(Delegate action, float delay = 0, params object[] parameters)
        {
            if (m_instance == null)
            {
                m_instance = new GameObject("DelayedActionManager", typeof(DelayedActionManager)).GetComponent<DelayedActionManager>();
                m_instance.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }
            m_instance.AddActionInternal(action, delay, parameters);
        }

        void AddActionInternal(Delegate action, float delay, params object[] parameters)
        {
            var del = new DelegateInfo(action, Time.unscaledTime + delay, parameters);
            if (delay > 0)
            {
                if (m_delayedActions.Count == 0)
                {
                    m_delayedActions.AddFirst(GetNode(ref del));
                }
                else
                {

                    var n = m_delayedActions.Last;
                    while (n != null && n.Value.InvocationTime > del.InvocationTime)
                        n = n.Previous;
                    if (n == null)
                        m_delayedActions.AddFirst(GetNode(ref del));
                    else
                        m_delayedActions.AddBefore(n, GetNode(ref del));
                }
            }
            else
                m_actions[m_collectionIndex].Add(del);
        }

        void Awake()
        {
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Returns whether the DelayedActionManager is created and has pending actions to run.
        /// </summary>
        public static bool IsActive
        {
            get
            {
                if (m_instance == null)
                    return false;
                if (m_instance.m_delayedActions.Count > 0)
                    return true;
                for (int i = 0; i < m_instance.m_actions.Length; i++)
                    if (m_instance.m_actions[i].Count > 0)
                        return true;
                return false;
            }
        }

        void LateUpdate()
        {
            int iterationCount = 0;
            var t = Time.unscaledTime;
            while (m_delayedActions.Count > 0 && m_delayedActions.First.Value.InvocationTime <= t)
            {
                m_actions[m_collectionIndex].Add(m_delayedActions.First.Value);
                m_nodeCache.Push(m_delayedActions.First);
                m_delayedActions.RemoveFirst();
            }

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
                Debug.Assert(iterationCount < 100);
            } while (m_actions[m_collectionIndex].Count > 0);

            if (m_destroyOnCompletion && !IsActive)
                Destroy(gameObject);
        }
    }

}
