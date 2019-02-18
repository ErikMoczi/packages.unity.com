
namespace Unity.Rendering
{
    // Poor mans system ordering... Until we have [UpdateBefore / UpdateAfter etc]
    class RendererConversion : GameObjectConversionSystem
    {
        protected override void OnCreateManager()
        {
            World.GetOrCreateManager<MeshRendererConversion>();
            World.GetOrCreateManager<LODGroupConversion>();
            World.GetOrCreateManager<HLODGroupConversion>();
        }

        protected override void OnUpdate()
        {
        
        }
    }    
}
