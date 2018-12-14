

using JetBrains.Annotations;

using UnityEngine;
using UnityEngine.UI;

using Unity.Tiny.Runtime.UILayout;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Core2D.TransformNode,
        CoreGuids.UILayout.RectTransform,
        CoreGuids.UILayout.UICanvas)]
    [UsedImplicitly]
    internal class UICanvasBindings : BindingProfile
    {
        public override void LoadBindings(TinyEntity entity)
        {
            AddMissingComponent<Canvas>(entity);
            AddMissingComponent<CanvasScaler>(entity);
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            RemoveComponent<CanvasScaler>(entity);
            RemoveComponent<Canvas>(entity);
        }

        public override void Transfer(TinyEntity entity)
        {
            var canvas = GetComponent<Canvas>(entity);
            var tinyCanvas = entity.GetComponent<TinyUICanvas>();

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.pixelPerfect = false;
            var cameraRef = tinyCanvas.camera;
            var camera = cameraRef.Dereference(entity.Registry);
            canvas.worldCamera = camera?.View.GetComponent<Camera>();

            var scaler = GetComponent<CanvasScaler>(entity);
            scaler.referenceResolution = tinyCanvas.referenceResolution;
            scaler.matchWidthOrHeight = tinyCanvas.matchWidthOrHeight;
            scaler.uiScaleMode = tinyCanvas.uiScaleMode;
            scaler.referencePixelsPerUnit = 1;

            LayoutRebuilder.MarkLayoutForRebuild(canvas.transform as RectTransform);
        }
    }
}

