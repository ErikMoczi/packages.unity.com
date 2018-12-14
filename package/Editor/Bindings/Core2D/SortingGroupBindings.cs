

using JetBrains.Annotations;
using UnityEngine.Rendering;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Core2D.SortingGroup,
        CoreGuids.Core2D.TransformNode)]
    [WithoutComponent(
        CoreGuids.Core2D.LayerSorting)]
    [UsedImplicitly]
    internal class SortingGroupBindings : BindingProfile
    {
        public override void LoadBindings(TinyEntity entity)
        {
            AddMissingComponent<SortingGroup>(entity);
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            RemoveComponent<SortingGroup>(entity);
        }
    }
}

