

using JetBrains.Annotations;

using UnityEngine;

using Unity.Tiny.Runtime.Core2D;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Core2D.TransformNode)]
    [UsedImplicitly]
    internal class TransformBinding : BindingProfile
    {
        public override void Transfer(TinyEntity entity)
        {
            // Parenting should be handled by the entity graph.
        }
    }

    [WithComponent(
        CoreGuids.Core2D.TransformLocalPosition)]
    [UsedImplicitly]
    internal class TransformPositionBinding : BindingProfile
    {
        public override void Transfer(TinyEntity entity)
        {
            var transform = GetComponent<Transform>(entity);
            transform.localPosition = entity.GetComponent<TinyTransformLocalPosition>().position;
        }
    }

    [WithComponent(
        CoreGuids.Core2D.TransformLocalRotation)]
    [UsedImplicitly]
    internal class TransformRotationBinding : BindingProfile
    {
        public override void Transfer(TinyEntity entity)
        {
            var transform = GetComponent<Transform>(entity);
            var tinyTransform = entity.GetComponent<TinyTransformLocalRotation>();
            transform.localRotation = tinyTransform.rotation;
        }
    }

    [WithComponent(
        CoreGuids.Core2D.TransformLocalScale)]
    [UsedImplicitly]
    internal class TransformScaleBinding : BindingProfile
    {
        public override void Transfer(TinyEntity entity)
        {
            var transform = GetComponent<Transform>(entity);
            var tinyTransform = entity.GetComponent<TinyTransformLocalScale>();
            transform.localScale = tinyTransform.scale;
        }
    }
}

