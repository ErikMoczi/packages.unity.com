using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    [CustomEditor(typeof(UTModuleScriptedImporter))]
    internal class UTModuleScriptedImporterEditor : TinyScriptedImporterEditorBase<UTModuleScriptedImporter, TinyModule>
    {
        private ModuleReferencesTreeView m_TreeView;
        [SerializeField] private ModuleReferencesTreeView.ReferencedState State;

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

        protected override void RefreshObject(ref TinyModule module)
        {
            module = module.Ref.Dereference(Registry);
        }

        protected override void OnHeader(TinyModule module)
        {
            var isCurrent = TinyEditorApplication.Module == module && null != TinyEditorApplication.Module;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (!isCurrent)
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

                            TinyEditorApplication.LoadModule(AssetPath);
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

        protected override void Reload()
        {
            if (null == m_TreeView && null != MainTarget)
            {
                m_TreeView = new ModuleReferencesTreeView(MainTarget, ContextType, State);
            }

            m_TreeView?.Reload();
        }

        protected override void OnInspect(TinyModule module)
        {
            GUI.enabled = true;
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

            if (ContextType == EditorContextType.Project || TinyEditorApplication.Module != module)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var content = new GUIContent($"Close {module.Name}");

            Rect rect = GUILayoutUtility.GetRect(content, TinyStyles.AddComponentStyle);
            if (EditorGUI.DropdownButton(rect, content, FocusType.Passive, TinyStyles.AddComponentStyle))
            {
                TinyEditorApplication.SaveChanges();
                TinyEditorApplication.Close();
                GUIUtility.ExitGUI();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }
}
