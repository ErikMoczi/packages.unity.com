using JetBrains.Annotations;

namespace Unity.Tiny
{
    [TinyCustomEditor(CoreGuids.Tilemap2D.TilemapRenderer)]
    [UsedImplicitly]
    internal class TilemapRendererEditor : ComponentEditor
    {
        public TilemapRendererEditor(TinyContext context)
            : base(context) { }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            VisitField(ref context, "color");
            return true;
        }

    }
}

