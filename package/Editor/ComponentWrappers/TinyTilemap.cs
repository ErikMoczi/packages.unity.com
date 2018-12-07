using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Unity.Tiny.Runtime.Tilemap2D
{
    internal partial struct TinyTilemap
    {
        public static bool SyncTilemapActive { get; set; } = true;

        private class TilemapTileDataCache : ITilemapTileDataCache
        {
            private Dictionary<Vector2Int, int> m_TileDataIndexes = new Dictionary<Vector2Int, int>();

            public TilemapTileDataCache()
            {
            }

            public TilemapTileDataCache(TinyTilemap tilemap)
            {
                for (var i = 0; i < tilemap.tiles.Count; ++i)
                {
                    var tileData = new TinyTileData((TinyObject)tilemap.tiles[i]);
                    SetTileDataIndex(new Vector2Int((int)tileData.position.x, (int)tileData.position.y), i);
                }
            }

            public void SetTileDataIndex(Vector2Int position, int index)
            {
                m_TileDataIndexes[position] = index;
            }

            public int GetTileDataIndex(Vector2Int position)
            {
                if (m_TileDataIndexes.TryGetValue(position, out var index))
                {
                    return index;
                }
                return -1;
            }

            public int GetTileDataSize()
            {
                return m_TileDataIndexes.Count;
            }
        }

        public TinyTilemap(Tilemap tilemap) : this(GetTinyComponent(tilemap))
        {
        }

        private static TinyObject GetTinyComponent(Tilemap tilemap)
        {
            var view = tilemap.GetComponent<TinyEntityView>();
            var entity = view.EntityRef.Dereference(view.Registry);
            return entity.GetComponent<TinyTilemap>().Tiny;
        }

        private static ITilemapTileDataCache GetTilemapCache(Tilemap tilemap)
        {
            var view = tilemap.GetComponent<TinyEntityView>();
            var cache = view.Registry.CacheManager.GetTilemapCache(view.EntityRef);
            if (cache == null)
            {
                cache = new TilemapTileDataCache(view.EntityRef.Dereference(view.Registry).GetComponent<TinyTilemap>());
                view.Registry.CacheManager.SetTilemapCache(view.EntityRef, cache);
            }
            return cache;
        }

        public static void CompressTilemaps()
        {
            if (TinyEditorApplication.Project == null || TinyEditorApplication.EditorContext == null)
            {
                return;
            }

            var context = TinyEditorApplication.EditorContext.Context;
            var groupManager = context != null ? context.GetManager<IEntityGroupManager>() : null;
            if (groupManager != null)
            {
                foreach (var entityGroupRef in groupManager.LoadedEntityGroups)
                {
                    CompressTilemaps(entityGroupRef);
                }
            }
        }

        public static void CompressTilemaps(TinyEntityGroup.Reference entityGroupRef)
        {
            var entityGroup = entityGroupRef.Dereference(TinyEditorApplication.Registry);

            if (null == entityGroup)
            {
                return;
            }
            
            foreach (var entity in entityGroup.Entities.Select(e => e.Dereference(entityGroup.Registry)).Where(e => e.HasComponent<TinyTilemap>()))
            {
                var tilemap = entity.View.GetComponent<Tilemap>();
                var tinyTilemap = entity.GetComponent<TinyTilemap>();
                tinyTilemap.CompressBounds(tilemap);
            }
        }

        private void CompressBounds(Tilemap tilemap)
        {
            // Modifying Tilemap tiles causes a tilemap sync event, so temporarily deactivate the tilemap sync
            SyncTilemapActive = false;
            tilemap.CompressBounds();
            SyncTilemapActive = true;

            // Rebuild TinyTileData array from the Tilemap
            tiles.Clear();
            foreach (var position in tilemap.cellBounds.allPositionsWithin)
            {
                if (position.z != 0 || !tilemap.HasTile(position))
                {
                    continue;
                }

                var tile = tilemap.GetTile<Tile>(position);
                if (tile == null)
                {
                    continue;
                }

                var tileData = new TinyTileData(Tiny.Registry);
                tileData.position = new Vector2(position.x, position.y);
                tileData.tile = tile;
                tiles.Add(tileData.Tiny);
            }

            // Rebuild tilemap tile data cache
            var view = tilemap.GetComponent<TinyEntityView>();
            view.Registry.CacheManager.SetTilemapCache(view.EntityRef, new TilemapTileDataCache(this));
        }

        public static void SyncTileEvent(Tilemap tilemap, TinyRuntimeBridge.TilemapSyncTile[] syncTiles)
        {
            if (!SyncTilemapActive || tilemap.GetComponent<TinyEntityView>() == null)
            {
                return;
            }

            var tinyTilemap = new TinyTilemap(tilemap);
            var cache = GetTilemapCache(tilemap);

            // Rebuild tilemap tile data cache if its no longer sync with the tiles array
            if (cache.GetTileDataSize() != tinyTilemap.tiles.Count)
            {
                cache = new TilemapTileDataCache(tinyTilemap);
                var view = tilemap.GetComponent<TinyEntityView>();
                view.Registry.CacheManager.SetTilemapCache(view.EntityRef, cache);
            }

            // Assign sync tile data to tiny tilemap tile data
            foreach (var syncTile in syncTiles)
            {
                if (syncTile.Tile == null)
                {
                    continue;
                }

                var tinyTileDataIndex = cache.GetTileDataIndex(syncTile.Position);
                if (tinyTileDataIndex != -1)
                {
                    var tinyTileData = new TinyTileData((TinyObject)tinyTilemap.tiles[tinyTileDataIndex]);
                    tinyTileData.tile = syncTile.Tile;
                }
                else
                {
                    var tinyTileData = new TinyTileData(tinyTilemap.Tiny.Registry);
                    tinyTileData.position = syncTile.Position;
                    tinyTileData.tile = syncTile.Tile;
                    tinyTilemap.tiles.Add(tinyTileData.Tiny);
                    cache.SetTileDataIndex(syncTile.Position, tinyTilemap.tiles.Count - 1);
                }
            }
        }
    }
}
