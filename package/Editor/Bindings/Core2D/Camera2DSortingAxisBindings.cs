

using JetBrains.Annotations;

using UnityEngine;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Core2D.Camera2D,
        CoreGuids.Core2D.TransformNode,
        CoreGuids.Core2D.Camera2DAxisSort)]
    [BindingDependency(
        typeof(Camera2DBindings))]
    [UsedImplicitly]
    internal class Camera2DSortingAxisBindings : BindingProfile
    {
        public override void UnloadBindings(TinyEntity entity)
        {
            var camera = GetComponent<Camera>(entity);
            camera.transparencySortMode = TransparencySortMode.Default;
            TinyEditorBridge.RepaintGameViews();
        }

        public override void Transfer(TinyEntity entity)
        {
            var camera = GetComponent<Camera>(entity);
            var axisSort = entity.GetComponent<Runtime.Core2D.TinyCamera2DAxisSort>();
            camera.transparencySortMode = TransparencySortMode.CustomAxis;
            camera.transparencySortAxis = axisSort.axis;
            TinyEditorBridge.RepaintGameViews();
        }
    }
}

