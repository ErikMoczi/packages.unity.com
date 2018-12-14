using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    [CustomEditor(typeof(UTPrefabScriptedImporter))]
    internal class UTPrefabScriptedImporterEditor : TinyScriptedImporterEditorBase<UTPrefabScriptedImporter, TinyEntityGroup>
    {
        protected override void RefreshObject(ref TinyEntityGroup group)
        {
            group = group.Ref.Dereference(Registry);
        }
        
        protected override bool IsPartOfModule(TinyModule module, TinyId mainAssetId)
        {
            return module.EntityGroups.Any(g => g.Id == mainAssetId);
        }
    }
}