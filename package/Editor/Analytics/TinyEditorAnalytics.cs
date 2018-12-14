

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;
using Debug = UnityEngine.Debug;

// ReSharper disable InconsistentNaming

// Note to contributors: Use snake_case for serialized fields sent as part of event payloads
// this convention is used by the Data Science team

#pragma warning disable 649
#pragma warning disable 414

namespace Unity.Tiny
{
    [TinyInitializeOnLoad]
    internal static class TinyEditorAnalytics
    {
        private static bool s_Registered;

        private enum EventName
        {
            tinyEditor,
            tinyEditorBuild
        }
        
        private static HashSet<int> s_OnceHashCodes = new HashSet<int>();

        static TinyEditorAnalytics()
        {
            EditorApplication.delayCall += () =>
            {
                Application.logMessageReceived += (condition, trace, type) =>
                {
                    if (type == LogType.Exception &&
                        !string.IsNullOrEmpty(trace) &&
                        trace.Contains(TinyConstants.PackageName))
                    {
                        if (s_OnceHashCodes.Add(trace.GetHashCode()))
                        {
                            SendErrorEvent("__uncaught__", condition, trace);
                        }
                    }
                };
            };
        }

        private static bool RegisterEvents()
        {
            if (UnityEditorInternal.InternalEditorUtility.inBatchMode)
            {
                return false;
            }
            if (!EditorAnalytics.enabled)
            {
                TraceError("Editor analytics are disabled");
                return false;
            }
            
            if (s_Registered)
            {
                return true;
            }

            var allNames = Enum.GetNames(typeof(EventName));
            if (allNames.Any(eventName => !RegisterEvent(eventName)))
            {
                return false;
            }
            
            s_Registered = true;
            return true;
        }

        private static bool RegisterEvent(string eventName)
        {
            const string vendorKey = "unity.tiny.editor";
            var result = EditorAnalytics.RegisterEventWithLimit(eventName, 100, 1000, vendorKey);
            switch (result)
            {
                case AnalyticsResult.Ok:
                {
                    #if UNITY_TINY_INTERNAL
                    Debug.Log($"Tiny: Registered event: {eventName}");
                    #endif
                    return true;
                }
                case AnalyticsResult.TooManyRequests:
                    // this is fine - event registration survives domain reload (native)
                    return true;
                default:
                {
                    TraceError($"failed to register analytics event '{eventName}'. Result: '{result}'");
                    return false;
                }
            }

        }

        private static void TraceError(string message)
        {
            message = "Tiny: " + message;
#if UNITY_TINY_INTERNAL
                Debug.LogError(message);
#else
            Console.WriteLine(message);
#endif
        }

        [Serializable]
        private struct ContextInfo
        {
            public bool internal_build;
            public string platform;
            public string configuration;
            public bool run;

            public static ContextInfo Default
            {
                get
                {
                    var result = new ContextInfo()
                    {
                        #if UNITY_TINY_INTERNAL
                        internal_build = true,
                        #endif
                    };

                    if (TinyEditorApplication.ContextType != EditorContextType.Project)
                        return result;

                    var context = TinyEditorApplication.EditorContext;
                    if (context?.Project?.Settings == null)
                        return result;

                    var workspace = TinyEditorApplication.EditorContext.Workspace;
                    result.configuration = workspace.BuildConfiguration.ToString();
                    result.run = workspace.Preview;
                    result.platform = workspace.Platform.ToString();
                    return result;
                }
            }

            public static ContextInfo Create(TinyBuildOptions options)
            {
                return new ContextInfo()
                {
                    #if UNITY_TINY_INTERNAL
                    internal_build = true,
                    #endif
                    platform = options.Platform.ToString(),
                    configuration = options.Configuration.ToString()
                };
            }
        }

        [Serializable]
        private struct ProjectInfo
        {
            public string[] modules;

            public static ProjectInfo Default => Create(TinyEditorApplication.Project);

            public static ProjectInfo Create(TinyProject project)
            {
                var result = new ProjectInfo();

                if (project == null)
                    return result;
                
                var main = project.Module.Dereference(project.Registry);
                
                if (main == null)
                    return result;

                var deps = main.EnumerateDependencies().Select(d => d.Name);
                result.modules = deps.ToArray();

                return result;
            }
        }

        public enum EventCategory
        {
            Custom = 0,
            Information = 1,
            Warning = 2,
            Error = 3,
            Usage = 4
        }

        [Serializable]
        private struct GenericEvent
        {
            public TinyPackageUtility.PackageInfo package;
            public ContextInfo context;
            public ProjectInfo project;

            public string category;
            public int category_id;
            public string name;
            public string message;
            public string description;
            public long duration;
        }
        
        public static void SendCustomEvent(string category, string name, string message = null, string description = null)
        {
            SendEvent(EventCategory.Custom, category, name, message, description, TimeSpan.Zero);
        }
        
        public static void SendCustomEvent(string category, string name, TimeSpan duration, string message = null, string description = null)
        {
            SendEvent(EventCategory.Custom, category, name, message, description, duration);
        }

