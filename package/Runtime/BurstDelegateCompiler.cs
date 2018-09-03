using System;

namespace Unity.Burst
{
    public static class BurstDelegateCompiler
    {
        public static unsafe T CompileDelegate<T>(T delegateMethod) where T : class
        {
#if UNITY_EDITOR
            int delegateMethodID = Unity.Burst.LowLevel.BurstCompilerService.CompileAsyncDelegateMethod(delegateMethod, "-enable-synchronous-compilation");
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
    }
}
