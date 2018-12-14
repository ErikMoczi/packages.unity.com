

namespace Unity.Tiny
{
    [ContextManager(ContextUsage.Edit | ContextUsage.LiveLink)]
    internal class EntityTemplateManager : ContextManager
    {
        private IEntityGroupManager EntityGroupManager { get; set; }

        private TinyEntityGroup ActiveEntityGroup => EntityGroupManager.ActiveEntityGroup.Dereference(Registry);

        public EntityTemplateManager(TinyContext context)
            :base(context)
        {
        }

        public override void Load()
        {
            EntityGroupManager = Context.GetManager<IEntityGroupManager>();
        }

        public TinyEntity CreateFromTemplate(string name, TinyEntityGroup entityGroup, EntityTemplate template)
        {
            return Create(name, entityGroup, null, template);
        }

        public TinyEntity CreateFromTemplate(string name, TinyEntity parent, EntityTemplate template)
        {
            return Create(name, parent?.EntityGroup ?? ActiveEntityGroup, parent, template);
        }

        internal static TinyEntity CreateFromTemplate(IRegistry registry, string name, TinyEntityGroup entityGroup, EntityTemplate template)
        {
            var entity = registry.CreateEntity(TinyId.New(), name);
            entity.EntityGroup = entityGroup;
            entityGroup.AddEntityReference((TinyEntity.Reference)entity);
            AddFromTemplate(entity, template ?? EntityTemplates.Empty);
            return entity;
        }

        private TinyEntity Create(string name, TinyEntityGroup entityGroup, TinyEntity parent, EntityTemplate template)
        {
            var entity = CreateFromTemplate(Registry, name, entityGroup, template);
            // Make sure that it has the necessary components when parenting.
            if (null != parent)
            {
                entity.Layer = parent.Layer;
                parent.GetOrAddComponent(TypeRefs.Core2D.TransformNode);
                AddFromTemplate(entity, EntityTemplates.Transform);
                var transformNode = entity.GetComponent(TypeRefs.Core2D.TransformNode);
                transformNode["parent"] = (TinyEntity.Reference)parent;

                if (null != parent.GetComponent(TypeRefs.UILayout.RectTransform))
                {
                    AddFromTemplate(entity, EntityTemplates.RectTransform);
                }
            }
            return entity;
        }

        private static void AddFromTemplate(TinyEntity entity, EntityTemplate template)
        {
            foreach (var id in template.Ids)
            {
                var type = entity.Registry.FindById<TinyType>(id);
                if (!type.IsComponent)
                {
                    continue;
                }

                var typeRef = (TinyType.Reference)type;
                entity.GetOrAddComponent(typeRef);
            }
        }
    }
}


