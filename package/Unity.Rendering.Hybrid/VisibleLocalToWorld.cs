using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    // #TODO Bulk add/remove SystemStateComponentData
    public struct VisibleLocalToWorld : IComponentData
    {
        public float4x4 Value;
    };
}