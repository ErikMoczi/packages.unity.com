

using UnityEngine.Assertions;

namespace Unity.Tiny
{
    using Tiny;

    internal static class TinyTransformExtensions
    {
        public static bool HasTransformNode(this TinyEntity self)
        {
            if (null == self)
            {
                return false;
            }
            return null != self.GetComponent(TypeRefs.Core2D.TransformNode);
        }

        public static void SetParent(this TinyEntity self, TinyEntity.Reference parentRef)
        {
            var parent = parentRef.Dereference(self.Registry);
            Assert.AreNotEqual(self, parent);
            TinyObject transform = null;
            if (!self.HasTransformNode() && null != parent)
            {
                self.AddComponent(TypeRefs.Core2D.TransformNode);
            }

            parent?.GetOrAddComponent(TypeRefs.Core2D.TransformNode);

            transform = self.GetComponent(TypeRefs.Core2D.TransformNode);
            if (null != transform)
            {
                // Set new parent
                transform["parent"] = parentRef;
            }

            if (TinyEntity.Reference.None.Id == parentRef.Id)
            {
                return;
            }

            // Rebind groups
            if (parent.EntityGroup != self.EntityGroup)
            {
                var selfRef = AsReference(self);
                self.EntityGroup.RemoveEntityReference(selfRef); ;
                parent.EntityGroup.AddEntityReference(selfRef);
            }
        }

        public static TinyEntity.Reference Parent(this TinyEntity self)
        {
            var transform = self.GetComponent(TypeRefs.Core2D.TransformNode);
            if (null == transform)
            {
                return TinyEntity.Reference.None;
            }

            return (TinyEntity.Reference)transform["parent"];
        }

        private static TinyEntity.Reference AsReference(this TinyEntity entity)
        {
            if (null == entity)
            {
                return TinyEntity.Reference.None;
            }
            return (TinyEntity.Reference)entity;
        }
    }


}

