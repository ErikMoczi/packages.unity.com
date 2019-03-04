using Unity.Entities;
using Unity.Transforms;

namespace Unity.Rendering
{
    [WorldSystemFilter(WorldSystemFilterFlags.EntitySceneOptimizations)]
    [UpdateAfter(typeof(RenderBoundsUpdateSystem))]
    class RemoveLocalBounds : ComponentSystem
    {
        protected override void OnUpdate()
        {
            var group = GetComponentGroup(
                new EntityArchetypeQuery
                {
                    All = new ComponentType[] { typeof(RenderBounds), typeof(Static) }
                });
            
            EntityManager.RemoveComponent(group, new ComponentTypes (typeof(RenderBounds)));
        }
    }
}