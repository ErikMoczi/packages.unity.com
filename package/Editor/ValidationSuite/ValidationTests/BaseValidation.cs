using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal abstract class BaseValidation : IValidationTest, IValidationTestResult
    {
        public ValidationSuite Suite { get; set; }
        
        public string TestName { get; protected set; }

        public string TestDescription { get; protected set; }

        // Category mostly used for sorting tests, or grouping in UI.
        public TestCategory TestCategory { get; protected set; }

        public IValidationTest ValidationTest { get { return this; } }

        public TestState TestState { get; set; }

        // Output string from test
        public List<string> TestOutput { get; set; }

        public DateTime StartTime { get; private set; }
        
        public DateTime EndTime { get; private set; }

        public VettingContext Context { get; set; }

        public bool ShouldRun { get; set; }

        protected BaseValidation()
        {
            TestState = TestState.NotRun;
            TestOutput = new List<string>();
            ShouldRun = true;
            StartTime = DateTime.Now;
            EndTime = DateTime.Now;
        }

        // This method is called synchronously during initialization, 
        // and allows a test to interact with APIs, which need to run from the main thread.
        public virtual void Setup()
        {
        }

        public void RunTest()
        {
            StartTime = DateTime.Now;
            Run();
            EndTime = DateTime.Now;
        }

        // This needs to be implemented for every test
        protected abstract void Run();
    }
}