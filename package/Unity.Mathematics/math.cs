// * TODO: Validate -1 vs 1 behaviour in math lib. eg < returns 0 / -1
//   Check if all operators act accordingly
// * Also int3 etc bool casts? should they exist?
// * Should we allow float4 value = 5; it is convenient and how it is in hlsl but maybe not the right fit in C#?

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Unity.Mathematics
{
    public static partial class math
    {
        public enum RotationOrder
        {
            XYZ,
            XZY,
            YXZ,
            YZX,
            ZXY,    // Unity Default
            ZYX,
        };

        public enum ShuffleComponent
        {
            LeftX,
            LeftY,
            LeftZ,
            LeftW,
            RightX,
            RightY,
            RightZ,
            RightW
        };

        // asint
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int asint(float v) {
            IntFloatUnion u;
            u.intValue = 0;
            u.floatValue = v;
            return u.intValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 asint(float2 v) { return int2(asint(v.x), asint(v.y)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 asint(float3 v) { return int3(asint(v.x), asint(v.y), asint(v.z)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 asint(float4 v) { return int4(asint(v.x), asint(v.y), asint(v.z), asint(v.w)); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int  asint(uint v) { return (int)v; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 asint(uint2 v) { return int2((int)v.x, (int)v.y); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 asint(uint3 v) { return int3((int)v.x, (int)v.y, (int)v.z); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 asint(uint4 v) { return int4((int)v.x, (int)v.y, (int)v.z, (int)v.w); }

        // asuint
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint asuint(float v)
        {
            return (uint)asint(v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint2 asuint(float2 v) { return uint2(asuint(v.x), asuint(v.y)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint3 asuint(float3 v) { return uint3(asuint(v.x), asuint(v.y), asuint(v.z)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint4 asuint(float4 v) { return uint4(asuint(v.x), asuint(v.y), asuint(v.z), asuint(v.w)); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint  asuint(int v) { return (uint)v; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint2 asuint(int2 v) { return uint2((uint)v.x, (uint)v.y); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint3 asuint(int3 v) { return uint3((uint)v.x, (uint)v.y, (uint)v.z); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint4 asuint(int4 v) { return uint4((uint)v.x, (uint)v.y, (uint)v.z, (uint)v.w); }


        // asfloat
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float asfloat(int v)
        {
            IntFloatUnion u;
            u.floatValue = 0;
            u.intValue = v;

            return u.floatValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 asfloat(int2 v) { return float2(asfloat(v.x), asfloat(v.y)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 asfloat(int3 v) { return float3(asfloat(v.x), asfloat(v.y), asfloat(v.z)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 asfloat(int4 v) { return float4(asfloat(v.x), asfloat(v.y), asfloat(v.z), asfloat(v.w)); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float  asfloat(uint v) { return asfloat((int)v); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 asfloat(uint2 v) { return float2(asfloat(v.x), asfloat(v.y)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 asfloat(uint3 v) { return float3(asfloat(v.x), asfloat(v.y), asfloat(v.z)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 asfloat(uint4 v) { return float4(asfloat(v.x), asfloat(v.y), asfloat(v.z), asfloat(v.w)); }


        // min
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float min(float a, float b) { return float.IsNaN(b) || a < b ? a : b; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 min(float2 a, float2 b) { return new float2(min(a.x, b.x), min(a.y, b.y)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 min(float3 a, float3 b) { return new float3(min(a.x, b.x), min(a.y, b.y), min(a.z, b.z)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 min(float4 a, float4 b) { return new float4(min(a.x, b.x), min(a.y, b.y), min(a.z, b.z), min(a.w, b.w)); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int min(int a, int b) { return a < b ? a : b; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 min(int2 a, int2 b) { return new int2(min(a.x, b.x), min(a.y, b.y)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 min(int3 a, int3 b) { return new int3(min(a.x, b.x), min(a.y, b.y), min(a.z, b.z)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 min(int4 a, int4 b) { return new int4(min(a.x, b.x), min(a.y, b.y), min(a.z, b.z), min(a.w, b.w)); }

        // max
        [MethodImpl((MethodImplOptions) 0x100)] // agressive inline
        public static float max(float a, float b) { return float.IsNaN(b) || a > b ? a : b; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 max(float2 a, float2 b) { return new float2(max(a.x, b.x), max(a.y, b.y)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 max(float3 a, float3 b) { return new float3(max(a.x, b.x), max(a.y, b.y), max(a.z, b.z)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 max(float4 a, float4 b) { return new float4(max(a.x, b.x), max(a.y, b.y), max(a.z, b.z), max(a.w, b.w)); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int max(int a, int b) { return a > b ? a : b; } // Use Math.Max as it is handling properly NaN
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 max(int2 a, int2 b) { return new int2(max(a.x, b.x), max(a.y, b.y)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 max(int3 a, int3 b) { return new int3(max(a.x, b.x), max(a.y, b.y), max(a.z, b.z)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 max(int4 a, int4 b) { return new int4(max(a.x, b.x), max(a.y, b.y), max(a.z, b.z), max(a.w, b.w)); }

        // lerp
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float lerp(float a, float b, float w) { return a + w * (b - a); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 lerp(float2 a, float2 b, float w) { return a + w * (b - a); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 lerp(float3 a, float3 b, float w) { return a + w * (b - a); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 lerp(float4 a, float4 b, float w) { return a + w * (b - a); }

        // mad
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float mad(float a, float b, float c) { return a * b + c; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 mad(float2 a, float2 b, float2 c) { return a * b + c; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 mad(float3 a, float3 b, float3 c) { return a * b + c; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 mad(float4 a, float4 b, float4 c) { return a * b + c; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int mad(int a, int b, int c) { return a * b + c; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 mad(int2 a, int2 b, int2 c) { return a * b + c; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 mad(int3 a, int3 b, int3 c) { return a * b + c; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 mad(int4 a, int4 b, int4 c) { return a * b + c; }

        // TODO: madint version????


        // clamp
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float clamp(float x, float a, float b) { return max(a, min(b, x)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 clamp(float2 x, float2 a, float2 b) { return max(a, min(b, x)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 clamp(float3 x, float3 a, float3 b) { return max(a, min(b, x)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 clamp(float4 x, float4 a, float4 b) { return max(a, min(b, x)); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int clamp(int x, int a, int b) { return max(a, min(b, x)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 clamp(int2 x, int2 a, int2 b) { return max(a, min(b, x)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 clamp(int3 x, int3 a, int3 b) { return max(a, min(b, x)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 clamp(int4 x, int4 a, int4 b) { return max(a, min(b, x)); }

        // saturate
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float saturate(float x) { return clamp(x, 0.0F, 1.0F); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 saturate(float2 x) { return clamp(x, new float2(0.0F), new float2(1.0F)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 saturate(float3 x) { return clamp(x, new float3(0.0F), new float3(1.0F)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 saturate(float4 x) { return clamp(x, new float4(0.0F), new float4(1.0F)); }

        // abs
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float abs(float a) { return max(-a, a); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int abs(int a) { return max(-a, a); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 abs(float2 a) { return max(-a, a); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 abs(float3 a) { return max(-a, a); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 abs(float4 a) { return max(-a, a); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 abs(int2 a) { return max(-a, a); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 abs(int3 a) { return max(-a, a); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 abs(int4 a) { return max(-a, a); }

        // dot
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float dot(float pt1, float pt2) { return pt1 * pt2; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float dot(float2 pt1, float2 pt2) { return pt1.x * pt2.x + pt1.y * pt2.y; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float dot(float3 pt1, float3 pt2) { return pt1.x * pt2.x + pt1.y * pt2.y + pt1.z * pt2.z; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float dot(float4 pt1, float4 pt2) { return pt1.x * pt2.x + pt1.y * pt2.y + pt1.z * pt2.z + pt1.w * pt2.w; }

        // tan
        public static float tan(float value) { return (float)System.Math.Tan(value); }
        public static float2 tan(float2 value) { return new float2(tan(value.x), tan(value.y)); }
        public static float3 tan(float3 value) { return new float3(tan(value.x), tan(value.y), tan(value.z)); }
        public static float4 tan(float4 value) { return new float4(tan(value.x), tan(value.y), tan(value.z), tan(value.w)); }

        // atan
        public static float atan(float value) { return (float)System.Math.Atan(value); }
        public static float2 atan(float2 value) { return new float2(atan(value.x), atan(value.y)); }
        public static float3 atan(float3 value) { return new float3(atan(value.x), atan(value.y), atan(value.z)); }
        public static float4 atan(float4 value) { return new float4(atan(value.x), atan(value.y), atan(value.z), atan(value.w)); }

        // atan2
        public static float atan2(float pt1, float pt2) { return (float)System.Math.Atan2(pt1, pt2); }
        public static float2 atan2(float2 pt1, float2 pt2) { return new float2(atan2(pt1.x, pt2.x), atan2(pt1.y, pt2.y)); }
        public static float3 atan2(float3 pt1, float3 pt2) { return new float3(atan2(pt1.x, pt2.x), atan2(pt1.y, pt2.y), atan2(pt1.z, pt2.z)); }
        public static float4 atan2(float4 pt1, float4 pt2) { return new float4(atan2(pt1.x, pt2.x), atan2(pt1.y, pt2.y), atan2(pt1.z, pt2.z), atan2(pt1.w, pt2.w)); }

        // cos
        public static float cos(float a) { return (float)System.Math.Cos((float)a); }
        public static float2 cos(float2 a) { return new float2(cos(a.x), cos(a.y)); }
        public static float3 cos(float3 a) { return new float3(cos(a.x), cos(a.y), cos(a.z)); }
        public static float4 cos(float4 a) { return new float4(cos(a.x), cos(a.y), cos(a.z), cos(a.w)); }

        // acos
        public static float acos(float a) { return (float)System.Math.Acos((float)a); }
        public static float2 acos(float2 a) { return new float2(acos(a.x), acos(a.y)); }
        public static float3 acos(float3 a) { return new float3(acos(a.x), acos(a.y), acos(a.z)); }
        public static float4 acos(float4 a) { return new float4(acos(a.x), acos(a.y), acos(a.z), acos(a.w)); }

        // sin
        public static float sin(float a) { return (float)System.Math.Sin((float)a); }
        public static float2 sin(float2 a) { return new float2(sin(a.x), sin(a.y)); }
        public static float3 sin(float3 a) { return new float3(sin(a.x), sin(a.y), sin(a.z)); }
        public static float4 sin(float4 a) { return new float4(sin(a.x), sin(a.y), sin(a.z), sin(a.w)); }

        // asin
        public static float asin(float a) { return (float)System.Math.Asin((float)a); }
        public static float2 asin(float2 a) { return new float2(asin(a.x), asin(a.y)); }
        public static float3 asin(float3 a) { return new float3(asin(a.x), asin(a.y), asin(a.z)); }
        public static float4 asin(float4 a) { return new float4(asin(a.x), asin(a.y), asin(a.z), asin(a.w)); }

        // floor
        public static float floor(float a) { return (float)System.Math.Floor((float)a); }
        public static float2 floor(float2 a) { return new float2(floor(a.x), floor(a.y)); }
        public static float3 floor(float3 a) { return new float3(floor(a.x), floor(a.y), floor(a.z)); }
        public static float4 floor(float4 a) { return new float4(floor(a.x), floor(a.y), floor(a.z), floor(a.w)); }

        // ceil
        public static float ceil(float a) { return (float)System.Math.Ceiling((float)a); }
        public static float2 ceil(float2 a) { return new float2(ceil(a.x), ceil(a.y)); }
        public static float3 ceil(float3 a) { return new float3(ceil(a.x), ceil(a.y), ceil(a.z)); }
        public static float4 ceil(float4 a) { return new float4(ceil(a.x), ceil(a.y), ceil(a.z), ceil(a.w)); }

        // round
        public static float round(float a) { return (float)System.Math.Round((float)a); }
        public static float2 round(float2 a) { return new float2(round(a.x), round(a.y)); }
        public static float3 round(float3 a) { return new float3(round(a.x), round(a.y), round(a.z)); }
        public static float4 round(float4 a) { return new float4(round(a.x), round(a.y), round(a.z), round(a.w)); }

        // frac
        public static float frac(float a) { return a - floor(a); }
        public static float2 frac(float2 a) { return a - floor(a); }
        public static float3 frac(float3 a) { return a - floor(a); }
        public static float4 frac(float4 a) { return a - floor(a); }

        // rcp
        public static float rcp(float a) { return 1f / a; }
        public static float2 rcp(float2 a) { return 1f / a; }
        public static float3 rcp(float3 a) { return 1f / a; }
        public static float4 rcp(float4 a) { return 1f / a; }

        // sign
        public static float sign(float f) { return f == 0f ? 0f : (f > 0f ? 1f : 0.0f) - (f < 0f ? 1.0f : 0.0f); }
        public static float2 sign(float2 f) { return new float2(sign(f.x), sign(f.y)); }
        public static float3 sign(float3 f) { return new float3(sign(f.x), sign(f.y), sign(f.z)); }
        public static float4 sign(float4 f) { return new float4(sign(f.x), sign(f.y), sign(f.z), sign(f.w)); }

        // mix
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float mix(float a, float b, float x) { return x * (b - a) + a; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 mix(float2 a, float2 b, float2 x) { return x * (b - a) + a; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 mix(float3 a, float3 b, float3 x) { return x * (b - a) + a; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 mix(float4 a, float4 b, float4 x) { return x * (b - a) + a; }

        // pow
        public static float pow(float a, float b) { return (float)System.Math.Pow((float)a, (float)b); }
        public static float2 pow(float2 a, float2 b) { return new float2(pow(a.x, b.x), pow(a.y, b.y)); }
        public static float3 pow(float3 a, float3 b) { return new float3(pow(a.x, b.x), pow(a.y, b.y), pow(a.z, b.z)); }
        public static float4 pow(float4 a, float4 b) { return new float4(pow(a.x, b.x), pow(a.y, b.y), pow(a.z, b.z), pow(a.w, b.w)); }

        // powr - assumes sign of a is 0 or greater
        public static float powr(float a, float b) { return pow(a, b); }
        public static float2 powr(float2 a, float2 b) { return pow(a, b); }
        public static float3 powr(float3 a, float3 b) { return pow(a, b); }
        public static float4 powr(float4 a, float4 b) { return pow(a, b); }

        // exp
        public static float exp(float x) { return (float)System.Math.Exp((float)x); }
        public static float2 exp(float2 a) { return new float2(exp(a.x), exp(a.y)); }
        public static float3 exp(float3 a) { return new float3(exp(a.x), exp(a.y), exp(a.z)); }
        public static float4 exp(float4 a) { return new float4(exp(a.x), exp(a.y), exp(a.z), exp(a.w)); }

        // log
        public static float log(float x) { return (float)System.Math.Log((float)x); }
        public static float2 log(float2 a) { return new float2(log(a.x), log(a.y)); }
        public static float3 log(float3 a) { return new float3(log(a.x), log(a.y), log(a.z)); }
        public static float4 log(float4 a) { return new float4(log(a.x), log(a.y), log(a.z), log(a.w)); }

        // log10
        public static float log10(float x) { return (float)System.Math.Log10((float)x); }
        public static float2 log10(float2 a) { return new float2(log10(a.x), log10(a.y)); }
        public static float3 log10(float3 a) { return new float3(log10(a.x), log10(a.y), log10(a.z)); }
        public static float4 log10(float4 a) { return new float4(log10(a.x), log10(a.y), log10(a.z), log10(a.w)); }

        // mod
        public static float mod(float a, float b) { return a % b; }
        public static float2 mod(float2 a, float2 b) { return new float2(a.x % b.x, a.y % b.y); }
        public static float3 mod(float3 a, float3 b) { return new float3(a.x % b.x, a.y % b.y, a.z % b.z); }
        public static float4 mod(float4 a, float4 b) { return new float4(a.x % b.x, a.y % b.y, a.z % b.z, a.w % b.w); }

        // sqrt
        public static float sqrt(float a) { return (float)System.Math.Sqrt((float)a); }
        public static float2 sqrt(float2 a) { return new float2(sqrt(a.x), sqrt(a.y)); }
        public static float3 sqrt(float3 a) { return new float3(sqrt(a.x), sqrt(a.y), sqrt(a.z)); }
        public static float4 sqrt(float4 a) { return new float4(sqrt(a.x), sqrt(a.y), sqrt(a.z), sqrt(a.w)); }

        // rsqrt
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float rsqrt(float a) { return 1.0F / sqrt(a); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 rsqrt(float2 a) { return 1.0f / sqrt(a); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 rsqrt(float3 a) { return 1.0f / sqrt(a); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 rsqrt(float4 a) { return 1.0f / sqrt(a); }

        // normalize
        public static float2 normalize(float2 v) { return rsqrt(dot(v, v)) * v; }
        public static float3 normalize(float3 v) { return rsqrt(dot(v, v)) * v; }
        public static float4 normalize(float4 v) { return rsqrt(dot(v, v)) * v; }

        // length
        public static float length(float v) { return abs(v); }
        public static float length(float2 v) { return sqrt(dot(v, v)); }
        public static float length(float3 v) { return sqrt(dot(v, v)); }
        public static float length(float4 v) { return sqrt(dot(v, v)); }

        // length squared
        public static float lengthSquared(float v) { return v*v; }
        public static float lengthSquared(float2 v) { return dot(v, v); }
        public static float lengthSquared(float3 v) { return dot(v, v); }
        public static float lengthSquared(float4 v) { return dot(v, v); }

        // distance
        public static float distance(float pt1, float pt2) { return length(pt2 - pt1); }
        public static float distance(float2 pt1, float2 pt2) { return length(pt2 - pt1); }
        public static float distance(float3 pt1, float3 pt2) { return length(pt2 - pt1); }
        public static float distance(float4 pt1, float4 pt2) { return length(pt2 - pt1); }

        // cross
        public static float3 cross(float3 p0, float3 p1) { return (p0 * p1.yzx - p0.yzx * p1).yzx; }

        public static float smoothstep(float a, float b, float x)
        {
            var t = saturate((x - a) / (b - a));
            return t * t * (3.0F - (2.0F * t));
        }

        public static float2 smoothstep(float2 a, float2 b, float2 x)
        {
            var t = saturate((x - a) / (b - a));
            return t * t * (3.0F - (2.0F * t));
        }

        public static float3 smoothstep(float3 a, float3 b, float3 x)
        {
            var t = saturate((x - a) / (b - a));
            return t * t * (3.0F - (2.0F * t));
        }

        public static float4 smoothstep(float4 a, float4 b, float4 x)
        {
            var t = saturate((x - a) / (b - a));
            return t * t * (3.0F - (2.0F * t));
        }

        // any
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool any(bool a) { return a; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool any(bool2 a) { return a.x || a.y; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool any(bool3 a) { return a.x || a.y || a.z; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool any(bool4 a) { return a.x || a.y || a.z || a.w; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool any(int a) { return a != 0; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool any(int2 a) { return a.x != 0 || a.y != 0; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool any(int3 a) { return a.x != 0 || a.y != 0 || a.z != 0; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool any(int4 a) { return a.x != 0 || a.y != 0 || a.z != 0 || a.w != 0; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool any(float a) { return a != 0.0F; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool any(float2 a) { return a.x != 0.0F || a.y != 0.0F; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool any(float3 a) { return a.x != 0.0F || a.y != 0.0F || a.z != 0.0F; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool any(float4 a) { return a.x != 0.0F || a.y != 0.0F || a.z != 0.0F || a.w != 0.0F; }

        // all
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(bool a) { return a; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(bool2 a) { return a.x && a.y; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(bool3 a) { return a.x && a.y && a.z; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(bool4 a) { return a.x && a.y && a.z && a.w; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(int a) { return a != 0; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(int2 a) { return a.x != 0 && a.y != 0; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(int3 a) { return a.x != 0 && a.y != 0 && a.z != 0; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(int4 a) { return a.x != 0 && a.y != 0 && a.z != 0 && a.w != 0; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(float a) { return a != 0.0F; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(float2 a) { return a.x != 0.0F && a.y != 0.0F; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(float3 a) { return a.x != 0.0F && a.y != 0.0F && a.z != 0.0F; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(float4 a) { return a.x != 0.0F && a.y != 0.0F && a.z != 0.0F && a.w != 0.0F; }

        // Select
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int select(int a, int b, bool c)    { return c ? b : a; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 select(int2 a, int2 b, bool c) { return c ? b : a; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 select(int3 a, int3 b, bool c) { return c ? b : a; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 select(int4 a, int4 b, bool c) { return c ? b : a; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 select(int2 a, int2 b, bool2 c) { return new int2(c.x ? b.x : a.x, c.y ? b.y : a.y); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 select(int3 a, int3 b, bool3 c) { return new int3(c.x ? b.x : a.x, c.y ? b.y : a.y, c.z ? b.z : a.z); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 select(int4 a, int4 b, bool4 c) { return new int4(c.x ? b.x : a.x, c.y ? a.y : b.y, c.z ? b.z : a.z, c.w ? b.w : a.w); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float select(float a, float b, bool c)    { return c ? b : a; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 select(float2 a, float2 b, bool c) { return c ? b : a; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 select(float3 a, float3 b, bool c) { return c ? b : a; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 select(float4 a, float4 b, bool c) { return c ? b : a; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 select(float2 a, float2 b, bool2 c) { return new float2(c.x ? b.x : a.x, c.y ? b.y : a.y); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 select(float3 a, float3 b, bool3 c) { return new float3(c.x ? b.x : a.x, c.y ? b.y : a.y, c.z ? b.z : a.z); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 select(float4 a, float4 b, bool4 c) { return new float4(c.x ? b.x : a.x, c.y ? b.y : a.y, c.z ? b.z : a.z, c.w ? b.w : a.w); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float pickShuffleComponent(float4 a, float4 b, ShuffleComponent component)
        {
            switch(component)
            {
                case ShuffleComponent.LeftX:
                    return a.x;
                case ShuffleComponent.LeftY:
                    return a.y;
                case ShuffleComponent.LeftZ:
                    return a.z;
                case ShuffleComponent.LeftW:
                    return a.w;
                case ShuffleComponent.RightX:
                    return b.x;
                case ShuffleComponent.RightY:
                    return b.y;
                case ShuffleComponent.RightZ:
                    return b.z;
                case ShuffleComponent.RightW:
                    return b.w;
                default:
                    throw new System.ArgumentException("Invalid shuffle component: " + (int)component);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 shuffle(float4 a, float4 b, ShuffleComponent x, ShuffleComponent y, ShuffleComponent z, ShuffleComponent w)
        {
            // Naive implementation for non-burst
            return float4(  pickShuffleComponent(a, b, x),
                            pickShuffleComponent(a, b, y),
                            pickShuffleComponent(a, b, z),
                            pickShuffleComponent(a, b, w));
        }

        //Step
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float step(float a, float b) { return select(0.0f, 1.0f, b >= a); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 step(float2 a, float2 b) { return select(0.0f, 1.0f, b >= a); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 step(float3 a, float3 b) { return select(0.0f, 1.0f, b >= a); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 step(float4 a, float4 b) { return select(0.0f, 1.0f, b >= a); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float reflect(float i, float n) { return i - 2f * n * dot(i, n); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 reflect(float2 i, float2 n) { return i - 2f * n * dot(i, n); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 reflect(float3 i, float3 n) { return i - 2f * n * dot(i, n); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 reflect(float4 i, float4 n) { return i - 2f * n * dot(i, n); }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void sincos(float x, out float s, out float c) { s = sin(x); c = cos(x); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void sincos(float2 x, out float2 s, out float2 c) { s = sin(x); c = cos(x); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void sincos(float3 x, out float3 s, out float3 c) { s = sin(x); c = cos(x); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void sincos(float4 x, out float4 s, out float4 c) { s = sin(x); c = cos(x); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool4 lessThan(float4 x, float4 y) { return x < y; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool3 lessThan(float3 x, float3 y) { return x < y; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 lessThan(float2 x, float2 y) { return x < y; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool lessThan(float x, float y) { return x < y; }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool4 lessThanEqual(float4 x, float4 y) { return x <= y; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool3 lessThanEqual(float3 x, float3 y) { return x <= y; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 lessThanEqual(float2 x, float2 y) { return x <= y; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool lessThanEqual(float x, float y) { return x <= y; }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool4 greaterThan(float4 x, float4 y) { return x > y; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool3 greaterThan(float3 x, float3 y) { return x > y; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 greaterThan(float2 x, float2 y) { return x > y; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool greaterThan(float x, float y) { return x > y; }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool4 greaterThanEqual(float4 x, float4 y) { return x >= y; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool3 greaterThanEqual(float3 x, float3 y) { return x >= y; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 greaterThanEqual(float2 x, float2 y) { return x >= y; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool greaterThanEqual(float x, float y) { return x >= y; }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool4 equal(float4 x, float4 y) { return x == y; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool3 equal(float3 x, float3 y) { return x == y; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 equal(float2 x, float2 y) { return x == y; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool equal(float x, float y) { return x == y; }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool4 notEqual(float4 x, float4 y) { return x != y; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool3 notEqual(float3 x, float3 y) { return x != y; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 notEqual(float2 x, float2 y) { return x != y; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool notEqual(float x, float y) { return x != y; }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 up() { return new float3(0.0f,1.0f,0.0f); }

        // SSE shuffles
        public static float4 unpacklo(float4 a, float4 b)
        {
            return shuffle(a, b, ShuffleComponent.LeftX, ShuffleComponent.RightX, ShuffleComponent.LeftY, ShuffleComponent.RightY);
        }

        public static float4 unpackhi(float4 a, float4 b)
        {
            return shuffle(a, b, ShuffleComponent.LeftZ, ShuffleComponent.RightZ, ShuffleComponent.LeftW, ShuffleComponent.RightW);
        }

        public static float4 movelh(float4 a, float4 b)
        {
            return shuffle(a, b, ShuffleComponent.LeftX, ShuffleComponent.LeftY, ShuffleComponent.RightX, ShuffleComponent.RightY);
        }

        public static float4 movehl(float4 a, float4 b)
        {
            return shuffle(b, a, ShuffleComponent.LeftZ, ShuffleComponent.LeftW, ShuffleComponent.RightZ, ShuffleComponent.RightW);
        }


        [StructLayout(LayoutKind.Explicit)]
        internal struct IntFloatUnion
        {
            [FieldOffset(0)]
            public int intValue;
            [FieldOffset(0)]
            public float floatValue;
        }
    }
}
