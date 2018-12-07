using System.Collections.Generic;
using UnityEngine;

namespace Unity.Tiny
{
    internal static class EntityDragAndDropUtility
    {
        public static void HandleComponentDragAndDrop(Rect rect, IRegistry registry, List<TinyEntity> targets, int atIndex)
        {
            var list = ListPool<TinyType>.Get();
            try
            {
                var status = TinyDragAndDrop.HandleComponentDrop(rect, registry, ref list);
                if (status == TinyDragAndDrop.DragAndDropStatus.Dropped)
                {
                    var target = targets[0];
                    if (atIndex < 0 || atIndex >= target.Components.Count)
                    {
                        atIndex = target.Components.Count;
                    }
                    foreach (var typeToAdd in list)
                    {
                        foreach (var tt in targets)
                        {
                            tt.GetOrAddComponent(typeToAdd.Ref, atIndex + 1);
                        }
                    }
                }
            }
            finally
            {
                ListPool<TinyType>.Release(list);
            }}
    }
}