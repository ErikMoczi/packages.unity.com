

using JetBrains.Annotations;
using UnityEngine;

namespace Unity.Tiny
{
    [UsedImplicitly]
    internal class CircleCollider2DInvertedBindings : InvertedBindingsBase<CircleCollider2D>
    {
        #region Static
        [TinyInitializeOnLoad]
        [UsedImplicitly]
        private static void Register()
        {
            GameObjectTracker.RegisterForComponentModification<CircleCollider2D>(SyncCircleCollider2D);
        }

        private static void SyncCircleCollider2D(CircleCollider2D from, TinyEntityView view)
        {
            var registry = view.Registry;
            var collider = view.EntityRef.Dereference(registry).GetComponent(TypeRefs.Physics2D.CircleCollider2D);
            if (null != collider)
            {
                SyncCircleCollider2D(from, collider);
            }
        }

        private static void SyncCircleCollider2D(CircleCollider2D circle, [NotNull] TinyObject collider)
        {
            collider.Refresh();
            collider.AssignIfDifferent("radius", circle.radius);
            collider.AssignIfDifferent("pivot", -new Vector2(circle.offset.x / circle.radius - 0.5f, circle.offset.y / circle.radius - 0.5f));
        }
        #endregion

        #region InvertedBindingsBase<CircleCollider2D>
        public override void Create(TinyEntityView view, CircleCollider2D from)
        {
            var collider = new TinyObject(view.Registry, GetMainTinyType());
            SyncCircleCollider2D(from, collider);

            var entity = view.EntityRef.Dereference(view.Registry);
            var rectCollider = entity.GetOrAddComponent(GetMainTinyType());
            rectCollider.CopyFrom(collider);
        }

        public override TinyType.Reference GetMainTinyType()
        {
            return TypeRefs.Physics2D.CircleCollider2D;
        }
        #endregion
    }
}

