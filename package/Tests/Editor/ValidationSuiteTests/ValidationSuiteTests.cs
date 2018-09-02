using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using UnityEditor.PackageManager.ValidationSuite.ValidationTests;


namespace UnityEditor.PackageManager.ValidationSuite.Tests
{
    internal class ValidationSuiteTests
    {
        private int testSuccessCallbackCount = 0;
        private int testFailureCallbackCount = 0;
        private bool testComplete = false;

        [SetUp]
        public void Setup()
        {
            testSuccessCallbackCount = 0;
            testFailureCallbackCount = 0;
            testComplete = false;
        }

        [Test]
        public void When_AllTests_Pass_Suite_Succeeds()
        {
            var testList = new List<FakeValidationTest>()
            {
                new FakeValidationTest() {TestState = TestState.Succeeded, TestOutput = new List<string> { "All Looks Good" }},
                new FakeValidationTest() {TestState = TestState.Succeeded, TestOutput = new List<string> { "All Looks Good" }},
                new FakeValidationTest() {TestState = TestState.Succeeded, TestOutput = new List<string> { "All Looks Good" }},
                new FakeValidationTest() {TestState = TestState.Succeeded, TestOutput = new List<string> { "All Looks Good" }},
            };

            var validationSuite = new ValidationSuite(SingleTestCompletedDelegate, AllTestsCompletednDelegate, "c:\\Path");

            // Setup tests we want to run
            validationSuite.ValidationTests = testList.Cast<IValidationTest>();

            validationSuite.RunAsync();

            WaitForAsyncTestCompletion();

            // Validate Results
            Assert.AreEqual(4, testSuccessCallbackCount);
            Assert.AreEqual(0, testFailureCallbackCount);
        }

        [Test]
        public void When_NewTest_Is_Written_Its_AutoDiscovered()
        {
            // Check that our Fake validation test below was auto-detected by the Validation Suite,
            // which uses reflection to find all tests.
            var validationSuite = new ValidationSuite(SingleTestCompletedDelegate, AllTestsCompletednDelegate, "c:\\Path");

            Assert.AreEqual(1, validationSuite.ValidationTests.Where(t => t.TestName == "Manifest Validation").Count());
        }

        [Test]
        public void When_OneTests_Fails_Suite_Fails()
        {
            var testList = new List<FakeValidationTest>()
            {
                new FakeValidationTest() {TestState = TestState.Succeeded, TestOutput = new List<string> { "All Looks Good" }},
                new FakeValidationTest() {TestState = TestState.Failed, TestOutput = new List<string> { "Bad Result" }},
                new FakeValidationTest() {TestState = TestState.Succeeded, TestOutput = new List<string> { "All Looks Good" }},
                new FakeValidationTest() {TestState = TestState.Failed, TestOutput = new List<string> { "Horrible breakage" }}
            };

            var validationSuite = new ValidationSuite(SingleTestCompletedDelegate, AllTestsCompletednDelegate, "c:\\Path");

            // Setup tests we want to run
            validationSuite.ValidationTests = testList.Cast<IValidationTest>();

            validationSuite.RunAsync();

            WaitForAsyncTestCompletion();

            // Validate Results
            Assert.AreEqual(2, testSuccessCallbackCount);
            Assert.AreEqual(2, testFailureCallbackCount);
        }

        [Test]
        public void When_OneTests_Crashes_Suite_Fails()
        {
            var testList = new List<FakeValidationTest>()
            {
                new FakeValidationTest() {TestState = TestState.Succeeded, TestOutput = new List<string> { "All Looks Good" }},
                new FakeValidationTest() {TestState = TestState.Succeeded, TestOutput = new List<string> { "All Looks Good" }, ThrowException = true},
                new FakeValidationTest() {TestState = TestState.Succeeded, TestOutput = new List<string> { "All Looks Good" }},
                new FakeValidationTest() {TestState = TestState.Succeeded, TestOutput = new List<string> { "All Looks Good" }},
            };

            var validationSuite = new ValidationSuite(SingleTestCompletedDelegate, AllTestsCompletednDelegate, "c:\\Path");

            // Setup tests we want to run
            validationSuite.ValidationTests = testList.Cast<IValidationTest>();

            validationSuite.RunAsync();

            WaitForAsyncTestCompletion();

            // Validate Results
            Assert.AreEqual(3, testSuccessCallbackCount);
            Assert.AreEqual(1, testFailureCallbackCount);

            // Find the test that crashed, make sure it's data was updated properly.
            Assert.AreEqual(1, validationSuite.ValidationTestResults.Where(t => t.TestState == TestState.Failed).Count());
        }

        [Test]
        public void When_Cancel_PartialResults_Registered()
        {
            var testList = new List<FakeValidationTest>()
            {
                new FakeValidationTest() {TestState = TestState.Succeeded, TestOutput = new List<string> { "All Looks Good" }},
                new FakeValidationTest() {TestState = TestState.Succeeded, TestOutput = new List<string> { "All Looks Good" }, CreateDelay = true},
                new FakeValidationTest() {TestState = TestState.Succeeded, TestOutput = new List<string> { "All Looks Good" }},
                new FakeValidationTest() {TestState = TestState.Succeeded, TestOutput = new List<string> { "All Looks Good" }},
            };

            var validationSuite = new ValidationSuite(SingleTestCompletedDelegate, AllTestsCompletednDelegate, "c:\\Path");

            // Setup tests we want to run
            validationSuite.ValidationTests = testList.Cast<IValidationTest>();

            validationSuite.RunAsync();

            // Allow the Validation Suite to do some work before we cancel the run.
            Thread.Sleep(500);
            validationSuite.Cancel();

            // Validate Results
            Assert.AreEqual(1, testSuccessCallbackCount);
            Assert.AreEqual(0, testFailureCallbackCount);
        }

        private void SingleTestCompletedDelegate(IValidationTestResult testResult)
        {
            if (testResult.TestState == TestState.Succeeded)
                testSuccessCallbackCount++;
            else if (testResult.TestState == TestState.Failed)
                testFailureCallbackCount++;
            else
            {
                Assert.Fail("Unsupported test state " + testResult.TestState);
            }
        }

        private void AllTestsCompletednDelegate(TestState testRunState)
        {
            testComplete = true;
        }

        private void WaitForAsyncTestCompletion()
        {
            for (int i = 0; i < 4; i++)
            {
                // test completed, so let's continue.
                if (testComplete)
                    return;

                Thread.Sleep(500);
            }

            // If we get there, the test never completed
            Assert.Fail("Validation test didn't complete properly.");
        }
    }

    internal class FakeValidationTest : BaseValidation
    {
        public bool ThrowException { get; set; }

        public bool CreateDelay { get; set; }

        public FakeValidationTest()
        {
            ThrowException = false;
            TestName = "___FakeValidationTest___";
            TestState = TestState.Succeeded;
            TestDescription = "My Test Example";
            TestCategory = TestCategory.DataValidation;
        }

        public override void Run(string packagePath)
        {
            // Set the Test State when done
            if (ThrowException)
                throw new System.Exception("TestException");

            // Create 10 second delay in the test, used for testing canceling...
            if (CreateDelay)
                Thread.Sleep(10000);
        }
    }
}
