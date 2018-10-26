using System.Collections.Generic;

namespace UnityEditor.PackageManager.ValidationSuite
{
    internal enum TestState
    {
        Succeeded,
        Failed,
        NotRun
    }

    internal interface IValidationTestResult
    {
        // The test associated to this result
        IValidationTest ValidationTest { get; }

        TestState TestState { get;}

        // Output string from test
        List<string> TestOutput { get;}
    }

}