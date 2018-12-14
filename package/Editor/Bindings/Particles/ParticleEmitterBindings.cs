using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using Unity.Tiny.Runtime.Core2D;
using UnityEngine;

using Unity.Tiny.Runtime.Particles;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Particles.ParticleEmitter,
        CoreGuids.Core2D.TransformNode)]
    [UsedImplicitly]
    internal class ParticleEmitterBindings : BindingProfile
    {
        private static readonly Vector3[] k_TempVerts = new Vector3[4];

        private static readonly Vector2[] k_TempUVs =
            {new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)};
        private static readonly int[] k_TempTris = { 0, 2, 1, 0, 3, 2 };

        private static readonly Dictionary<TinyEntity.Reference, TinyEntity.Reference> k_TemporaryLink =
            new Dictionary<TinyEntity.Reference, TinyEntity.Reference>();

        public override void LoadBindings(TinyEntity entity)
        {
            AddMissingComponent<ParticleSystem>(entity, (system) =>
            {
                var renderer = system.GetComponent<ParticleSystemRenderer>();
                renderer.material = new Material(Shader.Find("Tiny/Particle2D"))
                { 
                    renderQueue = 3000
                };
                renderer.mesh = GenerateQuad();
                var emission = GetComponent<ParticleSystem>(entity).emission;
                emission.enabled = entity.GetComponent<TinyEmitterBoxSource>().IsValid;
            });
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            RemoveComponent<ParticleSystem>(entity);
        }

        public override void Transfer(TinyEntity entity)
        {
            var registry = entity.Registry;
            var particleSystem = GetComponent<ParticleSystem>(entity);

            var component = entity.GetComponent<TinyParticleEmitter>();
            var main = particleSystem.main;
            main.maxParticles = (int)component.maxParticles;

            // float => bool conversion.
            var lifetime = component.lifetime;

            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.start, lifetime.end);

            var emission = particleSystem.emission;
            emission.rateOverTime = component.emitRate;

            InitializeSpeed(entity, particleSystem);
            InitializeRotation(entity, particleSystem);
            
            if (component.attachToEmitter)
            {
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
            }
            else
            {
                main.simulationSpace = ParticleSystemSimulationSpace.World;
            }
            
            // Renderer settings
            {
                var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Mesh;

                var particleRef = component.particle;
                var particle = particleRef.Dereference(registry);
                var sprite2DRenderer = particle.GetComponent<Runtime.Core2D.TinySprite2DRenderer>();

                var selfRef = entity.Ref;
                if (k_TemporaryLink.TryGetValue(entity.Ref, out _))
                {
                    Bindings.RemoveTemporaryDependency(particleRef, selfRef);
                }

                if (null == particle)
                {
                    k_TemporaryLink.Remove(selfRef);
                    SyncMesh(renderer.mesh, null, Vector3.zero);
                }
                else
                {
                    k_TemporaryLink[selfRef] = particleRef;
                    Bindings.SetTemporaryDependency(particleRef, selfRef);

                    Sprite sprite;
                    var scaleCompensation = 1.0f;

                    if (!sprite2DRenderer.IsValid || null == sprite2DRenderer.sprite)
                    {
                        sprite = TinySprites.WhiteSprite;
                    }
                    else
                    {
                        sprite = sprite2DRenderer.sprite;
                    }
                    scaleCompensation = sprite.pixelsPerUnit;

                    var particleSpriteRenderer = GetComponent<SpriteRenderer>(particle);
                    if (null == particleSpriteRenderer)
                    {
                        SyncMesh(renderer.mesh, sprite, Vector3.zero);
                        return;
                    }

                    var localScale = particle.GetComponent<TinyTransformLocalScale>();
                    var options = particle.GetComponent<TinySprite2DRendererOptions>();

                    var actualScale = Vector3.one;
                    if (options.IsValid)
                    {
                        actualScale.x = options.size.x * scaleCompensation / sprite.rect.width;
                        actualScale.y = options.size.y * scaleCompensation / sprite.rect.height;
                    }
                    
                    if (localScale.IsValid)
                    {
                        actualScale.x *= localScale.scale.x;
                        actualScale.y *= localScale.scale.y;
                    }

                    SyncMesh(renderer.mesh, sprite, actualScale);

                    var sprite2DBlock = new MaterialPropertyBlock();
                    particleSpriteRenderer.GetPropertyBlock(sprite2DBlock);

                    var particle2DBlock = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(particle2DBlock);
                    particle2DBlock.Clear();

                    particle2DBlock.SetTexture("_MainTex", sprite.texture);

                    main.startColor = sprite2DBlock.GetColor("_Color");


                    renderer.SetPropertyBlock(particle2DBlock);

                    // Transfer blending mode
                    var particleMaterial = particleSpriteRenderer.sharedMaterial;
                    renderer.sharedMaterial.SetFloat("_SrcMode", particleMaterial.GetFloat("_SrcMode"));
                    renderer.sharedMaterial.SetFloat("_DstMode", particleMaterial.GetFloat("_DstMode"));

                    var sortingLayer = particle.GetComponent(TypeRefs.Core2D.LayerSorting);
                    if (null != sortingLayer)
                    {
                        renderer.sortingLayerID = sortingLayer.GetProperty<int>("layer");
                        renderer.sortingOrder = sortingLayer.GetProperty<int>("order");
                    }
                }
            }
        }

        private static void SyncMesh(Mesh particle, Sprite sprite, Vector3 localScale)
        {
            particle.Clear();
            if (null == sprite)
            {
                particle.vertices = k_TempVerts;
                particle.triangles = k_TempTris;
                particle.uv = k_TempUVs;
                
            }
            else
            {
                particle.vertices = sprite.vertices.Select(v => new Vector3(v.x * localScale.x, v.y * localScale.y, 0.0f)).ToArray();
                particle.triangles = sprite.triangles.Select(t => (int)t).ToArray();
                particle.uv = sprite.uv;
            }
        }

        private static Mesh GenerateQuad()
        {
            return new Mesh
            {
                vertices = k_TempVerts,
                triangles = k_TempTris,
                uv = k_TempUVs
            };
        }

        private void InitializeRotation(TinyEntity entity, ParticleSystem particleSystem)
        {
            if (!entity.HasComponent<TinyEmitterInitialVelocity>())
            {
                var shape = particleSystem.shape;
                var rotation = shape.rotation;
                rotation.x = 0.0f;
                shape.rotation = rotation;
                shape.randomDirectionAmount = 0.0f;
            }

        }

        private void InitializeSpeed(TinyEntity entity, ParticleSystem particleSystem)
        {
            if (!entity.HasComponent<TinyEmitterInitialVelocity>())
            {
                var main = particleSystem.main;
                main.startSpeed = 0;
            }
        }
    }
}
