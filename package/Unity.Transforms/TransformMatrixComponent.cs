using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Transforms
{
    /// <summary>
    /// Store (calculated) object to world matrix.
    /// Required by other systems. e.g. MeshInstanceRenderer
    /// </summary>
    public struct TransformMatrix : IComponentData
    {
        public float4x4 Value;
    }

    public class TransformMatrixComponent : ComponentDataWrapper<TransformMatrix> { }
}
