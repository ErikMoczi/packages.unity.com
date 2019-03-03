#pragma warning disable 0414

using System;
using System.Collections;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;
using NUnit.Framework.Internal.Commands;
using UnityEngine;
using UnityEngine.TestRunner.NUnitExtensions.Runner;
using UnityEngine.TestTools;

namespace FrameworkTests
{
    [AttributeUsage(AttributeTargets.Method)]
    public class UnityTestExpectedToFailAttribute : CombiningStrategyAttribute, IWrapSetUpTearDown, ISimpleTestBuilder, IImplyFixture
    {
        public UnityTestExpectedToFailAttribute()
            : base(new UnityCombinatorialStrategy(), new ParameterDataSourceProvider()) {}

        readonly NUnitTestCaseBuilder _builder = new NUnitTestCaseBuilder();

        TestMethod ISimpleTestBuilder.BuildFrom(IMethodInfo method, Test suite)
        {
            TestCaseParameters parms = new TestCaseParameters();
            parms.ExpectedResult = new object();
            parms.HasExpectedResult = true;

            var t = _builder.BuildTestMethod(method, suite, parms);

            if (t.parms != null)
                t.parms.HasExpectedResult = false;

            return t;
        }

        public TestCommand Wrap(TestCommand command)
        {
            var enumerableTestMethodCommand = new EnumerableTestMethodCommand((TestMethod)command.Test);
            var unityLogCheckDelegatingCommand = new UnityLogCheckDelegatingCommand(enumerableTestMethodCommand);

            return new TestExpectedToFailCommand(unityLogCheckDelegatingCommand);
        }
    }

    public class TestExpectedToFailCommand : DelegatingTestCommand, IEnumerableTestMethodCommand
    {
        readonly TestCommand m_InnerCommand;

        public TestExpectedToFailCommand(TestCommand innerCommand) : base(innerCommand)
        {
            m_InnerCommand = innerCommand;
        }

        public override TestResult Execute(ITestExecutionContext context)
        {
            try
            {
                m_InnerCommand.Execute(context);
                context.CurrentResult.SetResult(ResultState.Failure);

                if (context.CurrentResult.ResultState == ResultState.Success)
                {
                    context.CurrentResult.SetResult(ResultState.Failure);
                }
                else
                {
                    context.CurrentResult.SetResult(ResultState.Success);
                }
            }
            catch (Exception)
            {
                context.CurrentResult.SetResult(ResultState.Success);
            }

            return context.CurrentResult;
        }

        public IEnumerable ExecuteEnumerable(ITestExecutionContext context)
        {
            var enumerableTestMethodCommand = (IEnumerableTestMethodCommand)m_InnerCommand;
            foreach (var step in enumerableTestMethodCommand.ExecuteEnumerable(context))
            {
                yield return step;
            }

            if (context.CurrentResult.ResultState == ResultState.Success)
            {
                context.CurrentResult.SetResult(ResultState.Failure);
            }
            else
            {
                context.CurrentResult.SetResult(ResultState.Success);
            }
        }

        public IEnumerator CurrentExecutingTestEnumerator { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestExpectedToFailAttribute : CombiningStrategyAttribute, IWrapTestMethod, IImplyFixture
    {
        public TestExpectedToFailAttribute()
            : base(new CombinatorialStrategy(), new ParameterDataSourceProvider()) {}

        public TestCommand Wrap(TestCommand command)
        {
            if (command is TestMethodCommand)
            {
                return new TestExpectedToFailCommand(new UnityLogCheckDelegatingCommand(command));
            }
            return null;
        }
    }


    public class LogAssertTestsHelper
    {
        static string s_LogMessage = "Some log message of type ";
        static LogType[] s_FailingLogTypes = { LogType.Exception, LogType.Assert, LogType.Error };

        public static void AssertMessage(LogType logType)
        {
            var msg = s_LogMessage + logType;
            if (logType == LogType.Exception)
                msg = "Exception: " + msg;
            LogAssert.Expect(logType, msg);
        }

        public static void LogMessage(LogType logType)
        {
            var msg = s_LogMessage + logType;
            switch (logType)
            {
                case LogType.Exception:
                    Debug.LogException(new Exception(msg));
                    break;
                case LogType.Assert:
                    Debug.LogAssertion(msg);
                    break;
                case LogType.Error:
                    Debug.LogError(msg);
                    break;
                default:
                    throw new Exception("Incorrect log type");
            }
        }

        public void CheckUnityAPI()
        {
            var go = new GameObject();
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
