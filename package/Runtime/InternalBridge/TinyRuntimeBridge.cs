using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Unity.Tiny
{
#if UNITY_EDITOR
    internal static class TinyRuntimeBridge
    {
        #region Tilemaps

        internal class TilemapSyncTile
        {
            public Vector2Int Position { get; internal set; }
            public Tile Tile { get; internal set; }
        }

        internal delegate void TilemapSyncTileEvent(Tilemap tilemap, TilemapSyncTile[] syncTiles);
        internal static event TilemapSyncTileEvent OnTilemapSyncTileEvent;

        private static void TilemapSyncTileCallback(Tilemap tilemap, Tilemap.SyncTile[] syncTiles)
        {
            OnTilemapSyncTileEvent?.Invoke(tilemap, syncTiles.Select(t => new TilemapSyncTile
            {
                Position = new Vector2Int(t.m_Position.x, t.m_Position.y),
                Tile = t.m_Tile as Tile
            }).ToArray());
        }

        internal static void EnableTilemapSyncTileCallback()
        {
            Tilemap.SetSyncTileCallback(TilemapSyncTileCallback);
        }

        internal static void DisableTilemapSyncTileCallback()
        {
            Tilemap.RemoveSyncTileCallback(TilemapSyncTileCallback);
        }

        #endregion
    }
#endif
}
