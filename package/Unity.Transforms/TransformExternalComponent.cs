using Unity.Entities;

namespace Unity.Transforms
{
    /// <summary>
    /// Any associated Transform components are ignored by the TransformSystem. (Assumed they are updated by an external system.)
    /// </summary>
    public struct TransformExternal : IComponentData
    {
    }

    public class TransformExternalComponent : ComponentDataWrapper<TransformExternal>
    {
    }
}
