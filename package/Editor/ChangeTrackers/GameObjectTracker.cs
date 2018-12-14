

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Component = UnityEngine.Component;
using Object = UnityEngine.Object;

namespace Unity.Tiny
{
    internal static class GameObjectTracker
    {
        public class DontTrackScope : IDisposable
        {
            private readonly bool m_Value;

            public DontTrackScope()
            {
                m_Value = s_Process;
                s_Process = false;
            }

            public void Dispose()
            {
                s_Process = m_Value;
            }
        }

        private static bool s_Process = true;

        private struct ComponentViewPair
        {
            public Component Component { get; set; }
            public TinyEntityView View { get; set; }
        }

        private static List<Action<List<ComponentViewPair>>> InvertedBindingsMethods { get; } = new List<Action<List<ComponentViewPair>>>();
        private static HashSet<TinyEntityView> ActiveViews { get; } = new HashSet<TinyEntityView>();
        private static HashSet<TinyEntityGroup.Reference> UnloadingEntityGroups { get; } = new HashSet<TinyEntityGroup.Reference>();
        private static Action<TinyTrackerRegistration, TinyEventHandler<TinyTrackerRegistration, TinyEntityView>> AddListener { get; }
            = TinyEventDispatcher<TinyTrackerRegistration>.AddListener;

        private static IRegistry Registry => TinyEditorApplication.Registry;
        private static IEntityGroupManagerInternal EntityGroupManager { get; set; }

        [TinyInitializeOnLoad]
        private static void Init()
        {
            AddListener(TinyTrackerRegistration.Register,   HandleRegistration);
            AddListener(TinyTrackerRegistration.Unregister, HandleRegistration);
            TinyEditorApplication.OnLoadProject += HandleProjectLoaded;
        }

        public static void RegisterForComponentModification<TComponent>(Action<TComponent, TinyEntityView> invertedBindingsMethod)
            where TComponent : Component
        {
            InvertedBindingsMethods.Add(pairs =>
            {
                foreach (var pair in pairs.Where(p => p.Component is TComponent))
                {
                    if (pair.View.EntityRef.Equals(TinyEntity.Reference.None))
                    {
                        continue;
                    }
                    invertedBindingsMethod((TComponent)pair.Component, pair.View);
                }
            });
        }

        private static void HandleRegistration(TinyTrackerRegistration trackerRegistration, TinyEntityView view)
        {
            switch (trackerRegistration)
            {
                case TinyTrackerRegistration.Register:
                    Register(view);
                    break;
                case TinyTrackerRegistration.Unregister:
                    Unregister(view);
                    break;
                default:
                    throw new InvalidEnumArgumentException();
            }
        }

        private static void Register(TinyEntityView view)
        {
            if (ActiveViews.Add(view) && ActiveViews.Count == 1)
            {
                ObjectChangeTracker.AddHandler(ObjectChangedHandler);
            }
        }

        private static void Unregister(TinyEntityView view)
        {
            if (!ActiveViews.Remove(view))
            {
                return;
            }

            if (ActiveViews.Count == 0)
            {
                ObjectChangeTracker.RemoveHandler(ObjectChangedHandler);
            }

            if (!view || null == view)
            {
                return;
            }

            if (null == view.Registry)
            {
                return;
            }

            // From this point, we know that the entity view is being destroyed. What we do not know is if the view is
            // being destroyed because we are unloading the scene or if the user deleted the entity through the hierarchy
            // or the scene view.
            
            var entity = view.EntityRef.Dereference(Registry);
            if (entity?.EntityGroup == null)
            {
                return;
            }

            var entityGroupRef = (TinyEntityGroup.Reference) entity.EntityGroup;

            if (UnloadingEntityGroups.Contains(entityGroupRef))
            {
                return;
            }

            entity.View = null;

            if (view.Disposed)
            {
                return;
            }
            
            var graph = EntityGroupManager.GetSceneGraph(entityGroupRef);
            if (null == graph)
            {
                return;
            }

            graph.Delete(graph.FindNode(view.EntityRef));
            TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.SceneGraph);
        }

        private static void HandleProjectLoaded(TinyProject project, TinyContext context)
        {
            EntityGroupManager = context.GetManager<IEntityGroupManagerInternal>();
            EntityGroupManager.OnWillUnloadEntityGroup += HandleEntityGroupWillUnload;
            EntityGroupManager.OnEntityGroupUnloaded += HandleEntityGroupUnloaded;
        }

        private static void HandleEntityGroupWillUnload(TinyEntityGroup.Reference entityGroupRef)
        {
            UnloadingEntityGroups.Add(entityGroupRef);
        }

        private static void HandleEntityGroupUnloaded(TinyEntityGroup.Reference entityGroupRef)
        {
            UnloadingEntityGroups.Remove(entityGroupRef);
        }

        private static void ObjectChangedHandler(IEnumerable<Object> modifications)
        {
            if (!s_Process)
            {
                return;
            }
            var pairs = ListPool<ComponentViewPair>.Get();

            try
            {
                // Gather all the modifications that occured on a Component which also have an Entity View.
                foreach (var component in modifications.OfType<Component>())
                {
                    var view = component.GetComponent<TinyEntityView>();
                    if (null == view)
                    {
                        continue;
                    }

                    pairs.Add(new ComponentViewPair {Component = component, View = view});
                }

                foreach (var method in InvertedBindingsMethods)
                {
                    method(pairs);
                }

                foreach (var pair in pairs)
                {
                    pair.View.Context.GetManager<IBindingsManager>().Transfer(pair.View.EntityRef.Dereference(pair.View.Registry));
                }
            }
            finally
            {
                ListPool<ComponentViewPair>.Release(pairs);
            }
        }
    }
}

