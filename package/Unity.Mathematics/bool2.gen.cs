// GENERATED CODE
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;

#pragma warning disable 0660, 0661

namespace Unity.Mathematics
{
    [DebuggerTypeProxy(typeof(bool2.DebuggerProxy))]
    [System.Serializable]
    public partial struct bool2 : System.IEquatable<bool2>
    {
        [MarshalAs(UnmanagedType.U1)]
        public bool x;
        [MarshalAs(UnmanagedType.U1)]
        public bool y;


        // constructors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool2(bool x, bool y)
        { 
            this.x = x;
            this.y = y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool2(bool2 xy)
        { 
            this.x = xy.x;
            this.y = xy.y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool2(bool v)
        {
            this.x = v;
            this.y = v;
        }


        // conversions
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool2(bool v) { return new bool2(v); }


        // equal 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 operator == (bool2 lhs, bool2 rhs) { return new bool2 (lhs.x == rhs.x, lhs.y == rhs.y); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 operator == (bool2 lhs, bool rhs) { return new bool2 (lhs.x == rhs, lhs.y == rhs); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 operator == (bool lhs, bool2 rhs) { return new bool2 (lhs == rhs.x, lhs == rhs.y); }

        // not equal 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 operator != (bool2 lhs, bool2 rhs) { return new bool2 (lhs.x != rhs.x, lhs.y != rhs.y); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 operator != (bool2 lhs, bool rhs) { return new bool2 (lhs.x != rhs, lhs.y != rhs); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 operator != (bool lhs, bool2 rhs) { return new bool2 (lhs != rhs.x, lhs != rhs.y); }

        // operator &
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 operator & (bool2 lhs, bool2 rhs) { return new bool2 (lhs.x & rhs.x, lhs.y & rhs.y); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 operator & (bool2 lhs, bool rhs) { return new bool2 (lhs.x & rhs, lhs.y & rhs); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 operator & (bool lhs, bool2 rhs) { return new bool2 (lhs & rhs.x, lhs & rhs.y); }

        // operator |
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 operator | (bool2 lhs, bool2 rhs) { return new bool2 (lhs.x | rhs.x, lhs.y | rhs.y); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 operator | (bool2 lhs, bool rhs) { return new bool2 (lhs.x | rhs, lhs.y | rhs); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 operator | (bool lhs, bool2 rhs) { return new bool2 (lhs | rhs.x, lhs | rhs.y); }

        // operator ^
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 operator ^ (bool2 lhs, bool2 rhs) { return new bool2 (lhs.x ^ rhs.x, lhs.y ^ rhs.y); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 operator ^ (bool2 lhs, bool rhs) { return new bool2 (lhs.x ^ rhs, lhs.y ^ rhs); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 operator ^ (bool lhs, bool2 rhs) { return new bool2 (lhs ^ rhs.x, lhs ^ rhs.y); }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool4 xxxx
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool4(x, x, x, x); }
        }


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool4 xxxy
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool4(x, x, x, y); }
        }


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool4 xxyx
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool4(x, x, y, x); }
        }


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool4 xxyy
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool4(x, x, y, y); }
        }


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool4 xyxx
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool4(x, y, x, x); }
        }


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool4 xyxy
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool4(x, y, x, y); }
        }


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool4 xyyx
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool4(x, y, y, x); }
        }


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool4 xyyy
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool4(x, y, y, y); }
        }


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool4 yxxx
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool4(y, x, x, x); }
        }


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool4 yxxy
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool4(y, x, x, y); }
        }


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool4 yxyx
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool4(y, x, y, x); }
        }


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool4 yxyy
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool4(y, x, y, y); }
        }


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool4 yyxx
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool4(y, y, x, x); }
        }


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool4 yyxy
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool4(y, y, x, y); }
        }


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool4 yyyx
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool4(y, y, y, x); }
        }


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool4 yyyy
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool4(y, y, y, y); }
        }


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool3 xxx
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool3(x, x, x); }
        }


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool3 xxy
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool3(x, x, y); }
        }


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool3 xyx
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool3(x, y, x); }
        }


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool3 xyy
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool3(x, y, y); }
        }


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool3 yxx
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool3(y, x, x); }
        }


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool3 yxy
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool3(y, x, y); }
        }


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool3 yyx
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool3(y, y, x); }
        }


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool3 yyy
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool3(y, y, y); }
        }


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool2 xx
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool2(x, x); }
        }


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool2 xy
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool2(x, y); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { x = value.x; y = value.y; }
        }


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool2 yx
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool2(y, x); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { y = value.x; x = value.y; }
        }


        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool2 yy
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new bool2(y, y); }
        }



        // [int index] 
        unsafe public bool this[int index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if ((uint)index >= 2)
                    throw new System.ArgumentException("index must be between[0...1]");
#endif
                fixed (bool2* array = &this) { return ((bool*)array)[index]; }
            }
            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if ((uint)index >= 2)
                    throw new System.ArgumentException("index must be between[0...1]");
#endif
                fixed (bool* array = &x) { array[index] = value; }
            }
        }

        // Equals 
        public bool Equals(bool2 rhs) { return x == rhs.x && y == rhs.y; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object o) { return Equals((bool2)o); }


        // GetHashCode 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() { return (int)math.hash(this); }


        // ToString 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return string.Format("bool2({0}, {1})", x, y);
        }

        internal sealed class DebuggerProxy
        {
            public bool x;
            public bool y;
            public DebuggerProxy(bool2 v)
            {
                x = v.x;
                y = v.y;
            }
        }

    }

    public static partial class math
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 bool2(bool x, bool y) { return new bool2(x, y); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 bool2(bool2 xy) { return new bool2(xy); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 bool2(bool v) { return new bool2(v); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint hash(bool2 v)
        {
            return csum(select(uint2(0xDCDD5341u, 0x94DDD769u), uint2(0xA1E92D39u, 0x4583C801u), v));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint2 hash_wide(bool2 v)
        {
            return (select(uint2(0x9536A0F5u, 0xAF816615u), uint2(0x9AF8D62Du, 0xE3600729u), v));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool select_shuffle_component(bool2 a, bool2 b, ShuffleComponent component)
        {
            switch(component)
            {
                case ShuffleComponent.LeftX:
                    return a.x;
                case ShuffleComponent.LeftY:
                    return a.y;
                case ShuffleComponent.RightX:
                    return b.x;
                case ShuffleComponent.RightY:
                    return b.y;
                default:
                    throw new System.ArgumentException("Invalid shuffle component: " + component);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool shuffle(bool2 a, bool2 b, ShuffleComponent x)
        {
            return select_shuffle_component(a, b, x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 shuffle(bool2 a, bool2 b, ShuffleComponent x, ShuffleComponent y)
        {
            return bool2(
                select_shuffle_component(a, b, x),
                select_shuffle_component(a, b, y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool3 shuffle(bool2 a, bool2 b, ShuffleComponent x, ShuffleComponent y, ShuffleComponent z)
        {
            return bool3(
                select_shuffle_component(a, b, x),
                select_shuffle_component(a, b, y),
                select_shuffle_component(a, b, z));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool4 shuffle(bool2 a, bool2 b, ShuffleComponent x, ShuffleComponent y, ShuffleComponent z, ShuffleComponent w)
        {
            return bool4(
                select_shuffle_component(a, b, x),
                select_shuffle_component(a, b, y),
                select_shuffle_component(a, b, z),
                select_shuffle_component(a, b, w));
        }

    }
}
