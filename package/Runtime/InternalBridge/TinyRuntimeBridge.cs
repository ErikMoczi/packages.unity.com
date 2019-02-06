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

        internal static TileData GetTilemapTileData(Tilemap tilemap, TileBase tileBase, Vector3Int position)
        {
            var tileData = new TileData()
            {
                color = Color.white,
                transform = Matrix4x4.identity,
                flags = TileFlags.LockColor,
                colliderType = Tile.ColliderType.Sprite
            };

            ITilemap.s_Instance.m_Tilemap = tilemap;
            tileBase.GetTileData(position, ITilemap.s_Instance, ref tileData);
            return tileData;
        }

        internal class TilemapSyncTile
        {
            public Vector2Int Position { get; internal set; }
            public TileBase Tile { get; internal set; }
            public Sprite BakedSprite { get; internal set; }
            public Color BakedColor { get; internal set; }
            public Tile.ColliderType BakedColliderType { get; internal set; }
        }

        internal delegate void TilemapSyncTileEvent(Tilemap tilemap, TilemapSyncTile[] syncTiles);
        internal static event TilemapSyncTileEvent OnTilemapSyncTileEvent;

        private static void TilemapSyncTileCallback(Tilemap tilemap, Tilemap.SyncTile[] syncTiles)
        {
            OnTilemapSyncTileEvent?.Invoke(tilemap, syncTiles.Select(syncTile => new TilemapSyncTile
            {
                Position = new Vector2Int(syncTile.m_Position.x, syncTile.m_Position.y),
                Tile = syncTile.m_Tile,
                BakedSprite = syncTile.m_TileData.sprite,
                BakedColor = syncTile.m_TileData.color,
                BakedColliderType = syncTile.m_TileData.colliderType
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
