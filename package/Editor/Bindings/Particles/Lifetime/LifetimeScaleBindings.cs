using JetBrains.Annotations;
using Unity.Tiny.Runtime.Particles;
using UnityEngine;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Particles.LifetimeScale,
        CoreGuids.Particles.ParticleEmitter)]
    [BindingDependency(
        typeof(ParticleEmitterBindings))]
    [UsedImplicitly]
    internal class LifetimeScaleBindings : BindingProfile
    {
        public override void Transfer(TinyEntity entity)
        {
            var lifetimeScale = entity.GetComponent<TinyLifetimeScale>();
            var module = GetComponent<ParticleSystem>(entity).sizeOverLifetime;

            module.enabled = true;
            module.separateAxes = false;
            module.size = new ParticleSystem.MinMaxCurve(1.0f, lifetimeScale.curve);
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            var module = GetComponent<ParticleSystem>(entity).sizeOverLifetime;
            module.enabled = false;
        }
    }
}
