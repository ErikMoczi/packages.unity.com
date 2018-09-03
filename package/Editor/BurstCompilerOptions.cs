using System;

namespace Burst.Compiler.IL
{
    /// <summary>
    /// Options shared between the package and the burst compiler.
    /// NOTE: This file is shared via a csproj cs link in Burst.Compiler.IL
    /// </summary>
    internal static class BurstCompilerOptions
    {
        public const string DefaultLibraryName = "lib_burst_generated";

        // -------------------------------------------------------
        // Common options used by the compiler
        // -------------------------------------------------------
        public const string OptionBackend = "backend=";
        public const string OptionSafetyChecks = "safety-checks";
        public const string OptionDisableSafetyChecks = "disable-safety-checks";
        public const string OptionNoAlias = "noalias";
        public const string OptionDisableNoAlias = "disable-noalias";
        public const string OptionDisableOpt = "disable-opt";
        public const string OptionFastMath = "fastmath";
        public const string OptionTarget = "target=";
        public const string OptionIROpt = "ir-opt";
        public const string OptionCpuOpt = "cpu-opt=";
        public const string OptionPrecision = "precision=";
        public const string OptionMath = "math=";
        public const string OptionDump = "dump=";
        public const string OptionFormat = "format=";

        // -------------------------------------------------------
        // Options used by the Jit compiler
        // -------------------------------------------------------

        public const string OptionJitDisableFunctionCaching = "disable-function-caching";
        public const string OptionJitEnableModuleCaching = "enable-module-caching";
        public const string OptionJitEnableModuleCachingDebugger = "enable-module-caching-debugger";
        public const string OptionJitEnableSynchronousCompilation = "enable-synchronous-compilation";

        // TODO: Remove this option and use proper dump flags or revisit how we log timings
        public const string OptionJitLogTimings = "log-timings";
        public const string OptionJitCacheDirectory = "cache-directory";

        // -------------------------------------------------------
        // Options used by the Aot compiler
        // -------------------------------------------------------
        public const string OptionAotPlatform = "platform=";
        public const string OptionAotAssemblyFolder = "assembly-folder=";
        public const string OptionAotMethod = "method=";
        public const string OptionAotType = "type=";
        public const string OptionAotAssembly = "assembly=";
        public const string OptionAotOutputPath = "output=";

        public static string GetOption(string optionName, object value = null)
        {
            if (optionName == null) throw new ArgumentNullException(nameof(optionName));
            return "--" + optionName + (value ?? String.Empty);
        }
    }

    /// <summary>
    /// Flags used by <see cref="NativeCompiler.CompileMethod"/> to dump intermediate compiler results.
    /// </summary>
    [Flags]
#if UNITY_EDITOR
    internal enum NativeDumpFlags
#else
    public enum NativeDumpFlags
#endif
    {
        /// <summary>
        /// Nothing is selected.
        /// </summary>
        None = 0,

        /// <summary>
        /// Dumps the IL of the method being compiled
        /// </summary>
        IL = 1 << 0,

        /// <summary>
        /// Dumps the reformated backend API Calls
        /// </summary>
        Backend = 1 << 1,

        /// <summary>
        /// Dumps the generated module without optimizations
        /// </summary>
        IR = 1 << 2,

        /// <summary>
        /// Dumps the generated backend code after optimizations (if enabled)
        /// </summary>
        IROptimized = 1 << 3,

        /// <summary>
        /// Dumps the generated ASM code (by default will also compile the function as using <see cref="Function"/> flag)
        /// </summary>
        Asm = 1 << 4,

        /// <summary>
        /// Generate the native code
        /// </summary>
        Function = 1 << 5,

        /// <summary>
        /// Dumps the result of analysis
        /// </summary>
        Analysis = 1 << 6,

        All = IL | Backend | IR | IROptimized | Asm | Function | Analysis
    }
}