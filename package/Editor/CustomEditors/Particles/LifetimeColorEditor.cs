
using JetBrains.Annotations;

namespace Unity.Tiny 
{
    [TinyCustomEditor(CoreGuids.Particles.LifetimeColor)]
    [UsedImplicitly]
    internal class LifetimeColorEditor : ParticleComponentEditor
    {
        public LifetimeColorEditor(TinyContext context)
            : base(context) { }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            ShowParticleEmitterWarning(ref context);
            return base.Visit(ref context);
        }
    }
}

