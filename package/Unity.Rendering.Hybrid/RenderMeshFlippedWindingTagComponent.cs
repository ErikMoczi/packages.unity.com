using System;
using Unity.Entities;

namespace Unity.Rendering
{
    [Serializable]
    public struct RenderMeshFlippedWindingTag : IComponentData
    {
    }

    [UnityEngine.DisallowMultipleComponent]
    public class RenderMeshFlippedWindingTagComponent : ComponentDataWrapper<RenderMeshFlippedWindingTag> { }
}
