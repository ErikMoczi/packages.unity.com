using System;
using System.Runtime.InteropServices;

namespace Unity.Burst
{
#if BURST_FEATURE_FUNCTION_POINTER
    public interface IFunctionPointer
    {
        IFunctionPointer FromIntPtr(IntPtr ptr);
    }

    public struct FunctionPointer<T> : IFunctionPointer
    {
        private readonly IntPtr _ptr;

        public FunctionPointer(IntPtr ptr)
        {
            _ptr = ptr;
        }

        public IFunctionPointer FromIntPtr(IntPtr ptr)
        {
            return new FunctionPointer<T>(ptr);
        }

        public T Invoke => (T) (object) Marshal.GetDelegateForFunctionPointer(_ptr, typeof(T));
    }
#endif
}