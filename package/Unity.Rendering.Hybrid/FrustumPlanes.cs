using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Rendering
{
    public struct FrustumPlanes
    {
        public enum IntersectResult
        {
            Out,
            In,
            Partial
        };

        static public void FromCamera(Camera camera, NativeArray<float4> planes)
        {
            Plane[] sourcePlanes = GeometryUtility.CalculateFrustumPlanes(camera);

            for (int i = 0; i < 6; ++i)
            {
                planes[i] = new float4(sourcePlanes[i].normal.x, sourcePlanes[i].normal.y, sourcePlanes[i].normal.z,
                    sourcePlanes[i].distance);
            }
        }
        
        static public IntersectResult Intersect(NativeArray<float4> cullingPlanes, AABB a)
        {
            float3 m = a.Center;
            float3 extent = a.Extents;

            var inCount = 0;
            for (int i = 0; i < cullingPlanes.Length; i++)
            {
                float3 normal = cullingPlanes[i].xyz;
                float dist = math.dot(normal, m) + cullingPlanes[i].w;
                float radius = math.dot(extent, math.abs(normal));
                if (dist + radius <= 0)
                    return IntersectResult.Out;
                
                if (dist > radius)
                    inCount++;
                
            }
            
            return (inCount == cullingPlanes.Length) ? IntersectResult.In : IntersectResult.Partial;
        }
        
        static public IntersectResult Intersect(NativeArray<float4> planes, float3 center, float radius)
        {
            var inCount = 0;

            for (int i = 0; i < planes.Length; i++)
            {
                var d = math.dot(planes[i].xyz, center) + planes[i].w;
                if (d < -radius)
                {
                    return IntersectResult.Out;
                }

                if (d > radius)
                {
                    inCount++;
                }
            }

            return (inCount == planes.Length) ? IntersectResult.In : IntersectResult.Partial;
        }
    }

}
