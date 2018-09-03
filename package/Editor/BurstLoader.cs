using System;
using System.IO;
using Unity.Burst.LowLevel;
using UnityEditor;

namespace Unity.Burst.Editor
{
    /// <summary>
    /// Main entry point for initializing the burst compiler service for both JIT and AOT
    /// </summary>
    [InitializeOnLoad]
    internal class BurstLoader
    {
        static BurstLoader()
        {
            // Un-comment the following to log compilation steps to log.txt in the .Runtime folder
            // Environment.SetEnvironmentVariable("UNITY_BURST_DEBUG", "1");

            // Try to load the runtime through an environment variable
            var runtimePath = Environment.GetEnvironmentVariable("UNITY_BURST_RUNTIME_PATH");

            // Otherwise try to load it from the package itself
            if (!Directory.Exists(runtimePath))
            {
                runtimePath = Path.GetFullPath("Packages/com.unity.burst/.Runtime");
            }
            BurstCompilerService.Initialize(runtimePath, BurstReflection.ExtractBurstCompilerOptions);
        }
    }
}
