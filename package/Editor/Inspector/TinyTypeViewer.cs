

using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal partial class TinyTypeViewer : ScriptableObject
    {
        [TinyInitializeOnLoad]
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
                    .Where(obj => obj is TinyTypeViewer)
                    .Cast<TinyTypeViewer>()
                    .ToList();
                var toDestroy = ListPool<TinyTypeViewer>.Get();
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
                    ListPool<TinyTypeViewer>.Release(toDestroy);
                }
            };
        }

        private static List<TinyTypeViewer> Instances { get; } = new List<TinyTypeViewer>();

        private static TinyTypeViewer GetInstance()
        {
            var viewer = CreateInstance<TinyTypeViewer>();
            Instances.Add(viewer);
            return viewer;
        }

        private IRegistry m_Registry;
        private TinyType.Reference m_Reference;

        public TinyType.Reference TypeReference => m_Reference;

        static partial void InitializeCustomProperties()
        {
            TypeProperty = new ValueClassProperty<TinyTypeViewer, TinyType>(
                "Type",
                c => null == c.m_Registry ? null : c.m_Reference.Dereference(c.m_Registry),
                null
            );
        }

        public static void SetType(TinyType type, bool additive = false)
        {
            var instance = GetInstance();
            instance.m_Registry = type.Registry;
            instance.m_Reference = (TinyType.Reference) type;

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

