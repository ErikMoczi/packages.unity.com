// For some reasons Unity.Burst.LowLevel is not part of UnityEngine in 2018.2 but only in UnityEditor
// In 2018.3 It should be fine
#if (UNITY_2018_2_OR_NEWER && UNITY_EDITOR) || UNITY_2018_3_OR_NEWER
using System;

namespace Unity.Burst
{
    /// <summary>
    /// The burst compiler runtime frontend.
    /// </summary>
    public static class BurstCompiler
    {
        static BurstCompiler()
        {
        }


        private static unsafe void* CompileInternal<T>(T delegateMethod) where T : class
        {
            string defaultOptions = "--enable-synchronous-compilation";
            int delegateMethodID = Unity.Burst.LowLevel.BurstCompilerService.CompileAsyncDelegateMethod(delegateMethod, defaultOptions);
            void* function = Unity.Burst.LowLevel.BurstCompilerService.GetAsyncCompiledAsyncDelegateMethod(delegateMethodID);
            return function;
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
            void* function = CompileInternal(delegateMethod);
            if (function == null)
                return delegateMethod;

            object res = System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer((IntPtr)function, delegateMethod.GetType());
            return (T)res;
        }

#if BURST_FEATURE_FUNCTION_POINTER
        /// <summary>
        /// Compile the following delegate into a function pointer with burst.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="delegateMethod"></param>
        /// <returns></returns>
        public static unsafe FunctionPointer<T> CompileFunctionPointer<T>(T delegateMethod) where T : class
        {
            // We have added support for runtime CompileDelegate in 2018.2+
            void* function = CompileInternal(delegateMethod);
            if (function == null)
                throw new InvalidOperationException($"Burst failed to compile the given delegate.");

            return new FunctionPointer<T>(new IntPtr(function));
        }
#endif
    }
}
#endif
