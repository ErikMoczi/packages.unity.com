using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEditor.PackageManager.ValidationSuite.ValidationTests;

namespace UnityEditor.PackageManager.ValidationSuite
{
    // Delegate called after every test to provide immediate feedback on single test results.
    internal delegate void SingleTestCompletedDelegate(IValidationTestResult testResult);

    // Delegate called after the test run completed, whether it succeeded, failed or got canceled.
    internal delegate void AllTestsCompletedDelegate(TestState testRunState);

    internal class ValidationSuite
    {
        // List of validation tests
        private IEnumerable<BaseValidation> validationTests;

        // Delegate called after every test to provide immediate feedback on single test results.
        private SingleTestCompletedDelegate singleTestCompletionDelegate;

        // Delegate called after the test run completed, whether it succeeded, failed or got canceled.
        private AllTestsCompletedDelegate allTestsCompletednDelegate;

        // Path of the package within the project
        private VettingContext context;

        // Thread we will use to run tests.
        private Thread testRunnerThread = null;

        public ValidationSuite(SingleTestCompletedDelegate singleTestCompletionDelegate,
                               AllTestsCompletedDelegate allTestsCompletednDelegate,
                               VettingContext context)
        {
            this.singleTestCompletionDelegate += singleTestCompletionDelegate;
            this.allTestsCompletednDelegate += allTestsCompletednDelegate;
            this.context = context;

            BuildTestSuite();
        }

        public IEnumerable<IValidationTest> ValidationTests
        {
            get { return validationTests.Cast<IValidationTest>(); }
            set { validationTests = value.Cast<BaseValidation>(); }
        }

        public IEnumerable<IValidationTestResult> ValidationTestResults
        {
            get { return validationTests.Cast<IValidationTestResult>(); }
        }

        public void RunAsync()
        {
            // Start by calling "setup" on each test to allow them to prepare editor data they cant query async.
            foreach (var test in validationTests)
            {
                test.Context = context;
                test.Setup();
            }

            // Run the tests in another thread, so we can get results as tests complete, and we can cancel a test run easily
            testRunnerThread = new Thread(TestRunnerThread);
            testRunnerThread.Start();
        }

        public void Cancel()
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

        private void TestRunnerThread()
        {
            TestState testRunState = TestState.Succeeded;

            // Run through tests
            foreach (var test in validationTests)
            {
                try
                {
                    test.Run();

                    if (test.TestState == TestState.Failed)
                    {
                        testRunState = TestState.Failed;
                    }

                    // Signal single test results to caller.
                    singleTestCompletionDelegate(test);
                }
                catch (Exception ex)
                {
                    // if the test didn't behave, return an error.
                    testRunState = TestState.Failed;

                    // Change the test outcome.
                    test.TestState = TestState.Failed;
                    test.TestOutput.Add(ex.ToString());
                    singleTestCompletionDelegate(test);
                }
            }

            // when we're done, signal the main thread and all other interested
            allTestsCompletednDelegate(testRunState);
        }
    }
}