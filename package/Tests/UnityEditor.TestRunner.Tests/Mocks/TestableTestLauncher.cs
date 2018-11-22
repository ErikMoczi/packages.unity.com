using NUnit.Framework.Interfaces;
using UnityEngine.TestTools;

namespace FrameworkTests
{
    internal class TestableTestLauncher : UnityEditor.TestTools.TestRunner.TestLauncherBase
    {
        public override void Run()
        {
        }

        internal void ExposedExecutePreBuildSetupMethods(ITest tests, ITestFilter testRunnerFilter)
        {
            ExecutePreBuildSetupMethods(tests, testRunnerFilter);
        }

        internal void ExposedExecutePostBuildCleanupMethods(ITest tests, ITestFilter testRunnerFilter)
        {
            ExecutePostBuildCleanupMethods(tests, testRunnerFilter);
        }
    }
}
