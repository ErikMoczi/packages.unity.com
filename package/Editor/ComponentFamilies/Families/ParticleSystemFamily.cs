
using JetBrains.Annotations;

namespace Unity.Tiny
{
    [ComponentFamily(
         requiredGuids: new []
         {
             CoreGuids.Particles.ParticleEmitter,
         },
         optionalGuids: new []
         {
             // Shapes
             CoreGuids.Particles.EmitterBoxSource,
             CoreGuids.Particles.EmitterCircleSource,
             CoreGuids.Particles.EmitterConeSource,
             
             // Initial values
             CoreGuids.Particles.EmitterInitialRotation,
             CoreGuids.Particles.EmitterInitialScale,
             CoreGuids.Particles.EmitterInitialVelocity,
             CoreGuids.Particles.EmitterInitialColor,
             CoreGuids.Particles.EmitterInitialAngularVelocity,
             
             // Lifetime values
             CoreGuids.Particles.LifetimeAngularVelocity,
             CoreGuids.Particles.LifetimeScale,
             CoreGuids.Particles.LifetimeVelocity,
             CoreGuids.Particles.LifetimeColor,
             CoreGuids.Particles.LifetimeSpeedMultiplier,
             
             // Emission
             CoreGuids.Particles.BurstEmission
         }),
     UsedImplicitly]
    internal class ParticleSystemFamily : ComponentFamily
    {
        public override string Name => "Particle System";

        public ParticleSystemFamily(FamilyDefinition definition, TinyContext tinyContext)
            : base(definition, tinyContext) { }
    }
}
