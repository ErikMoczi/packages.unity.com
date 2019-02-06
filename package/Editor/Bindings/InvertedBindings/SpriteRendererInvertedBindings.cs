

using JetBrains.Annotations;
using UnityEngine;

namespace Unity.Tiny
{
    [UsedImplicitly]
    internal class SpriteRendererInvertedBindings : InvertedBindingsBase<SpriteRenderer>
    {
        #region Static
        [TinyInitializeOnLoad]
        [UsedImplicitly]
        private static void Register()
        {
            GameObjectTracker.RegisterForComponentModification<SpriteRenderer>(SyncRenderer);
        }

        private static void SyncRenderer(SpriteRenderer from, TinyEntityView view)
        {
            var registry = view.Registry;
            var entity = view.EntityRef.Dereference(registry);

            var tinyRenderer = entity.GetComponent(TypeRefs.Core2D.Sprite2DRenderer);
            if (null != tinyRenderer)
            {
                SyncRenderer(from, tinyRenderer);
            }

            if (from.drawMode == SpriteDrawMode.Simple)
            {
                entity.RemoveComponent(TypeRefs.Core2D.Sprite2DRendererOptions);
            }
            else
            {
                var rendererOptions = entity.GetOrAddComponent(TypeRefs.Core2D.Sprite2DRendererOptions);
                if (null != rendererOptions)
                {
                    SyncRendererOptions(from, rendererOptions);
                }
            }
            TransformInvertedBindings.SyncTransform(from.transform, view);
        }

        private static void SyncRenderer(SpriteRenderer from, [NotNull] TinyObject renderer)
        {
            from.sharedMaterial = new Material(Shader.Find("Tiny/Sprite2D"));
            renderer.AssignIfDifferent("sprite", from.sprite);
            renderer.AssignIfDifferent("color", from.color);
        }

        private static void SyncRendererOptions(SpriteRenderer from, [NotNull] TinyObject tiling)
        {
            tiling.AssignIfDifferent("drawMode", Translate(from.drawMode, from.tileMode));
            tiling.AssignIfDifferent("size", from.size);
        }

        private static DrawMode Translate(SpriteDrawMode drawMode, SpriteTileMode tileMode)
        {
            switch (drawMode)
            {
                case SpriteDrawMode.Sliced:
                    return DrawMode.Stretch;
                case SpriteDrawMode.Tiled:
                    return tileMode == SpriteTileMode.Continuous ? DrawMode.ContinuousTiling : DrawMode.AdaptiveTiling;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(drawMode));
            }
        }
        #endregion

        #region InvertedBindingsBase<SpriteRenderer>
        public override void Create(TinyEntityView view, SpriteRenderer spriteRenderer)
        {
            var sr = new TinyObject(view.Registry, GetMainTinyType());
            SyncRenderer(spriteRenderer, sr);

            TinyObject srt = null;
            if (spriteRenderer.drawMode != SpriteDrawMode.Simple)
            {
                srt = new TinyObject(view.Registry, TypeRefs.Core2D.Sprite2DRendererOptions);
                SyncRendererOptions(spriteRenderer, srt);
            }

            var entity = view.EntityRef.Dereference(view.Registry);
            var sprite2DRenderer = entity.GetOrAddComponent(GetMainTinyType());
            sprite2DRenderer.CopyFrom(sr);

            if (null != srt)
            {
                var rendererOptions = entity.GetOrAddComponent(TypeRefs.Core2D.Sprite2DRendererOptions);
                rendererOptions.CopyFrom(srt);
            }
        }

        public override TinyType.Reference GetMainTinyType()
        {
            return TypeRefs.Core2D.Sprite2DRenderer;
        }
        #endregion
    }
}

