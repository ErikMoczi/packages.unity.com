
using JetBrains.Annotations;

namespace Unity.Tiny
{
    [ComponentFamily(
        requiredGuids: new []
        {
            CoreGuids.Core2D.Sprite2DRenderer
        },
        optionalGuids: new []
        {
            CoreGuids.Core2D.Sprite2DRendererOptions,
            CoreGuids.Core2D.SortingGroup,
            CoreGuids.Core2D.LayerSorting,
        }),
    UsedImplicitly]
    internal class SpriteRendererFamily : ComponentFamily
    {
        public override string Name => "Sprite Renderer";

        public SpriteRendererFamily(FamilyDefinition definition, TinyContext tinyContext)
            : base(definition, tinyContext) { }
    }
}
