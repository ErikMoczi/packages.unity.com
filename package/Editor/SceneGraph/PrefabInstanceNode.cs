namespace Unity.Tiny
{
    /// <summary>
    /// Single root prefabs
    /// </summary>
    internal class PrefabInstanceNode : EntityNode
    {
        public TinyPrefabInstance.Reference PrefabInstanceRef { get; set; }
        
        public PrefabInstanceNode(ISceneGraph graph, IRegistry registry, TinyPrefabInstance prefabInstance, TinyEntity entity)
            : base(graph, registry, entity)
        {
            PrefabInstanceRef = prefabInstance?.Ref ?? TinyPrefabInstance.Reference.None;
        }
    }
}