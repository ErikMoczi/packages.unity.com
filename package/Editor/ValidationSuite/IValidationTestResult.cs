using System.Collections.Generic;

namespace UnityEditor.PackageManager.ValidationSuite
{
    public enum TestState
    {
        Succeeded,

        Failed,

        NotRun
    }

    public interface IValidationTestResult
    {
        // The test associated to this result
        IValidationTest ValidationTest { get; }

        TestState TestState { get;}

        // Output string from test
        List<string> TestOutput { get;}
    }

}