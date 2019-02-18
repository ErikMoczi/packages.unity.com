using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.Rendering
{
    /// <summary>
    /// Render Mesh with Material (must be instanced material) by object to world matrix.
    /// Specified by the LocalToWorld associated with Entity.
    /// </summary>
    [Serializable]
    public struct RenderMesh : ISharedComponentData
    {
        public Mesh                 mesh;
        public Material             material;
        public int                  subMesh;

        [LayerField]
        public int                  layer;

        public ShadowCastingMode    castShadows;
        public bool                 receiveShadows;
    }

    public class RenderMeshProxy : SharedComponentDataProxy<RenderMesh> { }
}
