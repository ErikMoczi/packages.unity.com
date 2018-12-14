
using JetBrains.Annotations;

namespace Unity.Tiny 
{
    [TinyCustomEditor(CoreGuids.Particles.LifetimeSpeedMultiplier)]
    [UsedImplicitly]
    internal class LifetimeSpeedMultiplierEditor : ParticleComponentEditor
    {
        public LifetimeSpeedMultiplierEditor(TinyContext context)
            : base(context) { }


        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            ShowParticleEmitterWarning(ref context);
            return base.Visit(ref context);
        }
    }
}
