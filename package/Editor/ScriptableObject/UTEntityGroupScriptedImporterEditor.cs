using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    [CustomEditor(typeof(UTEntityGroupScriptedImporter))]
    internal class UTEntityGroupScriptedImporterEditor : TinyScriptedImporterEditorBase<UTEntityGroupScriptedImporter, TinyEntityGroup>
    {
        protected override void RefreshObject(ref TinyEntityGroup group)
        {
            group = group.Ref.Dereference(Registry);
        }

        protected override void OnHeader(TinyEntityGroup project)
        {
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (null != project)
                    {
                        var buttonStyle = GUI.skin.button;
                        var groupRef = (TinyEntityGroup.Reference)project;
                        var manager = Registry.Context.GetManager<IEntityGroupManager>();
                        if (!manager.LoadedEntityGroups.Contains(groupRef))
                        {
                            var size = buttonStyle.CalcSize(new GUIContent("Load"));
                            if (GUILayout.Button("Load", GUILayout.Width(size.x)))
                            {
                                var ids = TinyGUIDs;
                                if (ids.Length > 0)
                                {
                                    Registry.Context.GetManager<IEntityGroupManager>().LoadEntityGroup(groupRef);
                                }
                            }
                        }
                        else
                        {
                            var oldEnable = GUI.enabled;

                            GUI.enabled = manager.LoadedEntityGroupCount > 1;
                            var size = buttonStyle.CalcSize(new GUIContent("Unload"));
                            if (GUILayout.Button("Unload", GUILayout.Width(size.x)))
                            {
                                var ids = TinyGUIDs;
                                if (ids.Length > 0)
                                {
                                    manager.UnloadEntityGroup(groupRef);
                                }
                            }

                            GUI.enabled = oldEnable;
                        }
                    }
                }
                GUILayout.Space(2);
            }
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

            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                var module = TinyEditorApplication.Module;
                var selfRef = MainTarget.Ref;

                if (module.StartupEntityGroup.Equals(selfRef))
                {
                    EditorGUILayout.HelpBox("This entity group is the startup entity group, it will be loaded automatically at the start of the game.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    try
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Set as Startup"))
                        {
                            module.StartupEntityGroup = selfRef;
                        }

                        GUILayout.FlexibleSpace();
                    }
                    finally
                    {
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
        }
    }
}
