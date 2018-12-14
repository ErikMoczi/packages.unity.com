
using JetBrains.Annotations;

namespace Unity.Tiny 
{
    [TinyCustomEditor(CoreGuids.Particles.LifetimeScale)]
    [UsedImplicitly]
    internal class LifetimeScaleEditor : ParticleComponentEditor
    {
        public LifetimeScaleEditor(TinyContext context)
            : base(context) { }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            ShowParticleEmitterWarning(ref context);
            return base.Visit(ref context);
        }
    }
}

