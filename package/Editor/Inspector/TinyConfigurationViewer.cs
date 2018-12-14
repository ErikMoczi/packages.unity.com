

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal partial class TinyConfigurationViewer : ScriptableObject
    {
        [TinyInitializeOnLoad]
        [UsedImplicitly]
        private static void Register()
        {
            Selection.selectionChanged += HandleSelectionChanged;
        }

        private static void HandleSelectionChanged()
        {
            EditorApplication.delayCall += () =>
            {
                // Try to release instances of the viewer.
                var selection = Selection.instanceIDs
                    .Select(EditorUtility.InstanceIDToObject)
                    .Where(obj => obj is TinyConfigurationViewer)
                    .Cast<TinyConfigurationViewer>()
                    .ToList();
                var toDestroy = ListPool<TinyConfigurationViewer>.Get();
                try
                {
                    foreach (var instance in Instances)
                    {
                        if (selection.Contains(instance) || TinyInspector.IsBeingInspected(instance))
                        {
                            continue;
                        }

                        toDestroy.Add(instance);
                    }

                    foreach (var viewer in toDestroy)
                    {
                        Instances.Remove(viewer);
                        DestroyImmediate(viewer, false);
                    }
                }
                finally
                {
                    ListPool<TinyConfigurationViewer>.Release(toDestroy);
                }
            };
        }

        private static List<TinyConfigurationViewer> Instances { get; } = new List<TinyConfigurationViewer>();

        private static TinyConfigurationViewer GetInstance()
        {
            var viewer = CreateInstance<TinyConfigurationViewer>();
            Instances.Add(viewer);
            return viewer;
        }

        private IRegistry m_Registry;
        private TinyEntity.Reference m_Reference;
        
        public TinyEntity Entity => null == m_Registry ? null : m_Reference.Dereference(m_Registry);

        public static void SetEntity(TinyEntity entity, bool additive = false)
        {
            var instance = GetInstance();
            instance.m_Registry = entity.Registry;
            instance.m_Reference = (TinyEntity.Reference) entity;

            if (!additive)
            {
                Selection.activeInstanceID = instance.GetInstanceID();
            }
            else
            {
                if (Selection.instanceIDs.Contains(instance.GetInstanceID()))
                {
                    return;
                }
                
                var selection = Selection.instanceIDs.ToList();
                selection.Add(instance.GetInstanceID());
                Selection.instanceIDs = selection.ToArray();
            }
        }
    }
}

