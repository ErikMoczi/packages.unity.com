using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

namespace Unity.Tiny
{
    internal abstract class IncludedInBuildSettingsProvider : SettingsProvider
    {
        private static TinyContext s_Context;
        private static bool s_HasChanges;
        private static IncludedInBuildTreeView m_TreeView;

        private RegistryTreeView.Filters Filter { get; }
        private EditorContextType Context { get; }

        protected IncludedInBuildSettingsProvider(string localPath, RegistryTreeView.Filters filter,
            EditorContextType context)
            : base("Project/Tiny/" + localPath
#if UNITY_2019_1_OR_NEWER
                , SettingsScope.Project
#endif
            )
        {
            label = localPath;
            Filter = filter;
            Context = context;
        }

        [TinyInitializeOnLoad]
        [UsedImplicitly]
        private static void ResetState()
        {
            TinyEditorApplication.OnLoadProject += SetUpTree;
            TinyEditorApplication.OnCloseProject += (p, c) => m_TreeView = null;
        }

        private static void SetUpTree(TinyProject project, TinyContext context)
        {
            s_Context = context;
            m_TreeView = new IncludedInBuildTreeView(project.Registry, new RegistryTreeView.State());
            m_TreeView.AlternatingBackground = true;
            var undo = context.GetManager<IUndoManager>();
            undo.OnUndoPerformed += (changes) => m_TreeView.Reload();
            undo.OnRedoPerformed += (changes) => m_TreeView.Reload();
            context.Caretaker.OnBeginUpdate += OnBeginUpdate;
            context.Caretaker.OnEndUpdate += OnEndUpdate;
            context.Caretaker.OnGenerateMemento += SetChanged;
        }

        private static void OnBeginUpdate()
        {
            s_HasChanges = false;
        }

        private static void SetChanged(IOriginator originator, IMemento memento)
        {
            s_HasChanges = true;
        }

        private static void OnEndUpdate()
        {
            if (s_HasChanges)
            {
                m_TreeView.Reload();
            }
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_TreeView?.SetFilter(Filter);
        }

        public override void OnGUI(string searchContext)
        {

            if (TinyEditorApplication.ContextType == EditorContextType.None)
            {
                EditorGUILayout.LabelField("No Tiny context is currently opened.");
            }
            else if (Context.HasFlag(TinyEditorApplication.ContextType))
            {
                EditorGUILayout.BeginHorizontal();
                try
                {
                    m_TreeView?.DrawToolbar();
                }
                finally
                {
                    EditorGUILayout.EndHorizontal();
                }

                m_TreeView?.SetFilter(Filter);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical(GUI.skin.box);
                try
                {
                    m_TreeView?.OnGUI(GUILayoutUtility.GetRect(0, m_TreeView.totalHeight));
                }
                finally
                {
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.LabelField($"No Tiny {TinyEditorApplication.ContextType} is currently opened.");
            }
        }
    }
    
    
    [UsedImplicitly]
    internal class IncludedEntityGroupsSettingProvider: IncludedInBuildSettingsProvider
    {
        public IncludedEntityGroupsSettingProvider() 
            : base("Entities", RegistryTreeView.Filters.EntityGroup, EditorContextType.Project)
        {
        }
        
        [SettingsProvider]
        [UsedImplicitly]
        public static SettingsProvider Provider()
        {
            return new IncludedEntityGroupsSettingProvider();
        }
    }
    
    [UsedImplicitly]
    internal class IncludedTypesSettingProvider: IncludedInBuildSettingsProvider
    {
        public IncludedTypesSettingProvider() 
            : base("Components", RegistryTreeView.Filters.Type, EditorContextType.Project | EditorContextType.Module)
        {
        }
        
        [SettingsProvider]
        [UsedImplicitly]
        public static SettingsProvider Provider()
        {
            return new IncludedTypesSettingProvider();
        }
    }
    
    [UsedImplicitly]
    internal class IncludedAssetsSettingProvider: IncludedInBuildSettingsProvider
    {
        public IncludedAssetsSettingProvider() 
            : base("Assets", RegistryTreeView.Filters.Asset, EditorContextType.Project | EditorContextType.Module)
        {
        }
        
        [SettingsProvider]
        [UsedImplicitly]
        public static SettingsProvider Provider()
        {
            return new IncludedAssetsSettingProvider();
        }
    }
    
    [UsedImplicitly]
    internal class IncludedModulesSettingProvider: IncludedInBuildSettingsProvider
    {
        public IncludedModulesSettingProvider() 
            : base("Modules", RegistryTreeView.Filters.Module, EditorContextType.Project | EditorContextType.Module)
        {
        }
        
        [SettingsProvider]
        [UsedImplicitly]
        public static SettingsProvider Provider()
        {
            return new IncludedModulesSettingProvider();
        }
    }
}
