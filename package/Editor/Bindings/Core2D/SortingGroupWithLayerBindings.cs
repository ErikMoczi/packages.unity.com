

using JetBrains.Annotations;
using UnityEditor;
using UnityEngine.Rendering;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Core2D.SortingGroup,
        CoreGuids.Core2D.LayerSorting,
        CoreGuids.Core2D.TransformNode)]
    [UsedImplicitly]
    internal class SortingGroupWithLayerBindings : BindingProfile
    {
        public override void LoadBindings(TinyEntity entity)
        {
            AddMissingComponent<SortingGroup>(entity);
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            RemoveComponent<SortingGroup>(entity);
        }

        public override void Transfer(TinyEntity entity)
        {
            var group = GetComponent<SortingGroup>(entity);
            var tinyLayer = entity.GetComponent<Runtime.Core2D.TinyLayerSorting>();

            group.sortingLayerID = tinyLayer.layer;
            group.sortingOrder = tinyLayer.order;
            EditorUtility.SetDirty(group);
        }
    }
}

