

using JetBrains.Annotations;

using UnityEngine;

namespace Unity.Tiny
{
    [UsedImplicitly]
    internal class CameraInvertedBindings : InvertedBindingsBase<Camera>
    {
        #region Static
        [TinyInitializeOnLoad]
        [UsedImplicitly]
        private static void Register()
        {
            GameObjectTracker.RegisterForComponentModification<Camera>(SyncCamera);
        }

        private static void SyncCamera(Camera from, TinyEntityView view)
        {
            var registry = view.Registry;
            var camera = view.EntityRef.Dereference(registry).GetComponent(TypeRefs.Core2D.Camera2D);
            if (null != camera)
            {
                SyncCamera(from, camera);
            }
        }

        private static void SyncCamera(Camera from, [NotNull] TinyObject camera)
        {
            switch (from.clearFlags)
            {
                case CameraClearFlags.Color:
                case CameraClearFlags.Skybox:
                    from.clearFlags = CameraClearFlags.SolidColor;
                    break;
                case CameraClearFlags.Nothing:
                case CameraClearFlags.Depth:
                    from.clearFlags = CameraClearFlags.Nothing;
                    break;
            }

            from.orthographic = true;
            from.nearClipPlane = 0;
            from.useOcclusionCulling = false;
            from.allowHDR = false;
            from.allowMSAA = false;
#if UNITY_2017_3_OR_NEWER
            from.allowDynamicResolution = false;
#endif

            camera.AssignIfDifferent("clearFlags", from.clearFlags);
            camera.AssignIfDifferent("backgroundColor", from.backgroundColor);
            camera.AssignIfDifferent("layerMask", from.cullingMask);
            camera.AssignIfDifferent("halfVerticalSize", from.orthographicSize);
            camera.AssignIfDifferent("rect", from.rect);
            camera.AssignIfDifferent("depth", from.depth);
        }
        #endregion

        #region InvertedBindingsBase<Camera>
        public override void Create(TinyEntityView view, Camera @from)
        {
            var camera = new TinyObject(view.Registry, GetMainTinyType());
            SyncCamera(from, camera);

            var entity = view.EntityRef.Dereference(view.Registry);
            var tiny = entity.GetOrAddComponent(GetMainTinyType());
            tiny.CopyFrom(camera);
        }

        public override TinyType.Reference GetMainTinyType()
        {
            return TypeRefs.Core2D.Camera2D;
        }
        #endregion
    }
}

