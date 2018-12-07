using JetBrains.Annotations;
using Unity.Tiny.Runtime.Particles;
using UnityEngine;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Particles.LifetimeVelocity,
        CoreGuids.Particles.ParticleEmitter)]
    [BindingDependency(
        typeof(ParticleEmitterBindings))]
    [UsedImplicitly]
    internal class LifetimeVelocityBindings : BindingProfile
    {
        public override void Transfer(TinyEntity entity)
        {
            var lifetimeVelocity = entity.GetComponent<TinyLifetimeVelocity>();
            var module = GetComponent<ParticleSystem>(entity).velocityOverLifetime;
            var curve = lifetimeVelocity.curve;
            
            module.enabled = true;
            module.x = new ParticleSystem.MinMaxCurve(1, curve.x);
            module.y = new ParticleSystem.MinMaxCurve(1, curve.y);
            module.z = new ParticleSystem.MinMaxCurve(1, curve.z);
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            var module = GetComponent<ParticleSystem>(entity).velocityOverLifetime;
            module.enabled = false;
        }
    }
}
