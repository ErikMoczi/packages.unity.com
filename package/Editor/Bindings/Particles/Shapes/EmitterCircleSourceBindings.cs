using JetBrains.Annotations;
using Unity.Tiny.Runtime.Particles;
using UnityEngine;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Particles.EmitterCircleSource,
        CoreGuids.Particles.ParticleEmitter)]
    [WithoutComponent(
        CoreGuids.Particles.EmitterBoxSource
        )]
    [BindingDependency(
        typeof(ParticleEmitterBindings))]
    [UsedImplicitly]
    internal class EmitterCircleSourceBindings : BindingProfile
    {
        public override void Transfer(TinyEntity entity)
        {
            var emitterCircleSource = entity.GetComponent<TinyEmitterCircleSource>();
            var particleSystem = GetComponent<ParticleSystem>(entity);
            var main = particleSystem.main;

            var shape = particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.enabled = true;

            shape.radius = emitterCircleSource.radius.end;
            shape.radiusThickness = 1 - emitterCircleSource.radius.start / emitterCircleSource.radius.end; 
            shape.scale = Vector3.one;
            
            main.startSpeed = new ParticleSystem.MinMaxCurve(emitterCircleSource.speed.start, emitterCircleSource.speed.end);
            
            var emission = particleSystem.emission;
            emission.enabled = true;
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            var particleSystem = GetComponent<ParticleSystem>(entity);
            var main = particleSystem.main;

            var shape = particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.enabled = false;
            shape.radius = 0.0f;
            shape.radiusThickness = 0.0f; 
            
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.0f, 0.0f);
            
            var emission = particleSystem.emission;
            emission.enabled = false;
        }
    }
}
