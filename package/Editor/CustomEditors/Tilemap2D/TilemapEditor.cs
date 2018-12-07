
using JetBrains.Annotations;

namespace Unity.Tiny
{
    [TinyCustomEditor(CoreGuids.Tilemap2D.Tilemap)]
    [UsedImplicitly]
    internal class TilemapEditor : ComponentEditor
    {
        public TilemapEditor(TinyContext context)
            : base(context) { }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            VisitField(ref context, "anchor");
            VisitField(ref context, "position");
            VisitField(ref context, "rotation");
            VisitField(ref context, "scale");
            VisitField(ref context, "cellSize");
            VisitField(ref context, "cellGap");
            return true;
        }

    }
}

