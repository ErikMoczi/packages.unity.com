#if UNITY_EDITOR
using System;
using System.Text;
using System.IO;
using UnityEditor;
using Unity.Jobs;
using Unity.Burst.LowLevel;

namespace Unity.Burst
{
    public static class BurstDelegateCompiler
    {
        unsafe public static T CompileDelegate<T>(T delegateMethod) where T : class
        {
            int delegateMethodID = BurstCompilerService.CompileAsyncDelegateMethod(delegateMethod, "-enable-synchronous-compilation");
            void* function = BurstCompilerService.GetAsyncCompiledAsyncDelegateMethod(delegateMethodID);
            if (function == null)
                return null;
            
            object res = System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer((IntPtr)function, typeof(T));
            return (T)res;
        }
    }
}

#endif
