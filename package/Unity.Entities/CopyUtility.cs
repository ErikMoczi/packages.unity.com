using Unity.Collections;
using Unity.Jobs;

namespace Unity.Entities
{
    /// <summary>
    /// Copy ComponentDataArray to NativeArray Job.
    /// </summary>
    /// <typeparam name="T">Component data type stored in ComponentDataArray to be copied to NativeArray<T></typeparam>
    
    [ComputeJobOptimization]
    public struct CopyComponentData<T> : IJobParallelFor
    where T : struct, IComponentData
    {
        [ReadOnly] public ComponentDataArray<T> source;
        public NativeArray<T> results;

        public void Execute(int index)
        {
            results[index] = source[index];
        }
    }
    
    [ComputeJobOptimization]
    public struct CopyEntities : IJobParallelFor
    {
        [ReadOnly] public EntityArray source;
        public NativeArray<Entity> results;

        public void Execute(int index)
        {
            results[index] = source[index];
        }
    }
}
