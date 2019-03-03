using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal abstract class BaseValidation : IValidationTest, IValidationTestResult
    {
        public ValidationType[] SupportedValidations { get; set; }

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
            SupportedValidations = new[] { ValidationType.AssetStore, ValidationType.CI, ValidationType.LocalDevelopment, ValidationType.Publishing, ValidationType.VerifiedSet };
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
        protected void Error(string message, params object[] args)
        {
            TestOutput.Add(string.Format("Error: " + message, args));
            TestState = TestState.Failed;
        }

        protected void Warning(string message, params object[] args)
        {
            TestOutput.Add(string.Format("Warning: " + message, args));
        }

        protected void Information(string message, params object[] args)
        {
            TestOutput.Add(string.Format(message, args));
        }

        protected void DirectorySearch(string path, string searchPattern, List<string> matches)
        {
            if (!Directory.Exists(path))
                return;

            var files = Directory.GetFiles(path, searchPattern);
            if (files.Any())
                matches.AddRange(files);

            foreach (string subDir in Directory.GetDirectories(path))
                DirectorySearch(subDir, searchPattern, matches);
        }
    }
}
