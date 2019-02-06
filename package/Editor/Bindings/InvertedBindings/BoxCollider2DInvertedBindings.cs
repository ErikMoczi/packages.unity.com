using JetBrains.Annotations;
using UnityEngine;

namespace Unity.Tiny
{
    [UsedImplicitly]
    internal class BoxCollider2DInvertedBindings : InvertedBindingsBase<BoxCollider2D>
    {
        #region Static
        [TinyInitializeOnLoad]
        [UsedImplicitly]
        private static void Register()
        {
            GameObjectTracker.RegisterForComponentModification<BoxCollider2D>(SyncBoxCollider2D);
        }

        private static void SyncBoxCollider2D(BoxCollider2D from, TinyEntityView view)
        {
            var registry = view.Registry;
            var collider = view.EntityRef.Dereference(registry).GetComponent(TypeRefs.Physics2D.BoxCollider2D);
            if (null != collider)
            {
                SyncBoxCollider2D(from, collider);
            }
        }

        private static void SyncBoxCollider2D(BoxCollider2D box, [NotNull] TinyObject collider)
        {
            collider.AssignIfDifferent("size", box.size);
            collider.AssignIfDifferent("pivot", -new Vector2(box.offset.x / box.size.x - 0.5f, box.offset.y / box.size.y - 0.5f));
        }
        #endregion

        #region InvertedBindingsBase<BoxCollider2D>
        public override void Create(TinyEntityView view, BoxCollider2D from)
        {
            var collider = new TinyObject(view.Registry, GetMainTinyType());
            SyncBoxCollider2D(from, collider);

            var entity = view.EntityRef.Dereference(view.Registry);
            var boxCollider2D = entity.GetOrAddComponent(GetMainTinyType());
            boxCollider2D.CopyFrom(collider);
        }

        public override TinyType.Reference GetMainTinyType()
        {
            return TypeRefs.Physics2D.BoxCollider2D;
        }
        #endregion
    }
}
