using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using NUnit.Framework.Internal.Execution;
using UnityEngine.TestRunner.NUnitExtensions.Runner;

namespace UnityEngine.TestTools
{
    internal class TestActionCommand : DelegatingTestCommand, IEnumerableTestMethodCommand
    {
        public ITestAction[] TestActions { get; }

        public TestActionCommand(TestCommand innerCommand)
            : base(innerCommand)
        {
            if (Test.TypeInfo.Type != null)
            {
                if (Test.Method.MethodInfo.ReturnType == typeof(IEnumerator))
                    TestActions = GetTestActionsFromMethod(Test.Method.MethodInfo);
                else
                    TestActions = new ITestAction[0];
            }
        }

        private static ITestAction[] GetTestActionsFromMethod(MethodInfo method)
        {
            var attributes = method.GetCustomAttributes(false);
            List<ITestAction> actions = new List<ITestAction>();
            foreach (var attribute in attributes)
            {
                if (attribute is ITestAction)
                    actions.Add(attribute as ITestAction);
            }
            return actions.ToArray();
        }

        public IEnumerable ExecuteEnumerable(ITestExecutionContext context)
        {
            foreach (var testAction in TestActions)
            {
                testAction.BeforeTest(Test);
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

            foreach (var testAction in TestActions)
            {
                testAction.AfterTest(Test);
            }
        }

        public override TestResult Execute(ITestExecutionContext context)
        {
            throw new NotImplementedException("Use ExecuteEnumerable");
        }
    }
}
