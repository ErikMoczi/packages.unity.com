using System;
using System.Collections;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace UnityEngine.TestTools
{
    internal class TestEnumerator
    {
        private readonly ITestExecutionContext m_Context;
        private static IEnumerator m_TestEnumerator;

        public static IEnumerator Enumerator { get { return m_TestEnumerator; } }

        public TestEnumerator(ITestExecutionContext context, IEnumerator testEnumerator)
        {
            m_Context = context;
            m_TestEnumerator = testEnumerator;
        }

        public IEnumerator Execute()
        {
            while (true)
            {
                object current = null;
                try
                {
                    if (!m_TestEnumerator.MoveNext())
                    {
                        //If we set the result state in the runner
                        if (m_Context.CurrentResult.ResultState != ResultState.Error &&
                            m_Context.CurrentResult.ResultState != ResultState.Failure)
                        {
                            m_Context.CurrentResult.SetResult(ResultState.Success);
                        }
                        yield break;
                    }

                    if (m_Context.CurrentResult.ResultState == ResultState.Error ||
                        m_Context.CurrentResult.ResultState == ResultState.Failure)
                    {
                        yield break;
                    }

                    current = m_TestEnumerator.Current;
                }
                catch (Exception exception)
                {
                    m_Context.CurrentResult.RecordException(exception);
                }
                yield return current;
            }
        }
    }
}
