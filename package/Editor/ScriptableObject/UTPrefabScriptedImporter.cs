using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace Unity.Tiny
{
    [ScriptedImporter(2, new[] {Persistence.PrefabFileImporterExtension})]
    internal class UTPrefabScriptedImporter : TinyScriptedImporter<UTPrefab>
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            CreateAsset(ctx);
        }

        protected override Texture2D GetThumbnailForAsset(AssetImportContext ctx, UTPrefab asset)
        {
            return TinyIcons.ScriptableObjects.Prefab;
        }
    }
}