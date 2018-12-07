using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace Unity.Tiny
{
    [ScriptedImporter(11, new[] {Persistence.ModuleFileImporterExtension})]
    internal class UTModuleScriptedImporter : TinyScriptedImporter<UTModule>
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            CreateAsset(ctx);
        }

        protected override Texture2D GetThumbnailForAsset(AssetImportContext ctx, UTModule asset)
        {
            return TinyIcons.Module;
        }
    }
}

