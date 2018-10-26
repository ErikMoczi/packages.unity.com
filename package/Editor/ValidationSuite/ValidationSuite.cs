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
        // List of validation tests
        private IEnumerable<BaseValidation> validationTests;

        // Delegate called after every test to provide immediate feedback on single test results.
        private SingleTestCompletedDelegate singleTestCompletionDelegate;

        // Delegate called after the test run completed, whether it succeeded, failed or got canceled.
        private AllTestsCompletedDelegate allTestsCompletedDelegate;

        // Vetting context
        private readonly VettingContext context;
        private readonly ValidationSuiteReport report;

        internal TestState testSuiteState;

        internal DateTime StartTime;

        internal DateTime EndTime;

        internal ValidationSuite(SingleTestCompletedDelegate singleTestCompletionDelegate,
                               AllTestsCompletedDelegate allTestsCompletedDelegate,
                               VettingContext context,
                               ValidationSuiteReport report)
        {
            this.singleTestCompletionDelegate += singleTestCompletionDelegate;
            this.allTestsCompletedDelegate += allTestsCompletedDelegate;
            this.context = context;
            this.report = report;
            testSuiteState = TestState.NotRun;

            BuildTestSuite();
        }

        internal IEnumerable<BaseValidation> ValidationTests
        {
            get { return validationTests.Where(test => test.SupportedValidations.Contains(context.ValidationType)); }
            set { validationTests = value; }
        }

        internal IEnumerable<IValidationTestResult> ValidationTestResults
        {
            get { return validationTests.Cast<IValidationTestResult>(); }
        }

        public static bool RunValidationSuite(string packageId, PackageSource source)
        {
            var parts = packageId.Split('@');
            var packageName = parts[0];
            var packageVersion = parts[1];
            var packagePath = FindPackagePath(packageName);
            if (string.IsNullOrEmpty(packagePath))
                return false;

            var report = new ValidationSuiteReport(packageId, packageName, packageVersion, packagePath);
            var validEmbeddedPath = source == PackageSource.Embedded && packagePath.StartsWith(Directory.GetCurrentDirectory());
            var validRegistryPath = source == PackageSource.Registry && packagePath.EndsWith(packageVersion);
            if (!(validEmbeddedPath || validRegistryPath || source == PackageSource.Local))
            {
                report.OutputErrorReport(string.Format("Package version mismatch: expecting \"{0}\" but was \"{1}\"", packageVersion, packagePath));
                return false;
            }

            try
            {
                // publish locally for embedded and local packages
                var context = VettingContext.CreatePackmanContext(packagePath, source == PackageSource.Embedded || source == PackageSource.Local);
                var testSuite = new ValidationSuite(SingleTestCompletedDelegate, AllTestsCompletedDelegate, context, report);
                testSuite.RunSync();
                return testSuite.testSuiteState == TestState.Succeeded;
            }
            catch (Exception e)
            {
                report.OutputErrorReport(string.Format("\r\nTest Setup Error: \"{0}\"\r\n", e));
                return false;
            }
        }

        public static bool RunAssetStoreValidationSuite(string packageName, string packageVersion, string packagePath, string previousPackagePath = null)
        {
            var report = new ValidationSuiteReport(packageName + "@" + packageVersion, packageName, packageVersion, packagePath);

            try
            {
                var context = VettingContext.CreateAssetStoreContext(packagePath, previousPackagePath);
                var testSuite = new ValidationSuite(SingleTestCompletedDelegate, AllTestsCompletedDelegate, context, report);
                testSuite.RunSync();
                return testSuite.testSuiteState == TestState.Succeeded;
            }
            catch (Exception e)
            {
                report.OutputErrorReport(string.Format("\r\nTest Setup Error: \"{0}\"\r\n", e));
                return false;
            }
        }

        internal void RunSync()
        {
            foreach (var test in validationTests)
            {
                test.Context = context;
                test.Suite = this;
                test.Setup();
            }
            
            Run();
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
            foreach (var test in ValidationTests)
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
        }

        private static void AllTestsCompletedDelegate(ValidationSuite suite, TestState testRunState)
        {            
            suite.report.OutputTextReport(suite);
            suite.report.OutputJsonReport(suite);
        }
    }
}