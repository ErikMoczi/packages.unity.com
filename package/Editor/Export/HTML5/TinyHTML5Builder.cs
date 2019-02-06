using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Tiny
{
    internal sealed class TinyHTML5Builder : ITinyBuilder
    {
        public const string k_GlobalAssetsName = "UT_ASSETS";
        public const string k_EntityGroupNamespace = "entities";

        public ITinyBuildStep[] GetBuildSteps()
        {
            return new ITinyBuildStep[]
            {
                new BuildStepPackageSettings(),
                new BuildStepPackageRuntime(),
                new BuildStepPackageAssets(),
                new BuildStepGenerateEntityGroups(),
                new BuildStepPackageBindings(),
                new BuildStepPackageScripts(),
                new BuildStepGenerateMain(),
                new BuildStepInstallWSClientCode(),
                new BuildStepInstallWebPDecompressorCode(),
                new BuildStepGenerateHTML(),
                new BuildStepGenerateBuildReport()
            };
        }

        /// <summary>
        /// Packages settings to `settings.js`
        /// </summary>
        private class BuildStepPackageSettings : ITinyBuildStep
        {
            public string Name => "Packaging Settings";

            public bool Enabled(TinyBuildContext context) => true;

            public static FileInfo OutputFile(TinyBuildContext context) => context.GetBuildFile("settings.js");

            public bool Run(TinyBuildContext context)
            {
                var options = context.Options;
                var settings = options.Project.Settings;
                var writer = new TinyCodeWriter(CodeStyle.JavaScript);

                writer.Line($"var Module = {{TOTAL_MEMORY: {settings.MemorySize * 1024 * 1024}}};")
                      .Line();

                if (options.Configuration != TinyBuildConfiguration.Release)
                {
                    writer.Line($"Module.WSServerURL = {WebSocketServer.Instance.URL.AbsoluteUri.DoubleQuoted()};");
                    writer.Line($"Module.ProfilerServerURL = {ProfilerServer.URL.AbsoluteUri.DoubleQuoted()};");
                    writer.Line();
                }

                // <HACK>
                // Workaround for issue `UTINY-1091`
                // Systems will not force binding generation to create namespace objects
                var namespaces = new HashSet<string>();
                foreach (var m in options.Project.Module.Dereference(options.Registry).EnumerateDependencies())
                {
                    // If we don't have types our module namespace is not generated automatically
                    if (!m.Types.Any())
                    {
                        var parts = m.Namespace.Split('.');
                        var name = parts[0];

                        namespaces.Add(name);

                        for (var i = 1; i < parts.Length; i++)
                        {
                            name = $"{name}.{parts[i]}";
                            namespaces.Add(name);
                        }
                    }
                }

                if (namespaces.Count > 0)
                {
                    writer.Line("/*");
                    writer.Line(" * Workaround for issue UTINY-1091");
                    writer.Line(" */");
                }

                foreach (var n in namespaces)
                {
                    writer.Line(!n.Contains('.') ? $"var {n} = {n} || {{}}" : $"{n} = {n} || {{}}");
                }
                // <HACK>

                if (writer.Length <= 0)
                {
                    // No settings, nothing to write
                    return true;
                }

                PrependGeneratedHeader(writer, options.Project.Name);
                File.WriteAllText(OutputFile(context).FullName, writer.ToString(), Encoding.UTF8);
                return true;
            }
        }

        /// <summary>
        /// Packages the runtime to `runtime.js`
        /// </summary>
        private class BuildStepPackageRuntime : ITinyBuildStep
        {
            public string Name => "Packaging Runtime";

            public bool Enabled(TinyBuildContext context) => true;

            public static FileInfo OutputFile(TinyBuildContext context) => context.GetBuildFile("runtime.js");

            public bool Run(TinyBuildContext context)
            {
                var options = context.Options;
                var buildReport = context.BuildReport;
                var runtimeVariant = TinyRuntimeInstaller.GetJsRuntimeVariant(options);
                var runtimePath = TinyRuntimeInstaller.GetRuntimeDirectory(options.Platform, options.Configuration);
                var report = buildReport.AddChild(TinyBuildReport.RuntimeNode);

                // special case the modularizable build for release builds with symbols disabled
                if (options.Configuration == TinyBuildConfiguration.Release &&
                    options.Platform == TinyPlatform.Html5 &&
                    options.ProjectSettings.SymbolsInReleaseBuild == false)
                {
                    var runtimeFile = Path.Combine(runtimePath, "RuntimeGemini.js");
                    var buildFile = context.GetBuildFile("runtime.js");
                    var dependencies = options.Project.Module.Dereference(options.Project.Registry).EnumerateDependencies();
                    var regex = new System.Text.RegularExpressions.Regex(@"\/\*if\(([\s\S]*?)\)\*\/([\s\S]*?)\/\*endif\(([\s\S]*?)\)\*\/");
                    var runtime = File.ReadAllText(runtimeFile);
                    runtime = regex.Replace(runtime, match => match.Groups[match.Groups[1].Value.Split('|').Any(module => dependencies.WithName("UTiny." + module).Any() || module == "RendererGLWebGL") ? 2 : 3].Value);
                    File.WriteAllText(buildFile.FullName, runtime);
                    report.AddChild(buildFile);
                    return true;
                }

                var runtimeFiles = new DirectoryInfo(runtimePath).GetFiles(runtimeVariant + "*", SearchOption.TopDirectoryOnly);
                foreach (var runtimeFile in runtimeFiles)
                {
                    if (runtimeFile.Name.EndsWith(".js.symbols") || runtimeFile.Name.EndsWith(".js.map") || runtimeFile.Name.EndsWith(".dll"))
                    {
                        continue;
                    }
                    var buildFile = context.GetBuildFile($"runtime{runtimeFile.Extension}");
                    report.AddChild(runtimeFile.CopyTo(buildFile.FullName));
                }
                return true;
            }
        }

        /// <summary>
        /// Packages assets to `assets.js` or `assets/*.*`
        /// </summary>
        private class BuildStepPackageAssets : ITinyBuildStep
        {
            public string Name => "Packaging Assets";

            public bool Enabled(TinyBuildContext context) => true;

            public static FileInfo OutputFile(TinyBuildContext context) => context.GetBuildFile("assets.js");

            public bool Run(TinyBuildContext context)
            {
                var options = context.Options;
                var buildReport = context.BuildReport;
                var artifactFolder = context.GetArtifactFolder("assets");

                // Delete previous output directories
                if (artifactFolder.Exists)
                {
                    artifactFolder.Delete(true);
                }
                artifactFolder.Create();

                // Export assets to intermediate directory
                var exportInfos = TinyAssetExporter.Export(options, artifactFolder);

                // Generate assets file content
                var writer = new TinyCodeWriter();
                PrependGeneratedHeader(writer, options.Project.Name);

                var reportAssets = buildReport.AddChild(TinyBuildReport.AssetsNode);
                var reportJavaScript = reportAssets.AddChild("JavaScript");

                using (var jsdoc = new TinyJsdoc.Writer(writer))
                {
                    jsdoc.Type("object");
                    jsdoc.Desc("Map containing URLs for all assets.  If assets are included as base64 blobs, these will be data URLs.");
                    jsdoc.Line("@example var assetUrl = UT_ASSETS[\"MyCustomAsset\"]");
                }

                long totalBase64Size = 0;
                using (writer.Scope($"var {k_GlobalAssetsName} ="))
                {
                    var i = 0;
                    foreach (var exportInfo in exportInfos)
                    {
                        TinyBuildReport.TreeNode reportAsset;
                        if (string.IsNullOrEmpty(exportInfo.AssetInfo.AssetPath))
                        {
                            reportAsset = reportAssets.AddChild(exportInfo.AssetInfo.Name, 0);
                        }
                        else
                        {
                            reportAsset = reportAssets.AddChild(exportInfo.AssetInfo.AssetPath, 0, exportInfo.AssetInfo.Object);
                        }

                        var settings = TinyUtility.GetAssetExportSettings(options.Project, exportInfo.AssetInfo.Object);
                        if (settings.Embedded)
                        {
                            foreach (var file in exportInfo.ExportedFiles)
                            {
                                var buffer = File.ReadAllBytes(file.FullName);
                                var base64 = Convert.ToBase64String(buffer);
                                var fileExtension = Path.GetExtension(file.FullName).ToLower();

                                string mimeType;
                                switch (fileExtension)
                                {
                                    case ".png":
                                        mimeType = "image/png";
                                        break;
                                    case ".jpg":
                                    case ".jpeg":
                                        mimeType = "image/jpeg";
                                        break;
                                    case ".webp":
                                        mimeType = "image/webp";
                                        break;
                                    case ".mp3":
                                        mimeType = "audio/mpeg";
                                        break;
                                    case ".wav":
                                        mimeType = "audio/wav";
                                        break;
                                    case ".json":
                                        mimeType = "application/json";
                                        break;
                                    case ".ttf":
                                        mimeType = "font/truetype";
                                        break;
                                    default:
                                        Debug.LogWarningFormat("Asset {0} has unknown extension, included as text/plain in assets", file);
                                        mimeType = "text/plain";
                                        break;
                                }

                                var comma = i != 0 ? "," : "";
                                writer.Line($"{comma}\"{Path.GetFileNameWithoutExtension(file.Name)}\": \"data:{mimeType};base64,{base64}\"");
                                i++;

                                reportAsset.AddChild(Persistence.GetPathRelativeToProjectPath(file.FullName), Encoding.ASCII.GetBytes(base64), exportInfo.AssetInfo.Object);
                                totalBase64Size += base64.Length;

                                file.Delete();
                            }
                        }
                        else
                        {
                            foreach (var file in exportInfo.ExportedFiles)
                            {
                                var comma = i != 0 ? "," : "";
                                writer.Line($"{comma}\"{Path.GetFileNameWithoutExtension(file.Name)}\": \"assets/{file.Name}\"");
                                i++;

                                reportAsset.AddChild(file, exportInfo.AssetInfo.Object);
                            }
                        }
                    }
                }

                writer.Line();
                writer.WriteRaw("var UT_ASSETS_SETUP = ");
                {
                    var registry = new TinyRegistry();
                    Persistence.LoadAllModules(registry);
                    registry.Context = options.Context;
                    var entityGroup = registry.CreateEntityGroup(TinyId.New(), "Assets_Generated");
                    TinyAssetEntityGroupGenerator.Generate(registry, options.Project, entityGroup);
                    TinyEditorExtensionsGenerator.Generate(registry, options.Project, entityGroup);
                    EntityGroupSetupVisitor.WriteEntityGroupSetupFunction(writer, options.Project, entityGroup, EntityGroupSetupOptions.Resources);
                }

                // Write `assets.js`
                var buildFile = OutputFile(context);
                File.WriteAllText(buildFile.FullName, writer.ToString());

                reportJavaScript.Item.Size = buildFile.Length - totalBase64Size;

                // Remaining assets are binplaced
                var buildFolder = context.GetBuildFolder("assets");
                foreach (var exportInfo in exportInfos)
                {
                    foreach (var file in exportInfo.ExportedFiles)
                    {
                        if (!file.Exists)
                        {
                            // this asset has been packaged already
                            continue;
                        }

                        if (!buildFolder.Exists)
                        {
                            buildFolder.Create();
                        }
                        file.MoveTo(Path.Combine(buildFolder.FullName, file.Name));
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Packages entity group objects to `entities.js`.
        /// Since we don't have a scene format, groups are written as setup functions.
        /// </summary>
        private class BuildStepGenerateEntityGroups : ITinyBuildStep
        {
            public string Name => "Generating Entity Groups";

            public bool Enabled(TinyBuildContext context) => true;

            public static FileInfo OutputFile(TinyBuildContext context) => context.GetBuildFile("entities.js");

            public bool Run(TinyBuildContext context)
            {
                var options = context.Options;
                var writer = new TinyCodeWriter(CodeStyle.JavaScript);
                var report = context.BuildReport.GetOrAddChild(TinyBuildReport.CodeNode).AddChild();

                PrependGeneratedHeader(writer, options.Project.Name);

                // @NOTE Namespaces are generated through through `BindGen.exe`
                // e.g. `{ENTITY_GROUPS}.{PROJECT_NAMESPACE}.{GROUP_NAME}` will already exist as a component

                using (var visitor = new EntityGroupSetupVisitor { Writer = writer, Report = report })
                {
                    options.Project.Visit(visitor);
                }

                var buildFile = OutputFile(context);
                File.WriteAllText(buildFile.FullName, writer.ToString(), Encoding.UTF8);
                report.Reset(buildFile);
                return true;
            }
        }

        /// <summary>
        /// Writes components, structs and enums `bindings.js`
        /// </summary>
        private class BuildStepPackageBindings : ITinyBuildStep
        {
            public string Name => "Packaging Bindings";

            public bool Enabled(TinyBuildContext context) => true;

            public static FileInfo OutputFile(TinyBuildContext context) => context.GetBuildFile("bindings.js");

            public bool Run(TinyBuildContext context)
            {
                // @NOTE `bind-generated.js` is the exported name from the `BindGen.exe` application
                var artifactFile = context.GetArtifactFile("bindings", "bind-generated.js");
                var buildFile = OutputFile(context);
                artifactFile.CopyTo(buildFile.FullName);

                using (var writer = File.AppendText(buildFile.FullName))
                {
                    var extensionFile = new FileInfo(Path.Combine(TinyConstants.PackagePath, "RuntimeExtensions/bindings.js.txt"));
                    writer.Write(File.ReadAllText(extensionFile.FullName));
                }

                context.BuildReport.GetOrAddChild("Code").AddChild(buildFile);
                return true;
            }
        }

        /// <summary>
        /// Writes user code to `code.js`
        /// Any free standing code written by users is written to this file
        /// </summary>
        private class BuildStepPackageScripts : ITinyBuildStep
        {
            public string Name => "Packaging Scripts";

            public bool Enabled(TinyBuildContext context) => true;

            public static FileInfo OutputFile(TinyBuildContext context) => context.GetBuildFile("code.js");

            public bool Run(TinyBuildContext context)
            {
                var options = context.Options;
                var project = options.Project;
                var registry = project.Registry;
                var module = project.Module.Dereference(registry);
                var buildReport = context.BuildReport.GetOrAddChild(TinyBuildReport.CodeNode).AddChild();

                // Install modules as needed
                var isSourceCodeLinked = UsesLinkedSourceCode(context);

                if (isSourceCodeLinked)
                {
                    var tsOutFile = TinyScriptUtility.GetTypeScriptOutputFile(options);
                    if (tsOutFile.Exists)
                    {
                        buildReport.AddChild(tsOutFile);
                    }
                    buildReport.Reset("Linked Code Files");
                }
                else
                {
                    var writer = new TinyCodeWriter(CodeStyle.JavaScript);

                    PrependGeneratedHeader(writer, options.Project.Name);

                    var tsOutFile = TinyScriptUtility.GetTypeScriptOutputFile(options);
                    if (tsOutFile.Exists)
                    {
                        var reportSystemPos = writer.Length;
                        writer.WriteRaw(File.ReadAllText(tsOutFile.FullName))
                            .Line();
                        buildReport.AddChild(tsOutFile);
                    }

                    var buildFile = OutputFile(context);
                    File.WriteAllText(buildFile.FullName, writer.ToString(), Encoding.UTF8);
                    buildReport.Reset(buildFile);
                }
                return true;
            }
        }

        /// <summary>
        /// Generates entry point for the applicaton `main.js`
        /// This script will contain the system scheduling, window setup and initial group loading
        /// </summary>
        private class BuildStepGenerateMain : ITinyBuildStep
        {
            public string Name => "Generating Main";

            public bool Enabled(TinyBuildContext context) => true;

            public static FileInfo OutputFile(TinyBuildContext context) => context.GetBuildFile("main.js");

            public bool Run(TinyBuildContext context)
            {
                var options = context.Options;
                var scripting = options.Context.GetScriptMetadata();
                var project = options.Project;
                var registry = project.Registry;
                var module = project.Module.Dereference(registry);
                var writer = new TinyCodeWriter();

                PrependGeneratedHeader(writer, options.Project.Name);

                var distVersionFile = new FileInfo("Tiny/version.txt");
                var versionString = "internal";
                if (distVersionFile.Exists)
                {
                    versionString = File.ReadAllText(distVersionFile.FullName);
                }
                writer.LineFormat("console.log('runtime version: {0}');", versionString)
                      .Line();

                var namespaces = new Dictionary<string, string>();
                var exportedModules = module.EnumerateDependencies().ToList();
                foreach (var m in exportedModules)
                {
                    if (string.IsNullOrEmpty(m.Namespace))
                    {
                        continue;
                    }

                    if (m.ExportFlags.HasFlag(TinyExportFlags.EditorExtension))
                    {
                        continue;
                    }

                    if (m.IsRuntimeIncluded)
                    {
                        writer.Line($"ut.importModule({m.Namespace});");
                        continue;
                    }

                    namespaces.TryGetValue(m.Namespace, out var content);
                    content += m.Documentation.Summary;
                    namespaces[m.Namespace] = content;
                }

                using (writer.Scope("ut.main = function()"))
                {
                    WriteEntityFilterCode(scripting, writer);

                    WriteComponentBehaviourCode(scripting, writer);

                    // Create and setup the world
                    writer.Line("// Singleton world");
                    writer.Line("var world = new ut.World();");

                    // Setup the scheduler                
                    writer.Line();
                    writer.Line("// Schedule all systems");
                    writer.Line("var scheduler = world.scheduler();");

                    // Schedule all systems
                    var systems = scripting.GetSystemExecutionOrder();
                    foreach (var system in systems)
                    {
                        if (system.IsRuntime)
                        {
                            continue;
                        }
                        var systemName = system.QualifiedName;
                        // populate the update function by instancing the user-defined system classes
                        if (system.IsBehaviour)
                        {
                            writer.Line(
                                $"{systemName}JS.update = {system.Behaviour.QualifiedName}.Instance._Make{system.BehaviourMethod}();");
                        }
                        else
                        {
                            writer.Line($"{systemName}JS.update = new {systemName}()._MakeSystemFn();");
                        }
                    }

                    foreach (var system in systems)
                    {
                        var systemName = system.QualifiedName;
                        if (false == system.IsRuntime)
                        {
                            systemName += "JS";
                        }
                        writer.LineFormat("scheduler.schedule({0});", systemName);
                    }

                    writer.Line();
                    writer.Line("// Initialize all configuration data");

                    // Write configurations
                    var visitorContext = new EntityGroupSetupVisitor.VisitorContext
                    {
                        Project = project,
                        Module = project.Module.Dereference(project.Registry),
                        Registry = project.Registry,
                        Writer = writer
                    };

                    var configuration = project.Configuration.Dereference(registry);
                    foreach (var component in configuration.Components)
                    {
                        var moduleContainingType = registry.FindAllByType<TinyModule>().FirstOrDefault(m => m.Types.Contains(component.Type));
                        if (null == moduleContainingType)
                        {
                            continue;
                        }

                        if (!module.EnumerateDependencies().Contains(moduleContainingType))
                        {
                            // Silently ignore components if the module is not included.
                            // This is by design to preserve user data
                            continue;
                        }

                        var type = component.Type.Dereference(component.Registry);
                        var componentIndex = visitorContext.ComponentIndexMap.GetOrAddValue(component);
                        writer.Line($"var c{componentIndex} = world.getConfigData({TinyScriptUtility.GetJsTypeName(type)});");
                        component.Properties.Visit(new EntityGroupSetupVisitor.ComponentVisitor
                        {
                            VisitorContext = visitorContext,
                            Path = $"c{componentIndex}",
                        });
                        writer.Line($"world.setConfigData(c{componentIndex});");
                    }

                    writer.Line();
                    writer.Line("// Create and initialize all resource entities");
                    writer.LineFormat("UT_ASSETS_SETUP(world);");

                    var startupEntityGroup = module.StartupEntityGroup.Dereference(module.Registry);

                    if (null != startupEntityGroup)
                    {
                        // The streaming service is always included
                        writer.Line();
                        writer.Line("// Create and initialize all startup entities");
                        writer.Line($"ut.EntityGroup.instantiate(world, \"{module.Namespace}.{module.StartupEntityGroup.Dereference(module.Registry).Name}\");");
                    }
                    else
                    {
                        Debug.LogError($"{TinyConstants.ApplicationName}: BuildError - No startup group has been set");
                    }

                    // Store world handle in wsclient in debug/development configs
                    if (options.Configuration != TinyBuildConfiguration.Release)
                    {
                        writer.Line();
                        writer.Line("// Set up the WebSocket client");
                        writer.Line("ut._wsclient = ut._wsclient || {};");
                        writer.Line("ut._wsclient.world = world;");
                    }

                    writer.Line();
                    writer.Line("// Start the player loop");
                    writer.Line("try { ut.Runtime.Service.run(world); } catch (e) { if (e !== 'SimulateInfiniteLoop') throw e; }");

                    if (exportedModules.Contains(registry.FindByName<TinyModule>("UTiny.Profiler")) &&
                        options.Configuration != TinyBuildConfiguration.Release &&
                        options.AutoConnectProfiler)
                    {
                        writer.Line();
                        writer.Line("ut.Profiler.startProfiling(world);");
                    }
                }

                var buildFile = OutputFile(context);
                File.WriteAllText(buildFile.FullName, writer.ToString(), Encoding.UTF8);
                context.BuildReport.GetOrAddChild(TinyBuildReport.CodeNode).AddChild(buildFile);
                return true;
            }

            private static void WriteComponentBehaviourCode(ScriptMetadata scripting, TinyCodeWriter writer)
            {
                foreach (var behaviour in scripting.Behaviours)
                {
                    writer.Line($"{behaviour.QualifiedName}.Instance = new {behaviour.QualifiedName}();");
                    writer.Line($"{behaviour.QualifiedName}._StateType = {behaviour.QualifiedName}_State;");
                    writer.Line(
                        $@"{behaviour.QualifiedName}.prototype._GetFilter = function() {{ if (!this.{behaviour.MainFilterField.Name}) {{ this.{behaviour.MainFilterField.Name} = new {behaviour.MainFilter.QualifiedName}(); }} return this.{behaviour.MainFilterField.Name}; }}");
                }
            }

            private static void WriteEntityFilterCode(ScriptMetadata scripting, TinyCodeWriter writer)
            {
                foreach (var filter in scripting.Filters)
                {
                    // static _Components: ut.Component[]
                    writer.Line($"{filter.QualifiedName}._Components = [ut.Entity, ");
                    writer.IncrementIndent();
                    writer.Line(string.Join(", ",
                        filter.Fields.Where(f => f.Type == ScriptFieldType.Component && !f.IsOptional)
                            .Select(f => f.QualifiedName)));
                    writer.DecrementIndent();
                    writer.Line("];");

                    // private _Read(world: ut.World, entity: ut.Entity): void
                    writer.Line($"{filter.QualifiedName}.prototype.Read = function(world, entity) {{");
                    writer.IncrementIndent();

                    foreach (var field in filter.Fields)
                    {
                        switch (field.Type)
                        {
                            case ScriptFieldType.Entity:
                                writer.Line($"this.{field.Name} = entity;");
                                break;
                            case ScriptFieldType.World:
                                writer.Line($"this.{field.Name} = world;");
                                break;
                            case ScriptFieldType.Component:
                                if (field.IsOptional)
                                {
                                    writer.Line(
                                        $"this.{field.Name} = world.hasComponent(entity, {field.QualifiedName}) ? world.getComponentData(entity, {field.QualifiedName}) : undefined;");
                                }
                                else
                                {
                                    writer.Line($"this.{field.Name} = world.getComponentData(entity, {field.QualifiedName});");
                                }

                                break;
                        }
                    }

                    writer.DecrementIndent();
                    writer.Line("};");

                    // private Reset(): void
                    writer.Line($"{filter.QualifiedName}.prototype.Reset = function() {{");
                    writer.IncrementIndent();

                    foreach (var field in filter.Fields)
                    {
                        writer.Line($"this.{field.Name} = undefined;");
                    }

                    writer.DecrementIndent();
                    writer.Line("};");

                    // private Write(world, entity): void
                    writer.Line($"{filter.QualifiedName}.prototype.Write = function(world, entity) {{");
                    writer.IncrementIndent();

                    foreach (var field in filter.Fields)
                    {
                        if (field.Type != ScriptFieldType.Component)
                        {
                            continue;
                        }
                        if (field.IsOptional)
                        {
                            writer.Line($"if (this.{field.Name}) {{ world.setOrAddComponentData(entity, this.{field.Name}); }}");
                        }
                        else
                        {
                            writer.Line($"world.setComponentData(entity, this.{field.Name});");
                        }
                    }

                    writer.DecrementIndent();
                    writer.Line("};");

                    // public ForEach
                    // needs codegen since the underlying implementation of world.forEach is using the Function arg count...
                    writer.Line($"{filter.QualifiedName}.prototype.ForEach = function(world, callback) {{");
                    writer.IncrementIndent();

                    writer.Line("var _this = this;");
                    writer.WriteIndent();
                    writer.WriteRaw("world.forEach(this.constructor._Components, function($entity");
                    foreach (var field in filter.Fields)
                    {
                        if (field.Type == ScriptFieldType.Component && !field.IsOptional)
                        {
                            writer.WriteRaw(", " + field.Name);
                        }
                    }

                    writer.WriteRaw(") {" + writer.CodeStyle.NewLine);
                    writer.IncrementIndent();

                    writer.Line("_this.Read(world, $entity);");
                    writer.Line("callback($entity);");
                    writer.Line("if (world.exists($entity)) { _this.Write(world, $entity); }");

                    writer.DecrementIndent();
                    writer.Line("});");

                    writer.DecrementIndent();
                    writer.Line("};");
                }
            }
        }

        /// <summary>
        /// Generates `wsclient.js` to handle live-linking
        /// </summary>
        private class BuildStepInstallWSClientCode : ITinyBuildStep
        {
            public string Name => "Installing WebSocket Client Code";

            public bool Enabled(TinyBuildContext context) => context.Options.Configuration != TinyBuildConfiguration.Release;

            public static FileInfo OutputFile(TinyBuildContext context) => context.GetBuildFile("wsclient.js");

            public bool Run(TinyBuildContext context)
            {
                var buildFile = OutputFile(context);
                var toolFile = new FileInfo(Path.Combine(TinyRuntimeInstaller.GetToolDirectory("wsclient"), buildFile.Name));
                context.BuildReport.GetOrAddChild(TinyBuildReport.CodeNode).AddChild(toolFile.CopyTo(buildFile.FullName));
                return true;
            }
        }

        /// <summary>
        /// Generates `libwebp.js` to handle WebP decompressor
        /// </summary>
        private class BuildStepInstallWebPDecompressorCode : ITinyBuildStep
        {
            public string Name => "Installing WebP Decompressor Code";

            public bool Enabled(TinyBuildContext context)
            {
                var options = context.Options;
                var project = options.Project;
                var module = project.Module.Dereference(options.Registry);
                var webpUsed = AssetIterator.EnumerateRootAssets(module)
                    .Select(assetInfo => assetInfo.Object)
                    .OfType<Texture2D>()
                    .Select(texture => TinyUtility.GetAssetExportSettings(project, texture))
                    .OfType<TinyTextureSettings>()
                    .Any(textureSettings => textureSettings.FormatType == TextureFormatType.WebP);

                // Warn about WebP usages
                if (options.Project.Settings.IncludeWebPDecompressor)
                {
                    if (!webpUsed)
                    {
                        Debug.LogWarning("This project does not uses the WebP texture format, but includes the WebP decompressor code. To reduce build size, it is recommended to disable \"Include WebP Decompressor\" in project settings.");
                    }
                    return true;
                }
                else // WebP decompressor not included, do not copy to binary dir
                {
                    if (webpUsed)
                    {
                        Debug.LogWarning("This project uses the WebP texture format, but does not include the WebP decompressor code. The content will not load in browsers that do not natively support the WebP format. To ensure maximum compatibility, enable \"Include WebP Decompressor\" in project settings.");
                    }
                    return false;
                }
            }

            public static FileInfo OutputFile(TinyBuildContext context) => context.GetBuildFile("libwebp.js");

            public bool Run(TinyBuildContext context)
            {
                var buildFile = OutputFile(context);
                var toolFile = new FileInfo(Path.Combine(TinyRuntimeInstaller.GetToolDirectory("libwebp"), buildFile.Name));
                context.BuildReport.GetOrAddChild(TinyBuildReport.CodeNode).AddChild(toolFile.CopyTo(buildFile.FullName));
                return true;
            }
        }

        /// <summary>
        /// Outputs the final `index.html` file
        /// </summary>
        private class BuildStepGenerateHTML : ITinyBuildStep
        {
            public string Name => "Generating HTML";

            public bool Enabled(TinyBuildContext context) => true;

            public static FileInfo OutputFile(TinyBuildContext context) => context.GetBuildFile("index.html");

            public bool Run(TinyBuildContext context)
            {
                var options = context.Options;
                var project = options.Project;
                var registry = project.Registry;

                var isSourceCodeLinked = UsesLinkedSourceCode(context);

                var settingsFile = BuildStepPackageSettings.OutputFile(context);
                var runtimeFile = BuildStepPackageRuntime.OutputFile(context);
                var bindingsFile = BuildStepPackageBindings.OutputFile(context);
                var assetsFile = BuildStepPackageAssets.OutputFile(context);
                var entityGroupsFile = BuildStepGenerateEntityGroups.OutputFile(context);
                var codeFile = isSourceCodeLinked ? null : BuildStepPackageScripts.OutputFile(context);
                var mainFile = BuildStepGenerateMain.OutputFile(context);
                var webSocketClientFile = BuildStepInstallWSClientCode.OutputFile(context);
                var webpDecompressorFile = BuildStepInstallWebPDecompressorCode.OutputFile(context);

                // nb: this writer is not HTML-friendly
                var writer = new TinyCodeWriter()
                {
                    CodeStyle = new CodeStyle()
                    {
                        BeginBrace = string.Empty,
                        EndBrace = string.Empty,
                        codeBraceLayout = CodeBraceLayout.EndOfLine,
                        Indent = "  ",
                        NewLine = Environment.NewLine
                    }
                };
                writer.Line("<!DOCTYPE html>");
                using (writer.Scope("<html>"))
                {
                    using (writer.Scope("<head>"))
                    {
                        writer.Line("<meta charset=\"UTF-8\">");
                        writer.Line("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
                        if (UsesAdSupport(project))
                        {
                            writer.Line("<script src=\"mraid.js\"></script>");
                        }

                        if (project.Settings.RunBabel)
                        {
                            // Babelize user code
                            var title = $"{TinyConstants.ApplicationName} Build";
                            const string messageFormat = "Transpiling {0} to ECMAScript 5";

                            using (var progress = new TinyEditorUtility.ProgressBarScope(title, "Transpiling to ECMAScript 5"))
                            {
                                // We only need to transpile user authored code
                                if (!isSourceCodeLinked)
                                {
                                    progress.Update(string.Format(messageFormat, codeFile.Name));
                                    TinyShell.RunTool("babel",
                                        $"-i {codeFile.FullName.DoubleQuoted()}",
                                        $"-o {codeFile.FullName.DoubleQuoted()}");
                                    progress.Update(string.Format(messageFormat, codeFile.Name), 1);
                                }
                            }
                        }

                        // Gather all game files (order is important)
                        var files = new List<FileInfo>
                        {
                            settingsFile,
                            runtimeFile,
                            bindingsFile,
                            assetsFile,
                            entityGroupsFile
                        };

                        if (isSourceCodeLinked)
                        {
                            var tsOutFile = TinyScriptUtility.GetTypeScriptOutputFile(options);
                            if (tsOutFile.Exists)
                                files.Add(tsOutFile);
                        }
                        else
                        {
                            files.Add(codeFile);
                        }

                        files.Add(mainFile);
                        files.Add(webSocketClientFile);
                        files.Add(webpDecompressorFile);

                        files = files.Where(file => file != null && file.Exists).ToList();

                        // Extra steps for Release config
                        if (options.Configuration == TinyBuildConfiguration.Release)
                        {
                            var gameFile = context.GetBuildFile("game.js");
                            if (project.Settings.MinifyJavaScript)
                            {
                                // Minify JavaScript
                                using (new TinyEditorUtility.ProgressBarScope($"{TinyConstants.ApplicationName} Build", "Minifying JavaScript Code..."))
                                {
                                    TinyShell.RunTool("minify",
                                        $"-i {string.Join(" ", files.Select(f => f.FullName.DoubleQuoted()))}",
                                        $"-o {gameFile.FullName.DoubleQuoted()}");
                                }
                            }
                            else
                            {
                                // concatenate code
                                using (var sw = new StreamWriter(gameFile.FullName, false, Encoding.UTF8))
                                {
                                    foreach (var file in files)
                                    {
                                        sw.Write(File.ReadAllText(file.FullName));
                                        sw.WriteLine();
                                    }
                                }
                            }

                            // clean-up
                            files.ForEach(file => file.Delete());

                            // Package as single html file
                            if (project.Settings.SingleFileHtml || options.Platform == TinyPlatform.PlayableAd)
                            {
                                writer.Line("<script type=\"text/javascript\">");
                                writer.WriteRaw(File.ReadAllText(gameFile.FullName));
                                writer.Line();
                                writer.Line("</script>");
                                gameFile.Delete();
                            }
                            else
                            {
                                writer.LineFormat("<script src=\"{0}\"></script>", gameFile.Name);
                            }
                        }
                        else
                        {
                            var projectDir = new DirectoryInfo(".").FullName;
                            var packageFileIndex = 1;
                            foreach (var file in files)
                            {
                                var path = file.Name;
                                if (isSourceCodeLinked)
                                {
                                    var fullPath = file.FullName;
                                    if (!fullPath.StartsWith(projectDir))
                                    {
                                        // FullName resolves the physical file location, not the AssetDatabase path

                                        // /TinyExport contains all generated code files
                                        // /Assets contains all project code files
                                        // /Packages contains all **embedded** package code files

                                        // heuristic: if a code file is not physically located under the project root
                                        //    then it's assumed to be in a non-embedded package, and we should not link
                                        //    directly to it (Packman stores packages in a cache outside the project root).

                                        // Note: we could add a route to the HTTP server allowing static content to come
                                        //    from any location on disk, but it would be a security concern...

                                        path = $"package_code_{packageFileIndex:000}.js";
                                        ++packageFileIndex;

                                        var tmpFile = context.GetBuildFile(path);

                                        File.WriteAllText(tmpFile.FullName, File.ReadAllText(fullPath, Encoding.UTF8), Encoding.UTF8);
                                    }
                                    else if (!fullPath.Contains(options.ExportPath) || fullPath.Contains(TinyScriptUtility.ScriptAssembliesDirectory))
                                    {
                                        // heuristic: within the project root, but not in TinyExport -> direct link
                                        path = "/~project" + fullPath.Substring(projectDir.Length).ToForwardSlash();
                                    }
                                }
                                
                                if (options.Platform == TinyPlatform.PlayableAd)
                                {
                                    writer.Line("<script type=\"text/javascript\">");
                                    writer.WriteRaw(File.ReadAllText(file.FullName));
                                    writer.Line();
                                    writer.Line("</script>");
                                    file.Delete();
                                }
                                else
                                {
                                    writer.LineFormat("<script src=\"{0}\"></script>", path);
                                }
                            }
                        }
                        if (context.Options.Platform != TinyPlatform.PlayableAd || options.Configuration == TinyBuildConfiguration.Release)
                        {
                            writer.Line("<script>window.addEventListener('load',function(){ut._HTML.main()})</script>"); // Kick off Tiny app launch once all scripts have loaded
                        }
                        writer.LineFormat("<title>{0}</title>", project.Name);
                        writer.CodeStyle.EndBrace = "</head>";
                    }
                    using (writer.Scope("<body>"))
                    {
                        if (context.Options.Platform == TinyPlatform.PlayableAd && options.Configuration != TinyBuildConfiguration.Release)
                        {
                            writer.Line("<div id=\"unity_ads_bootstrap_container\" />");
                        }
                        writer.CodeStyle.EndBrace = "</body>";
                    }
                    writer.CodeStyle.EndBrace = "</html>";
                }

                // Write final index.html file
                var htmlFile = OutputFile(context);
                File.WriteAllText(htmlFile.FullName, writer.ToString(), Encoding.UTF8);
                return true;
            }
        }

        /// <summary>
        /// Generates the `build-report.json` file
        /// </summary>
        private class BuildStepGenerateBuildReport : ITinyBuildStep
        {
            public string Name => "Generating Build Report";

            public bool Enabled(TinyBuildContext context) => true;

            public bool Run(TinyBuildContext context)
            {
                var buildReportFile = context.GetArtifactFile("build-report.json");
                File.WriteAllText(buildReportFile.FullName, context.BuildReport.ToString(), Encoding.UTF8);
                return true;
            }
        }

        private static void PrependGeneratedHeader(TinyCodeWriter writer, string name)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"/**");
            builder.AppendLine($" * {TinyConstants.ApplicationName.ToUpperInvariant()} GENERATED CODE, DO NOT EDIT BY HAND");
            builder.AppendLine($" * @project {name}");
            builder.AppendLine($" */");
            builder.AppendLine();
            writer.Prepend(builder.ToString());
        }

        private static string EscapeJsString(string content)
        {
            return Unity.Properties.Serialization.Json.EncodeJsonString(content);
        }

        public static bool UsesLinkedSourceCode(TinyBuildContext context)
        {
            return context.Options.Configuration != TinyBuildConfiguration.Release && context.Options.Platform != TinyPlatform.PlayableAd &&
                context.Options.Project.Settings.LinkToSource;
        }

        public static bool UsesAdSupport(TinyProject project)
        {
            var registry = project.Registry;
            var module = project.Module.Dereference(registry);
            foreach (var m in module.EnumerateDependencies())
            {
                if (m.Namespace == "ut.PlayableAd")
                {
                    return true;
                }
            }
            return false;
        }
    }
}
