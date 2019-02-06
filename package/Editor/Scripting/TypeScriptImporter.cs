

using System;
using System.IO;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace Unity.Tiny
{
    [ScriptedImporter(2, "ts")]
    internal class TypeScriptImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            try
            {
                var textAsset = new TextAsset(File.ReadAllText(ctx.assetPath));
                ctx.AddObjectToAsset("main obj", textAsset, TinyIcons.TypeScript);
                ctx.SetMainObject(textAsset);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}

