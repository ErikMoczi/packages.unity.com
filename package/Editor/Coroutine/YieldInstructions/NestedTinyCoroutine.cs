

using System;

namespace Unity.Tiny
{
    internal struct NestedCoroutine : ITinyYieldInstruction, ITinyThrowingYieldInstruction
    {
        private readonly TinyCoroutine m_Coroutine;

        public NestedCoroutine(TinyCoroutine coroutine)
        {
            m_Coroutine = coroutine;
        }

        public bool HasCompleted => m_Coroutine?.HasCompleted ?? true;
        public void Cancel() => m_Coroutine?.Cancel();
        public Exception ThrownDuringExecution => m_Coroutine.ThrownDuringExecution;
    }
}


