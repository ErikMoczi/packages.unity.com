

using JetBrains.Annotations;

using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Core2D.Sprite2DRenderer,
        CoreGuids.Core2D.LayerSorting,
        CoreGuids.Core2D.TransformNode)]
    [WithoutComponent(
        CoreGuids.Core2D.SortingGroup,
        CoreGuids.Tilemap2D.TilemapRenderer,
        CoreGuids.UILayout.RectTransform)]
    [BindingDependency(
        typeof(Sprite2DRendererBinding))]
    [UsedImplicitly]
    internal class LayerSortingWithSprite2DRendererBindings : BindingProfile
    {
        public override void Transfer(TinyEntity entity)
        {
            var spriteRenderer = GetComponent<SpriteRenderer>(entity);
            var tinyLayer = entity.GetComponent<Runtime.Core2D.TinyLayerSorting>();

            spriteRenderer.sortingLayerID = tinyLayer.layer;
            spriteRenderer.sortingOrder = tinyLayer.order;
            EditorUtility.SetDirty(spriteRenderer);
        }
    }

    [WithComponent(
        CoreGuids.Tilemap2D.TilemapRenderer,
        CoreGuids.Core2D.LayerSorting,
        CoreGuids.Core2D.TransformNode)]
    [WithoutComponent(
        CoreGuids.Core2D.SortingGroup,
        CoreGuids.Core2D.Sprite2DRenderer,
        CoreGuids.UILayout.RectTransform)]
    [BindingDependency(
        typeof(TilemapRendererBindings))]
    [UsedImplicitly]
    internal class LayerSortingWithTilemapRendererBindings : BindingProfile
    {
        public override void Transfer(TinyEntity entity)
        {
            var tilemapRenderer = GetComponent<TilemapRenderer>(entity);
            var tinyLayer = entity.GetComponent<Runtime.Core2D.TinyLayerSorting>();

            tilemapRenderer.sortingLayerID = tinyLayer.layer;
            tilemapRenderer.sortingOrder = tinyLayer.order;
            EditorUtility.SetDirty(tilemapRenderer);
        }
    }

    [WithComponent(
        CoreGuids.Core2D.Sprite2DRenderer,
        CoreGuids.Core2D.LayerSorting,
        CoreGuids.Core2D.TransformNode,
        CoreGuids.UILayout.RectTransform)]
    [WithoutComponent(
        CoreGuids.Core2D.SortingGroup,
        CoreGuids.Tilemap2D.TilemapRenderer)]
    [BindingDependency(
        typeof(SpriteRendererWithRectTransformBindings))]
    [UsedImplicitly]
    internal class LayerSortingWithRectTransformBindings : BindingProfile
    {
        public override void LoadBindings(TinyEntity entity)
        {
            AddMissingComponent<Canvas>(entity);
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            RemoveComponent<Canvas>(entity);
        }

        public override void Transfer(TinyEntity entity)
        {
            var canvas = GetComponent<Canvas>(entity);
            var tinyLayer = entity.GetComponent<Runtime.Core2D.TinyLayerSorting>();
            canvas.overrideSorting = true;
            canvas.sortingLayerID = tinyLayer.layer;
            canvas.sortingOrder = tinyLayer.order;
            EditorUtility.SetDirty(canvas);
        }
    }
}

