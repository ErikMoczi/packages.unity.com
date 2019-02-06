using System;
using System.Collections.Generic;
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
        Html5,
        PlayableAd
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

    /// <summary>
    /// Build context used throughout the build pipeline.
    /// </summary>
    internal class TinyBuildContext
    {
        private readonly TinyBuildReport m_BuildReport = new TinyBuildReport(TinyBuildReport.ProjectNode);

        internal TinyBuildContext(TinyBuildOptions options, List<ITinyBuildStep> buildSteps)
        {
            Options = options;
            BuildSteps = buildSteps;
        }

        /// <summary>
        /// Build options.
        /// </summary>
        public TinyBuildOptions Options { get; }

        /// <summary>
        /// Build steps.
        /// </summary>
        public IReadOnlyList<ITinyBuildStep> BuildSteps { get; }

        /// <summary>
        /// Build report.
        /// </summary>
        internal TinyBuildReport.TreeNode BuildReport => m_BuildReport.Root;

        /// <summary>
        /// Get an artifact directory combining the array of <paramref name="paths"/>.
        /// </summary>
        /// <param name="paths">An array of parts of the path.</param>
        /// <returns>Fully qualified artifact directory.</returns>
        public DirectoryInfo GetArtifactFolder(params string[] paths) => Options.GetArtifactFolder(paths);

        /// <summary>
        /// Get an artifact file combining the array of <paramref name="paths"/>.
        /// </summary>
        /// <param name="paths">An array of parts of the path.</param>
        /// <returns>Fully qualified artifact file.</returns>
        public FileInfo GetArtifactFile(params string[] paths) => Options.GetArtifactFile(paths);

        /// <summary>
        /// Get a build directory combining the array of <paramref name="paths"/>.
        /// </summary>
        /// <param name="paths">An array of parts of the path.</param>
        /// <returns>Fully qualified build directory.</returns>
        public DirectoryInfo GetBuildFolder(params string[] paths) => Options.GetBuildFolder(paths);

        /// <summary>
        /// Get a build file combining the array of <paramref name="paths"/>.
        /// </summary>
        /// <param name="paths">An array of parts of the path.</param>
        /// <returns>Fully qualified build file.</returns>
        public FileInfo GetBuildFile(params string[] paths) => Options.GetBuildFile(paths);
    }

    internal interface ITinyBuildStep
    {
        /// <summary>
        /// Name of the build step. Also used as the progress info when running build steps.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Get whether or not the build step is enabled in the build.
        /// </summary>
        /// <param name="context">The build context.</param>
        /// <returns>Whether or not the build step is enabled.</returns>
        bool Enabled(TinyBuildContext context);

        /// <summary>
        /// Execute the build step.
        /// </summary>
        /// <param name="context">The build context.</param>
        /// <returns>Whether or not the build step succeeded.</returns>
        bool Run(TinyBuildContext context);
    }

    internal interface ITinyBuilder
    {
        ITinyBuildStep[] GetBuildSteps();
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
        /// Registry for the build.
        /// </summary>
        internal IRegistry Registry => Context.Registry;

        /// <summary>
        /// The root export directory path name, defaults to "TinyExport" in the Unity project directory.
        /// </summary>
        public string ExportPath { get; set; } = new DirectoryInfo("TinyExport").FullName;

        /// <summary>
        /// The project root output directory path name.
        /// It combines <see cref="ExportPath"/>, <see cref="TinyProject.Name"/>, <see cref="TinyPlatform"/> and <see cref="TinyBuildConfiguration"/>.
        /// The artifacts and build directories are nested under this directory.
        /// </summary>
        internal string OutputPath => Path.Combine(ExportPath, Project.Name, Platform.ToString().ToLower(), Configuration.ToString().ToLower());

        /// <summary>
        /// The project artifacts directory path name. This directory is used as a staging location for the build.
        /// To get a fully qualified artifact directory or file, use <see cref="GetArtifactFolder"/> or <see cref="GetArtifactFile"/> respectively.
        /// </summary>
        internal string ArtifactPath => Path.Combine(OutputPath, "artifacts");

        /// <summary>
        /// The project build directory path name. This directory will contain the final binaries for the build.
        /// To get a fully qualified build directory or file, use <see cref="GetBuildFolder"/> or <see cref="GetBuildFile"/> respectively.
        /// </summary>
        internal string BuildPath => Path.Combine(OutputPath, "build");

        /// <summary>
        /// Get an artifact directory combining the array of <paramref name="paths"/>.
        /// </summary>
        /// <param name="paths">An array of parts of the path.</param>
        /// <returns>Fully qualified artifact directory.</returns>
        public DirectoryInfo GetArtifactFolder(params string[] paths)
        {
            var path = paths.Any() ? paths.Aggregate((c, n) => Path.Combine(c, n)) : string.Empty;
            return new DirectoryInfo(Path.Combine(ArtifactPath, path));
        }

        /// <summary>
        /// Get an artifact file combining the array of <paramref name="paths"/>.
        /// </summary>
        /// <param name="paths">An array of parts of the path.</param>
        /// <returns>Fully qualified artifact file.</returns>
        public FileInfo GetArtifactFile(params string[] paths)
        {
            var path = paths.Any() ? paths.Aggregate((c, n) => Path.Combine(c, n)) : string.Empty;
            return new FileInfo(Path.Combine(ArtifactPath, path));
        }

        /// <summary>
        /// Get a build directory combining the array of <paramref name="paths"/>.
        /// </summary>
        /// <param name="paths">An array of parts of the path.</param>
        /// <returns>Fully qualified build directory.</returns>
        public DirectoryInfo GetBuildFolder(params string[] paths)
        {
            var path = paths.Any() ? paths.Aggregate((c, n) => Path.Combine(c, n)) : string.Empty;
            return new DirectoryInfo(Path.Combine(BuildPath, path));
        }

        /// <summary>
        /// Get a build file combining the array of <paramref name="paths"/>.
        /// </summary>
        /// <param name="paths">An array of parts of the path.</param>
        /// <returns>Fully qualified build file.</returns>
        public FileInfo GetBuildFile(params string[] paths)
        {
            var path = paths.Any() ? paths.Aggregate((c, n) => Path.Combine(c, n)) : string.Empty;
            return new FileInfo(Path.Combine(BuildPath, path));
        }
    }

    /// <summary>
    /// Object created by the <see cref="TinyBuildPipeline"/> and populated during a build.
    /// </summary>
    public class TinyBuildResult
    {
        /// <summary>
        /// Whether or not the build succeeded.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Duration of the entire build process.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Where the final build binaries are located.
        /// This folder contains the files you need to deploy after a successful build.
        /// </summary>
        public string BuildFolder { get; }

        internal TinyBuildReport.TreeNode BuildReport { get; }

        internal TinyBuildResult(bool success, TimeSpan duration, TinyBuildOptions options, TinyBuildReport.TreeNode buildReport)
        {
            Success = success;
            Duration = duration;
            BuildFolder = options.GetBuildFolder().FullName;
            BuildReport = buildReport;
            BuildReport.Update();
        }
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

    #region Build Steps

    internal class BuildStepCompileScripts : ITinyBuildStep
    {
        public string Name => "Compiling Scripts";
        public bool Enabled(TinyBuildContext context) => true;
        public bool Run(TinyBuildContext context) => TinyBuildUtilities.CompileScripts(context.Options);
    }

    internal class BuildStepValidateProject : ITinyBuildStep
    {
        public string Name => "Validating Project";
        public bool Enabled(TinyBuildContext context) => true;
        public bool Run(TinyBuildContext context) => TinyProjectValidation.Validate(context.Options.Project);
    }

    internal class BuildStepGenerateBindings : ITinyBuildStep
    {
        public string Name => "Generating Bindings";
        public bool Enabled(TinyBuildContext context) => true;
        public bool Run(TinyBuildContext context) => TinyBuildUtilities.RunBindGem(context.Options);
    }

    internal class BuildStepRegenerateTSDefinitionFiles : ITinyBuildStep
    {
        public string Name => "Regenerating TypeScript Definition Files";
        public bool Enabled(TinyBuildContext context) => true;
        public bool Run(TinyBuildContext context) => TinyBuildUtilities.RegenerateTSDefinitionFiles(context.Options);
    }

    #endregion

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
        public static TinyBuildResult BuildAndLaunch()
        {
            if (EditorApplication.isCompiling)
            {
                throw new Exception($"{TinyConstants.ApplicationName}: Exporting a project is not allowed while Unity is compiling.");
            }

            using (var progress = new TinyEditorUtility.ProgressBarScope())
            {
                var workspace = TinyEditorApplication.EditorContext.Workspace;
                var options = WorkspaceBuildOptions;
                var buildResult = Build(options);

                if (buildResult.Success && workspace.Preview)
                {
                    HTTPServer.Instance.ReloadOrOpen(buildResult.BuildFolder, options.Project.Settings.LocalHTTPServerPort);
                }
                else if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    // Don't enter playmode if build failed or preview is not enabled
                    EditorApplication.isPlaying = false;
                }
                return buildResult;
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
        public static TinyBuildResult Build(TinyBuildOptions options)
        {
            if (options?.Project == null)
            {
                throw new ArgumentException($"{TinyConstants.ApplicationName}: invalid build options provided", nameof(options));
            }

            try
            {
                // Platform builder
                ITinyBuilder builder = null;
                switch (options.Platform)
                {
                    case TinyPlatform.Html5:
                        builder = new TinyHTML5Builder();
                        break;
                    case TinyPlatform.PlayableAd:
                        builder = new PlayableAdHTML5Builder();
                        break;
                    default:
                        throw new NotImplementedException(options.Platform.ToString());
                }

                // Common build steps
                var buildSteps = new List<ITinyBuildStep>()
                {
                    new BuildStepCompileScripts(),
                    new BuildStepValidateProject(),
                    new BuildStepGenerateBindings(),
                    new BuildStepRegenerateTSDefinitionFiles()
                };

                // Clean-up legacy build folders structure
                var rootOutputFolder = new DirectoryInfo(options.OutputPath);
                if (rootOutputFolder.Exists)
                {
                    var legacyScriptFolder = new DirectoryInfo(Path.Combine(options.OutputPath, "ScriptAssemblies"));
                    var legacyBindingsFile = new FileInfo(Path.Combine(options.OutputPath, "bind-generated.cs"));
                    var legacyBuildReportFile = new FileInfo(Path.Combine(options.OutputPath, "build-report.json"));
                    if (legacyScriptFolder.Exists || legacyBindingsFile.Exists || legacyBuildReportFile.Exists)
                    {
                        rootOutputFolder.Delete(true);
                    }
                }

                // Clean-up build folder (we preserve artifacts folder)
                if (options.GetBuildFolder().Exists)
                {
                    options.GetBuildFolder().Delete(true);
                }

                // Create all build folders
                options.GetArtifactFolder().Create();
                options.GetBuildFolder().Create();

                // Platform specific build steps
                buildSteps.AddRange(builder.GetBuildSteps());

                var context = new TinyBuildContext(options, buildSteps);
                var title = $"{TinyConstants.ApplicationName} Build {options.Platform.ToString()} {options.Configuration.ToString()}";
                using (var progress = new TinyEditorUtility.ProgressBarScope(title))
                {
                    // Run pre-build handlers
                    CallPreBuildHandlers(progress, options);

                    // Run build steps
                    var buildResult = RunBuildSteps(progress, context);
                    TinyEditorAnalytics.SendBuildEvent(options.Project, buildResult);

                    // Run post-build handlers
                    CallPostBuildHandlers(progress, options, buildResult);
                    return buildResult;
                }
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

        private static TinyBuildResult RunBuildSteps(TinyEditorUtility.ProgressBarScope progress, TinyBuildContext context)
        {
            var startTime = DateTime.Now;
            for (var i = 0; i < context.BuildSteps.Count; ++i)
            {
                var buildStep = context.BuildSteps[i];
                if (!buildStep.Enabled(context))
                {
                    continue;
                }

                progress.Update($"{buildStep.Name}...", (float)i / context.BuildSteps.Count);
                var buildStepStartTime = DateTime.Now;
                try
                {
                    if (!buildStep.Run(context))
                    {
                        TinyEditorAnalytics.SendErrorEvent("TinyBuildPipeline.RunBuildSteps", $"Build step '{buildStep.Name}' failed.");
                        return new TinyBuildResult(false, DateTime.Now - startTime, context.Options, context.BuildReport);
                    }
                }
                catch (Exception ex)
                {
                    TinyEditorAnalytics.SendException($"TinyBuildPipeline.RunBuildSteps: {buildStep.Name}", ex);
                    Debug.LogError($"Build step '{buildStep.Name}' failed with exception: {ex}");
                    return new TinyBuildResult(false, DateTime.Now - startTime, context.Options, context.BuildReport);
                }
            }
            var result = new TinyBuildResult(true, DateTime.Now - startTime, context.Options, context.BuildReport);
            Debug.Log($"{TinyConstants.ApplicationName} project generated in {GetTimeSpanShortString(result.Duration)} at {context.GetBuildFolder().FullName}");
            return result;
        }

        private static bool CallPreBuildHandlers(TinyEditorUtility.ProgressBarScope progress, TinyBuildOptions options)
        {
            foreach (var handler in TinyAttributeScanner.GetMethodAttributes<TinyPreprocessBuildAttribute>())
            {
                var method = handler.Method;
                var handlerName = $"{method.DeclaringType?.FullName}.{method.Name}";
                progress.Update($"Pre-Build Handler: {handlerName}", 0f);

                var parameters = method.GetParameters();
                if (method.IsStatic &&
                    false == method.IsGenericMethod &&
                    parameters.Length == 1 &&
                    parameters[0].ParameterType == typeof(TinyBuildOptions)
                )
                {
                    method.Invoke(null, new object[] { options });
                }
                else
                {
                    Debug.LogWarning($"{method.DeclaringType?.FullName}.{method.Name}: {nameof(TinyPreprocessBuildAttribute)} can " +
                        $"only be applied to static, non-generic methods with a single argument of type {nameof(TinyBuildOptions)}.");
                }
            }
            return true;
        }

        private static bool CallPostBuildHandlers(TinyEditorUtility.ProgressBarScope progress, TinyBuildOptions options, TinyBuildResult buildResult)
        {
            foreach (var handler in TinyAttributeScanner.GetMethodAttributes<TinyPostprocessBuildAttribute>())
            {
                var method = handler.Method;
                var handlerName = $"{method.DeclaringType?.FullName}.{method.Name}";
                progress.Update($"Post-Build Handler: {handlerName}", 1f);

                var parameters = method.GetParameters();
                if (method.IsStatic &&
                    false == method.IsGenericMethod &&
                    parameters.Length == 2 &&
                    parameters[0].ParameterType == typeof(TinyBuildOptions) &&
                    parameters[1].ParameterType == typeof(TinyBuildResult)
                )
                {
                    method.Invoke(null, new object[] { options, buildResult });
                }
                else
                {
                    Debug.LogWarning($"{handlerName}: {nameof(TinyPostprocessBuildAttribute)} can only be applied to static, non-generic " +
                        $"methods with two arguments of types {nameof(TinyBuildOptions)} and {nameof(TinyBuildResult)}, respectively.");
                }
            }
            return true;
        }

        private static string GetTimeSpanShortString(TimeSpan s)
        {
            return s.TotalHours > 1.0 ? $"{s.TotalHours:F1}h" : s.TotalMinutes > 1.0 ? $"{s.TotalMinutes:F1}m" : s.TotalSeconds > 1.0 ? $"{s.TotalSeconds:F1}s" : $"{s.TotalMilliseconds:F0}ms";
        }
    }
}

