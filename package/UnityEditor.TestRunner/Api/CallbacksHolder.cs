using System.Collections.Generic;

namespace UnityEditor.TestTools.TestRunner.Api
{
    internal class CallbacksHolder : ScriptableSingleton<CallbacksHolder>
    {
        private List<ICallbacks> m_Callbacks = new List<ICallbacks>();
        public void Add(ICallbacks callback)
        {
            m_Callbacks.Add(callback);
        }

        public ICallbacks[] GetAll()
        {
            return m_Callbacks.ToArray();
        }

        public void Clear()
        {
            m_Callbacks.Clear();
        }
    }
}
