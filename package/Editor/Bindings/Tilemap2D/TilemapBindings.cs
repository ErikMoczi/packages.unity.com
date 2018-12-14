

using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Tiny.Runtime.Tilemap2D;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Core2D.TransformNode,
        CoreGuids.Tilemap2D.Tilemap)]
    internal class TilemapBindings : BindingProfile
    {
        public override void LoadBindings(TinyEntity entity)
        {
            AddMissingComponent<Grid>(entity);
            AddMissingComponent<Tilemap>(entity);
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            RemoveComponent<Tilemap>(entity);
            RemoveComponent<Grid>(entity);
        }

        public override void Transfer(TinyEntity entity)
        {
            var grid = GetComponent<Grid>(entity);
            var tilemap = GetComponent<Tilemap>(entity);
            var tinyTilemap = entity.GetComponent<TinyTilemap>();

            if (grid && grid != null)
            {
                grid.cellSize = tinyTilemap.cellSize;
                grid.cellGap = tinyTilemap.cellGap;
            }
            if (tilemap && tilemap != null)
            {
                tilemap.tileAnchor = tinyTilemap.anchor;
                var T = tinyTilemap.position;
                var R = tinyTilemap.rotation;
                var S = tinyTilemap.scale;
                var orientationMatrix = Matrix4x4.TRS(T, R, S);
                tilemap.orientation = orientationMatrix.isIdentity ? Tilemap.Orientation.XY : Tilemap.Orientation.Custom;
                tilemap.orientationMatrix = orientationMatrix;

                var positions = new Vector3Int[tinyTilemap.tiles.Count];
                var tiles = new TileBase[tinyTilemap.tiles.Count];
                for (var i = 0; i < tinyTilemap.tiles.Count; ++i)
                {
                    var tileData = new TinyTileData((TinyObject)tinyTilemap.tiles[i]);
                    positions[i] = new Vector3Int((int)tileData.position.x, (int)tileData.position.y, 0);
                    tiles[i] = tileData.tile;
                }

                // Modifying Tilemap tiles causes a tilemap sync event, so temporarily deactivate the tilemap sync
                TinyTilemap.SyncTilemapActive = false;
                tilemap.ClearAllTiles();
                tilemap.SetTiles(positions, tiles);
                TinyTilemap.SyncTilemapActive = true;
            }
        }
    }
}

