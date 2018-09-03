using System;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.TestTools.TestRunner
{
    internal abstract class TestLauncherBase
    {
        public abstract void Run();

        protected bool ExecutePreBuildSetupMethods(ITest tests, ITestFilter testRunnerFilter, BuildTarget? buildTarget = null)
        {
            var attributeFinder = new PrebuildSetupAttributeFinder();
            var logString = "Executing setup for: {0}";
            var platformTestFilter = new UnityPlatformTestFilter(testRunnerFilter, buildTarget);
            return ExecuteMethods<IPrebuildSetup>(tests, platformTestFilter, attributeFinder, logString, targetClass => targetClass.Setup());
        }

        public static void ExecutePostBuildCleanupMethods(ITest tests, ITestFilter testRunnerFilter, BuildTarget? buildTarget = null)
        {
            var attributeFinder = new PostbuildCleanupAttributeFinder();
            var logString = "Executing cleanup for: {0}";
            var platformTestFilter = new UnityPlatformTestFilter(testRunnerFilter, buildTarget);
            ExecuteMethods<IPostBuildCleanup>(tests, platformTestFilter, attributeFinder, logString, targetClass => targetClass.Cleanup());
        }

        private static bool ExecuteMethods<T>(ITest tests, ITestFilter testRunnerFilter, AttributeFinderBase attributeFinder, string logString, Action<T> action)
        {
            var exceptionsThrown = false;

            foreach (var targetClassType in attributeFinder.Search(tests, testRunnerFilter))
            {
                try
                {
                    var targetClass = (T)Activator.CreateInstance(targetClassType);

                    Debug.LogFormat(logString, targetClassType.FullName);
                    action(targetClass);
                }
                catch (InvalidCastException) {}
                catch (Exception e)
                {
                    Debug.LogException(e);
                    exceptionsThrown = true;
                }
            }

            return exceptionsThrown;
        }
    }
}
