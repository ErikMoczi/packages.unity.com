
using JetBrains.Annotations;
using Unity.Tiny.Runtime.Particles;
using UnityEngine;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Particles.EmitterInitialScale,
        CoreGuids.Particles.ParticleEmitter)]
    [BindingDependency(
        typeof(ParticleEmitterBindings))]
    [UsedImplicitly]
    internal class EmitterInitialScaleBindings : BindingProfile
    {
        public override void Transfer(TinyEntity entity)
        {
            var initialScale = entity.GetComponent<TinyEmitterInitialScale>();
            var module = entity.View.GetComponent<ParticleSystem>().main;
            var scale = initialScale.scale;
            SetScaleRange(module, scale);
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            var module = entity.View.GetComponent<ParticleSystem>().main;
            SetScaleRange(module, new Range(1.0f, 1.0f));
        }

        private static void SetScaleRange(ParticleSystem.MainModule module, Range scale)
        {
            module.startSize = new ParticleSystem.MinMaxCurve(scale.start, scale.end);
        }
    }
}

