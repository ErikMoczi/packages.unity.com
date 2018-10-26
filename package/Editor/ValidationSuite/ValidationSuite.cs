using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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

        public static bool ValidatePackage(string packageId, ValidationType validationType)
        {
            if (string.IsNullOrWhiteSpace(packageId))
                throw new ArgumentNullException(packageId);

            var packageIdParts = packageId.Split('@');

            if (packageIdParts.Length != 2)
                throw new ArgumentException("Malformed package Id " + packageId);

            return ValidatePackage(packageIdParts[0], packageIdParts[1], validationType);
        }

        public static bool ValidatePackage(string packageName, string packageVersion, ValidationType validationType)
        {
            if (string.IsNullOrWhiteSpace(packageName))
                throw new ArgumentNullException(packageName);

            if (string.IsNullOrWhiteSpace(packageVersion))
                throw new ArgumentNullException(packageVersion);

            var packageId = Utilities.CreatePackageId(packageName, packageVersion);
            var packagePath = FindPackagePath(packageName);
            var report = new ValidationSuiteReport(packageId, packageName, packageVersion, packagePath);

            if (string.IsNullOrEmpty(packagePath))
            {
                report.OutputErrorReport(string.Format("Unable to find package \"{0}\" on disk.", packageName));
                return false;
            }

            try
            {
                // publish locally for embedded and local packages
                var context = VettingContext.CreatePackmanContext(packageId, validationType);
                var testSuite = new ValidationSuite(SingleTestCompletedDelegate, AllTestsCompletedDelegate, context, report);

                report.Initialize(testSuite.context);
                testSuite.RunSync();
                return testSuite.testSuiteState == TestState.Succeeded;
            }
            catch (Exception e)
            {
                report.OutputErrorReport(string.Format("\r\nTest Setup Error: \"{0}\"\r\n", e));
                return false;
            }
        }

        public static void ValidateEmbeddedPackages()
        {
            var packageIdList = new List<string>();
            var directories = Directory.GetDirectories("Packages/", "*", SearchOption.TopDirectoryOnly);
            foreach (var directory in directories)
            {
                Debug.Log("Starting package validation for " + directory);
                packageIdList.Add(VettingContext.GetManifest(directory).Id);
            }

            if (packageIdList.Any())
            {
                var success = ValidatePackages(packageIdList, ValidationType.LocalDevelopment);
                Debug.Log("Package validation done and batchmode is set. Shutting down Editor");
                EditorApplication.Exit(success ? 0 : 1);
            }
            else
            {
                EditorApplication.Exit(1);
            }
        }


        public static bool RunAssetStoreValidationSuite(string packageName, string packageVersion, string packagePath, string previousPackagePath = null)
        {
            if (string.IsNullOrWhiteSpace(packageName))
                throw new ArgumentNullException(packageName);

            if (string.IsNullOrWhiteSpace(packageVersion))
                throw new ArgumentNullException(packageVersion);

            if (string.IsNullOrWhiteSpace(packagePath))
                throw new ArgumentNullException(packageName);

            var report = new ValidationSuiteReport(packageName + "@" + packageVersion, packageName, packageVersion, packagePath);

            try
            {
                var context = VettingContext.CreateAssetStoreContext(packageName, packageVersion, packagePath, previousPackagePath);
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

        public static string GetValidationSuiteReport(string packageName, string packageVersion)
        {
            if (string.IsNullOrWhiteSpace(packageName))
                throw new ArgumentNullException(packageName);

            if (string.IsNullOrWhiteSpace(packageVersion))
                throw new ArgumentNullException(packageVersion);

            var packageId = Utilities.CreatePackageId(packageName, packageVersion);
            return GetValidationSuiteReport(packageId);
        }

        public static string GetValidationSuiteReport(string packageId)
        {
            if (string.IsNullOrWhiteSpace(packageId))
                throw new ArgumentNullException(packageId);

            return ValidationSuiteReport.ReportExists(packageId) ? File.ReadAllText(ValidationSuiteReport.TextReportPath(packageId)) : null;
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

        private static bool ValidatePackages(IEnumerable<string> packageIds, ValidationType validationType)
        {
            var success = true;
            foreach (var packageId in packageIds)
            {
                var result = ValidatePackage(packageId, validationType);
                if (result)
                {
                    Debug.Log("Validation succeeded for " + packageId);
                }
                else
                {
                    success = false;
                    Debug.LogError("Validation failed for " + packageId);
                }
            }

            return success;
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