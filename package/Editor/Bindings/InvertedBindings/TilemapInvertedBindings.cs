

using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Tiny.Runtime.Tilemap2D;

namespace Unity.Tiny
{
    [UsedImplicitly]
    internal class TilemapInvertedBindings : InvertedBindingsBase<Tilemap>
    {
        [TinyInitializeOnLoad]
        [UsedImplicitly]
        private static void Register()
        {
            TinyEditorApplication.OnLoadProject += (project, context) =>
            {
                context.GetManager<IEntityGroupManagerInternal>().OnWillUnloadEntityGroup += TinyTilemap.CompressTilemaps;
                TinyRuntimeBridge.OnTilemapSyncTileEvent += TinyTilemap.SyncTileEvent;
                TinyRuntimeBridge.EnableTilemapSyncTileCallback();
            };
            TinyEditorApplication.OnCloseProject += (project, context) =>
            {
                TinyRuntimeBridge.DisableTilemapSyncTileCallback();
                TinyRuntimeBridge.OnTilemapSyncTileEvent -= TinyTilemap.SyncTileEvent;
                context.GetManager<IEntityGroupManagerInternal>().OnWillUnloadEntityGroup -= TinyTilemap.CompressTilemaps;
            };
            TinyEditorApplication.OnWillSaveProject += (project, context) => { TinyTilemap.CompressTilemaps(); };
        }

        [TinyPreprocessBuild(0)]
        private static void PreBuild(TinyBuildOptions options)
        {
            TinyTilemap.CompressTilemaps();
        }

        private static void SyncTilemap(Tilemap from, TinyEntityView view)
        {
            var registry = view.Registry;
            var entity = view.EntityRef.Dereference(registry);

            var tilemap = entity.GetComponent(TypeRefs.Tilemap2D.Tilemap);
            if (null != tilemap)
            {
                SyncTilemap(from, tilemap);
            }
        }

        private static void SyncTilemap(Tilemap from, [NotNull] TinyObject tilemap)
        {
            tilemap.AssignIfDifferent<Vector3>("anchor", from.tileAnchor);
            tilemap.AssignIfDifferent<Vector3>("position", GetTranslation(from.orientationMatrix));
            tilemap.AssignIfDifferent<Quaternion>("rotation", from.orientationMatrix.rotation);
            tilemap.AssignIfDifferent<Vector3>("scale", from.orientationMatrix.lossyScale);
            tilemap.AssignIfDifferent<Vector3>("cellSize", from.cellSize);
            tilemap.AssignIfDifferent<Vector3>("cellGap", from.cellGap);
        }

        public override void Create(TinyEntityView view, Tilemap from)
        {
            var tilemap = new TinyObject(view.Registry, GetMainTinyType());
            SyncTilemap(from, tilemap);
        }

        public override TinyType.Reference GetMainTinyType()
        {
            return TypeRefs.Tilemap2D.Tilemap;
        }

        private static Vector3 GetTranslation(Matrix4x4 matrix)
        {
            var translation = matrix.GetColumn(3);
            return new Vector3(translation.x, translation.y, translation.z);
        }
    }
}

