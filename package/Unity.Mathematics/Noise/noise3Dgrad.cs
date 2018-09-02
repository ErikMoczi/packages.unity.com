//
// Description : Array and textureless GLSL 2D/3D/4D simplex 
//               noise functions.
//      Author : Ian McEwan, Ashima Arts.
//  Maintainer : stegu
//     Lastmath.mod : 20110822 (ijm)
//     License : Copyright (C) 2011 Ashima Arts. All rights reserved.
//               Distributed under the MIT License. See LICENSE file.
//               https://github.com/ashima/webgl-noise
//               https://github.com/stegu/webgl-noise
// 

namespace Unity.Mathematics
{
    public static partial class noise
    {
        public static float snoise(float3 v, out float3 gradient)
        {
            float2 C = new float2(1.0f / 6.0f, 1.0f / 3.0f);
            float4 D = new float4(0.0f, 0.5f, 1.0f, 2.0f);

            // First corner
            float3 i = math.floor(v + math.dot(v, C.yyy));
            float3 x0 = v - i + math.dot(i, C.xxx);

            // Other corners
            float3 g = math.step(x0.yzx, x0.xyz);
            float3 l = 1.0f - g;
            float3 i1 = math.min(g.xyz, l.zxy);
            float3 i2 = math.max(g.xyz, l.zxy);

            //   x0 = x0 - 0.0 + 0.0 * C.xxx;
            //   x1 = x0 - i1  + 1.0 * C.xxx;
            //   x2 = x0 - i2  + 2.0 * C.xxx;
            //   x3 = x0 - 1.0 + 3.0 * C.xxx;
            float3 x1 = x0 - i1 + C.xxx;
            float3 x2 = x0 - i2 + C.yyy; // 2.0*C.x = 1/3 = C.y
            float3 x3 = x0 - D.yyy; // -1.0+3.0*C.x = -0.5 = -D.y

            // Permutations
            i = mod289(i);
            float4 p = permute(permute(permute(
                                           i.z + new float4(0.0f, i1.z, i2.z, 1.0f))
                                       + i.y + new float4(0.0f, i1.y, i2.y, 1.0f))
                               + i.x + new float4(0.0f, i1.x, i2.x, 1.0f));

            // Gradients: 7x7 points over a square, mapped onto an octahedron.
            // The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
            float n_ = 0.142857142857f; // 1.0/7.0
            float3 ns = n_ * D.wyz - D.xzx;

            float4 j = p - 49.0f * math.floor(p * ns.z * ns.z); //  math.mod(p,7*7)

            float4 x_ = math.floor(j * ns.z);
            float4 y_ = math.floor(j - 7.0f * x_); // math.mod(j,N)

            float4 x = x_ * ns.x + ns.yyyy;
            float4 y = y_ * ns.x + ns.yyyy;
            float4 h = 1.0f - math.abs(x) - math.abs(y);

            float4 b0 = new float4(x.xy, y.xy);
            float4 b1 = new float4(x.zw, y.zw);

            //float4 s0 = float4(math.lessThan(b0,0.0))*2.0 - 1.0;
            //float4 s1 = float4(math.lessThan(b1,0.0))*2.0 - 1.0;
            float4 s0 = math.floor(b0) * 2.0f + 1.0f;
            float4 s1 = math.floor(b1) * 2.0f + 1.0f;
            float4 sh = -math.step(h, new float4(0.0f));

            float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
            float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;

            float3 p0 = new float3(a0.xy, h.x);
            float3 p1 = new float3(a0.zw, h.y);
            float3 p2 = new float3(a1.xy, h.z);
            float3 p3 = new float3(a1.zw, h.w);

            //Normalise gradients
            float4 norm = taylorInvSqrt(new float4(math.dot(p0, p0), math.dot(p1, p1), math.dot(p2, p2), math.dot(p3, p3)));
            p0 *= norm.x;
            p1 *= norm.y;
            p2 *= norm.z;
            p3 *= norm.w;

            // Mix final noise value
            float4 m = math.max(0.6f - new float4(math.dot(x0, x0), math.dot(x1, x1), math.dot(x2, x2), math.dot(x3, x3)), 0.0f);
            float4 m2 = m * m;
            float4 m4 = m2 * m2;
            float4 pdotx = new float4(math.dot(p0, x0), math.dot(p1, x1), math.dot(p2, x2), math.dot(p3, x3));

            // Determath.mine noise gradient
            float4 temp = m2 * m * pdotx;
            gradient = -8.0f * (temp.x * x0 + temp.y * x1 + temp.z * x2 + temp.w * x3);
            gradient += m4.x * p0 + m4.y * p1 + m4.z * p2 + m4.w * p3;
            gradient *= 42.0f;

            return 42.0f * math.dot(m4, pdotx);
        }
    }
}
