using JetBrains.Annotations;
using Unity.Tiny.Runtime.Particles;
using UnityEngine;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Particles.LifetimeColor,
        CoreGuids.Particles.ParticleEmitter)]
    [BindingDependency(
        typeof(ParticleEmitterBindings))]
    [UsedImplicitly]
    internal class LifetimeColorBindings : BindingProfile
    {
        public override void Transfer(TinyEntity entity)
        {
            var lifetimeColor = entity.GetComponent<TinyLifetimeColor>();
            var module = GetComponent<ParticleSystem>(entity).colorOverLifetime;
            module.enabled = true;
            module.color = new ParticleSystem.MinMaxGradient(lifetimeColor.curve);
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            var module = GetComponent<ParticleSystem>(entity).colorOverLifetime;
            module.enabled = false;
        }
    }
}