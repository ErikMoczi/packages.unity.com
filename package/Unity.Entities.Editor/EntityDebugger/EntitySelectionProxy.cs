
using Unity.Entities;
using Unity.Entities.Properties;
using UnityEngine;

namespace Unity.Entities.Editor
{
    public class EntitySelectionProxy : ScriptableObject
    {
        public EntityContainer Container;

        public Entity Entity => entity;
        private Entity entity;

        public EntityManager Manager => manager;
        private EntityManager manager;

        public bool Exists => manager.Exists(entity);

        public void SetEntity(EntityManager manager, Entity entity)
        {
            this.entity = entity;
            this.manager = manager;
            this.Container = new EntityContainer(manager, entity);
        }
    }
}
