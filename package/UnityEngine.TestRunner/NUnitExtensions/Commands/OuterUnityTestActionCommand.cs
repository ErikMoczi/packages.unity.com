using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using UnityEngine.TestRunner.NUnitExtensions.Runner;

namespace UnityEngine.TestTools
{
    internal class OuterUnityTestActionCommand : DelegatingTestCommand, IEnumerableTestMethodCommand
    {
        public IOuterUnityTestAction[] OuterUnityTestActions { get; }

        public OuterUnityTestActionCommand(TestCommand innerCommand)
            : base(innerCommand)
        {
            if (Test.TypeInfo.Type != null)
            {
                OuterUnityTestActions = GetUnityTestActionsFromMethod(Test.Method.MethodInfo);
            }
        }

        private static IOuterUnityTestAction[] GetUnityTestActionsFromMethod(MethodInfo method)
        {
            var attributes = method.GetCustomAttributes(false);
            List<IOuterUnityTestAction> actions = new List<IOuterUnityTestAction>();
            foreach (var attribute in attributes)
            {
                if (attribute is IOuterUnityTestAction)
                    actions.Add(attribute as IOuterUnityTestAction);
            }
            return actions.ToArray();
        }

        public IEnumerable ExecuteEnumerable(ITestExecutionContext context)
        {
            foreach (var testAction in OuterUnityTestActions)
            {
                yield return testAction.BeforeTest(Test);
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

            foreach (var testAction in OuterUnityTestActions)
            {
                yield return testAction.AfterTest(Test);
            }
        }

        public override TestResult Execute(ITestExecutionContext context)
        {
            throw new NotImplementedException("Use ExecuteEnumerable");
        }
    }
}
