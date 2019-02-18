using System;
using System.Collections;
using NUnit.Framework;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using NUnit.Framework.Internal.Execution;
using UnityEngine.TestRunner.NUnitExtensions.Runner;

namespace UnityEngine.TestTools
{
    internal class SetUpTearDownCommand : BeforeAfterTestCommandBase<SetUpTearDownItem>
    {
        public SetUpTearDownCommand(TestCommand innerCommand)
            : base(innerCommand, true)
        {
            var actions = CommandBuilder.BuildSetUpTearDownList(Test.TypeInfo.Type, typeof(SetUpAttribute), typeof(TearDownAttribute));
            actions.Reverse();
            BeforeActions = actions.ToArray();
            AfterActions = BeforeActions;
        }

        protected override IEnumerator InvokeBefore(SetUpTearDownItem action, Test test, UnityTestExecutionContext context)
        {
            action.RunSetUp(context);
            yield return null;
        }

        protected override IEnumerator InvokeAfter(SetUpTearDownItem action, Test test, UnityTestExecutionContext context)
        {
            action.RunTearDown(context);
            yield return null;
        }

        protected override BeforeAfterTestCommandState GetState(UnityTestExecutionContext context)
        {
            return null;
        }
    }
}
