using System;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    /// <summary>
    /// Target build platform.
    /// </summary>
    public enum TinyPlatform
    {
        /// <summary>
        /// Generic Web platform.
        /// Works in browsers and Web views.
        /// </summary>
        Html5
    }
    
    /// <summary>
    /// Target build configuration.
    /// </summary>
    public enum TinyBuildConfiguration
    {
        /// <summary>
        /// Includes full debugging information and checks.
        /// </summary>
        /// <remarks>
        /// Debug builds take longer to produce, and they are slower, but they may help you debug low-level issues.
        /// Do not use Debug builds to profile your game.
        /// </remarks>
        Debug,
        
        /// <summary>
        /// Optimized with partial debugging information and checks.
        /// </summary>
        /// <remarks>
        /// This configuration should be used by default during development, as it reduces iteration times while giving
        /// enough information to debug most common issues.
        /// </remarks>
        Development,
        
        /// <summary>
        /// Optimized with no debugging information or checks.
        /// </summary>
        /// <remarks>
        /// Use this configuration when creating release candidates.
        /// </remarks>
        Release
    }
    
    internal interface ITinyBuilder
    {
        void Build(TinyBuildOptions options, TinyBuildResults results);
    }

    /// <summary>
    /// Information required by the <see cref="TinyBuildPipeline"/> to build a project.
    /// </summary>
    public sealed class TinyBuildOptions
    {
        /// <summary>
        /// Context for the build.
        /// </summary>
        internal TinyContext Context { get; set; }
        
        /// <summary>
        /// Project to build, part of the given <see cref="Context"/>.
        /// </summary>
        internal TinyProject Project { get; set; }

        /// <summary>
        /// Settings of the project to build.
        /// </summary>
        public TinyProjectSettings ProjectSettings => Project.Settings;

        /// <summary>
        /// Target build platform.
        /// </summary>
        public TinyPlatform Platform { get; set; } = TinyPlatform.Html5;

        /// <summary>
        /// Target build configuration.
        /// </summary>
        public TinyBuildConfiguration Configuration { get; set; } = TinyBuildConfiguration.Development;
        
        /// <summary>
        /// Whether or not the generated build should automatically try to connect to the Unity Profiler over network.
        /// This option is not taken into account in <see cref="TinyBuildConfiguration.Release"/> builds.
        /// </summary>
        public bool AutoConnectProfiler { get; set; }
        
        /// <summary>
        /// The root build output directory, defaults to "TinyExport" in the Unity project directory.
        /// Note that sub directories will be created under this directory to reflect the selected <see cref="Project"/>,
        /// <see cref="Platform"/>, and <see cref="Configuration"/>.
        /// </summary>
        public DirectoryInfo ExportFolder { get; set; } = new DirectoryInfo("TinyExport");
        
        internal IRegistry Registry => Context.Registry;

        /// <summary>
        /// Computes and returns the final build directory.
        /// </summary>
        public DirectoryInfo BuildFolder => new DirectoryInfo(Path.Combine(
            ExportFolder.FullName,
            Project.Name,
            Platform.ToString().ToLower(),
            Configuration.ToString().ToLower()));
    }

    /// <summary>
    /// Object created by the <see cref="TinyBuildPipeline"/> and populated during a build.
    /// </summary>
    public class TinyBuildResults
    {
        #region Fields

        private readonly TinyBuildReport m_BuildReport = new TinyBuildReport(TinyBuildReport.ProjectNode);

        #endregion

        #region Properties
        
        /// <summary>
        /// Whether or not the build succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Where build artifacts are located.
        /// </summary>
        public DirectoryInfo OutputFolder { get; set; }

        /// <summary>
        /// Where the build binaries are located.
        /// This folder contains the files you need to deploy after a successful build.
        /// </summary>
        public DirectoryInfo BinaryFolder { get; set; }

        internal TinyBuildReport.TreeNode BuildReport => m_BuildReport.Root;

        #endregion
    }

    /// <summary>
    /// Add this attribute on a static method to execute it at the beginning of a Tiny build.
    /// </summary>
    /// <code>
    /// using Unity.Tiny;
    /// 
    /// static class Preprocessor
    /// {
    ///   [TinyPreprocessBuild(0)]
    ///   private static void PreprocessBuild(TinyBuildOptions options)
    ///   {
    ///       // your pre-processing code here
    ///   }
    ///}
    /// </code>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [UsedImplicitly]
    public sealed class TinyPreprocessBuildAttribute : TinyAttribute
    {
        /// <summary>
        /// Execute this method at the beginning of a Tiny build.
        /// </summary>
        /// <param name="order">Absolute order in which this method is executed.</param>
        public TinyPreprocessBuildAttribute(int order) : base(order)
        {
        }
    }
    
    /// <summary>
    /// Add this attribute on a static method to execute it at the end of a Tiny build.
    /// </summary>
    /// <code>
    /// using Unity.Tiny;
    /// 
    /// static class Postprocessor
    /// {
    ///   [TinyPostprocessBuild(0)]
    ///   private static void PostprocessBuild(TinyBuildOptions options, TinyBuildResults results)
    ///   {
    ///       // your post-processing code here
    ///   }
    ///}
    /// </code>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [UsedImplicitly]
    public sealed class TinyPostprocessBuildAttribute : TinyAttribute
    {
        /// <summary>
        /// Execute this method at the end of a Tiny build.
        /// </summary>
        /// <param name="order">Absolute order in which this method is executed.</param>
        public TinyPostprocessBuildAttribute(int order) : base(order)
        {
        }
    }

    /// <summary>
    /// Utility to build Tiny projects.
    /// </summary>
    public static class TinyBuildPipeline
    {
        /// <summary>
        /// Utility method to load a <see cref="TinyProject"/> in preparation for a build.
        /// </summary>
        /// <param name="projectPath">`.utproject` asset path to load.</param>
        /// <returns><see cref="TinyBuildOptions"/> with pre-populated <see cref="TinyBuildOptions.Context"/> and
        /// <see cref="TinyBuildOptions.Project"/> properties.</returns>
        public static TinyBuildOptions LoadProject(string projectPath)
        {
            var context = new TinyContext(ContextUsage.ImportExport);
            projectPath = Persistence.GetPathRelativeToProjectPath(projectPath);
            Persistence.LoadProject(projectPath, context.Registry);
            var project = context.Registry.FindAllByType<TinyProject>().First();
            return new TinyBuildOptions()
            {
                Context = context,
                Project = project
            };
        }

        /// <summary>
        /// Builds the <see cref="TinyProject"/> currently loaded in the Editor and launches the output program.
        /// </summary>
        /// <returns>The build results.</returns>
        public static TinyBuildResults BuildAndLaunch()
        {
            if (EditorApplication.isCompiling)
            {
                throw new Exception($"{TinyConstants.ApplicationName}: Exporting a project is not allowed while Unity is compiling.");
            }

            using (var progress = new TinyEditorUtility.ProgressBarScope())
            {
                var workspace = TinyEditorApplication.EditorContext.Workspace;
                var options = WorkspaceBuildOptions;
                var results = Build(options);

                if (results.Success && workspace.Preview)
                {
                    HTTPServer.Instance.ReloadOrOpen(results.BinaryFolder.FullName, options.Project.Settings.LocalHTTPServerPort);
                }
                else if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    // Don't enter playmode if build failed or preview is not enabled
                    EditorApplication.isPlaying = false;
                }
                return results;
            }
        }

        /// <summary>
        /// Builds a <see cref="TinyBuildOptions"/> instance according to the currently loaded <see cref="TinyProject"/>.
        /// </summary>
        public static TinyBuildOptions WorkspaceBuildOptions
        {
            get
            {
                var context = TinyEditorApplication.EditorContext;
                if (context?.Project == null)
                {
                    throw new Exception("No Tiny project loaded");
                }
                return new TinyBuildOptions()
                {
                    Context = context.Context,
                    Project = context.Project,
                    Configuration = context.Workspace.BuildConfiguration,
                    Platform = context.Workspace.Platform,
                    AutoConnectProfiler = context.Workspace.AutoConnectProfiler
                };
            }
        }

        /// <summary>
        /// Builds a <see cref="TinyProject"/>.
        /// </summary>
        /// <param name="options">Build input.</param>
        /// <returns>Build results.</returns>
        /// <exception cref="ArgumentException">If the input <see cref="options"/> are invalid.</exception>
        public static TinyBuildResults Build(TinyBuildOptions options)
        {
            if (options?.Project == null || options.ExportFolder == null)
            {
                throw new ArgumentException($"{TinyConstants.ApplicationName}: invalid build options provided",
                    nameof(options));
            }

            var buildStart = DateTime.Now;
            
            var results = new TinyBuildResults();
            ITinyBuilder builder;

            switch (options.Platform)
            {
                case TinyPlatform.Html5:
                    builder = new TinyHtml5Builder();
                    break;
                default:
                    throw new ArgumentException($"{TinyConstants.ApplicationName}: build platform not supported", nameof(options));
            }

            using (var progress = new TinyEditorUtility.ProgressBarScope(TinyConstants.ApplicationName + " Build", 
                "Build started for " + options.Platform))
            {
                try
                {
                    CallPreBuildHandlers(options, progress);
                    
                    var destFolder = options.ExportFolder;
                    destFolder.Create();

                    // BUILD = <DEST>/PLATFORM/CONFIG
                    var buildFolder = options.BuildFolder;
                    results.OutputFolder = buildFolder;

                    // read the last tsc build from disc before the purge
                    var lastBuild = TinyScriptUtility.ReadLastBuild(results.OutputFolder);
                    
                    TinyBuildUtilities.PurgeDirectory(buildFolder);
                    buildFolder.Create();

                    // assign the binary folder to the output folder by default
                    // builders can re-target this as needed
                    results.BinaryFolder = buildFolder;

                    // write the last build back to disc before the compilation
                    // if the build input did not change, compilation will be a no-op
                    // otherwise, it'll be overwritten
                    TinyScriptUtility.WriteLastBuild(results.OutputFolder, lastBuild);

                    if (false == TinyBuildUtilities.CompileScripts(options))
                    {
                        return results;
                    }

                    if (false == TinyProjectValidation.Validate(options.Project))
                    {
                        return results;
                    }

                    progress.Update("Generating bindings");
                    TinyBuildUtilities.RunBindGem(options);
                    TinyBuildUtilities.RegenerateTSDefinitionFiles(options);

                    // @TODO Perform a full refresh before building

                    builder.Build(options, results);

                    results.BuildReport.Update();

                    var buildDuration = DateTime.Now - buildStart;
                    Debug.Log(
                        $"{TinyConstants.ApplicationName} project generated in {GetTimeSpanShortString(buildDuration)} at {results.BinaryFolder.FullName}");

                    TinyEditorAnalytics.SendBuildEvent(options.Project, results, buildDuration);
                    results.Success = true;
                    
                    CallPostBuildHandlers(options, progress, results);
                    
                    return results;
                }
                catch (Exception ex)
                {
                    TinyEditorAnalytics.SendException("BuildPipeline.Build", ex);
                    throw;
                }
                finally
                {
                    TinyEditorUtility.RepaintAllWindows();
                }
            }
        }

        private static void CallPreBuildHandlers(TinyBuildOptions options, TinyEditorUtility.ProgressBarScope progress)
        {
            foreach (var handler in TinyAttributeScanner.GetMethodAttributes<TinyPreprocessBuildAttribute>())
            {
                var method = handler.Method;
                var handlerName = $"{method.DeclaringType?.FullName}.{method.Name}";
                progress.Update($"Pre-processing handler: {handlerName}");

                var parameters = method.GetParameters();
                if (method.IsStatic &&
                    false == method.IsGenericMethod &&
                    parameters.Length == 1 &&
                    parameters[0].ParameterType == typeof(TinyBuildOptions)
                )
                {
                    method.Invoke(null, new object[] {options});
                }
                else
                {
                    Debug.LogWarning(
                        $"{method.DeclaringType?.FullName}.{method.Name}: {nameof(TinyPreprocessBuildAttribute)} can only be applied to static, non-generic methods with a single argument of type {nameof(TinyBuildOptions)}.");
                }
            }
        }
        
        private static void CallPostBuildHandlers(TinyBuildOptions options, TinyEditorUtility.ProgressBarScope progress, TinyBuildResults results)
        {
            foreach (var handler in TinyAttributeScanner.GetMethodAttributes<TinyPostprocessBuildAttribute>())
            {
                var method = handler.Method;
                var handlerName = $"{method.DeclaringType?.FullName}.{method.Name}";
                progress.Update($"Post-processing handler: {handlerName}");

                var parameters = method.GetParameters();
                if (method.IsStatic &&
                    false == method.IsGenericMethod &&
                    parameters.Length == 2 &&
                    parameters[0].ParameterType == typeof(TinyBuildOptions) &&
                    parameters[1].ParameterType == typeof(TinyBuildResults)
                )
                {
                    method.Invoke(null, new object[] {options, results});
                }
                else
                {
                    Debug.LogWarning(
                        $"{handlerName}: {nameof(TinyPostprocessBuildAttribute)} can only be applied to static, non-generic methods with two arguments of types {nameof(TinyBuildOptions)} and {nameof(TinyBuildResults)}, respectively.");
                }
            }
        }

        private static string GetTimeSpanShortString(TimeSpan s)
        {
            return s.TotalHours > 1.0 ? $"{s.TotalHours:F1}h" : s.TotalMinutes > 1.0 ? $"{s.TotalMinutes:F1}m" : s.TotalSeconds > 1.0 ? $"{s.TotalSeconds:F1}s" : $"{s.TotalMilliseconds:F0}ms";
        }
    }
}

