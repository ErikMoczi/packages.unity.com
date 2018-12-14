
using JetBrains.Annotations;

namespace Unity.Tiny 
{
    [TinyCustomEditor(CoreGuids.Particles.LifetimeVelocity)]
    [UsedImplicitly]
    internal class LifetimeVelocityEditor : ParticleComponentEditor
    {
        public LifetimeVelocityEditor(TinyContext context)
            : base(context) { }


        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            ShowParticleEmitterWarning(ref context);
            return base.Visit(ref context);
        }
    }
}

