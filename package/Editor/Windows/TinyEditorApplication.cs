

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor.Compilation;

namespace Unity.Tiny
{
    internal delegate void ProjectEventHandler(TinyProject project, TinyContext context);

    [InitializeOnLoad]
    internal static class TinyEditorApplication
    {
        /// <inheritdoc />
        /// <summary>
        /// Helper class to hook into the save event from Unity
        /// </summary>
        [InitializeOnLoad]
        internal class SaveModificationProcessor : UnityEditor.AssetModificationProcessor
        {
            private class DontSave : IDisposable
            {
                private readonly bool m_Value;

                public DontSave()
                {
                    m_Value = s_DontSave;
                    s_DontSave = true;
                }

                public void Dispose()
                {
                    s_DontSave = m_Value;
                }
            }

            static SaveModificationProcessor()
            {
                EditorApplication.quitting += () =>
                {
                    s_DontSave = true;
                };
            }
            
            public static IDisposable DontSaveScope()
            {
                return new DontSave();
            }

            private static bool s_DontSave;

            /// <summary>
            /// This is called by Unity when it is about to write serialized assets or scene files to disc.
            ///
            /// We use this call as a means to hook into the save event (i.e. CTRL+S)
            /// </summary>
            /// <param name="paths"></param>
            /// <returns></returns>
            public static string[] OnWillSaveAssets(string[] paths)
            {
                // We only want to trigger a save if the project has some changes to it
                // @NOTE The change check is only performed in this path (e.g. NOT when explicitly clicking the save button from tiny)
                if (IsChanged && !s_DontSave)
                {
                    Save();
                }
                
                return paths;
            }
        }

        /// <summary>
        /// The name used for the 'workspace' container project. This is used when editing standalone modules
        /// </summary>
        private const string k_WorkspaceProjectName = "__workspace__";

        private static int s_WorkspaceVersion = -1;
        private static int s_ProjectVersion = -1;
        private static int s_ModuleVersion = -1;

        public static IRegistry Registry => EditorContext?.Registry;
        internal static TinyEditorContext EditorContext { get; private set; }

        public static TinyProject Project => EditorContext?.Project;
        public static TinyModule Module => EditorContext?.Module;
        
        internal static EditorContextType ContextType => EditorContext?.ContextType ?? EditorContextType.None;

        /// <summary>
        /// Returns true if the current project has any modifications
        /// </summary>
        public static bool IsChanged { get; private set; }
        
        public static event Action OnChangesDetected;
        public static event ProjectEventHandler OnLoadProject;
        public static event ProjectEventHandler OnWillSaveProject;
        public static event ProjectEventHandler OnSaveProject;
        public static event ProjectEventHandler OnCloseProject;

        static TinyEditorApplication()
        {
            // Register to unity application events
            Bridge.EditorApplication.RegisterGlobalUpdate(Update, int.MinValue);

            // Save the project during an assembly reload
            AssemblyReloadEvents.beforeAssemblyReload += SaveTemp;

            // Save the project when exiting the Unity process
            EditorApplication.quitting += SaveTemp;

            CompilationPipeline.assemblyCompilationStarted += HandleCompilationStarted;
        }

        private const string k_ReloadTemp = "TinyForceSaveTempOnCompilationErrors";
        private static void HandleCompilationStarted(string assemblyPath)
        {
            if (ContextType == EditorContextType.None)
            {
                return;
            }
            SaveTemp();
            Close(false);
        }

        private static void Update()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            {
                return;
            }

            // If we had a project opened when a compilation error occured, be sure to reload the temp project.
            if (EditorPrefs.GetBool(k_ReloadTemp, false))
            {
                EditorApplication.delayCall += () =>
                {
                    if (ContextType != EditorContextType.None)
                    {
                        return;
                    }

                    try
                    {
                        LoadTemp();
                    }
                    finally
                    {
                        EditorPrefs.SetBool(k_ReloadTemp, false);
                    }
                };
            }

            if (null == EditorContext)
            {
                // Flush asset changes from the persistence system
                // We don't care about any changes unless we have a project loaded
                TinyAssetWatcher.ClearChanges();
                return;
            }

