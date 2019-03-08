using System;
using System.Collections.Generic;
using UnityEditor;

namespace Unity.Tiny.Bridge
{
    internal static class EditorApplication
    {
        private struct ActionWrapper
        {
            public Action Action { get; }
            public int Order { get; }

            public ActionWrapper(Action action, int order)
            {
                Action = action;
                Order = order;
            }
        }
        
        private static readonly List<ActionWrapper> s_GlobalUpdates = new List<ActionWrapper>();
        private static readonly List<ActionWrapper> s_ContextualUpdates = new List<ActionWrapper>();
        private static readonly List<ActionWrapper> s_Buffer = new List<ActionWrapper>();
        private static bool s_Dirty = false;

        /// <summary>
        /// Indicates that Editor Application is currently Quitting.
        /// </summary>
        public static bool IsQuitting { get; private set; }
        
        /// <summary>
        /// Registers a global update method. These cannot be unregistered.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <param name="order">The order in which should be processed.</param>
        public static void RegisterGlobalUpdate(Action action, int order = 0)
        {
            CreateWrapper(s_GlobalUpdates, action, order);
        }
        
        /// <summary>
        /// Registers a contextual update method. These will be cleared automatically when closing a Tiny context.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <param name="order">The order in which should be processed.</param>
        public static void RegisterContextualUpdate(Action action, int order = 0)
        {
            CreateWrapper(s_ContextualUpdates, action, order);
        }
        
        /// <summary>
        /// Unregister a contextual update method.
        /// </summary>
        /// <param name="action">The action to unregister.</param>
        public static void UnregisterContextualUpdate(Action action)
        {
            s_ContextualUpdates.RemoveAll(wrapper => wrapper.Action == action);
        }

        /// <summary>
        /// Unregisters all contextual update methods. This is called when closing a Tiny context.
        /// </summary>
        public static void ClearContextualUpdates() 
        {
            s_ContextualUpdates.Clear();
        }

        [InitializeOnLoadMethod] 
        private static void Init()
        {
            UnityEditor.EditorApplication.update += Update;
            UnityEditor.EditorApplication.quitting += Quitting;
        }

        private static void Update()
        {
            ProcessUpdates(s_GlobalUpdates);
            ProcessUpdates(s_ContextualUpdates);
            s_Dirty = false;
        }
        
        private static void Quitting()
        {
            IsQuitting = true;
            UnityEditor.EditorApplication.quitting -= Quitting;
        }

        private static void CreateWrapper(List<ActionWrapper> list, Action action, int order)
        {
            list.Add(new ActionWrapper(action, order));
            s_Dirty = true;
        }
        
        private static void ProcessUpdates(List<ActionWrapper> list)
        {
            if (s_Dirty)
            {
                list.Sort(CompareWrapper);
            }
            
            // We buffer the list, because an update method might modify it. 
            s_Buffer.Clear();
            s_Buffer.AddRange(list);
            foreach (var update in s_Buffer)
            {
                update.Action.Invoke();
            }
        }

        private static int CompareWrapper(ActionWrapper lhs, ActionWrapper rhs)
        {
            return lhs.Order.CompareTo(rhs.Order);
        }
    }
}