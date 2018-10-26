using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Semver;
using UnityEditor.PackageManager.ValidationSuite.ValidationTests;
using UnityEngine;

namespace UnityEditor.PackageManager.ValidationSuite
{
    // Delegate called after every test to provide immediate feedback on single test results.
    internal delegate void SingleTestCompletedDelegate(IValidationTestResult testResult);

    // Delegate called after the test run completed, whether it succeeded, failed or got canceled.
    internal delegate void AllTestsCompletedDelegate(ValidationSuite suite, TestState testRunState);

    [InitializeOnLoad]
    public class ValidationSuite
    {
        public static readonly string resultsPath = "ValidationSuiteResults";

        // List of validation tests
        internal IEnumerable<BaseValidation> validationTests;

        // Delegate called after every test to provide immediate feedback on single test results.
        private SingleTestCompletedDelegate singleTestCompletionDelegate;

        // Delegate called after the test run completed, whether it succeeded, failed or got canceled.
        private AllTestsCompletedDelegate allTestsCompletedDelegate;

        // Vetting context
        private readonly VettingContext context;

        // Path of the result output
        internal readonly string resultOutputPath;

        // Thread we will use to run tests.
        private Thread testRunnerThread = null;

        internal TestState testSuiteState;

        internal DateTime StartTime;

        internal DateTime EndTime;

        internal ValidationSuite(SingleTestCompletedDelegate singleTestCompletionDelegate,
                               AllTestsCompletedDelegate allTestsCompletedDelegate,
                               VettingContext context,
                               string resultOutputPath)
        {
            this.singleTestCompletionDelegate += singleTestCompletionDelegate;
            this.allTestsCompletedDelegate += allTestsCompletedDelegate;
            this.context = context;
            this.resultOutputPath = resultOutputPath;
            testSuiteState = TestState.NotRun;

            BuildTestSuite();
        }

        internal IEnumerable<IValidationTest> ValidationTests
        {
            get { return validationTests.Cast<IValidationTest>(); }
            set { validationTests = value.Cast<BaseValidation>(); }
        }

        internal IEnumerable<IValidationTestResult> ValidationTestResults
        {
            get { return validationTests.Cast<IValidationTestResult>(); }
        }

        public static bool RunValidationSuite(string packageId)
        {
            var parts = packageId.Split('@');
            var packageName = parts[0];
            var packageVersion = parts[1];
            var packagePath = FindPackagePath(packageName);
            if (string.IsNullOrEmpty(packagePath))
                return false;

            if (!Directory.Exists(resultsPath))
                Directory.CreateDirectory(resultsPath);

            // Clear output file content
            var path = Path.Combine(resultsPath, packageId + ".txt");
            File.WriteAllText(path, string.Format("Validation Suite Results for package \"{0}\"\r\n - Path: {1}\r\n - Version: {2}\r\n\r\n", packageName, packagePath, packageVersion));

            if (!packagePath.StartsWith(Directory.GetCurrentDirectory()) && !packagePath.EndsWith(packageVersion))
            {
                File.AppendAllText(path, string.Format("Package version mismatch: expecting \"{0}\" but was \"{1}\"", packageVersion, packagePath));
                return false;
            }

            try
            {
                var context = VettingContext.CreatePackmanContext(packagePath);
                var testSuite = new ValidationSuite(SingleTestCompletedDelegate, AllTestsCompletedDelegate, context, path);
                testSuite.RunSync();
                return testSuite.testSuiteState == TestState.Succeeded;
            }
            catch (Exception e)
            {
                File.AppendAllText(path, string.Format("\r\nTest Setup Error: \"{0}\"\r\n", e.Message));
                return false;
            }
        }