        public static void SendExceptionOnce(string name, Exception ex)
        {
            if (ex == null)
            {
                return;
            }
            var hashCode = ex.StackTrace.GetHashCode();
            if (s_OnceHashCodes.Add(hashCode))
            {
                SendException(name, ex);
            }
        }
        
        public static void SendException(string name, Exception ex)
        {
            if (ex == null)
            {
                return;
            }
            SendErrorEvent(name, ex.Message, ex.ToString());
        }
        
        public static void SendErrorEvent(string name, string message = null, string description = null)
        {
            SendEvent(EventCategory.Error, name, TimeSpan.Zero, message, description);
        }

        public static void SendEvent(EventCategory category, string name, string message = null, string description = null)
        {
            SendEvent(category, category.ToString(), name, message, description, TimeSpan.Zero);
        }
        
        public static void SendEvent(EventCategory category, string name, TimeSpan duration, string message = null, string description = null)
        {
            SendEvent(category, category.ToString(), name, message, description, duration);
        }

        private static void SendEvent(EventCategory category, string categoryName, string name, string message, string description,
            TimeSpan duration)
        {
            if (string.IsNullOrEmpty(categoryName) || string.IsNullOrEmpty(name))
            {
                TraceError(new ArgumentNullException().ToString());
                return;
            }
            var e = new GenericEvent()
            {
                package = TinyPackageUtility.Package,
                context = ContextInfo.Default,
                project = ProjectInfo.Default,
                
                category = categoryName,
                category_id = (int)category,
                name = name,
                message = message,
                description = description,
                duration = duration.Ticks
            };
            
            Send(EventName.tinyEditor, e);
        }

        [Serializable]
        private struct BuildEvent
        {
            public TinyPackageUtility.PackageInfo package;
            public ContextInfo context;
            public ProjectInfo project;

            public long duration;
            
            public long total_bytes;
            public long runtime_bytes;
            public long assets_bytes;
            public long code_bytes;

            public long total_raw_bytes;
            public long runtime_raw_bytes;
            public long assets_raw_bytes;
            public long code_raw_bytes;

            public long heap_size;
            public bool opt_auto_resize;
            public bool opt_ws_client;
            public bool opt_webp_decompressor;
            public bool opt_ecma5;
            public bool opt_single_file_output;
            public bool opt_embed_assets;
            public string default_texture_format;
        }

        public static void SendBuildEvent(TinyProject project, TinyBuildResults buildResults, TimeSpan duration)
        {
            if (project?.Settings == null || buildResults == null)
                return;

            var buildReportRoot = buildResults.BuildReport.Root;
            var buildReportRuntime = buildReportRoot.GetChild(TinyBuildReport.RuntimeNode);
            var buildReportAssets = buildReportRoot.GetChild(TinyBuildReport.AssetsNode);
            var buildReportCode = buildReportRoot.GetChild(TinyBuildReport.CodeNode);

            var e = new BuildEvent()
            {
                package = TinyPackageUtility.Package,
                context = ContextInfo.Default,
                project = ProjectInfo.Create(project),
                
                duration = duration.Ticks,
                
                total_bytes = buildReportRoot?.Item.CompressedSize ?? 0,
                runtime_bytes = buildReportRuntime?.Item.CompressedSize ?? 0,
                assets_bytes = buildReportAssets?.Item.CompressedSize ?? 0,
                code_bytes = buildReportCode?.Item.CompressedSize ?? 0,

                total_raw_bytes = buildReportRoot?.Item.Size ?? 0,
                runtime_raw_bytes = buildReportRuntime?.Item.Size ?? 0,
                assets_raw_bytes = buildReportAssets?.Item.Size ?? 0,
                code_raw_bytes = buildReportCode?.Item.Size ?? 0,

                heap_size = project.Settings.MemorySize,
                opt_auto_resize = project.Settings.CanvasAutoResize,
                opt_webp_decompressor = project.Settings.IncludeWebPDecompressor,
                opt_ecma5 = project.Settings.RunBabel,
                opt_single_file_output = project.Settings.SingleFileHtml,
                opt_embed_assets = project.Settings.EmbedAssets,
                default_texture_format = project.Settings.DefaultTextureSettings.FormatType.ToString()
            };
            
            Send(EventName.tinyEditorBuild, e);
        }
        
        private static void Send(EventName eventName, object eventData)
        {
            if (!RegisterEvents())
            {
                return;
            }
            
            var result = EditorAnalytics.SendEventWithLimit(eventName.ToString(), eventData);
            if (result == AnalyticsResult.Ok)
            {
                #if UNITY_TINY_INTERNAL
                Console.WriteLine($"Tiny: event='{eventName}', time='{DateTime.Now:HH:mm:ss}', payload={EditorJsonUtility.ToJson(eventData, true)}");
                #endif
            }
            else
            {
                TraceError($"failed to send event {eventName}. Result: {result}");
            }
        }
    }
}

