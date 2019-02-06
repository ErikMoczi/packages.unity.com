

using JetBrains.Annotations;
using Unity.Tiny.Runtime.Particles;
using UnityEngine;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Particles.EmitterInitialRotation,
        CoreGuids.Particles.ParticleEmitter)]
    [BindingDependency(
        typeof(ParticleEmitterBindings))]
    [UsedImplicitly]
    internal class EmitterInitialRotationBindings : BindingProfile
    {
        public override void Transfer(TinyEntity entity)
        {
            var component = entity.GetComponent<TinyEmitterInitialRotation>();
            var module = entity.View.GetComponent<ParticleSystem>().main;
            var rotation = component.angle;
            rotation.start *= Mathf.Rad2Deg;
            rotation.end *= Mathf.Rad2Deg;
            SetRotationRange(module, rotation);
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            var module = entity.View.GetComponent<ParticleSystem>().main;
            SetRotationRange(module, new Range(0.0f, 0.0f));
        }

        private static void SetRotationRange(ParticleSystem.MainModule module, Range rotation)
        {
            module.startRotation3D = true;
            module.startRotationZ = new ParticleSystem.MinMaxCurve(rotation.start * Mathf.Deg2Rad, rotation.end * Mathf.Deg2Rad);
        }
    }
}

