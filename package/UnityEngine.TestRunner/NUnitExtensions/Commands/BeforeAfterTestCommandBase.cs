using System;
using System.Collections;
using System.Linq;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using UnityEngine.TestRunner.NUnitExtensions.Runner;
using UnityEngine.TestTools.Logging;
using UnityEngine.TestTools.TestRunner;

namespace UnityEngine.TestTools
{
    internal abstract class BeforeAfterTestCommandBase<T> : DelegatingTestCommand, IEnumerableTestMethodCommand
    {
        private bool m_SkipYieldAfterActions;
        protected BeforeAfterTestCommandBase(TestCommand innerCommand, bool skipYieldAfterActions = false)
            : base(innerCommand)
        {
            m_SkipYieldAfterActions = skipYieldAfterActions;
        }

        protected T[] BeforeActions = new T[0];

        protected T[] AfterActions = new T[0];

        protected abstract IEnumerator InvokeBefore(T action, Test test, UnityTestExecutionContext context);

        protected abstract IEnumerator InvokeAfter(T action, Test test, UnityTestExecutionContext context);

        protected abstract BeforeAfterTestCommandState GetState(UnityTestExecutionContext context);

        public IEnumerable ExecuteEnumerable(ITestExecutionContext context)
        {
            var unityContext = (UnityTestExecutionContext)context;
            var state = GetState(unityContext);

            if (state == null)
            {
                // We do not expect a state to exist in playmode
                state = ScriptableObject.CreateInstance<BeforeAfterTestCommandState>();
            }

            state.ApplyTestResult(context.CurrentResult);

            while (state.NextBeforeStepIndex < BeforeActions.Length)
            {
                var action = BeforeActions[state.NextBeforeStepIndex];
                var enumerator = InvokeBefore(action, Test, unityContext);
                ActivePcHelper.SetEnumeratorPC(enumerator, state.NextBeforeStepPc);

                using (var logScope = new LogScope())
                {
                    while (true)
                    {
                        try
                        {
                            if (!enumerator.MoveNext())
                            {
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            state.TestHasRun = true;
                            context.CurrentResult.RecordException(ex);
                            state.StoreTestResult(context.CurrentResult);
                            break;
                        }

                        state.NextBeforeStepPc = ActivePcHelper.GetEnumeratorPC(enumerator);
                        state.StoreTestResult(context.CurrentResult);
                        if (m_SkipYieldAfterActions)
                        {
                            break;
                        }
                        else
                        {
                            yield return enumerator.Current;
                        }
                    }

                    if (logScope.AnyFailingLogs())
                    {
                        state.TestHasRun = true;
                        context.CurrentResult.RecordException(new UnhandledLogMessageException(logScope.FailingLogs.First()));
                        state.StoreTestResult(context.CurrentResult);
                    }
                }

                state.NextBeforeStepIndex++;
                state.NextBeforeStepPc = 0;
            }

            if (!state.TestHasRun)
            {
                if (innerCommand is IEnumerableTestMethodCommand)
                {
                    var executeEnumerable = ((IEnumerableTestMethodCommand)innerCommand).ExecuteEnumerable(context);
                    foreach (var iterator in executeEnumerable)
                    {
                        state.StoreTestResult(context.CurrentResult);
                        yield return iterator;
                    }
                }
                else
                {
                    context.CurrentResult = innerCommand.Execute(context);
                    state.StoreTestResult(context.CurrentResult);
                }

                state.TestHasRun = true;
            }

            while (state.NextAfterStepIndex < AfterActions.Length)
            {
                state.TestAfterStarted = true;
                var action = AfterActions[state.NextAfterStepIndex];
                var enumerator = InvokeAfter(action, Test, unityContext);
                ActivePcHelper.SetEnumeratorPC(enumerator, state.NextAfterStepPc);

                using (var logScope = new LogScope())
                {
                    while (true)
                    {
                        try
                        {
                            if (!enumerator.MoveNext())
                            {
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            context.CurrentResult.RecordException(ex);
                            state.StoreTestResult(context.CurrentResult);
                            break;
                        }

                        state.NextAfterStepPc = ActivePcHelper.GetEnumeratorPC(enumerator);
                        state.StoreTestResult(context.CurrentResult);

                        if (m_SkipYieldAfterActions)
                        {
                            break;
                        }
                        else
                        {
                            yield return enumerator.Current;
                        }
                    }

                    if (logScope.AnyFailingLogs())
                    {
                        state.TestHasRun = true;
                        context.CurrentResult.RecordException(new UnhandledLogMessageException(logScope.FailingLogs.First()));
                        state.StoreTestResult(context.CurrentResult);
                    }
                }

                state.NextAfterStepIndex++;
                state.NextAfterStepPc = 0;
            }

            state.Reset();
        }

        public override TestResult Execute(ITestExecutionContext context)
        {
            throw new NotImplementedException("Use ExecuteEnumerable");
        }

        private static TestCommandPcHelper pcHelper;

        internal static TestCommandPcHelper ActivePcHelper
        {
            get
            {
                if (pcHelper == null)
                {
                    pcHelper = new TestCommandPcHelper();
                }

                return pcHelper;
            }
            set
            {
                pcHelper = value;
            }
        }
    }
}
