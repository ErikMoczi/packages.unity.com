using Unity.Collections;
using UnityEngine.Jobs;

namespace UnityEngine.XR.MagicLeap.Rendering
{
    internal static class RenderingJobs
    {
        public struct CalculateDistancesJob : IJobParallelForTransform
        {
            public CalculateDistancesJob(NativeArray<float> dist, Vector3 origin)
            {
                Distance = dist;
                Origin = origin;
            }
            public NativeArray<float> Distance;
            public Vector3 Origin;

            public void Execute(int index, TransformAccess transform)
            {
                Distance[index] = Vector3.Distance(Origin, transform.position);
            }
        }
    }
}
