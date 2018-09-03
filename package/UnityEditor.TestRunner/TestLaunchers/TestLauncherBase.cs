using System;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.TestTools.TestRunner
{
    internal abstract class TestLauncherBase
    {
        public abstract void Run();

        protected static bool ExecutePreBuildSetupMethods(ITest tests, ITestFilter testRunnerFilter)
        {
            var attributeFinder = new PrebuildSetupAttributeFinder();
            var logString = "Executing setup for: {0}";
            return ExecuteMethods<IPrebuildSetup>(tests, testRunnerFilter, attributeFinder, logString, targetClass => targetClass.Setup());
        }

        public static void ExecutePostBuildCleanupMethods(ITest tests, ITestFilter testRunnerFilter)
        {
            var attributeFinder = new PostbuildCleanupAttributeFinder();
            var logString = "Executing cleanup for: {0}";
            ExecuteMethods<IPostBuildCleanup>(tests, testRunnerFilter, attributeFinder, logString, targetClass => targetClass.Cleanup());
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
