using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace Unity.Tiny
{
    [ScriptedImporter(13, new[] {Persistence.ProjectFileImporterExtension})]
    internal class UTProjectScriptedImporter : TinyScriptedImporter<UTProject>
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            CreateAsset(ctx);
        }
        
        protected override Texture2D GetThumbnailForAsset(AssetImportContext ctx, UTProject asset)
        {
            return TinyIcons.Project;
        }
    }
}

