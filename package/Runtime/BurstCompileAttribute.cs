using System;
using System.Runtime.CompilerServices;
using Unity.Jobs.LowLevel.Unsafe;

// Make internals visible to Unity.Burst.Editor for BurstGlobalCompilerOptions
[assembly: InternalsVisibleTo("Unity.Burst.Editor")]
// Make internals visible to burst tests
[assembly: InternalsVisibleTo("btests")]
// Make internals visible to Unity.Physics
[assembly: InternalsVisibleTo("Unity.Physics")]
[assembly: InternalsVisibleTo("Unity.Physics.Tests")]
[assembly: InternalsVisibleTo("Unity.Audio.DSPGraph")]
[assembly: InternalsVisibleTo("Unity.UNode")]
[assembly: InternalsVisibleTo("Unity.UNode.Tests")]

namespace Unity.Burst
{
    // FloatMode and FloatPrecision must be kept in sync with burst.h / Burst.Backend

    public enum FloatMode
    {
        Default = 0,
        Strict = 1,
        Deterministic = 2,
        Fast = 3,
    }

    public enum FloatPrecision
    {
        Standard = 0,
        High = 1,
        Medium = 2,
        Low = 3,
    }

    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct|AttributeTargets.Method)]
    public class BurstCompileAttribute : System.Attribute
    {
        public FloatMode FloatMode { get; set; }

        public FloatPrecision FloatPrecision { get; set; }

        public bool CompileSynchronously { get; set; }

        public string[] Options { get; set; }

        public BurstCompileAttribute()
        {
        }

        public BurstCompileAttribute(FloatPrecision floatPrecision, FloatMode floatMode)
        {
            FloatMode = floatMode;
            FloatPrecision = floatPrecision;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class NoAliasAttribute : System.Attribute
    {
    }

    /// <summary>
    /// Global options that can be setup per executable
    /// </summary>
    internal static class BurstGlobalCompilerOptions
    {
        private const string DisableCompilationArg = "--burst-disable-compilation";

        private const string ForceSynchronousCompilationArg = "--burst-force-sync-compilation";

        /// <summary>
        /// <c>true</c> to disable compiling functions with burst (editor time only)
        /// </summary>
        public static readonly bool DisableCompilation;

        /// <summary>
        /// <c>true</c> to force synchronous compilation when compiling all functions (editor time only)
        /// </summary>
        /// <remarks>
        /// Typical use case when running with tests
        /// </remarks>
        public static readonly bool ForceSynchronousCompilation;

#if !UNITY_ZEROPLAYER && !UNITY_CSHARP_TINY
        /// <summary>
        /// Static initializer based on command line arguments
        /// </summary>
        static BurstGlobalCompilerOptions()
        {
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                switch (arg)
                {
                    case DisableCompilationArg:
                        DisableCompilation = true;
                        // We also force the Jobs to not compile at all
                        JobsUtility.JobCompilerEnabled = false;
                        break;
                    case ForceSynchronousCompilationArg:
                        ForceSynchronousCompilation = true;
                        break;
                }
            }
        }
#endif
    }
}