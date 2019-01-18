using Unity.Rendering;
using Unity.Transforms;
using Unity.Entities;
using UnityEngine;

class MeshRendererConversion : GameObjectConversionSystem
{
    const bool AttachToPrimaryEntityForSingleMaterial = true;
    
    protected override void OnUpdate()
    {
        ForEach((MeshRenderer meshRenderer, MeshFilter meshFilter) =>
        {
            var entity = GetPrimaryEntity(meshRenderer);

            var dst = new RenderMesh();
            dst.mesh = meshFilter.sharedMesh;
            dst.castShadows = meshRenderer.shadowCastingMode;
            dst.receiveShadows = meshRenderer.receiveShadows;

            var materials = meshRenderer.sharedMaterials;

            if (materials.Length == 1 && AttachToPrimaryEntityForSingleMaterial)
            {
                dst.material = materials[0];
                dst.subMesh = 0;
            
                DstEntityManager.AddSharedComponentData(entity, dst);
            }
            else
            {
                for (int m = 0; m != materials.Length; m++)
                {
                    var meshEntity = CreateAdditionalEntity(meshRenderer);
                
                    dst.material = materials[m];
                    dst.subMesh = m;
                
                    DstEntityManager.AddSharedComponentData(meshEntity, dst);
                
                    //@TODO: Shouldn't be necessary to add Position Component, but looks like TransformSystem doesn't setup LocalToWorld otherwise...
                    DstEntityManager.AddComponentData(meshEntity, new Position());
                
                    // Parent it
                    var attach = CreateAdditionalEntity(meshRenderer);
                    DstEntityManager.AddComponentData(attach, new Attach {Parent = entity, Child = meshEntity});
                }
            }

            
            //@TODO: Transform system should handle RenderMeshFlippedWindingTag
            // Flag this thing as positively or negatively scaled, so we can batch the renders correctly for the static case.
            //if (math.determinant(localToWorld) < 0.0)
            //    entityManager.AddComponent(meshEnt, ComponentType.Create<RenderMeshFlippedWindingTag>());
        });
    }
}
