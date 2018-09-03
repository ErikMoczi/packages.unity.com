using System;

namespace UnityEditor.TestTools.TestRunner
{
    internal interface ITestSettings : IDisposable
    {
        ScriptingImplementation? scriptingBackend { get; set; }

        string Architecture { get; set; }

        bool? useLatestScriptingRuntimeVersion { get; set; }

        ApiCompatibilityLevel? apiProfile { get; set; }

        void SetupProjectParameters();
    }
}
