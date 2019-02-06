using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    [CustomEditor(typeof(UTProjectScriptedImporter))]
    internal class UTProjectScriptedImporterEditor : TinyScriptedImporterEditorBase<UTProjectScriptedImporter, TinyProject>
    {
        private ModuleReferencesTreeView m_TreeView;
        private ModuleReferencesTreeView.ReferencedState State;
        private GUIVisitor m_ConfigurationVisitor;

        private TinyModule MainModule => MainTarget?.Module.Dereference(Registry);

        private bool ShowModules
        {
            get { return EditorPrefs.GetBool("Tiny_Modules_" + MainTarget.Id, false); }
            set
            {
                if (value)
                {
                    EditorPrefs.SetBool("Tiny_Modules_" + MainTarget.Id, value);
                }
                else
                {
                    EditorPrefs.DeleteKey("Tiny_Modules_" + MainTarget.Id);
                }
            }
        }
        
        private bool ShowConfigurations
        {
            get { return EditorPrefs.GetBool("Tiny_Configurations_" + MainTarget.Id, false); }
            set
            {
                if (value)
                {
                    EditorPrefs.SetBool("Tiny_Configurations_" + MainTarget.Id, value);
                }
                else
                {
                    EditorPrefs.DeleteKey("Tiny_Configurations_" + MainTarget.Id);
                }
            }
        }
        
        private ModuleReferencesTreeView TreeView
        {
            get
            {
                if (null == m_TreeView)
                {
                    Reload();
                }

                return m_TreeView;
            }
        }

        protected override void LoadState()
        {
            State = ModuleReferencesTreeView.LoadSharedState();
        }

        protected override void SaveState()
        {
            ModuleReferencesTreeView.SaveSharedState();
        }

        protected override void RefreshObject(ref TinyProject project)
        {
            project = project.Ref.Dereference(Registry);
        }

        protected override void OnHeader(TinyProject project)
        {
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (null == project)
                    {
                        var size = GUI.skin.button.CalcSize(new GUIContent("Open"));
                        if (GUILayout.Button("Open", GUILayout.Width(size.x)))
                        {
                            var ids = TinyGUIDs;
                            if (ids.Length > 0)
                            {
                                if (null != TinyEditorApplication.Project)
                                {
                                    TinyEditorApplication.SaveChanges();
                                    TinyEditorApplication.Close();
                                }

                                TinyEditorApplication.LoadProject(AssetPath);
                                GUIUtility.ExitGUI();
                            }
                        }
                    }
                    else
                    {
                        var size = GUI.skin.button.CalcSize(new GUIContent("Open Settings"));
                        if (GUILayout.Button("Open Settings", GUILayout.Width(size.x)))
                        {
                            UnifiedSettingsBridge.OpenAndFocusTinySettings();
                        }
                    }
                }
                GUILayout.Space(2);
            }
        }

        protected override void Reload()
        {
            if (null == m_TreeView && null != MainTarget)
            {
                m_TreeView = new ModuleReferencesTreeView(MainTarget, State);
            }
            m_TreeView.Reload();
            var context = TinyEditorApplication.EditorContext.Context;
            m_ConfigurationVisitor = new GUIVisitor(
                new ConfigurationAdapter(context),
                new EntityAdapter(context),
                new TinyIMGUIAdapter(context),
                new TinyVisibilityAdapter(context),
                new IMGUIUnityTypesAdapter(),
                new IMGUIPrimitivesAdapter(),
                new IMGUIAdapter());
        }

        protected override void OnInspect(TinyProject module)
        {
            if (TinyEditorApplication.EditorContext == null)
            {
                return;
            }

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);

            var workspace = TinyEditorApplication.EditorContext.Workspace;
            EditorGUILayout.Space();
            workspace.BuildConfiguration =
                (TinyBuildConfiguration) EditorGUILayout.EnumPopup("Build Configuration", workspace.BuildConfiguration);
            EditorGUILayout.BeginHorizontal();
            try
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Build", GUILayout.MinWidth(150)))
                {
                    TinyBuildPipeline.BuildAndLaunch();
                    GUIUtility.ExitGUI();
                }
            }
            finally
            {
                EditorGUILayout.EndHorizontal();

            }

            EditorGUILayout.Space();
            DrawSeparator();
            EditorGUILayout.Space();

            var showModulesRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);
            showModulesRect.x += 15.0f;
            ShowModules = EditorGUI.Foldout(showModulesRect, ShowModules, "Modules");
            if (ShowModules)
            {
                var treeViewRect = GUILayoutUtility.GetRect(0, TreeView.totalHeight);
                treeViewRect.x += 15.0f;
                TreeView.OnGUI(treeViewRect);
            }

            EditorGUILayout.Space();
            DrawSeparator();
            EditorGUILayout.Space();

            if (HaveConfiguration(MainTarget))
            {
                var showConfigurationsRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);
                showConfigurationsRect.x += 15.0f;
                ShowConfigurations =
                    EditorGUI.Foldout(showConfigurationsRect, ShowConfigurations, "Configurations");
                if (ShowConfigurations)
                {
                    m_ConfigurationVisitor.SetTargets(new List<Wrapper<TinyEntity>>
                        {Wrapper.Make(MainTarget.Configuration.Dereference(MainTarget.Registry))});
                    m_ConfigurationVisitor.VisitTargets();
                }

                EditorGUILayout.Space();
                DrawSeparator();
                EditorGUILayout.Space();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var content = new GUIContent($"Close {module.Name}");

            Rect rect = GUILayoutUtility.GetRect(content, TinyStyles.AddComponentStyle);
            if (EditorGUI.DropdownButton(rect, content, FocusType.Passive, TinyStyles.AddComponentStyle))
            {
                if (TinyEditorApplication.SaveChanges())
                {
                    TinyEditorApplication.Close();
                    GUIUtility.ExitGUI();
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();
        }

        private bool HaveConfiguration(TinyProject project)
        {
            var set = HashSetPool<TinyModule>.Get();
            try
            {
                set.UnionWith(MainModule.EnumerateDependencies());
                foreach (var module in project.Configuration.Dereference(Registry).Components
                    .Select(c => Registry.CacheManager.GetModuleOf(c.Type)))
                {
                    if (set.Contains(module))
                    {
                        return true;
                    }
                }
            }
            finally
            {
                HashSetPool<TinyModule>.Release(set);
            }

            return false;
        }
    }        
}
