using System.Collections.Generic;

namespace Unity.Tiny
{
    internal abstract class SceneGraphNodeBase : ISceneGraphNode
    {
        public ISceneGraphNode Parent { get; set; }
        public List<ISceneGraphNode> Children { get; } = new List<ISceneGraphNode>();
        public ISceneGraph Graph { get; set; }
        public virtual bool EnabledInHierarchy => true;

        protected SceneGraphNodeBase(ISceneGraph graph)
        {
            Graph = graph;
        }
        
        public void Insert(int siblingIndex, ISceneGraphNode child)
        {
            child.SetParent(siblingIndex, this);
        }
    }
}