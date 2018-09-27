using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using NUnit.Framework.Internal.Execution;
using UnityEngine.TestRunner.NUnitExtensions.Runner;

namespace UnityEngine.TestTools
{
    internal class SetUpTearDownCommand : DelegatingTestCommand, IEnumerableTestMethodCommand
    {
        private List<SetUpTearDownItem> _setUpTearDownItems;

        public SetUpTearDownCommand(TestCommand innerCommand)
            : base(innerCommand)
        {
            _setUpTearDownItems = CommandBuilder.BuildSetUpTearDownList(Test.TypeInfo.Type, typeof(SetUpAttribute), typeof(TearDownAttribute));
        }

        public IEnumerable ExecuteEnumerable(ITestExecutionContext context)
        {
            for (int i = _setUpTearDownItems.Count; i > 0;)
            {
                _setUpTearDownItems[--i].RunSetUp(context);
                yield return null;
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