            var registry = EditorContext.Registry;
            var rootPersistentObject = EditorContext.GetPersistentObjects().First();

            // Poll for workspace changes
            if (null != EditorContext.Workspace && s_WorkspaceVersion != EditorContext.Workspace.Version)
            {
                TinyEditorPrefs.SaveWorkspace(EditorContext.Workspace, rootPersistentObject.PersistenceId);
                s_WorkspaceVersion = EditorContext.Workspace.Version;
            }

            // Poll for file/asset changes
            var changes = TinyAssetWatcher.DetectChanges(Registry);

            if (changes.changesDetected)
            {
                // Get all opened persistent objects
                var persistentObjects = EditorContext.GetPersistentObjects().ToList();

                foreach (var change in changes.changedSources)
                {
                    // One of the currently opened assets has changed
                    if (persistentObjects.Any(o => o.PersistenceId == change))
                    {
                        // Ask the user if they want to keep their changes or reload from disc
                        if (EditorUtility.DisplayDialog(
                            $"{TinyConstants.ApplicationName} assets changed", 
                            $"'{AssetDatabase.GUIDToAssetPath(change)}' has been changed. Would you like to reload the file?", 
                            "Yes", 
                            "No"))
                        {
                            LoadPersistenceId(change);
                        }
                        else
                        {
                            SetDirty();
                        }
                    }
                    else
                    {
                        // This is some other file. We assume they are in a readonly state and we silently reload the object
                        Persistence.ReloadObject(registry, change);
                    }
                }

                foreach (var deletion in changes.deletedSources)
                {
                    // The currently opened project or module has been deleted on disc
                    if (deletion.Equals(rootPersistentObject.PersistenceId))
                    {
                        // Ask the user if they want to keep their changes or close the project
                        if (EditorUtility.DisplayDialog($"{TinyConstants.ApplicationName} assets changed", 
                            "The current project has been deleted, would you like to close the current project?", 
                            "Yes", 
                            "No"))
                        {
                            // Force close the project
                            Close();
                            return;
                        }
                        
                        SetDirty();
                        rootPersistentObject.PersistenceId = string.Empty;
                    }
                    else
                    {
                        foreach (var obj in registry.FindAllBySource(deletion))
                        {
                            var entityGroup = obj as TinyEntityGroup;
                            if (null != entityGroup)
                            {
                                using (entityGroup.Registry.DontTrackChanges())
                                {
                                    EditorContext.Module.RemoveEntityGroupReference((TinyEntityGroup.Reference) entityGroup);
                                }
                            }
                            
                            var type = obj as TinyType;
                            if (null != type)
                            {
                                EditorContext.Module.RemoveTypeReference((TinyType.Reference) type);
                            }
                            
                            // @TODO Handle other persistent types
                            // Can we do this in a more generic way?
                        }
                        
                        registry.UnregisterAllBySource(deletion);
                    }
                }

                foreach (var moved in changes.movedSources)
                {
                    var movedObject = persistentObjects.FirstOrDefault(o => o.PersistenceId == moved);
                    
                    if (null == movedObject)
                    {
                        continue;
                    }
                    
                    var path = AssetDatabase.GUIDToAssetPath(moved);
                    var asset = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
                    
                    if (null != asset)
                    {
                        movedObject.Name = asset.name;
                    }
                }

                TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.DataModel);
                OnChangesDetected?.Invoke();
            }
           
