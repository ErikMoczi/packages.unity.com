using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace Unity.EditorCoroutines.Editor
{
    /// <summary>
    /// Instances of the EditorCoroutine class do not expose properties or functions, their sole purpose is to reference the ongoing coroutine process.
    /// </summary>
    public class EditorCoroutine
    {
        private struct WaitforSecondsProcessor
        {
            double targetTime;
            EditorWaitForSeconds current;

            public void Set(EditorWaitForSeconds yieldStatement)
            {
                if (yieldStatement == current)
                    return;

                current = yieldStatement;
                targetTime = EditorApplication.timeSinceStartup + yieldStatement.WaitTime;
            }

            public bool MoveNext(IEnumerator enumerator)
            {
                if (targetTime <= EditorApplication.timeSinceStartup)
                {
                    current = null;
                    targetTime = 0;
                    return enumerator.MoveNext();
                }
                return true;
            }
        }

        private struct WaitForCoroutineProcessor
        {
            EditorCoroutine m_Current;

            public void Set(EditorCoroutine routine)
            {
                if (m_Current == routine)
                    return;

                m_Current = routine;
            }

            public bool MoveNext(IEnumerator enumerator)
            {
                if (m_Current.m_IsDone)
                {
                    m_Current = null;
                    return enumerator.MoveNext();
                }

                return true;
            }
        }

        private struct WaitForAsyncOPProcessor
        {
            AsyncOperation m_Current;

            public void Set(AsyncOperation operation)
            {
                if (m_Current != operation)
                    m_Current = operation;
            }

            public bool MoveNext(IEnumerator enumerator)
            {
                if (m_Current.isDone)
                {
                    m_Current = null;
                    return enumerator.MoveNext();
                }
                return true;
            }
        }

        WeakReference m_Owner;
        IEnumerator m_Routine;

        WaitforSecondsProcessor m_WaitProcessor;
        WaitForCoroutineProcessor m_WaitForCoroutine;
        WaitForAsyncOPProcessor m_WaitForAsyncOPProcessor;

        bool m_IsDone;

        internal EditorCoroutine(IEnumerator routine)
        {
            m_Owner = null;
            m_Routine = routine;
            EditorApplication.update += MoveNext;
        }

        internal EditorCoroutine(IEnumerator routine, object owner)
        {
            m_Owner = new WeakReference(owner);
            m_Routine = routine;
            EditorApplication.update += MoveNext;
        }

        internal void MoveNext()
        {
            if (m_Owner != null && !m_Owner.IsAlive)
            {
                EditorApplication.update -= MoveNext;
                return;
            }

            bool done = ProcessIEnumeratorRecursive(m_Routine, null);
            m_IsDone = !done;

            if (m_IsDone)
                EditorApplication.update -= MoveNext;
        }

        private bool ProcessIEnumeratorRecursive(IEnumerator child, IEnumerator root)
        {
            bool isRoot = root == null;

            var nestedEnumerator = child.Current as IEnumerator;
            var result = false;
            if (nestedEnumerator == null)
            {
                result = ProcessIEnumerator(child);
            }
            else
            {
                result = ProcessIEnumeratorRecursive(nestedEnumerator, child);
            }

            if (!result && !isRoot)
                return root.MoveNext();

            return result;
        }

        private bool ProcessIEnumerator(IEnumerator enumerator)
        {
            var nestedCoroutine = enumerator.Current as EditorCoroutine;
            if (nestedCoroutine != null)
            {
                m_WaitForCoroutine.Set(nestedCoroutine);
                return m_WaitForCoroutine.MoveNext(enumerator);
            }

            var waitForSeconds = enumerator.Current as EditorWaitForSeconds;
            if (waitForSeconds != null)
            {
                m_WaitProcessor.Set(waitForSeconds);
                return m_WaitProcessor.MoveNext(enumerator);
            }

            var waitForAsyncOP = enumerator.Current as AsyncOperation;
            if (waitForAsyncOP != null)
            {
                m_WaitForAsyncOPProcessor.Set(waitForAsyncOP);
                return m_WaitForAsyncOPProcessor.MoveNext(enumerator);
            }
            else
            {
                if (!enumerator.MoveNext())
                    return false;

                return true;
            }
        }

        internal void Stop()
        {
            m_Owner = null;
            m_Routine = null;
            EditorApplication.update -= MoveNext;
        }
    }
}