using JetBrains.Annotations;
using Unity.Tiny.Runtime.Particles;
using UnityEngine;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Particles.EmitterInitialVelocity,
        CoreGuids.Particles.ParticleEmitter,
        CoreGuids.Particles.EmitterBoxSource)]
    [BindingDependency(
        typeof(ParticleEmitterBindings))]
    [UsedImplicitly]
    internal class EmitterInitialVelocityBindings : BindingProfile
    {
        public override void Transfer(TinyEntity entity)
        {
            if (entity.HasComponent<TinyEmitterInitialVelocity>())
            {
                var initialVelocity = entity.GetComponent<TinyEmitterInitialVelocity>();
                var particleSystem = GetComponent<ParticleSystem>(entity);
                var main = particleSystem.main;

                var velocity = initialVelocity.velocity;
                var direction = Mathf.Atan2(velocity.y, velocity.x);

                main.startSpeed = velocity.magnitude;

                //direction per particle is set through the emitter orientation and random range given,
                //so we need to reorient the shape itself for closest match with runtime
                var shape = particleSystem.shape;
                var rotation = shape.rotation;
                rotation.x = direction;
                shape.rotation = -rotation * Mathf.Rad2Deg;
                shape.randomDirectionAmount = 0.0f;
            }
        }
    }
}
