using JetBrains.Annotations;
using Unity.Tiny.Runtime.Particles;
using UnityEngine;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Particles.LifetimeAngularVelocity,
        CoreGuids.Particles.ParticleEmitter)]
    [BindingDependency(
        typeof(ParticleEmitterBindings))]
    [UsedImplicitly]
    internal class LifetimeRotationBindings : BindingProfile
    {
        private static ParticleSystem.MinMaxCurve s_NoCurve =
            new ParticleSystem.MinMaxCurve(0.0f, AnimationCurve.Constant(0, 1, 0));

        public override void Transfer(TinyEntity entity)
        {
            var lifetimeRotation = entity.GetComponent<TinyLifetimeAngularVelocity>();
            var module = GetComponent<ParticleSystem>(entity).rotationOverLifetime;
            var curve = lifetimeRotation.curve;
            module.enabled = true;
            module.separateAxes = true;
            module.x = module.y = s_NoCurve;
            module.z = new ParticleSystem.MinMaxCurve(Mathf.Deg2Rad, curve);
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            var module = GetComponent<ParticleSystem>(entity).rotationOverLifetime;
            module.enabled = false;
        }
    }
}
