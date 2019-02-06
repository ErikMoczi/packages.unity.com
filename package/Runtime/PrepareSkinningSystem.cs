using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine.Jobs;
using Unity.Transforms;
using UnityEngine.Experimental.U2D.Common;

namespace UnityEngine.Experimental.U2D.Animation
{
    [UnityEngine.ExecuteInEditMode]
    [UpdateAfter(typeof(UnityEngine.Experimental.PlayerLoop.PreLateUpdate))]
    public class PrepareSkinningSystem : ComponentSystem
    {
        ComponentGroup m_ComponentGroup;

        protected override void OnCreateManager()
        {
            m_ComponentGroup = GetComponentGroup(typeof(SpriteSkin), typeof(WorldToLocal), typeof(SpriteComponent), typeof(Vertex), typeof(BoneTransform));
        }

        protected override void OnUpdate()
        {
            var entities = m_ComponentGroup.GetEntityArray();
            var spriteSkinComponents = m_ComponentGroup.GetComponentArray<SpriteSkin>();
            var spriteComponents = m_ComponentGroup.GetSharedComponentDataArray<SpriteComponent>();
            var worldToLocalComponents = m_ComponentGroup.GetComponentDataArray<WorldToLocal>();
            var vertexBuffers = m_ComponentGroup.GetBufferArray<Vertex>();
            var boneTransformBuffers = m_ComponentGroup.GetBufferArray<BoneTransform>();

            for (var i = 0; i < entities.Length; ++i)
            {
                var vertexBuffer = vertexBuffers[i];
                var boneTransformBuffer = boneTransformBuffers[i];
                var currentSprite = spriteComponents[i].Value;
                var currentWorldToLocal = worldToLocalComponents[i];
                Sprite sprite = null;
                var entity = entities[i];
                var spriteSkin = spriteSkinComponents[i];
                
                if (spriteSkin == null)
                    continue;
                    
                var spriteRenderer = spriteSkin.spriteRenderer;
                var isValid = spriteRenderer.enabled && spriteSkin.isValid;
                var isVisible = spriteRenderer.isVisible || spriteSkin.ForceSkinning;

                if (!isValid)
                    SpriteRendererDataAccessExtensions.DeactivateDeformableBuffer(spriteRenderer);
                else if (isVisible)
                {
                    spriteSkin.ForceSkinning = false;
                    sprite = spriteRenderer.sprite;
                    float4x4 worldToLocal = spriteSkin.transform.worldToLocalMatrix;

                    if (vertexBuffer.Length != sprite.GetVertexCount())
                    {
                        vertexBuffer = PostUpdateCommands.SetBuffer<Vertex>(entity);
                        vertexBuffer.ResizeUninitialized(sprite.GetVertexCount());
                    }

                    InternalEngineBridge.SetDeformableBuffer(spriteRenderer, vertexBuffer.Reinterpret<Vector3>().ToNativeArray());

                    if (boneTransformBuffer.Length != spriteSkin.boneTransforms.Length)
                    {
                        boneTransformBuffer = PostUpdateCommands.SetBuffer<BoneTransform>(entity);
                        boneTransformBuffer.ResizeUninitialized(spriteSkin.boneTransforms.Length);
                    }

                    for (var j = 0; j < boneTransformBuffer.Length; ++j)
                        boneTransformBuffer[j] = new BoneTransform() { Value = spriteSkin.boneTransforms[j].localToWorldMatrix };

                    PostUpdateCommands.SetComponent<WorldToLocal>(entity, new WorldToLocal() { Value = worldToLocal });
                }

                if (currentSprite != sprite)
                    PostUpdateCommands.SetSharedComponent<SpriteComponent>(entity, new SpriteComponent() { Value = sprite });

                if (!spriteRenderer.enabled)
                    spriteSkin.ForceSkinning = true;
            }
        }
    }
}
