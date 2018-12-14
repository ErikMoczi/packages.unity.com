
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace Unity.Tiny
{
    internal abstract class TinyScriptedImporterEditorBase<TAssetImporterType, TRegistryObject> : ScriptedImporterEditor
        where TAssetImporterType : ScriptedImporter
        where TRegistryObject : TinyRegistryObjectBase
    {
        protected sealed override bool useAssetDrawPreview => false;
        public sealed override bool showImportedObject => false;
        public override bool UseDefaultMargins() => false;

        protected TAssetImporterType Importer => target as TAssetImporterType;
        protected string AssetPath => Importer.assetPath;
        protected string AssetGUID => AssetDatabase.AssetPathToGUID(Importer.assetPath);
        protected string[] TinyGUIDs => Persistence.GetRegistryObjectIdsForAssetGuid(AssetGUID);
        private string MainTinyGUID => TinyGUIDs[0];

        private TinyId MainEditableAsset { get; set; }

        protected TinyId GetMainEditableId()
        {
            if (MainEditableAsset == TinyId.Empty)
            {
                MainEditableAsset = GetMainEditableAsset(new TinyId(MainTinyGUID));
            }

            return MainEditableAsset;
        }

        protected virtual bool IsPartOfModule(TinyModule module, TinyId mainAssetId)
        {
            return false;
        }

        private int m_LastVersion;

        public sealed override void OnEnable()
        {
            base.OnEnable();
            LoadState();
        }

        public sealed override void OnDisable()
        {
            base.OnDisable();
            SaveState();
        }

        protected string AssetName => Path.GetFileNameWithoutExtension(Importer.assetPath);

        private TRegistryObject GetMainObject()
        {
            var ids = TinyGUIDs;
            if (ids.Length > 0)
            {
                return TinyEditorApplication.Registry?.FindById<TRegistryObject>(new TinyId(ids[0]));
            }

            return null;
        }

        private TRegistryObject m_MainTarget;

        protected TRegistryObject MainTarget
        {
            get
            {
                if (null == m_MainTarget)
                {
                    m_MainTarget = GetMainObject();
                }

                if (null != m_MainTarget)
                {
                    RefreshObject(ref m_MainTarget);
                }

                return m_MainTarget;
            }
        }

        protected EditorContextType ContextType => TinyEditorApplication.ContextType;

        protected IRegistry Registry => m_MainTarget?.Registry;

        protected abstract void RefreshObject(ref TRegistryObject @object);

        protected sealed override void OnHeaderGUI()
        {
            var rect = GUILayoutUtility.GetRect(0, 36);
            var iconRect = rect;
            iconRect.x += 10;
            iconRect.y += 10;
            iconRect.width = 24;
            iconRect.height = 24;

            var icon = AssetDatabase.GetCachedIcon(Importer.assetPath);
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

            var labelRect = rect;
            labelRect.x += iconRect.width + 15.0f;
            labelRect.y += 10;
            var boldLabelStyle = new GUIStyle(EditorStyles.boldLabel);
            boldLabelStyle.fontSize = 15;
            EditorGUI.LabelField(labelRect, AssetName, boldLabelStyle);

            if (typeof(TRegistryObject) != typeof(TinyProject) &&
                typeof(TRegistryObject) != typeof(TinyModule))
            {
                var id = GetMainEditableId();
                if (id != TinyId.Empty)
                {
                    EditorGUILayout.BeginHorizontal();
                    try
                    {
                        DrawMainAsset(id);
                        if (null == MainTarget ||
                            (TinyEditorApplication.Registry.FindById<TinyModule>(id) != TinyEditorApplication.Module) &&
                            (TinyEditorApplication.Registry.FindById<TinyProject>(id) != TinyEditorApplication.Project))
                        {
                            if (GUILayout.Button("Open", GUILayout.MaxWidth(60)))
                            {
                                OpenMainAssetForEditing(id);
                            }
                        }
                    }
                    finally
                    {
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }

            OnHeader(MainTarget);
            DrawSeparator();
        }

        protected void DrawMainAsset(TinyId id)
        {
            var guid = Persistence.GetAssetGuidFromTinyGuid(id);
            if (!string.IsNullOrEmpty(guid))
            {
                var obj = AssetDatabase.LoadAssetAtPath<TinyScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
                GUI.enabled = false;
                var targetName = $"Tiny {obj.GetType().Name.Replace("UT", "")}";
                EditorGUILayout.ObjectField(targetName, obj, typeof(TinyScriptableObject), false);
                GUI.enabled = true;
            }
        }

        public sealed override void OnInspectorGUI()
        {
            var obj = MainTarget;
            var oldEnable = GUI.enabled;

            if (null != obj)
            {
                GUI.enabled &= !obj.IsRuntimeIncluded;
                try
                {
                    var versioned = (obj as IVersioned);
                    if (versioned.Version != m_LastVersion)
                    {
                        Reload();
                    }

                    m_LastVersion = versioned.Version;
                    OnInspect(obj);
                }
                finally
                {
                    GUI.enabled = oldEnable;
                }
            }
        }

        protected void DrawSeparator()
        {
            TinyGUILayout.Separator(TinyColors.Inspector.Separator, 3.0f);
        }

        protected virtual void Reload() { }

        protected virtual void OnHeader(TRegistryObject @object) { }
        protected virtual void OnInspect(TRegistryObject @object) { }
        protected virtual void LoadState(){ }
        protected virtual void SaveState(){ }

        protected void DrawDocumentation(IDocumented obj)
        {
            EditorGUILayout.LabelField("Documentation", GUILayout.MaxWidth(EditorGUIUtility.labelWidth - EditorGUI.indentLevel * 15.0f - 4.0f));
            obj.Documentation.Summary = EditorGUILayout.TextArea(obj.Documentation.Summary);
        }

        private static void OpenMainAssetForEditing(TinyId id)
        {
            var guid = Persistence.GetAssetGuidFromTinyGuid(id);
            var path = AssetDatabase.GUIDToAssetPath(guid);
            TinyEditorApplication.SaveChanges();
            TinyEditorApplication.Close();
            var type = AssetDatabase.GetMainAssetTypeAtPath(path);

            if (type == typeof(UTProject))
            {
                TinyEditorApplication.LoadProject(path);
            }
            else if (type == typeof(UTModule))
            {
                TinyEditorApplication.LoadModule(path);
            }
            GUIUtility.ExitGUI();
        }

        private TinyId GetMainEditableAsset(TinyId assetId)
        {
            // Fast path
            if (null != MainTarget)
            {
                switch (TinyEditorApplication.ContextType) {
                    case EditorContextType.Project:
                        return TinyEditorApplication.Project.Id;
                    case EditorContextType.Module:
                        return TinyEditorApplication.Module.Id;
                }
            }

            if (TryFindMainAsset<UTProject>(assetId, out var found) ||
                TryFindMainAsset<UTModule>(assetId, out found))
            {
                return found;
            }
            return default;
        }

        private bool TryFindMainAsset<TAsset>(TinyId assetId, out TinyId found)
            where TAsset : TinyScriptableObject
        {
            // Get the module
            foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(TAsset).FullName}"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var context = new TinyContext(ContextUsage.ImportExport);
                Persistence.ShallowLoad(path, context.Registry);
                var module = context.Registry.FindAllByType<TinyModule>().First();
                if (IsPartOfModule(module, assetId))
                {
                    found = new TinyId(Persistence.GetRegistryObjectIdsForAssetGuid(guid)[0]);
                    return true;
                }
            }

            found = default;
            return false;
        }
    }
}
