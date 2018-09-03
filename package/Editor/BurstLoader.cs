using System;
using System.Text;
using System.Reflection;
using System.IO;
using UnityEditor;
using Unity.Jobs;

#if UNITY_EDITOR
namespace Unity.Burst.LowLevel
{
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
            BurstCompilerService.Initialize(runtimePath, ExtractBurstCompilerOptions);
        }

        public static bool ExtractBurstCompilerOptions(Type type, out string optimizationFlags)
        {
            optimizationFlags = null;

            if (!EditorPrefs.GetBool(BurstMenu.kEnableBurstCompilation, true))
                return false;

            var attr = type.GetCustomAttribute<ComputeJobOptimizationAttribute>();
            if (attr == null)
                return false;

            var builder = new StringBuilder();

            if (EditorPrefs.GetBool(BurstMenu.kEnableSafetyChecks, true))
                AddOption(builder, "-enable-safety-checks");
            else
                AddOption(builder, "-disable-safety-checks");

            if (attr.CompileSynchronously)
                AddOption(builder, "-enable-synchronous-compilation");

            if (attr.Accuracy != Accuracy.Std)
                AddOption(builder, "-fast-math");

            //Debug.Log($"ExtractBurstCompilerOptions: {type} {optimizationFlags}");

            // AddOption(builder, "-enable-module-caching-debugger");
            // AddOption(builder, "-cache-directory=Library/BurstCache");

            optimizationFlags = builder.ToString();

            return true;
        }

        static void AddOption(StringBuilder builder, string option)
        {
            if (builder.Length != 0)
                builder.Append(' ');

            builder.Append(option);
        }
    }
}

#endif
