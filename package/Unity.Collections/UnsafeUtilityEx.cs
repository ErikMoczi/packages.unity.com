﻿using System;

namespace Unity.Collections.LowLevel.Unsafe
{
    unsafe public static class UnsafeUtilityEx
    {
        public static ref T AsRef<T>(void* ptr) where T : struct
        {
#if UNITY_CSHARP_TINY
            return ref UnsafeUtility.AsRef<T>(ptr);
#else
            return ref System.Runtime.CompilerServices.Unsafe.AsRef<T>(ptr);
#endif
        }

        public static ref T ArrayElementAsRef<T>(void* ptr, int index) where T : struct
        {
#if UNITY_CSHARP_TINY
            return ref UnsafeUtility.AsRef<T>((byte*)ptr + index * UnsafeUtility.SizeOf<T>());
#else
            return ref System.Runtime.CompilerServices.Unsafe.AsRef<T>((byte*)ptr + index * UnsafeUtility.SizeOf<T>());
#endif
        }

        public static void* RestrictNoAlias(void* ptr)
        {
            return ptr;
        }

        public static void MemSet(void* destination, byte value, int count)
        {
            if(value == 0)
                UnsafeUtility.MemClear(destination, count);
            else
                for (int i = 0; i < count; ++i)
                    ((byte*) destination)[i] = value;
        }

        public static bool IsUnmanaged<T>()
        {
            return UnsafeUtility.IsUnmanaged<T>();
        }
    }
}
