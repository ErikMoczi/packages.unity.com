using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Unity.QuickSearch
{
    namespace Providers
    {
        [UsedImplicitly]
        static class SearchUtility
        {
            public static void Goto(string baseUrl, List<Tuple<string, string>> query)
            {
                var url = baseUrl += "?";
                for (var i = 0; i < query.Count; ++i)
                {
                    var item = query[i];
                    url += item.Item1 + "=" + item.Item2;
                    if (i < query.Count - 1)
                    {
                        url += "&";
                    }
                }

                var uri = new Uri(url);
                Process.Start(uri.AbsoluteUri);
            }

            public static SearchItem GetSearchItem(SearchProvider provider, string title, SearchContext context)
            {
                return provider.CreateItem("Search " + title, null, "Search for: " + context.searchQuery);
            }
        }

        [UsedImplicitly]
        static class AnswersProvider
        {
            internal static string type = "answers";
            internal static string displayName = "Answers";
            internal static string urlTitle = "answers.unity.com";
            internal static string searchUrl = "https://answers.unity.com/search.html";

            [UsedImplicitly, SearchItemProvider]
            internal static SearchProvider CreateProvider()
            {
                return new SearchProvider(type, displayName)
                {
                    priority = 10000,
                    filterId = "ua:",
                    fetchItems = (context, items, provider) =>
                    {
                        items.Add(SearchUtility.GetSearchItem(provider, urlTitle, context));
                    },
                    fetchThumbnail = (item, context) => Icons.search
                };
            }

            [UsedImplicitly, SearchActionsProvider]
            internal static IEnumerable<SearchAction> ActionHandlers()
            {
                return new SearchAction[]
                {
                    new SearchAction(type, "search", null, "Search")
                    {
                        handler = (item, context) =>
                        {
                            // ex: https://answers.unity.com/search.html?f=&type=question&sort=relevance&q=Visual+scripting
                            var query = new List<Tuple<string, string>>();
                            query.Add(Tuple.Create("type", "question"));
                            query.Add(Tuple.Create("sort", "relevance"));
                            query.Add(Tuple.Create("q", string.Join("+", context.tokenizedSearchQuery)));
                            SearchUtility.Goto(searchUrl, query);
                        }
                    }
                };
            }
        }

        [UsedImplicitly]
        static class DocManualProvider
        {
            internal static string type = "manual";
            internal static string displayName = "Manual";
            internal static string urlTitle = "docs.unity3d.com/Manual";
            internal static string searchUrl = "https://docs.unity3d.com/Manual/30_search.html";

            [UsedImplicitly, SearchItemProvider]
            internal static SearchProvider CreateProvider()
            {
                return new SearchProvider(type, displayName)
                {
                    priority = 9999,
                    filterId = "um:",
                    fetchItems = (context, items, provider) =>
                    {
                        items.Add(SearchUtility.GetSearchItem(provider, urlTitle, context));
                    },
                    fetchThumbnail = (item, context) => Icons.search
                };
            }

            [UsedImplicitly, SearchActionsProvider]
            internal static IEnumerable<SearchAction> ActionHandlers()
            {
                return new SearchAction[]
                {
                    new SearchAction(type, "search", null, "Search") {
                        handler = (item, context) =>
                        {
                            // ex: https://docs.unity3d.com/Manual/30_search.html?q=Visual+Scripting
                            var query = new List<Tuple<string, string>>();
                            query.Add(Tuple.Create("q", string.Join("+", context.tokenizedSearchQuery)));
                            SearchUtility.Goto(searchUrl, query);
                        }
                    }
                };
            }
        }

        [UsedImplicitly]
        static class DocScriptingProvider
        {
            internal static string type = "scripting";
            internal static string displayName = "Scripting API";
            internal static string urlTitle = "docs.unity3d.com/ScriptReference";
            internal static string searchUrl = "https://docs.unity3d.com/ScriptReference/30_search.html";

            [UsedImplicitly, SearchItemProvider]
            internal static SearchProvider CreateProvider()
            {
                return new SearchProvider(type, displayName)
                {
                    priority = 9999,
                    filterId = "us:",
                    fetchItems = (context, items, provider) =>
                    {
                        items.Add(SearchUtility.GetSearchItem(provider, urlTitle, context));
                    },
                    fetchThumbnail = (item, context) => Icons.search
                };
            }

            [UsedImplicitly, SearchActionsProvider]
            internal static IEnumerable<SearchAction> ActionHandlers()
            {
                return new SearchAction[]
                {
                    new SearchAction(type, "search", null, "Search") {
                        handler = (item, context) =>
                        {
                            // ex: https://docs.unity3d.com/ScriptReference/30_search.html?q=Visual+Scripting
                            var query = new List<Tuple<string, string>>();
                            query.Add(Tuple.Create("q", string.Join("+", context.tokenizedSearchQuery)));
                            SearchUtility.Goto(searchUrl, query);
                        }
                    }
                };
            }
        }

        [UsedImplicitly]
        static class AssetStoreProvider
        {
            internal static string type = "store_search";
            internal static string displayName = "Store Search";
            internal static string urlTitle = "assetstore.unity.com";
            internal static string searchUrl = "https://assetstore.unity.com/search";

            [UsedImplicitly, SearchItemProvider]
            internal static SearchProvider CreateProvider()
            {
                return new SearchProvider(type, displayName)
                {
                    priority = 10000,
                    filterId = "as:",
                    fetchItems = (context, items, provider) => items.Add(SearchUtility.GetSearchItem(provider, urlTitle, context)),
                    fetchThumbnail = (item, context) => Icons.store
                };
            }

            [UsedImplicitly, SearchActionsProvider]
            internal static IEnumerable<SearchAction> ActionHandlers()
            {
                return new SearchAction[]
                {
                    new SearchAction(type, "search", null, "Search") {
                        handler = (item, context) =>
                        {
                            // ex: https://docs.unity3d.com/Manual/30_search.html?q=Visual+Scripting
                            var query = new List<Tuple<string, string>>();

                            foreach (var token in context.tokenizedSearchQuery)
                                query.Add(Tuple.Create("q", token));

                            query.Add(Tuple.Create("k", string.Join(" ", context.tokenizedSearchQuery)));
                            SearchUtility.Goto(searchUrl, query);
                        }
                    }
                };
            }
        }
    }
}