        public static bool RunAssetStoreValidationSuite(string packagePath, string previousPackagePath = null)
        {
            try
            {
                var context = VettingContext.CreateAssetStoreContext(packagePath, previousPackagePath);
                var testSuite = new ValidationSuite(SingleTestCompletedDelegate, AllTestsCompletedDelegate, context, Path.GetFileName(packagePath));
                testSuite.RunSync();
                return testSuite.testSuiteState == TestState.Succeeded;
            }
            catch (Exception e)
            {
                File.AppendAllText(packagePath, string.Format("\r\nTest Setup Error: \"{0}\"\r\n", e.Message));
                return false;
            }
        }

        internal void RunAsync()
        {
            // Start by calling "setup" on each test to allow them to prepare editor data they cant query async.
            foreach (var test in validationTests)
            {
                if (!test.ShouldRun)
                    continue;

                test.Context = context;
                test.Suite = this;
                test.Setup();
            }

            // Run the tests in another thread, so we can get results as tests complete, and we can cancel a test run easily
            testRunnerThread = new Thread(Run);
            testRunnerThread.Start();
        }

        internal void RunSync()
        {
            foreach (var test in validationTests)
            {
                test.Context = context;
                test.Setup();
            }
            
            Run();
        }

        internal void Cancel()
        {
            // Cancel validation tests running in the other thread
            testRunnerThread.Abort();
        }

        private void BuildTestSuite()
        {
            // Use reflection to discover all Validation Tests in the project with base type == BaseValidationTest.
            validationTests = (from t in Assembly.GetExecutingAssembly().GetTypes()
                                where t.BaseType == (typeof(BaseValidation)) && t.GetConstructor(Type.EmptyTypes) != null
                                select (BaseValidation)Activator.CreateInstance(t)).ToList();
        }

        private void Run()
        {
            testSuiteState = TestState.Succeeded;
            StartTime = DateTime.Now;
            testSuiteState = TestState.Running;

            // Run through tests
            foreach (var test in validationTests)
            {
                if (!test.ShouldRun)
                    continue;

                try
                {
                    test.RunTest();

                    if (test.TestState == TestState.Failed)
                    {
                        testSuiteState = TestState.Failed;
                    }

                    // Signal single test results to caller.
                    singleTestCompletionDelegate(test);
                }
                catch (Exception ex)
                {
                    // if the test didn't behave, return an error.
                    testSuiteState = TestState.Failed;

                    // Change the test outcome.
                    test.TestState = TestState.Failed;
                    test.TestOutput.Add(ex.ToString());
                    singleTestCompletionDelegate(test);
                }
            }

            EndTime = DateTime.Now;
            if (testSuiteState != TestState.Failed)
                testSuiteState = TestState.Succeeded;

            // when we're done, signal the main thread and all other interested
            allTestsCompletedDelegate(this, testSuiteState);
        }
        
        private static string FindPackagePath(string packageId)
        {
            var path = string.Format("Packages/{0}/package.json", packageId);
            var absolutePath = Path.GetFullPath(path);
            return !File.Exists(absolutePath) ? string.Empty : Directory.GetParent(absolutePath).FullName;
        }
        
        private static void SingleTestCompletedDelegate(IValidationTestResult testResult)
        {
            File.AppendAllText(testResult.ValidationTest.Suite.resultOutputPath, string.Format("\r\nTest: \"{0}\"\r\nResult: {1}\r\n", testResult.ValidationTest.TestName, testResult.TestState));
            if (testResult.TestOutput.Any())
                File.AppendAllText(testResult.ValidationTest.Suite.resultOutputPath, string.Join("\r\n", testResult.TestOutput.ToArray()) + "\r\n");
        }

        private static void AllTestsCompletedDelegate(ValidationSuite suite, TestState testRunState)
        {
            File.AppendAllText(suite.resultOutputPath, "\r\nAll Done!  Result = " + testRunState);

            var report = new ValidationSuiteReport(suite);
            var parent = Directory.GetParent(suite.resultOutputPath).FullName;
            var path = Path.Combine(parent, Path.GetFileNameWithoutExtension(suite.resultOutputPath) + ".json");
            if (File.Exists(path))
                File.Delete(path);
            
            report.OutputReport(path);
        }
    }
}