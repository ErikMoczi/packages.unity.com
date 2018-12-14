using JetBrains.Annotations;
using Unity.Tiny.Runtime.Particles;
using UnityEngine;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Particles.BurstEmission,
        CoreGuids.Particles.ParticleEmitter)]
    [BindingDependency(
        typeof(ParticleEmitterBindings))]
    [UsedImplicitly]
    internal class BurstEmissionBindings : BindingProfile
    {
        public override void Transfer(TinyEntity entity)
        {
            var burstEmission = entity.GetComponent<TinyBurstEmission>();
            var particleSystem = GetComponent<ParticleSystem>(entity);
            var emission = particleSystem.emission;

            var burst = new ParticleSystem.Burst();
            burst.count = new ParticleSystem.MinMaxCurve(burstEmission.count.start, burstEmission.count.end);
            burst.repeatInterval = (burstEmission.interval.start + burstEmission.interval.end) / 2.0f;
            burst.minCount = (short) burstEmission.count.start;
            burst.minCount = (short) burstEmission.count.end;
            burst.cycleCount = burstEmission.cycles;
            emission.rateOverTime = 0;
            emission.SetBursts(new [] { burst });
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            var particleSystem = GetComponent<ParticleSystem>(entity);
            var emission = particleSystem.emission;
            emission.SetBurst(0, new ParticleSystem.Burst());
        }
    }
}