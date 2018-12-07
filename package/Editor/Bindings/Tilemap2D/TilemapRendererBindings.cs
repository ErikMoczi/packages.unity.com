

using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Tiny.Runtime.Tilemap2D;

namespace Unity.Tiny
{
    [BindingDependency(typeof(TilemapBindings))]
    [WithComponent(
        CoreGuids.Core2D.TransformNode,
        CoreGuids.Tilemap2D.Tilemap,
        CoreGuids.Tilemap2D.TilemapRenderer)]
    internal class TilemapRendererBindings : BindingProfile
    {
        public override void LoadBindings(TinyEntity entity)
        {
            AddMissingComponent<TilemapRenderer>(entity);
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            RemoveComponent<TilemapRenderer>(entity);
        }

        public override void Transfer(TinyEntity entity)
        {
            var tilemap = GetComponent<Tilemap>(entity);
            var tinyTilemapRenderer = entity.GetComponent<TinyTilemapRenderer>();

            if (tilemap && tilemap != null)
            {
                tilemap.color = tinyTilemapRenderer.color;
                tinyTilemapRenderer.tilemap = tilemap;
            }
        }
    }
}

