using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using NUnit.Framework.Internal.Execution;
using UnityEngine.TestRunner.NUnitExtensions.Runner;
using UnityEngine.TestTools.TestRunner;

namespace UnityEngine.TestTools
{
    [AttributeUsage(AttributeTargets.Method)]
    public class UnitySetUpAttribute : NUnitAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class UnityTearDownAttribute : NUnitAttribute
    {
    }

    internal class EnumerableSetUpTearDownCommand : DelegatingTestCommand, IEnumerableTestMethodCommand
    {
        public MethodInfo[] Setups { get; private set; }
        public MethodInfo[] TearDowns { get; private set; }

        public EnumerableSetUpTearDownCommand(TestCommand innerCommand)
            : base(innerCommand)
        {
            if (Test.TypeInfo.Type != null)
            {
                Setups = GetSetupTypeFromFixture(Test.TypeInfo.Type, typeof(UnitySetUpAttribute));
                TearDowns = GetSetupTypeFromFixture(Test.TypeInfo.Type, typeof(UnityTearDownAttribute));
            }
        }

        private static MethodInfo[] GetSetupTypeFromFixture(Type fixtureType, Type setUpType)
        {
            MethodInfo[] withSetupAttribute = Reflect.GetMethodsWithAttribute(fixtureType, setUpType, true);
            return withSetupAttribute.Where(x => x.ReturnType == typeof(IEnumerator)).ToArray();
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
            foreach (var methodInfo in Setups)
            {
                var enumerator = Run(context, methodInfo);
                while (enumerator.MoveNext())
                {
                    yield return enumerator.Current;
                }
            }

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

            foreach (var methodInfo in TearDowns)
            {
                var enumerator = Run(context, methodInfo);
                while (enumerator.MoveNext())
                {
                    yield return enumerator.Current;
                }
            }
        }
    }

    internal class EnumerableTestMethodCommand : TestCommand, IEnumerableTestMethodCommand
    {
        private readonly TestMethod testMethod;
        private List<SetUpTearDownItem> _setUpTearDownItems;

        public EnumerableTestMethodCommand(TestMethod testMethod)
            : base(testMethod)
        {
            this.testMethod = testMethod;
            _setUpTearDownItems = CommandBuilder.BuildSetUpTearDownList(Test.TypeInfo.Type, typeof(SetUpAttribute), typeof(TearDownAttribute));
        }

        public IEnumerable ExecuteEnumerable(ITestExecutionContext context)
        {
            for (int i = _setUpTearDownItems.Count; i > 0;)
            {
                _setUpTearDownItems[--i].RunSetUp(context);
                yield return null;
            }

            var currentExecutingTestEnumerator = new TestEnumeratorWrapper(testMethod).GetEnumerator(context);
            if (currentExecutingTestEnumerator != null)
            {
                var testEnumeraterYieldInstruction = new TestEnumerator(context, currentExecutingTestEnumerator);

                yield return testEnumeraterYieldInstruction;

                var enumerator = testEnumeraterYieldInstruction.Execute();
                while (enumerator.MoveNext())
                {
                    yield return enumerator.Current;
                }
            }
            else
            {
                context.CurrentResult.SetResult(ResultState.Success);
            }

            if (context.ExecutionStatus != TestExecutionStatus.AbortRequested)
            {
                for (int i = 0; i < _setUpTearDownItems.Count; i++)
                {
                    _setUpTearDownItems[i].RunTearDown(context);
                    yield return null;
                }
            }
        }

        public override TestResult Execute(ITestExecutionContext context)
        {
            throw new NotImplementedException("Use ExecuteEnumerable");
        }
    }
}
