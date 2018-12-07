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

        protected override void OnHeader(TinyEntityGroup project)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (null != project)
                {
                    
                }
            }

            GUILayout.Space(2);
        }

        protected override bool IsPartOfModule(TinyModule module, TinyId mainAssetId)
        {
            return module.EntityGroups.Any(g => g.Id == mainAssetId);
        }

        protected override void OnInspect(TinyEntityGroup @object)
        {
            if (null == MainTarget)
            {
                return;
            }
        }
    }
}