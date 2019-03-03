using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Unity.QuickSearch
{
    namespace Providers
    {
        [UsedImplicitly]
        static class SceneObjects
        {
            internal static string type = "scene";
            internal static string displayName = "Scene";
            [UsedImplicitly, SearchItemProvider]
            internal static SearchProvider CreateProvider()
            {
                return new SearchProvider(type, displayName)
                {
                    priority = 50,
                    filterId = "h:",
                    fetchItems = (context, items, provider) =>
                    {
                        items.AddRange(
                            UnityEngine.Object.FindObjectsOfType(typeof(GameObject)).Cast<GameObject>()
                                .Where(go => SearchProvider.MatchSearchGroups(context.searchQuery, go.name))
                                .Select(go =>
                                     {
                                         var id = go.GetInstanceID().ToString();
                                         return provider.CreateItem(id, $"{go.name} ({id})", go.transform.GetPath());
                                     })
                        );
                    },

                    fetchThumbnail = (item, context) =>
                    {
                        if (item.thumbnail)
                            return item.thumbnail;

                        var obj = ObjectFromItem(item);
                        if (obj != null)
                        {
                            item.thumbnail = PrefabUtility.GetIconForGameObject(obj);
                            if (item.thumbnail)
                                return item.thumbnail;
                            item.thumbnail = EditorGUIUtility.ObjectContent(obj, obj.GetType()).image as Texture2D;
                        }

                        return item.thumbnail;
                    },

                    startDrag = (item, context) =>
                    {
                        var obj = ObjectFromItem(item);
                        if (obj != null)
                        {
                            DragAndDrop.PrepareStartDrag();
                            DragAndDrop.objectReferences = new[] { obj };
                            DragAndDrop.StartDrag("Drag scene object");
                        }
                    },

                    isItemValid = item => ObjectFromItem(item) != null
                };
            }

            [UsedImplicitly, SearchActionsProvider]
            internal static IEnumerable<SearchAction> ActionHandlers()
            {
                return new SearchAction[]
                {
                    new SearchAction(type, "select", null, "Select object in scene...") {
                        handler = (item, context) =>
                        {
                            var obj = ObjectFromItem(item);
                            if (obj != null)
                            {
                                Selection.activeGameObject = obj;
                                EditorGUIUtility.PingObject(obj);
                                SceneView.lastActiveSceneView.FrameSelected();
                            }
                        }
                    }
                };
            }

            private static GameObject ObjectFromItem(SearchItem item)
            {
                var instanceID = Convert.ToInt32(item.id);
                var obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
                return obj;
            }

            public static string GetPath(this Transform tform)
            {
                if (tform.parent == null)
                    return "/" + tform.name;
                return tform.parent.GetPath() + "/" + tform.name;
            }

            #if UNITY_2019_1_OR_NEWER
            [UsedImplicitly, Shortcut("Help/Quick Search/Scene", KeyCode.S, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
            public static void PopQuickSearch()
            {
                SearchService.Filter.ResetFilter(false);
                SearchService.Filter.SetFilter(true, type);
                QuickSearchTool.ShowWindow(false);
            }

            #endif
        }
    }
}
