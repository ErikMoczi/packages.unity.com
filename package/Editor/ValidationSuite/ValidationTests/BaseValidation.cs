using System.Collections.Generic;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal abstract class BaseValidation : IValidationTest, IValidationTestResult
    {
        public string TestName { get; set; }

        public string TestDescription { get; set; }

        // Category mostly used for sorting tests, or grouping in UI.
        public TestCategory TestCategory { get; set; }

        public IValidationTest ValidationTest { get { return this; } }

        public TestState TestState { get; set; }

        // Output string from test
        public List<string> TestOutput { get; set; }

        public VettingContext Context { get; set; }

        protected BaseValidation()
        {
            TestState = TestState.NotRun;
            TestOutput = new List<string>();
        }

        // This method is called synchronously during initialization, 
        // and allows a test to interact with APIs, which need to run from the main thread.
        public virtual void Setup()
        {
        }

        // This needs to be implemented for every test
        public abstract void Run();
    }
}