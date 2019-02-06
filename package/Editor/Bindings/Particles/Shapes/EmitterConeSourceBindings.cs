using System;
using JetBrains.Annotations;
using Unity.Tiny.Runtime.Particles;
using UnityEngine;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Particles.EmitterConeSource,
        CoreGuids.Particles.ParticleEmitter)]
    [WithoutComponent(
        CoreGuids.Particles.EmitterCircleSource,
        CoreGuids.Particles.EmitterBoxSource
    )]
    [BindingDependency(
        typeof(ParticleEmitterBindings))]
    [UsedImplicitly]
    internal class EmitterConeSourceBindings : BindingProfile
    {
        public override void Transfer(TinyEntity entity)
        {
            var emitterConeSource = entity.GetComponent<TinyEmitterConeSource>();
            var particleSystem = GetComponent<ParticleSystem>(entity);
            var main = particleSystem.main;

            var shape = particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.enabled = true;

            shape.radius = emitterConeSource.radius;
            shape.angle = emitterConeSource.angle * Mathf.Rad2Deg;
            
            var boxRotation = shape.rotation;
            boxRotation.x = -90;
            boxRotation.y = -90;
            shape.rotation = boxRotation;
            
            main.startSpeed = new ParticleSystem.MinMaxCurve(emitterConeSource.speed, emitterConeSource.speed);
            
            var emission = particleSystem.emission;
            emission.enabled = true;
        }
        
        public override void UnloadBindings(TinyEntity entity)
        {
            var particleSystem = GetComponent<ParticleSystem>(entity);
            var main = particleSystem.main;

            var shape = particleSystem.shape;
            shape.enabled = false;

            shape.radius = 0.0f;
            shape.angle = 0.0f;
            
            shape.rotation = Vector3.zero;
            
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.0f, 0.0f);
            
            var emission = particleSystem.emission;
            emission.enabled = true;
        }
    }
}
