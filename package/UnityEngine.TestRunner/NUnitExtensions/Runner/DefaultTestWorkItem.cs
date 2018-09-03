using System;
using System.Collections;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using NUnit.Framework.Internal.Execution;
using UnityEngine.TestTools;

namespace UnityEngine.TestRunner.NUnitExtensions.Runner
{
    internal class EditModeTestCallbacks
    {
        public static Action RestoringTestContext { get; set; }
    }

    internal class DefaultTestWorkItem : UnityWorkItem
    {
        private TestCommand _command;

        public DefaultTestWorkItem(TestMethod test, ITestFilter filter)
            : base(test, null)
        {
            _command = test.RunState == RunState.Runnable || test.RunState == RunState.Explicit && filter.IsExplicitMatch(test)
                ? CommandBuilder.MakeTestCommand(test)
                : CommandBuilder.MakeSkipCommand(test);
        }

        protected override IEnumerable PerformWork()
        {
            if (m_DontRunRestoringResult && EditModeTestCallbacks.RestoringTestContext != null)
            {
                EditModeTestCallbacks.RestoringTestContext();
                Result = Context.CurrentResult;
                yield break;
            }

            try
            {
                if (_command is SkipCommand)
                {
                    Result = _command.Execute(Context);
                    yield break;
                }

                if (GetFirstInnerCommandOfType<UnityLogCheckDelegatingCommand>(_command) == null)
                {
                    _command = new UnityLogCheckDelegatingCommand(_command);
                }

                _command = new EnumerableSetUpTearDownCommand(_command);

                foreach (var testAction in ((IEnumerableTestMethodCommand)_command).ExecuteEnumerable(Context))
                {
                    yield return testAction;
                }

                Result = Context.CurrentResult;
            }
            finally
            {
                WorkItemComplete();
            }
        }

        private static T GetFirstInnerCommandOfType<T>(TestCommand command) where T : TestCommand
        {
            if (command is T)
            {
                return (T)command;
            }

            if (command is DelegatingTestCommand)
            {
                DelegatingTestCommand delegatingTestCommand = (DelegatingTestCommand)command;
                return GetFirstInnerCommandOfType<T>(delegatingTestCommand.GetInnerCommand());
            }
            return null;
        }
    }
}
