using JetBrains.Annotations;
using Unity.Tiny.Runtime.Particles;
using UnityEngine;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Particles.EmitterInitialColor,
        CoreGuids.Particles.ParticleEmitter)]
    [BindingDependency(
        typeof(ParticleEmitterBindings))]
    [UsedImplicitly]
    internal class EmitterInitialColorBindings : BindingProfile
    {
        public override void Transfer(TinyEntity entity)
        {
            var component = entity.GetComponent<TinyEmitterInitialColor>();
            var module = entity.View.GetComponent<ParticleSystem>().main;
            var startColor = module.startColor;
            startColor.mode = ParticleSystemGradientMode.RandomColor;
            startColor.gradient = component.curve;
            module.startColor = startColor;
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            var main = entity.View.GetComponent<ParticleSystem>().main;
            main.startColor = new ParticleSystem.MinMaxGradient(new Gradient());
        }
    }
}