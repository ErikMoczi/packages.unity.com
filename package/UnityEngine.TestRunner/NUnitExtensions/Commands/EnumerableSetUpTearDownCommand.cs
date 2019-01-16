using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using UnityEngine.TestRunner.NUnitExtensions.Runner;
using UnityEngine.TestTools.Logging;

namespace UnityEngine.TestTools
{
    internal class EnumerableSetUpTearDownCommand : DelegatingTestCommand, IEnumerableTestMethodCommand
    {
        public MethodInfo[] SetUps { get; private set; }
        public MethodInfo[] TearDowns { get; private set; }

        public EnumerableSetUpTearDownCommand(TestCommand innerCommand)
            : base(innerCommand)
        {
            if (Test.TypeInfo.Type != null)
            {
                SetUps = GetMethodsWithAttributeFromFixture(Test.TypeInfo.Type, typeof(UnitySetUpAttribute));
                TearDowns = GetMethodsWithAttributeFromFixture(Test.TypeInfo.Type, typeof(UnityTearDownAttribute));
            }
        }

        private static MethodInfo[] GetMethodsWithAttributeFromFixture(Type fixtureType, Type setUpType)
        {
            MethodInfo[] methodsWithAttribute = Reflect.GetMethodsWithAttribute(fixtureType, setUpType, true);
            return methodsWithAttribute.Where(x => x.ReturnType == typeof(IEnumerator)).ToArray();
        }

        private static IEnumerator Run(ITestExecutionContext testExecutionContext, MethodInfo methodInfo)
        {
            return (IEnumerator)Reflect.InvokeMethod(methodInfo, testExecutionContext.TestObject);
        }

        public override TestResult Execute(ITestExecutionContext context)
        {
            throw new NotImplementedException("Use ExecuteEnumerable");
        }

        public IEnumerable ExecuteEnumerable(ITestExecutionContext context)
        {
            var unityContext = (UnityTestExecutionContext)context;
            var state = unityContext.SetUpTearDownState;

            if (state == null)
            {
                // We do not expect a state to exist in playmode
                state = ScriptableObject.CreateInstance<EnumerableSetUpTearDownCommandState>();
            }

            while (state.NextSetUpStepIndex < SetUps.Length)
            {
                var methodInfo = SetUps[state.NextSetUpStepIndex];
                var enumerator = Run(context, methodInfo);
                ActivePcHelper.SetEnumeratorPC(enumerator, state.NextSetUpStepPc);

                var logScope = new LogScope();

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
                        Debug.LogException(ex);
                        context.CurrentResult.SetResult(ResultState.Failure, ex.Message);
                        break;
                    }

                    state.NextSetUpStepPc = ActivePcHelper.GetEnumeratorPC(enumerator);
                    yield return enumerator.Current;
                }

                if (logScope.AnyFailingLogs())
                {
                    state.TestHasRun = true;
                    context.CurrentResult.SetResult(ResultState.Failure);
                }

                logScope.Dispose();

                state.NextSetUpStepIndex++;
                state.NextSetUpStepPc = 0;
            }

            if (!state.TestHasRun)
            {
                if (innerCommand is IEnumerableTestMethodCommand)
                {
                    var executeEnumerable = ((IEnumerableTestMethodCommand)innerCommand).ExecuteEnumerable(context);
                    foreach (var iterator in executeEnumerable)
                    {
                        yield return iterator;
                    }
                }
                else
                {
                    context.CurrentResult = innerCommand.Execute(context);
                }

                state.TestHasRun = true;
            }

            if (!state.TestTearDownStarted)
            {
                state.CurrentTestResultStatus = context.CurrentResult.ResultState.Status;
                state.CurrentTestResultLabel = context.CurrentResult.ResultState.Label;
                state.CurrentTestResultSite = context.CurrentResult.ResultState.Site;
                state.CurrentTestMessage = context.CurrentResult.Message;
                state.CurrentTestStrackTrace = context.CurrentResult.StackTrace;
            }

            while (state.NextTearDownStepIndex < TearDowns.Length)
            {
                state.TestTearDownStarted = true;
                var methodInfo = TearDowns[state.NextTearDownStepIndex];
                var enumerator = Run(context, methodInfo);
                ActivePcHelper.SetEnumeratorPC(enumerator, state.NextTearDownStepPc);

                var logScope = new LogScope();

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
                        Debug.LogException(ex);
                        context.CurrentResult.SetResult(ResultState.Failure, ex.Message);
                        break;
                    }

                    state.NextTearDownStepPc = ActivePcHelper.GetEnumeratorPC(enumerator);
                    yield return enumerator.Current;
                }

                if (logScope.AnyFailingLogs())
                {
                    state.TestHasRun = true;
                    context.CurrentResult.SetResult(ResultState.Failure);
                }

                logScope.Dispose();

                state.NextTearDownStepIndex++;
                state.NextTearDownStepPc = 0;
            }

            context.CurrentResult.SetResult(new ResultState(state.CurrentTestResultStatus, state.CurrentTestResultLabel, state.CurrentTestResultSite), state.CurrentTestMessage, state.CurrentTestStrackTrace);
            state.Reset();
        }

        private static PcHelper pcHelper;

        internal static PcHelper ActivePcHelper
        {
            get
            {
                if (pcHelper == null)
                {
                    pcHelper = new PcHelper();
                }

                return pcHelper;
            }
            set
            {
                pcHelper = value;
            }
        }

        internal class PcHelper
        {
            public virtual void SetEnumeratorPC(IEnumerator enumerator, int pc)
            {
                // Noop implementation used in playmode.
            }

            public virtual int GetEnumeratorPC(IEnumerator enumerator)
            {
                return 0;
            }
        }
    }
}
