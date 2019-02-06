
using JetBrains.Annotations;

namespace Unity.Tiny
{
    [TinyCustomEditor(CoreGuids.Particles.EmitterBoxSource)]
    [UsedImplicitly]
    internal class EmitterBoxSourceEditor : ParticleComponentEditor
    {
        public EmitterBoxSourceEditor(TinyContext context)
            : base(context) { }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            ShowParticleEmitterWarning(ref context);
            return base.Visit(ref context);
        }

    }
}

