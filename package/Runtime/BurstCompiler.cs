using System;
using System.Collections.Generic;

namespace Unity.Burst
{
    /// <summary>
    /// The burst compiler runtime frontend.
    /// </summary>
    public static class BurstCompiler
    {
        private static readonly List<ResolveBackendPathFromNameDelegate> BackendPathResolvers = new List<ResolveBackendPathFromNameDelegate>();

        public const string DefaultBackendName = "burst-llvm";

        static BurstCompiler()
        {
            BackendName = DefaultBackendName;
        }

        /// <summary>
        /// Gets or sets a default compiler backend path to dll (default will resolve to `burst-llvm`)
        /// </summary>
        /// <remarks>
        /// Note that this does not have any effect at runtime, only at editor time.
        /// </remarks>
        public static string BackendName { get; set; }

        /// <summary>
        /// A delegate to translate a backend name to a backend path to the shared DLL of the backend to load.
        /// </summary>
        /// <param name="name">Name of the backend (e.g `burst-llvm`)</param>
        /// <returns>Path to the shared dll of the backend</returns>
        public delegate string ResolveBackendPathFromNameDelegate(string name);

        /// <summary>
        /// Setup a callback to allow to resolve a backend name to a backend path.
        /// </summary>
        /// <remarks>
        /// Note that this does not have any effect at runtime, only at editor time.
        /// </remarks>
        public static event ResolveBackendPathFromNameDelegate BackendNameResolver
        {
            add
            {
                lock (BackendPathResolvers)
                {
                    if (!BackendPathResolvers.Contains(value))
                    {
                        BackendPathResolvers.Add(value);

                    }
                }
            }

            remove
            {
                lock (BackendPathResolvers)
                {
                    BackendPathResolvers.Remove(value);
                }
            }
        }

        /// <summary>
        /// Compile the following delegate with burst and return a new delegate.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="delegateMethod"></param>
        /// <returns></returns>
        public static unsafe T CompileDelegate<T>(T delegateMethod) where T : class
        {
            // We have added support for runtime CompileDelegate in 2018.2+
#if UNITY_EDITOR //|| UNITY_2018_2_OR_NEWER
            string defaultOptions = "--enable-synchronous-compilation";
            var backendPath = ResolveBackendPath(BackendName);
            if (backendPath != null)
            {
                defaultOptions = defaultOptions + "\n--backend=" + backendPath;
            }
            int delegateMethodID = Unity.Burst.LowLevel.BurstCompilerService.CompileAsyncDelegateMethod(delegateMethod, defaultOptions);
            void* function = Unity.Burst.LowLevel.BurstCompilerService.GetAsyncCompiledAsyncDelegateMethod(delegateMethodID);
            if (function == null)
                return delegateMethod;

            object res = System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer((IntPtr)function, delegateMethod.GetType());
            return (T)res;
#else
            //@TODO: Runtime implementation
            return delegateMethod;
#endif
        }

        /// <summary>
        /// Resolves the <see cref="BackendName"/> to a full backend path (if null, returns null)
        /// </summary>
        /// <returns>The path of the backend or null if default</returns>
        public static string ResolveBackendPath(string backendName)
        {
            // By default if it is null, let the default compiler resolve the backend
            if (backendName == null)
            {
                return null;
            }

            lock (BackendPathResolvers)
            {
                foreach (var resolveBackendPathFromNameDelegate in BackendPathResolvers)
                {
                    var newBackendPath = resolveBackendPathFromNameDelegate(backendName);
                    if (newBackendPath != null)
                    {
                        return newBackendPath;
                    }
                }
            }

            return BackendName;
        }
    }
}
