using JetBrains.Annotations;

namespace Unity.Tiny
{
    [TinyRequiredComponent(CoreGuids.Core2D.Camera2D)]
    [UsedImplicitly]
    internal class Camera2DRequirements : TinyComponentRequirement
    {
        public override void AddRequiredComponents(TinyEntity entity)
        {
            if (!HasComponents(entity, TypeRefs.Core2D.TransformNode))
            {
                AddComponentAtIndex(entity, TypeRefs.Core2D.TransformNode, 0);
            }
        }
    }
}