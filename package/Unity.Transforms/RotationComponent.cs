using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Transforms
{
    /// <summary>
    /// If Attached, in local space (relative to parent)
    /// If not Attached, in world space.
    /// </summary>
    [Serializable]
    public struct Rotation : IComponentData
    {
        public Quaternion Value;
    }

    public class RotationComponent : ComponentDataWrapper<Rotation>
    {
    }
}
