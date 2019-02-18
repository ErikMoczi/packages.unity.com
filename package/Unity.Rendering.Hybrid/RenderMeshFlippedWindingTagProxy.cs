using System;
using Unity.Entities;

namespace Unity.Rendering
{
    public struct RenderMeshFlippedWindingTag : IComponentData
    {
    }

    [UnityEngine.DisallowMultipleComponent]
    public class RenderMeshFlippedWindingTagProxy : ComponentDataProxy<RenderMeshFlippedWindingTag> { }
}
