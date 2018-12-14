using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace Unity.Tiny
{
    [ScriptedImporter(4, new[] {Persistence.EntityGroupFileImporterExtension})]
    internal class UTEntityGroupScriptedImporter : TinyScriptedImporter<UTEntityGroup>
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            CreateAsset(ctx);
        }

        protected override Texture2D GetThumbnailForAsset(AssetImportContext ctx, UTEntityGroup asset)
        {
            return TinyIcons.EntityGroup;
        }
    }
}

