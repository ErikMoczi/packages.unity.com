using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;

namespace UnityEngine.ResourceManagement.Util
{
    /// <summary>
    /// This object allows for scheduling the invocation of Actions until a specified time has elapsed.  It is used to delay callbacks until the end of the current frame in the case that the callback is assigned when an operation is already complete.
    /// </summary>
    public class DelayedActionManager : MonoBehaviour
    {

        struct DelegateInfo
        {
            static int s_Id;
            int m_Id;
            Delegate m_Delegate;
            float m_InvocationTime;
            object[] m_Target;
            public DelegateInfo(Delegate d, float invocationTime, params object[] p)
            {
                m_Delegate = d;
                m_Id = s_Id++;
                m_Target = p;
                m_InvocationTime = invocationTime;
            }

            public float InvocationTime { get { return m_InvocationTime; } }
            public override string ToString()
            {
                if (m_Delegate == null || m_Delegate.Method.DeclaringType == null)
                    return "Null m_delegate for " + m_Id;
                var n = m_Id + " (target=" + m_Delegate.Target + ") " + m_Delegate.Method.DeclaringType.Name + "." +  m_Delegate.Method.Name + "(";
                var sep = "";
                foreach (var o in m_Target)
                {
                    n += sep + o;
                    sep = ", ";
                }
                return n + ") @" + m_InvocationTime;
            }

            public void Invoke()
            {
                try
                {
                    m_Delegate.DynamicInvoke(m_Target);
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat("Exception thrown in DynamicInvoke: {0} {1}", e, this);
                }
            }
        }
        List<DelegateInfo>[] m_Actions = { new List<DelegateInfo>(), new List<DelegateInfo>() };
        LinkedList<DelegateInfo> m_DelayedActions = new LinkedList<DelegateInfo>();
        Stack<LinkedListNode<DelegateInfo>> m_NodeCache = new Stack<LinkedListNode<DelegateInfo>>(10);
        int m_CollectionIndex;
        static DelayedActionManager s_Instance;
        bool m_DestroyOnCompletion;
        LinkedListNode<DelegateInfo> GetNode(ref DelegateInfo del)
        {
            if (m_NodeCache.Count > 0)
            {
                var node = m_NodeCache.Pop();
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
            if (s_Instance != null)
                s_Instance.DestroyWhenComplete();
            s_Instance = null;
        }

        void DestroyWhenComplete()
        {
            m_DestroyOnCompletion = true;
        }

        /// <summary>
        /// Schedule an action to execute after a time delay.  If delay is less than or equal 0, the action will wait until the LateUpdate of this object to be invoked.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        /// <param name="delay">Time delay to wait until invocation.  If less than or equal to 0, the action will be invoked during the LateUpdate call of the current frame.</param>
        /// <param name="parameters">The parameters to be passed to the action when invoked.</param>
        public static void AddAction(Delegate action, float delay = 0, params object[] parameters)
        {
            if (s_Instance == null)
            { 
                s_Instance = new GameObject("DelayedActionManager", typeof(DelayedActionManager)).GetComponent<DelayedActionManager>();
                s_Instance.gameObject.hideFlags = HideFlags.HideAndDontSave;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
//                    Debug.Log("DelayedActionManager called outside of play mode, registering with EditorApplication.update.");
                    EditorApplication.update += s_Instance.LateUpdate;
                }
#endif
            }
            s_Instance.AddActionInternal(action, delay, parameters);
        }

        void AddActionInternal(Delegate action, float delay, params object[] parameters)
        {
            var del = new DelegateInfo(action, Time.unscaledTime + delay, parameters);
            if (delay > 0)
            {
                if (m_DelayedActions.Count == 0)
                {
                    m_DelayedActions.AddFirst(GetNode(ref del));
                }
                else
                {

                    var n = m_DelayedActions.Last;
                    while (n != null && n.Value.InvocationTime > del.InvocationTime)
                        n = n.Previous;
                    if (n == null)
                        m_DelayedActions.AddFirst(GetNode(ref del));
                    else
                        m_DelayedActions.AddBefore(n, GetNode(ref del));
                }
            }
            else
                m_Actions[m_CollectionIndex].Add(del);
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
                if (s_Instance == null)
                    return false;
                if (s_Instance.m_DelayedActions.Count > 0)
                    return true;
                for (int i = 0; i < s_Instance.m_Actions.Length; i++)
                    if (s_Instance.m_Actions[i].Count > 0)
                        return true;
                return false;
            }
        }

        /// <summary>
        /// Force the update loop to run until it is not active
        /// </summary>
        /// <param name="timeout">Optional timeout value in seconds.</param>
        /// <returns>True if the DelayedActionManager is not active, False if the timeout is hit.</returns>
        public static bool Wait(float timeout = 0, float timeAdvanceAmount = 0)
        {
            if (!IsActive)
                return true;

            var timer = new Stopwatch();
            timer.Start();
            var t = Time.unscaledTime;
            do
            {
                s_Instance.InternalLateUpdate(t);
                if(timeAdvanceAmount >= 0)
                    t += timeAdvanceAmount;
                else
                    t = Time.unscaledTime;

            } while (IsActive && (timeout <= 0 || timer.Elapsed.TotalSeconds < timeout));
            return !IsActive;
        }

        void LateUpdate()
        {
            InternalLateUpdate(Time.unscaledTime);
        }

        void InternalLateUpdate(float t)
        {
            int iterationCount = 0;
            while (m_DelayedActions.Count > 0 && m_DelayedActions.First.Value.InvocationTime <= t)
            {
                m_Actions[m_CollectionIndex].Add(m_DelayedActions.First.Value);
                m_NodeCache.Push(m_DelayedActions.First);
                m_DelayedActions.RemoveFirst();
            }

            do
            {
                int invokeIndex = m_CollectionIndex;
                m_CollectionIndex = (m_CollectionIndex + 1) % 2;
                var list = m_Actions[invokeIndex];
                if (list.Count > 0)
                {
                    for (int i = 0; i < list.Count; i++)
                        list[i].Invoke();
                    list.Clear();
                }
                iterationCount++;
                Debug.Assert(iterationCount < 100);
            } while (m_Actions[m_CollectionIndex].Count > 0);

            if (m_DestroyOnCompletion && !IsActive)
                Destroy(gameObject);
        }
    }

}
