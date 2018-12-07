

using JetBrains.Annotations;


namespace Unity.Tiny
{
    [TinyRequiredComponent(CoreGuids.UILayout.UICanvas)]
    [UsedImplicitly]
    internal class UICanvasRequirements : TinyComponentRequirement
    {
        public override void AddRequiredComponents(TinyEntity entity)
        {
            if (!HasComponents(entity, TypeRefs.Core2D.TransformNode))
            {
                AddComponentAtIndex(entity, TypeRefs.Core2D.TransformNode, 0);
            }

            if (!HasComponents(entity, TypeRefs.Core2D.TransformLocalPosition))
            {
                AddComponentAtIndex(entity, TypeRefs.Core2D.TransformLocalPosition, 1);
            }

            if (!HasComponents(entity, TypeRefs.Core2D.TransformLocalRotation))
            {
                AddComponentAtIndex(entity, TypeRefs.Core2D.TransformLocalRotation, 2);
            }

            if (!HasComponents(entity, TypeRefs.Core2D.TransformLocalScale))
            {
                AddComponentAtIndex(entity, TypeRefs.Core2D.TransformLocalScale, 3);
            }

            if (!HasComponents(entity, TypeRefs.UILayout.RectTransform))
            {
                AddComponentAfter(entity, TypeRefs.UILayout.RectTransform, TypeRefs.Core2D.TransformNode);
            }
        }
    }
}

