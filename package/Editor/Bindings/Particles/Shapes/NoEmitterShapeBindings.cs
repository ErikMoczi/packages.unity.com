using JetBrains.Annotations;
using UnityEngine;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Particles.ParticleEmitter)]
    [WithoutComponent(
        CoreGuids.Particles.EmitterConeSource,
        CoreGuids.Particles.EmitterCircleSource,
        CoreGuids.Particles.EmitterBoxSource
    )]
    [BindingDependency(
        typeof(ParticleEmitterBindings))]
    [UsedImplicitly]
    internal class NoEmitterShapeBindings : BindingProfile
    {
        public override void Transfer(TinyEntity entity)
        {
            var particleSystem = GetComponent<ParticleSystem>(entity);
            var emission = particleSystem.emission;
            emission.enabled = false;
        }
    }
}