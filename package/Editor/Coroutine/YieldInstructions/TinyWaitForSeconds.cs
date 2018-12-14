


using UnityEditor;

namespace Unity.Tiny
{
    internal struct TinyWaitForSeconds : ITinyYieldInstruction
    {
        private readonly double m_WaitUntil;

        public TinyWaitForSeconds(double time)
        {
            m_WaitUntil = EditorApplication.timeSinceStartup + time;
        }

        public bool HasCompleted => EditorApplication.timeSinceStartup >= m_WaitUntil;
    }
}

