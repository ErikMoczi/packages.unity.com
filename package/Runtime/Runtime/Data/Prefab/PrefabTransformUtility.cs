namespace Unity.Tiny
{
    internal static class PrefabTransformUtility
    {
        /// <summary>
        /// Returns true if the given entity is a root entity of a `PrefabInstance`
        /// </summary>
        internal static bool IsPrefabInstanceRootTransform(TinyEntity entity)
        {
            if (entity.Instance == null)
            {
                return false;
            }
            
            var parentReference = entity.Parent();

            if (parentReference.Equals(TinyEntity.Reference.None))
            {
                return true;
            }

            var parentEntity = parentReference.Dereference(entity.Registry);
            return null == parentEntity.Instance || !parentEntity.Instance.PrefabInstance.Equals(entity.Instance.PrefabInstance);
        }
        
        /// <summary>
        /// Returns true if the given entity is a descendant of an instanced entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        internal static bool IsDescendantOfPrefabInstance(TinyEntity entity)
        {
            do
            {
                var parent = entity.Parent();

                if (parent.Equals(TinyEntity.Reference.None))
                {
                    return false;
                }

                var parentEntity = parent.Dereference(entity.Registry);

                if (null != parent.Dereference(entity.Registry).Instance)
                {
                    return true;
                }

                entity = parentEntity;

            } while (true);
        }
    }
}