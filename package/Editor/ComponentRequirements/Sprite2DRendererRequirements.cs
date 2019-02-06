

using JetBrains.Annotations;

namespace Unity.Tiny
{
    [TinyRequiredComponent(CoreGuids.Core2D.Sprite2DRenderer)]
    [UsedImplicitly]
    internal class Sprite2DRendererRequirements : TinyComponentRequirement
    {
        public override void AddRequiredComponents(TinyEntity entity)
        {
            if (HasComponents(entity, TypeRefs.UILayout.RectTransform))
            {
                var options = AddComponentAfter(entity, TypeRefs.Core2D.Sprite2DRendererOptions, TypeRefs.Core2D.Sprite2DRenderer);
                options.AssignPropertyFrom("drawMode", DrawMode.Stretch);
            }
        }
    }
}

