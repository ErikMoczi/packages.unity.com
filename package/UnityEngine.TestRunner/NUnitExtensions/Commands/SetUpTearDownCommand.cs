using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using NUnit.Framework.Internal.Execution;
using UnityEngine.TestRunner.NUnitExtensions.Runner;
using UnityEngine.TestTools.Logging;

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
            var skipTest = false;
            for (int i = _setUpTearDownItems.Count; i > 0;)
            {
                var logScope = new LogScope();

                try
                {
                    _setUpTearDownItems[--i].RunSetUp(context);
                }
                catch (Exception ex)
                {
                    skipTest = true;
                    Debug.LogException(ex);
                    context.CurrentResult.SetResult(ResultState.Failure, ex.Message);
                    break;
                }

                if (logScope.AnyFailingLogs())
                {
                    skipTest = true;
                    context.CurrentResult.SetResult(ResultState.Failure);
                }
            }

            if (!skipTest)
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
            }

            if (context.ExecutionStatus != TestExecutionStatus.AbortRequested)
            {
                for (int i = 0; i < _setUpTearDownItems.Count; i++)
                {
                    var logScope = new LogScope();

                    try
                    {
                        _setUpTearDownItems[i].RunTearDown(context);
                    }
                    catch (Exception ex)
                    {
                        context.CurrentResult.SetResult(ResultState.Failure, ex.Message);
                        break;
                    }

                    if (logScope.AnyFailingLogs())
                    {
                        context.CurrentResult.SetResult(ResultState.Failure);
                    }

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
