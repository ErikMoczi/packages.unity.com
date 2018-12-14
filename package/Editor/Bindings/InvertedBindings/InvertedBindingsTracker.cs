

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Tiny
{
    internal static class InvertedBindingsTracker
    {
        private static TinyContext Context { get; set; }
        private static IRegistry Registry { get; set; }
        private static IEntityGroupManagerInternal EntityGroupManager { get; set; }
        private static IUndoManager Undo { get; set; }
        private static readonly List<TinyEntity> Current = new List<TinyEntity>();
        
        [TinyInitializeOnLoad]
        [UsedImplicitly]
        private static void Init()
        {
            TinyEditorApplication.OnLoadProject += OnProjectLoaded;
            TinyEditorApplication.OnCloseProject += OnProjectClosed;
        }

        private static void OnProjectLoaded(TinyProject project, TinyContext context)
        {
            EditorApplication.hierarchyChanged += HierarchyChanged;
            Context = context;
            Registry = Context.Registry;
            EntityGroupManager = Context.GetManager<IEntityGroupManagerInternal>();
            Undo = Context.GetManager<IUndoManager>();
            Undo.OnRedoPerformed += changes => HierarchyChanged();
        }
        
        private static void OnProjectClosed(TinyProject project, TinyContext context)
        {
            EditorApplication.hierarchyChanged -= HierarchyChanged;
            Registry = null;
            EntityGroupManager = null;
            Undo = null;
        }

        private static void HierarchyChanged()
        {
            if (null == TinyEditorApplication.Project)
            {
                return;
            }
            var changed = false;
            for (var i = 0; i < SceneManager.sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || !scene.IsValid())
                {
                    continue;
                }

                var graph = EntityGroupManager.GetSceneGraph(EntityGroupManager.ActiveEntityGroup);
                if (null == graph)
                {
                    continue;
                }

                var transforms = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Where(t => null != t && t)
                    // Dealing with prefabs
                    .Where(t =>  (t.root.gameObject.hideFlags & HideFlags.HideInHierarchy) != HideFlags.HideInHierarchy)
                    .GroupBy(t => null == t.GetComponent<TinyEntityView>());

                foreach (var group in transforms)
                {
                    // Without an entity view
                    // We will try to create an entity from the components of the object.
                    // If we couldn't (or if it is a prefab), we just display a dialog box and delete the GameObjects
                    if (group.Key)
                    {
                        changed = ProcessNewGameObjects(group);
                    }
                    // With an entity view
                    else
                    {
                        foreach (var t in group)
                        {
                            if (null != t && t)
                            {
                                var view = t.GetComponent<TinyEntityView>();
                                view.DestroyIfUnlinked();
                            }
                        }
                    }
                }
            }

            if (changed)
            {
                EditorApplication.delayCall += () =>
                {
                    TinyInspector.ForceRefreshSelection();
                    TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.SceneGraph);
                    TinyHierarchyWindow.SelectOnNextPaint(Current);
                    Current.Clear();
                };
            }
        }

        private static bool ProcessNewGameObjects(IEnumerable<Transform> transforms)
        {
            bool changed = false;
            foreach (var t in transforms)
            {
                var status = PrefabUtility.GetPrefabInstanceStatus(t.gameObject);
                if (status != PrefabInstanceStatus.NotAPrefab)
                {
                    t.gameObject.AddComponent<TinyEntityView>();
                    if (t.root == t)
                    {
                        EditorUtility.DisplayDialog($"{TinyConstants.ApplicationName}",
                            $"Dragging prefabs are not currently supported in {TinyConstants.ApplicationName}", "OK");
                    }
                }
                else
                {
                    var view = t.gameObject.AddComponent<TinyEntityView>();
                    EditorApplication.delayCall += () =>
                    {
                        view.ForceRelink = true;
                    };
                    view.Registry = Registry;
                    view.Context = Context;

                    var componentList = ListPool<Component>.Get();
                    var invertedBindings = ListPool<IInvertedBindings>.Get();
                    try
                    {
                        t.GetComponents(componentList);
                        // Check if we can actually convert the components
                        // We will actually create an object if and only if we can convert all the components.
                        foreach (var c in componentList)
                        {
                            var cType = c.GetType();
                            var creator = InvertedBindingsHelper.GetInvertedBindings(cType);
                            if (null == creator)
                            {
                                EditorUtility.DisplayDialog($"{TinyConstants.ApplicationName}",
                                    $"Component {cType.Name} is not currently supported in {TinyConstants.ApplicationName}",
                                    "OK");
                                invertedBindings.Clear();
                                break;
                            }

                            invertedBindings.Add(creator);
                        }

                        if (invertedBindings.Count > 0)
                        {
                            for (var index = 0; index < invertedBindings.Count; ++index)
                            {
                                invertedBindings[index].Create(view, componentList[index]);
                            }
                            var entity = view.EntityRef.Dereference(Registry);
                            Current.Add(entity);

                            changed = true;
                        }
                    }
                    finally
                    {
                        ListPool<Component>.Release(componentList);
                        ListPool<IInvertedBindings>.Release(invertedBindings);
                    }
                }
            }
            
            return changed;
        }
    }
}

