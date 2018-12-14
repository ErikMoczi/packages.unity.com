using JetBrains.Annotations;
using Unity.Tiny.Runtime.Particles;
using UnityEngine;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Particles.EmitterInitialAngularVelocity,
        CoreGuids.Particles.ParticleEmitter)]
    [BindingDependency(
        typeof(ParticleEmitterBindings))]
    [UsedImplicitly]
    internal class EmitterInitialAngularVelocityBindings : BindingProfile
    {
        public override void Transfer(TinyEntity entity)
        {
            var emitterInitialAngularVelocity = entity.GetComponent<TinyEmitterInitialAngularVelocity>();
            var angularVelocity = emitterInitialAngularVelocity.angularVelocity; 
            var particleSystem = GetComponent<ParticleSystem>(entity);
            var rotation = particleSystem.rotationOverLifetime;
            rotation.separateAxes = false;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(angularVelocity.start, angularVelocity.end);
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            var particleSystem = GetComponent<ParticleSystem>(entity);
            var rotation = particleSystem.rotationOverLifetime;
            rotation.separateAxes = false;
            rotation.enabled = false;
            rotation.z = new ParticleSystem.MinMaxCurve(0.0f, 0.0f);
        }
    }
}