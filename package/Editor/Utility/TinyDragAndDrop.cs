

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal static class TinyDragAndDrop
    {
        public enum DragAndDropStatus
        {
            NotDragging = 0,
            Rejected = 1,
            Hovering = 2,
            Dropped = 4,
            OutOfBounds = 8
        }
        
        public static DragAndDropStatus HandleComponentDrop(Rect rect, IRegistry registry, ref List<TinyType> outTypes)
        {
            var utTypes = ListPool<UTType>.Get();
            try
            {
                utTypes.AddRange(DragAndDrop.objectReferences.OfType<UTType>());
                var sameTypes = utTypes.Count == DragAndDrop.objectReferences.Length;

                switch (Event.current.type)
                {
                    case EventType.DragUpdated:
                        if (!rect.Contains(Event.current.mousePosition))
                        {
                            return DragAndDropStatus.OutOfBounds;
                        }
                        
                        if (!sameTypes)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                            return DragAndDropStatus.Rejected;
                        }

                        var allComponents = true;
                        foreach (var utType in utTypes)
                        {
                            var ids = Persistence.GetRegistryObjectIdsForAssetGuid(
                                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(utType)));
                            if (ids.Length <= 0)
                            {
                                continue;
                            }
                            // Fetch main asset.
                            var type = registry.FindById<TinyType>(new TinyId(ids[0]));
                            if (null == type || !type.IsComponent)
                            {
                                allComponents = false;
                            }
                        }

                        if (allComponents)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                            Event.current.Use();
                            return DragAndDropStatus.Hovering;
                        }
                        else
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                            return DragAndDropStatus.Rejected;
                        }

                    case EventType.DragPerform:
                        if (!rect.Contains(Event.current.mousePosition))
                        {
                            return DragAndDropStatus.OutOfBounds;
                        }
                        
                        outTypes.Clear();
                        foreach (var utType in utTypes)
                        {
                            var ids = Persistence.GetRegistryObjectIdsForAssetGuid(
                                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(utType)));
                            if (ids.Length > 0)
                            {
                                var id = ids[0];
                                var typeToAdd = registry.FindById<TinyType>(new TinyId(id));
                                if (null != typeToAdd && typeToAdd.IsComponent)
                                {
                                    outTypes.Add(typeToAdd);
                                }
                            }
                        }

                        if (outTypes.Count == 0)
                        {
                            return DragAndDropStatus.Rejected;
                        }
                        
                        DragAndDrop.AcceptDrag();
                        Event.current.Use();
                        return DragAndDropStatus.Dropped;
                }
            }
            finally
            {
                ListPool<UTType>.Release(utTypes);
            }

            return DragAndDropStatus.NotDragging;
        }
    }
}

