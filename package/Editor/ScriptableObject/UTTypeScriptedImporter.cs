using System.IO;
using System.Linq;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace Unity.Tiny
{
    [ScriptedImporter(2, new[] {Persistence.TypeFileImporterExtension})]
    internal class UTTypeScriptedImporter : TinyScriptedImporter<UTType>
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            CreateAsset(ctx);
        }

        protected override Texture2D GetThumbnailForAsset(AssetImportContext ctx, UTType asset)
        {
            var registry = new TinyRegistry();
            using (var stream = new MemoryStream())
            {
                Serialization.Json.JsonFrontEnd.Accept(ctx.assetPath, stream);
                stream.Position = 0;
                Serialization.CommandStream.CommandFrontEnd.Accept(stream, registry);
            }

            var typeCode = registry.FindById<TinyType>(new TinyId(asset.Objects.FirstOrDefault()))?.TypeCode;
            return TinyIcons.GetIconForTypeCode(typeCode ?? TinyTypeCode.Unknown);
        }
    }
}

