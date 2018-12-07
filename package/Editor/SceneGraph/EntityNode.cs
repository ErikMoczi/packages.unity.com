using UnityEngine;

namespace Unity.Tiny
{
    internal class EntityNode : SceneGraphNodeBase, ITransformNode
    {
        private readonly IRegistry m_Registry;
        
        public TinyEntity.Reference EntityRef { get; set; }
        
        public override bool EnabledInHierarchy
        {
            get
            {
                var self = EntityRef.Dereference(TinyEditorApplication.Registry)?.Enabled ?? false;
                return self && (Parent?.EnabledInHierarchy ?? true);
            }
        }
        
        public Transform Transform => EntityRef.Dereference(m_Registry).View.transform;
        
        public EntityNode(ISceneGraph graph, IRegistry registry, TinyEntity entity) 
            : base(graph)
        {
            m_Registry = registry;
            EntityRef = entity.Ref;
        }
    }
}

