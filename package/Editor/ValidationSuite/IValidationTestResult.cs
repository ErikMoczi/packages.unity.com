using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.PackageManager.ValidationSuite
{
    internal enum TestState
    {
        Succeeded,
        Failed,
        NotRun,
        Running,
        NotImplementedYet
    }

    internal interface IValidationTestResult
    {
        // The test associated to this result
        IValidationTest ValidationTest { get; }

        TestState TestState { get;}

        // Output string from test
        List<string> TestOutput { get;}

        DateTime StartTime { get; }
        
        DateTime EndTime { get; }
    }

}