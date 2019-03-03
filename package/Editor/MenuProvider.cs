using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Unity.QuickSearch
{
    namespace Providers
    {
        [UsedImplicitly]
        static class MenuProvider
        {
            internal static string type = "menu";
            internal static string displayName = "Menu";
            [UsedImplicitly, SearchItemProvider]
            internal static SearchProvider CreateProvider()
            {
                return new SearchProvider(type, displayName)
                {
                    priority = 80,
                    filterId = "me:",
                    fetchItems = (context, items, provider) =>
                    {
                        var itemNames = new List<string>();
                        var shortcuts = new List<string>();
                        GetMenuInfo(itemNames, shortcuts);

                        items.AddRange(itemNames.Where(menuName =>
                            SearchProvider.MatchSearchGroups(context.searchText, menuName))
                            .Select(menuName => provider.CreateItem(menuName, Utils.GetNameFromPath(menuName), menuName)));
                    },

                    fetchThumbnail = (item, context) => Icons.shortcut
                };
            }

            [UsedImplicitly, SearchActionsProvider]
            internal static IEnumerable<SearchAction> ActionHandlers()
            {
                return new[]
                {
                    new SearchAction("menu", "exec", null, "Execute shortcut...")
                    {
                        handler = (item, context) =>
                        {
                            EditorApplication.ExecuteMenuItem(item.id);
                        }
                    }
                };
            }

            #if UNITY_2019_1_OR_NEWER
            [UsedImplicitly, Shortcut("Window/Quick Search/Menu", KeyCode.M, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
            public static void PopQuickSearch()
            {
                SearchService.Filter.ResetFilter(false);
                SearchService.Filter.SetFilter(true, type);
                QuickSearchTool.ShowWindow();
            }

            #endif

            private static void GetMenuInfo(List<string> outItemNames, List<string> outItemDefaultShortcuts)
            {
                Assembly assembly = typeof(Menu).Assembly;
                var managerType = assembly.GetTypes().First(t => t.Name == "Menu");
                var method = managerType.GetMethod("GetMenuItemDefaultShortcuts", BindingFlags.NonPublic | BindingFlags.Static);
                var arguments = new object[] { outItemNames, outItemDefaultShortcuts };
                method.Invoke(null, arguments);
            }
        }
    }
}
