namespace Unity.Tiny
{
    internal static class PrefabEntityExtensions
    {
        /// <summary>
        /// Returns true if this entity is part of a `valid` prefab instance
        ///
        /// i.e. has an `EntityInstance` with a NON null source
        /// </summary>
        public static bool HasEntityInstanceComponent(this TinyEntity entity, bool checkSource = true)
        {
            if (entity?.Instance == null)
            {
                return false;
            }

            if (checkSource)
            {
                return entity.Instance?.Source.Dereference(entity.Registry) != null;
            }

            return true;
        }
        
        /// <summary>
        /// Returns true if this entity is a child of a prefab `EntityInstance` but NOT part of the prefab itself
        ///
        /// i.e. Is this entity an added override
        /// </summary>
        internal static bool IsAddedEntityOverride(this TinyEntity entity)
        {
            if (entity.HasEntityInstanceComponent() && !PrefabTransformUtility.IsPrefabInstanceRootTransform(entity))
            {
                // We are an instance of a prefab and NOT the root
                return false;
            }

            return PrefabTransformUtility.IsDescendantOfPrefabInstance(entity);
        }
    }
}