            // Poll for module or project changes
            if (EditorContext.ContextType == EditorContextType.Project && (s_ProjectVersion != EditorContext.Project.Version || s_ModuleVersion != EditorContext.Module.Version))
            {
                using (registry.DontTrackChanges())
                {
                    RefreshConfiguration();
                }
                
                s_ProjectVersion = EditorContext.Project.Version;
                s_ModuleVersion = EditorContext.Module.Version;
            }
        }
        
        /// <summary>
        /// Flags the current project/workspace as being changed
        ///
        /// @NOTE This will show the user a small * to show the project is "dirty"
        /// </summary>
        private static void SetDirty()
        {
            IsChanged = true;
        }

        /// <summary>
        /// Clears the changed flag on the current project/workspace
        /// </summary>
        private static void ClearDirty()
        {
            IsChanged = false;
        }

        /// <summary>
        /// Creates and loads a new .utproject
        /// @NOTE The project only exists in memory until Save() is called
        /// </summary>
        public static TinyProject NewProject()
        {
            Assert.IsFalse(EditorApplication.isPlayingOrWillChangePlaymode);
            
            var context = new TinyContext(ContextUsage.Edit);
            var registry = context.Registry;

            Persistence.LoadAllModules(registry);

            // Create new objects for the project
            var project = registry.CreateProject(TinyId.New(), "NewProject");
            var module = registry.CreateModule(TinyId.New(), TinyProject.MainProjectName);
            
            // Setup the start scene
            var entityGroup = registry.CreateEntityGroup(TinyId.New(), "NewEntityGroup");
            var entityGroupRef = (TinyEntityGroup.Reference) entityGroup;
            var cameraEntity = registry.CreateEntity(TinyId.New(), "Camera");
            var transform = cameraEntity.AddComponent(TypeRefs.Core2D.TransformNode);
            transform.Refresh();
            var camera = cameraEntity.AddComponent(TypeRefs.Core2D.Camera2D);
            camera.Refresh();
            camera["clearFlags"] = new TinyEnum.Reference(TypeRefs.Core2D.CameraClearFlags.Dereference(registry), 1);
            camera.AssignPropertyFrom("backgroundColor", Color.black);
            camera["depth"] = -1.0f;
            entityGroup.AddEntityReference((TinyEntity.Reference) cameraEntity);

            // Setup initial state for the project
            module.Options |= TinyModuleOptions.ProjectModule;
            module.Namespace = "game";
            module.StartupEntityGroup = (TinyEntityGroup.Reference) entityGroup;

            module.AddEntityGroupReference(entityGroupRef);

            project.Module = (TinyModule.Reference) module;
            project.Settings.EmbedAssets = true;
            project.Settings.RunBabel = true;
            project.Settings.LinkToSource = true;
            project.Settings.CanvasAutoResize = true;
            project.Settings.IncludeWSClient = true;
            project.Settings.CanvasWidth = 1920;
            project.Settings.CanvasHeight = 1080;
            project.Settings.RenderMode = RenderingMode.Auto;

            var initialModules = new[]
            {
                "UTiny.Core2D", 
                "Tiny.Streaming", 
                "UTiny.Core2DTypes",
                "UTiny.HTML", 
                "UTiny.Image2D", 
                "UTiny.ImageLoadingHTML", 
                "UTiny.Sprite2D",
            };
 
            foreach (var initialModule in initialModules)
            {
                module.AddExplicitModuleDependency(registry.FindByName<TinyModule>(initialModule).Ref);
            }

            SetupProject(registry, project);

            var workspace = new TinyEditorWorkspace
            {
                ActiveEntityGroup = entityGroupRef
            };
            workspace.OpenedEntityGroups.Add(entityGroupRef);

            var path = EditorUtility.SaveFilePanelInProject("New Project", project.Name, string.Empty, string.Empty);

            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            if (new FileInfo(path).Exists)
            {
                Debug.LogError($"Failed to create project, a file already exists at Path=[{path}]");
                return null;
            }
            
            project.Name = Path.GetFileNameWithoutExtension(path);
            
            var editorContext = new TinyEditorContext((TinyProject.Reference) project, EditorContextType.Project, context, workspace);

            new DirectoryInfo(Path.Combine(path, "Components")).Create();
            
            SavePersistentObjectsAs(editorContext, Path.Combine(path, Persistence.GetFileName(project)));

            LoadContext(editorContext, isChanged: false);

            return project;
        }

        /// <summary>
        /// Creates and loads a new .utmodule
        /// @NOTE The module only exists in memory until Save() is called
        /// </summary>
        public static TinyModule NewModule()
        {
            var context = new TinyContext(ContextUsage.Edit);
            var registry = context.Registry;

            Persistence.LoadAllModules(registry);

            // Create a `workspace` project to host the module for editing purposes
            var project = registry.CreateProject(TinyId.Generate(k_WorkspaceProjectName), k_WorkspaceProjectName);

            // Create objects for the new module
            var module = registry.CreateModule(TinyId.New(), "NewModule");

            // Setup initial state for the module
            module.Namespace = "module";
            project.Module = (TinyModule.Reference) module;

            SetupModule(registry, module);
            
            var workspace = new TinyEditorWorkspace();

            var path = EditorUtility.SaveFilePanelInProject("New Module", module.Name, string.Empty, string.Empty);

            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            if (new FileInfo(path).Exists)
            {
                Debug.LogError($"Failed to create module, a file already exists at Path=[{path}]");
                return null;
            }
            
            module.Name = Path.GetFileNameWithoutExtension(path);

            var editorContext = new TinyEditorContext((TinyProject.Reference) project, EditorContextType.Module, context, workspace);
            
            SavePersistentObjectsAs(editorContext, Path.Combine(path, Persistence.GetFileName(module)));
            
            LoadContext(editorContext, isChanged: false);

            return module;
        }

        /// <summary>
        /// Loads the utproject at the given file path
        /// </summary>
        /// <param name="projectFile">Relative path to the .utproject file</param>
        public static TinyProject LoadProject(string projectFile)
        {
            Assert.IsFalse(EditorApplication.isPlayingOrWillChangePlaymode);

            var context = new TinyContext(ContextUsage.Edit);
            var registry = context.Registry;
            
            Persistence.LoadProject(projectFile, registry);

            var project = registry.FindAllByType<TinyProject>().First();

            Assert.IsNotNull(project);
            
            var editorContext = new TinyEditorContext((TinyProject.Reference) project, EditorContextType.Project, context, TinyEditorPrefs.LoadWorkspace(project.PersistenceId));

            // Migration prompt
            if (project.LastSerializedVersion < TinyProject.CurrentSerializedVersion && project.LastSerializedVersion <= 2)
            {
                if (EditorUtility.DisplayDialog(
                    $"{TinyConstants.ApplicationName} asset version changed",
                    $"We have made some changes to the asset format in order to better support collaboration. \n" +
                    $"\n" +
                    $"Serialized version: {project.LastSerializedVersion}\n" +
                    $"Current version: {TinyProject.CurrentSerializedVersion}\n" +
                    $"\n" +
                    $"WARNING: If you continue your project will be migrated to the new format. Make sure to backup your project before proceeding\n" +
                    $"",
                    "Yes",
                    "No"))
                {
                    // Update the transient property
                    project.LastSerializedVersion = TinyProject.CurrentSerializedVersion;
                    SavePersistentObjectsAs(editorContext, projectFile);
                }
                else
                {
                    // Force close the project
                    Close();
                    return null;
                }
            }
            
            SetupProject(registry, project);
            
            LoadContext(editorContext, isChanged: false);
            return project;
        }

        /// <summary>
        /// Loads the utmodule at the given file path
        /// </summary>
        /// <param name="moduleFile">Relative path to the .utmodule file</param>
        public static TinyModule LoadModule(string moduleFile)
        {
            Assert.IsFalse(EditorApplication.isPlayingOrWillChangePlaymode);

            var context = new TinyContext(ContextUsage.Edit);
            var registry = context.Registry;

            Persistence.LoadModule(moduleFile, registry);

            var module = registry.FindAllBySource(TinyRegistry.DefaultSourceIdentifier).OfType<TinyModule>().First();
            
            Assert.IsNotNull(module);
            
            SetupModule(registry, module);

            var project = registry.CreateProject(TinyId.Generate(k_WorkspaceProjectName), k_WorkspaceProjectName);
            project.Module = (TinyModule.Reference) module;

            var editorContext = new TinyEditorContext((TinyProject.Reference) project, EditorContextType.Module, context, TinyEditorPrefs.LoadWorkspace(project.PersistenceId));
            LoadContext(editorContext, isChanged: false);
            return module;
        }

        /// <summary>
        /// Loads the given asset by its guid
        /// </summary>
        /// <param name="persistenceId"></param>
        public static void LoadPersistenceId(string persistenceId)
        {
            if (string.IsNullOrEmpty(persistenceId))
            {
                return;
            }

            var assetPath = AssetDatabase.GUIDToAssetPath(persistenceId);

            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            if (Path.GetExtension(assetPath).Equals(Persistence.ProjectFileExtension))
            {
                LoadProject(assetPath);
            }
            else if (Path.GetExtension(assetPath).Equals(Persistence.ModuleFileExtension))
            {
                LoadModule(assetPath);
            }
            else
            {
                Persistence.ReloadObject(Registry, persistenceId);
                TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.DataModel);
            }
        }

        /// <summary>
        /// Saves the current project or module to the assets directory
        ///
        /// NOTE: If the project has never been saved before a `Save As` is called instead
        /// </summary>
        public static bool Save()
        {
            var persistentObject = EditorContext.GetPersistentObjects().First();

            if (string.IsNullOrEmpty(persistentObject.PersistenceId))
            {
                return SaveAs();
            }

            SavePersistentObjectsAs(EditorContext, Persistence.GetAssetPath(persistentObject));
            return true;
        }
        
        public static bool SaveAs()
        {
            var persistentObject = EditorContext.GetPersistentObjects().First();
            
            var path = ShowSaveAsPrompt(persistentObject);

            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            SavePersistentObjectsAs(EditorContext, path);
            return true;
        }
        
        /// <summary>
        /// Save a standalone object to the project
        /// </summary>
        /// <param name="persistentObject"></param>
        /// <param name="extension"></param>
        public static void SaveObject(IPersistentObject persistentObject, string extension = null)
        {
            var name = persistentObject.Name;
            
            using (var transaction = new Persistence.PersistTransaction())
            {
                if (!string.IsNullOrEmpty(persistentObject.PersistenceId))
                {
                    var path = Persistence.GetAssetPath(persistentObject);
                    transaction.PersistObjectAs(persistentObject, path);
                }
                else
                {
                    var root = EditorContext.GetPersistentObjects().First();
                    var rootPath = Persistence.GetAssetPath(root);
                    var rootDirectory = new FileInfo(rootPath).Directory;
                
                    Assert.IsNotNull(rootDirectory);
                
                    var fileInfo = new FileInfo(Path.Combine(rootDirectory.FullName, Persistence.GetRelativePathForSubAsset(persistentObject)));
                
                    // Make sure the directory exists
                    fileInfo.Directory?.Create();

                    var path = fileInfo.FullName; 
                
                    if (null != extension)
                    {
                        path = Path.ChangeExtension(path, extension);
                    }
                
                    transaction.PersistObjectAs(persistentObject, path);
                }
            }
            
            // Fix to get prefab naming to follow when applying
            if (Path.GetFileName(Persistence.GetAssetPath(persistentObject)) != name)
            {
                var result = AssetDatabase.RenameAsset(Persistence.GetAssetPath(persistentObject), name);

                if (!string.IsNullOrEmpty(result))
                {
                    Debug.LogWarning(result);
                }
            }
            
            foreach (var obj in persistentObject.EnumerateContainers().OfType<IRegistryObject>())
            {
                // Re-register this object under to the `SourceIdentifier`
                Registry.ChangeSource(obj.Id, persistentObject.PersistenceId);
            }
        }

        private static void SavePersistentObjectsAs(TinyEditorContext context, string path)
        {
            var persistentObject = context.GetPersistentObjects().First();
            var subPersistentObjects = context.GetPersistentObjects().Skip(1).ToList();
            
            using (context.Registry.DontTrackChanges())
            using (SaveModificationProcessor.DontSaveScope())
            {
                OnWillSaveProject?.Invoke(context.Project, context.Context);

                var projectDirectory = new FileInfo(path).Directory;
                Assert.IsNotNull(projectDirectory);

                // @TEMP Generate the scripts directory for the user
                var scriptsPath = AssetDatabase.GUIDToAssetPath(context.Module.ScriptRootDirectory);
                var scriptsDirExisted = Directory.Exists(scriptsPath);
                if (string.IsNullOrEmpty(context.Module.ScriptRootDirectory) || false == scriptsDirExisted)
                {
                    var scriptsDirectory = new DirectoryInfo(Path.Combine(projectDirectory.FullName, "Scripts"));

                    scriptsDirectory.Create();

                    var scriptsAssetPath = Persistence.GetPathRelativeToProjectPath(scriptsDirectory.FullName);
                    AssetDatabase.ImportAsset(scriptsAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUncompressedImport);
                    context.Module.ScriptRootDirectory = AssetDatabase.AssetPathToGUID(scriptsAssetPath);

                    if (scriptsDirExisted)
                    {
                        Registry.Context.GetManager<TinyScriptingManager>().Refresh();
                    }
                }

                using (var transaction = new Persistence.PersistTransaction())
                {
                    // Save the root persistent object to the project directory
                    // @TODO This is a forced save to disc (ignoring the version check optimization)
                    //       This is a workaround since changes to the module are not propagated to the project
                    transaction.PersistObjectAs(persistentObject, path);
                
                    foreach (var obj in subPersistentObjects)
                    {
                        if (!string.IsNullOrEmpty(obj.PersistenceId))
                        {
                            // This asset has been saved before, save in place using the asset database to resolve the path
                            transaction.PersistObjectAs(obj, Persistence.GetAssetPath(obj));
                        }
                        else
                        {
                            var fileInfo = new FileInfo(Path.Combine(projectDirectory.FullName, Persistence.GetRelativePathForSubAsset(obj)));
                            Assert.IsNotNull(fileInfo.Directory);
                        
                            // Make sure the directory exists
                            fileInfo.Directory.Create();

                            // This is the first time saving this sub-asset (place it in the default directory)
                            transaction.PersistObjectAs(obj, fileInfo.FullName);
                        }
                    }
                }

                // This must be done AFTER the transaction is completed and assets are imported
                foreach (var obj in subPersistentObjects)
                {
                    // Re-register this object under to the `SourceIdentifier`
                    context.Registry.ChangeSource(obj.Id, obj.PersistenceId);
                }
                
                TinyEditorPrefs.SaveWorkspace(context.Workspace, persistentObject.PersistenceId);
                OnSaveProject?.Invoke(context.Project, context.Context);
                ClearDirty();
                TinyAssetWatcher.ClearChanges();
            }
        }

        private static string ShowSaveAsPrompt(IPersistentObject persistentObject)
        {
            var extension = Persistence.GetFileExtension(persistentObject);
            return EditorUtility.SaveFilePanelInProject($"Save {ContextType}", persistentObject.Name, extension.Substring(1), string.Empty);
        }

        /// <summary>
        /// Prompts the user to save the current project if any changes were detected
        /// </summary>
        /// <returns>True if the project has been saved or the user decided NOT to save; False if the user canceled the save operation</returns>
        public static bool SaveChanges()
        {
            if (null == EditorContext)
            {
                return true;
            }
            
            if (IsChanged)
            {
                var dialogResult = EditorUtility.DisplayDialogComplex(
                    $"Save {ContextType}",
                    $"There are unsaved changes in the {TinyConstants.ApplicationName} {ContextType}, do you want to save?",
                    "Yes",
                    "No",
                    "Cancel");
                
                switch (dialogResult)
                {
                    case 0:
                        // Yes: Save and continue closing the project
                        if (!Save())
                        {
                            // We failed to save the current project
                            // Bail out to avoid loss of data
                            return false;
                        }
                        
                        break;
                    
                    case 1:
                        // No: Don't save and continue closing the project
                        break;
                        
                    case 2: 
                        // Cancel: Opt out, the user has canceled the operation
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Closes the current Tiny project
        /// </summary>
        public static void Close()
        {
            Close(true);
        }

        internal static void Close(bool deleteTemp)
        {
            if (null == EditorContext)
            {
                return;
            }

            var selection = Selection.instanceIDs;

            // @NOTE Closing a project can cause a Unity scene to save. We dont want to trigger persitsence save for tiny
            using (SaveModificationProcessor.DontSaveScope())
            {
                OnCloseProject?.Invoke(EditorContext.Project, EditorContext.Context);

                EditorContext?.Unload();
                EditorContext = null;

                if (deleteTemp)
                {
                    TinyTemp.Delete();
                }

                ClearDirty();
            }

            Selection.instanceIDs = selection;
        }

        /// <summary>
        /// Saves the current context as a temp file
        /// </summary>
        private static void SaveTemp()
        {
            if (null == EditorContext)
            {
                return;
            }

            var persistentObjects = EditorContext.GetPersistentObjects().NotNull().ToList();
            
            TinyEditorPrefs.SaveWorkspace(EditorContext.Workspace, persistentObjects.First().PersistenceId);
            
            using (Serialization.SerializationContext.Scope(Serialization.SerializationContext.Temp))
            {
                if (string.IsNullOrEmpty(persistentObjects.First().PersistenceId))
                {
                    // This is a temporary asset
                    // Save the full object state
                    TinyTemp.SaveTemporary(persistentObjects);
                }
                else
                {
                    if (IsChanged || persistentObjects.Any(o => string.IsNullOrEmpty(o.PersistenceId)))
                    {
                        // This is a persistent asset but the user has made some changes
                        // Save the full object state WITH the persistent Id
                        TinyTemp.SavePersistentChanged(persistentObjects);
                    }
                    else
                    {
                        // We only need to save the persistentId in this case
                        // We will reload any asset changes from disc without prompting the user
                        TinyTemp.SavePersistentUnchanged(persistentObjects);
                    }
                }
                EditorPrefs.SetBool(k_ReloadTemp, true);
            }
        }

        /// <summary>
        /// Trys to loads the last saved temp file
        /// </summary>
        public static void LoadTemp()
        {
            if (UnityEditorInternal.InternalEditorUtility.inBatchMode)
            {
                return;
            }

            Assert.IsFalse(EditorApplication.isPlayingOrWillChangePlaymode);

            if (!TinyTemp.Exists())
            {
                return;
            }

            var context = new TinyContext(ContextUsage.Edit);
            var registry = context.Registry;

            string persistenceId;
            if (!TinyTemp.Accept(registry, out persistenceId))
            {
                LoadPersistenceId(persistenceId);
                return;
            }

            var project = registry.FindAllByType<TinyProject>().FirstOrDefault();
            TinyEditorContext editorContext = null;

            if (project != null)
            {
                SetupProject(registry, project);
                
                editorContext = new TinyEditorContext((TinyProject.Reference) project, EditorContextType.Project, context, TinyEditorPrefs.LoadLastWorkspace());
            }
            else
            {
                var module = registry.FindAllBySource(TinyRegistry.DefaultSourceIdentifier).OfType<TinyModule>().First();

                SetupModule(registry, module);
                
                if (null != module)
                {
                    project = registry.CreateProject(TinyId.Generate(k_WorkspaceProjectName), k_WorkspaceProjectName);
                    project.Module = (TinyModule.Reference) module;

                    editorContext = new TinyEditorContext((TinyProject.Reference) project, EditorContextType.Module, context, TinyEditorPrefs.LoadLastWorkspace());
                }
            }

            Assert.IsNotNull(project);
            LoadContext(editorContext, true);
        }
        
        /// <summary>
        /// Sets up or migrates the initial state of the project
        /// * Includes required modules
        /// * Perfrorms any migration
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="project"></param>
        private static void SetupProject(IRegistry registry, TinyProject project)
        {
            var module = project.Module.Dereference(registry);
            // Make sure there's a dependency on the core modules
            AddRequiredModuleDependencies(registry, module);

            if (project.Configuration.Equals(TinyEntity.Reference.None))
            {
                var configurationEntity = registry.CreateEntity(TinyId.New(), "Configuration");
                project.Configuration = (TinyEntity.Reference) configurationEntity;
            }
        }

        /// <summary>
        /// Sets up or migrates the initial state of a standalone module
        /// * Includes required modules
        /// * Perfrorms any migration
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="module"></param>
        private static void SetupModule(IRegistry registry, TinyModule module)
        {
            AddRequiredModuleDependencies(registry, module);
        }

        private static void AddRequiredModuleDependencies(IRegistry registry, TinyModule module)
        {
            foreach (TinyModule mod in registry.FindAllByType<TinyModule>())
            {
                if(mod.IsRequired)
                    module.AddExplicitModuleDependency((TinyModule.Reference)mod);
            }
        }

        private static void LoadContext(TinyEditorContext context, bool isChanged)
        {
            Assert.IsNotNull(context);

            ClearDirty();

            // @NOTE Loading a project can cause a Unity scene to change or be loaded during this operation we dont want to trigger a save 
            using (SaveModificationProcessor.DontSaveScope())
            {
                // Cleanup the previous context
                if (context != EditorContext)
                {
                    EditorContext?.Unload();
                }

                // Load the new context
                EditorContext = context;
                context.Load();
                RefreshConfiguration();

                // Setup the initial state
                s_WorkspaceVersion = context.Workspace.Version;
                s_ProjectVersion = context.Project.Version;
                s_ModuleVersion = context.Module.Version;

                OnLoadProject?.Invoke(context.Project, EditorContext.Context);

                // Flush the Undo stack
                var undo = EditorContext.Context.GetManager<TinyUndoManager>();
                undo.Update();

                // Listen for ANY changes and flag the project as changed (*)
                context.Caretaker.OnGenerateMemento += (originator, memento) => { SetDirty(); };
                undo.OnUndoPerformed += changes => SetDirty();
                undo.OnRedoPerformed += changes => SetDirty();

                // Regenerate the TypeScript definition files whenever something changes related to modules and layers
                context.Caretaker.OnGenerateMemento += OnTypeScriptDefinitionChanged;
                context.Context.GetManager<TinyEntityGroupManager>().OnWillUnloadEntityGroup += HandleOnWillUnloadEntityGroup;
            }

            if (isChanged)
            {
                SetDirty();
            }
            
            TinyBuildUtilities.CompileScripts();
        }

        private static void HandleOnWillUnloadEntityGroup(TinyEntityGroup.Reference entityGroupRef)
        {
            var group = entityGroupRef.Dereference(EditorContext.Registry);

            if (Persistence.IsPersistentObjectChanged(group))
            {
                if (EditorUtility.DisplayDialog(
                    $"Entity group has changes", 
                    $"'{group.Name}' has unsaved changes. Would you like to save before unloading?", 
                    "Yes", 
                    "No"))
                {
                    Persistence.PersistObject(group, Persistence.GetAssetPath(group));
                }
            }
        }

        private static void OnTypeScriptDefinitionChanged(IOriginator originator, IMemento memento)
        {
            if (originator is TinyModule)
            {
                TinyBuildUtilities.RegenerateTSDefinitionFiles(TinyBuildPipeline.WorkspaceBuildOptions);
                return;
            }

            if (Registry.AllUnregistered().OfType<TinyModule>().Any())
            {
                TinyBuildUtilities.RegenerateTSDefinitionFiles(TinyBuildPipeline.WorkspaceBuildOptions);
            }
        }

        private static void RefreshConfiguration()
        {
            if (Project == null)
            {
                throw new NullReferenceException("project");
            }
            if (Registry == null)
            {
                throw new NullReferenceException("registry");
            }

            var settings = Project.Settings;
            var entity = Project.Configuration.Dereference(Registry);
            if (null == entity)
            {
                return;
            }

            foreach (var typeRef in Project.Module.Dereference(Registry).EnumerateDependencies().ConfigurationTypeRefs())
            {
                var component = entity.GetComponent(typeRef) ?? entity.AddComponent(typeRef);

                // @HACK Until we move settings exclusively to configuration components
                if (component.Type.Equals(TypeRefs.Core2D.DisplayInfo))
                {
                    component.Refresh();
                    component.AssignIfDifferent("width", settings.CanvasWidth);
                    component.AssignIfDifferent("height", settings.CanvasHeight);
                    component.AssignIfDifferent("autoSizeToFrame", settings.CanvasAutoResize);
                    component.AssignIfDifferent("renderMode", settings.RenderMode);
                }
            }
        }
    }
}

