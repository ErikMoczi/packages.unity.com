

using UnityEngine;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Core2D.Camera2D)]
    [OptionalComponent(
        CoreGuids.Core2D.Camera2DAxisSort,
        CoreGuids.Core2D.Camera2DClippingPlanes)]
    [BindingDependency(
        typeof(TransformBinding))]
    internal class Camera2DBindings : BindingProfile
    {
        public override void LoadBindings(TinyEntity entity)
        {
            AddMissingComponent<Camera>(entity);
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            RemoveComponent<Camera>(entity);
        }

        public override void Transfer(TinyEntity entity)
        {
            var camera = GetComponent<Camera>(entity);
            var tinyCamera = entity.GetComponent<Runtime.Core2D.TinyCamera2D>();

            var clearFlags = tinyCamera.clearFlags;

            camera.clearFlags = clearFlags == CameraClearFlags.SolidColor ? CameraClearFlags.SolidColor : CameraClearFlags.Depth;

            camera.backgroundColor = tinyCamera.backgroundColor;
            camera.orthographic = true;
            camera.orthographicSize = tinyCamera.halfVerticalSize;
            camera.useOcclusionCulling = false;
            camera.allowHDR = false;
            camera.allowMSAA = false;
#if UNITY_2017_3_OR_NEWER
            camera.allowDynamicResolution = false;
#endif
            camera.cullingMask = tinyCamera.layerMask;
            camera.rect = tinyCamera.rect;
            camera.depth = tinyCamera.depth;

            if (entity.GetComponent<Runtime.Core2D.TinyCamera2DClippingPlanes>() is var clippingPlanes && clippingPlanes.IsValid)
            {
                camera.nearClipPlane = clippingPlanes.near;
                camera.farClipPlane = clippingPlanes.far;
            }
            else
            {
                camera.nearClipPlane = -100000.0f;
                camera.farClipPlane = 100000.0f;
            }
        }
    }
}

