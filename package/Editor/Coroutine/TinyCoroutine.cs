


using System;
using System.Collections;
using UnityEngine;

namespace Unity.Tiny
{
    internal class TinyCoroutine
    {
        #region Fields
        private ITinyYieldInstruction m_YieldInstruction;
        #endregion

        #region Properties
        public bool HasCompleted { get; private set; }
        public IEnumerator Routine { get; }
        public object Target { get; }
        public TinyCoroutineLifetime OnCloseProject { get; }
        public Exception ThrownDuringExecution { get; private set; }
        #endregion

        #region API
        public TinyCoroutine(IEnumerator routine, object target = null, TinyCoroutineLifetime onCloseProject = TinyCoroutineLifetime.Cancel)
        {
            Routine = routine;
            Target = target;
            OnCloseProject = onCloseProject;
            m_YieldInstruction = new FallbackYieldInstruction();
            HasCompleted = null == routine;
        }

        public bool MoveNext()
        {
            if (HasCompleted)
            {
                return false;
            }

            if (!m_YieldInstruction.HasCompleted)
            {
                return true;
            }

            try
            {
                CheckForInnerException();

                if (!Routine.MoveNext())
                {
                    HasCompleted = true;
                    return false;
                }
            }
            catch (Exception ex)
            {
                var innerException = ex as TinyCoroutineInnerException;
                if (null == innerException)
                {
                    ThrownDuringExecution = ex;
                    Debug.LogException(ex);
                }
                else
                {
                    ThrownDuringExecution = innerException.Inner;
                }
                
                HasCompleted = true;
                return false;
            }

            m_YieldInstruction = GetYieldInstructionForObject(Routine.Current);
            return true;
        }

        public void Cancel()
        {
            HasCompleted = true;
            if (m_YieldInstruction is NestedCoroutine)
            {
                var nested = (NestedCoroutine)m_YieldInstruction;
                nested.Cancel();
            }
        }
        #endregion

        #region Implementation
        private static ITinyYieldInstruction GetYieldInstructionForObject(object current)
        {
            if (null == current)
            {
                return new FallbackYieldInstruction();
            }

            var instruction = current as ITinyYieldInstruction;
            if (null != instruction)
            {
                return instruction;
            }

            var coroutine = current as TinyCoroutine;
            if (null != coroutine)
            {
                return new NestedCoroutine(coroutine);
            }

            // Here, we could do some special handling for UnityEngine.YieldInstruction and
            // UnityEngine.CustomYieldInstruction, but we can add that whenever we need it.

            return new FallbackYieldInstruction();
        }

        private void CheckForInnerException()
        {
            var canThrow = m_YieldInstruction as ITinyThrowingYieldInstruction;
            if (null == canThrow)
            {
                return;
            }

            if (null == canThrow.ThrownDuringExecution)
            {
                return;
            }

            throw new TinyCoroutineInnerException(canThrow.ThrownDuringExecution);
        }
        #endregion
    }
}

