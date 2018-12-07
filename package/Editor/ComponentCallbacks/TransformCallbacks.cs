

using JetBrains.Annotations;
using System.Linq;

using UnityEngine;


namespace Unity.Tiny
{
    [TinyComponentCallback(CoreGuids.Core2D.TransformNode)]
    [UsedImplicitly]
    internal class TransformCallbacks : ComponentCallback
    {
        protected override void OnRemoveComponent(TinyEntity entity, TinyObject component)
        {
            // We need to set the parent of the associated Unity Object to null.
            // And potentially move them outside of view.

            entity.SetParent(TinyEntity.Reference.None);
            var children = entity.EntityGroup.Entities
                .Deref(entity.Registry)
                .Where(e =>
                {
                    var t = e.GetComponent(TypeRefs.Core2D.TransformNode);
                    return null != t && (t.GetProperty<TinyEntity.Reference>("parent")).Equals((TinyEntity.Reference)entity);
                });

            foreach (var child in children)
            {
                child.SetParent(TinyEntity.Reference.None);
            }
        }
    }

    [TinyComponentCallback(CoreGuids.Core2D.TransformLocalPosition)]
    [UsedImplicitly]
    internal class TransformLocalPositionCallbacks : ComponentCallback
    {
        protected override void OnRemoveComponent(TinyEntity entity, TinyObject component)
        {
            entity.View.transform.localPosition = Vector3.zero;
        }
    }

    [TinyComponentCallback(CoreGuids.Core2D.TransformLocalRotation)]
    [UsedImplicitly]
    internal class TransformLocalRotationCallbacks : ComponentCallback
    {
        protected override void OnRemoveComponent(TinyEntity entity, TinyObject component)
        {
            entity.View.transform.localRotation = Quaternion.identity;
        }
    }

    [TinyComponentCallback(CoreGuids.Core2D.TransformLocalScale)]
    [UsedImplicitly]
    internal class TransformLocalScaleCallbacks : ComponentCallback
    {
        protected override void OnRemoveComponent(TinyEntity entity, TinyObject component)
        {
            entity.View.transform.localScale = Vector3.one;
        }
    }
}

