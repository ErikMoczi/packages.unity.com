// For some reasons Unity.Burst.LowLevel is not part of UnityEngine in 2018.2 but only in UnityEditor
// In 2018.3 It should be fine
#if !UNITY_ZEROPLAYER && !UNITY_CSHARP_TINY && ((UNITY_2018_2_OR_NEWER && UNITY_EDITOR) || UNITY_2018_3_OR_NEWER)
using System;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("Unity.Burst.Experimental")]

namespace Unity.Burst
{
    /// <summary>
    /// Internal, accessing the burst compiler service in Unity.
    /// NOTE: only used by the com.unity.burst.experimental for now
    /// </summary>
    internal static class BurstCompilerInternal
    {
        public static unsafe void* Compile<T>(T delegateObj) where T : class
        {
            if (delegateObj == null) throw new ArgumentNullException(nameof(delegateObj));
            if (!(delegateObj is Delegate)) throw new ArgumentException("object instance must be a System.Delegate", nameof(delegateObj));

            var delegateMethod = (Delegate)(object)delegateObj;
            if (!delegateMethod.Method.IsStatic)
            {
                throw new InvalidOperationException($"The method `{delegateMethod.Method}` must be static. Instance methods are not supported");
            }

            string defaultOptions = "--enable-synchronous-compilation";
            // TODO: Disable this part as it is using Editor code that is not accessible from the runtime. We will have to move the editor code to here
//#if UNITY_EDITOR
//            string extraOptions;
//            // The attribute is directly on the method, so we recover the underlying method here
//            if (Editor.BurstReflection.ExtractBurstCompilerOptions(delegateMethod.Method, out extraOptions))
//            {
//                return null;
//            }
//            if (!string.IsNullOrWhiteSpace(extraOptions))
//            {
//                defaultOptions += "\n" + extraOptions;
//            }
//#endif
            int delegateMethodID = Unity.Burst.LowLevel.BurstCompilerService.CompileAsyncDelegateMethod(delegateObj, defaultOptions);
            void* function = Unity.Burst.LowLevel.BurstCompilerService.GetAsyncCompiledAsyncDelegateMethod(delegateMethodID);
            return function;
        }
    }
}
#endif
