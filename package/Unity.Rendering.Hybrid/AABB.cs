using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace Unity.Mathematics
{
    public struct MinMaxAABB
    {
        public float3 Min;
        public float3 Max;

        public static MinMaxAABB Empty
        {
            get { return new MinMaxAABB { Min = float3(float.PositiveInfinity), Max = float3(float.NegativeInfinity) }; }
        }

        public void Encapsulate(MinMaxAABB aabb)
        {
            Min = math.min(Min, aabb.Min);
            Max = math.max(Max, aabb.Max);
        }
        
        public void Encapsulate(float3 point)
        {
            Min = math.min(Min, point);
            Max = math.max(Max, point);
        }
        
        public static implicit operator MinMaxAABB(AABB aabb)
        {
            return new MinMaxAABB {Min = aabb.Center - aabb.Extents, Max = aabb.Center + aabb.Extents};
        }
        
        public static implicit operator AABB(MinMaxAABB aabb)
        {
            return new AABB { Center = (aabb.Min + aabb.Max) * 0.5F, Extents = (aabb.Max - aabb.Min) * 0.5F};
        }
    }

    
    //@TODO: Make Mathematics extensions package

    
    public struct AABB
    {
        public float3 Center;
        public float3 Extents;

        public float3 Size { get { return Extents * 2; } }


        public bool Contains(float3 point)
        {
            if (point[0] < Center[0] - Extents[0])
                return false;
            if (point[0] > Center[0] + Extents[0])
                return false;

            if (point[1] < Center[1] - Extents[1])
                return false;
            if (point[1] > Center[1] + Extents[1])
                return false;

            if (point[2] < Center[2] - Extents[2])
                return false;
            if (point[2] > Center[2] + Extents[2])
                return false;

            return true;
        }

        public bool Contains(AABB b)
        {
            return    Contains(b.Center + float3(-b.Extents.x, -b.Extents.y, -b.Extents.z))
                   && Contains(b.Center + float3(-b.Extents.x, -b.Extents.y,  b.Extents.z))
                   && Contains(b.Center + float3(-b.Extents.x,  b.Extents.y, -b.Extents.z))
                   && Contains(b.Center + float3(-b.Extents.x,  b.Extents.y,  b.Extents.z))
                   && Contains(b.Center + float3( b.Extents.x, -b.Extents.y, -b.Extents.z))
                   && Contains(b.Center + float3( b.Extents.x, -b.Extents.y,  b.Extents.z))
                   && Contains(b.Center + float3( b.Extents.x,  b.Extents.y, -b.Extents.z))
                   && Contains(b.Center + float3( b.Extents.x,  b.Extents.y,  b.Extents.z));
        }
        
        static float3 RotateExtents(float3 extents, float3 m0, float3 m1, float3 m2)
        {
            return math.abs(m0 * extents.x) + math.abs(m1 * extents.y) + math.abs(m2 * extents.z);
        }

        public static AABB Transform(float4x4 transform, AABB localBounds)
        {
            AABB transformed;
            transformed.Extents = RotateExtents(localBounds.Extents, transform.c0.xyz, transform.c1.xyz, transform.c2.xyz);
            transformed.Center = math.transform(transform, localBounds.Center);
            return transformed;
        }
        
        public static implicit operator AABB(Bounds bounds)
        {
            return new AABB { Center = bounds.center, Extents = bounds.extents};
        }
        
        public static implicit operator Bounds(AABB aabb)
        {
            return new Bounds { center = aabb.Center, extents = aabb.Extents};
        }
        /*
        public static AABB TransformPrecise(float4x4 transform, AABB localBounds)
        {
            var transformed = MinMaxAABB.Empty;

            Vector3f v[8];
            aabb.CalculateVertices(v);
            for (int i = 0; i < 8; i++)
            {
                Vector3f point = transform.MultiplyPoint3(v[i]);
                transformed.Encapsulate(point);
            }

            result = AABB(transformed);

            ASSERT_VALID_AABB(result);
        }
       */ 
        
    }
}