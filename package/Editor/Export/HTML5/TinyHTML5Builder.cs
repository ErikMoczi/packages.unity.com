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
    internal sealed class TinyHtml5Builder : ITinyBuilder
    {
        public const string KGlobalAssetsName = "UT_ASSETS";
        public const string KEntityGroupNamespace = "entities";

        private const string KSettingsFileName = "settings.js";
        private const string KRuntimeFileName = "runtime.js";
        private const string KBindingsFileName = "bindings.js";
        private const string KAssetsFileName = "assets.js";
        private const string KEntityGroupsFileName = "entities.js";
        private const string KCodeFileName = "code.js";
        private const string KMainFileName = "main.js";
        private const string KWebSocketClientFileName = "wsclient.js";
        private const string KWebPDecompressorFileName = "libwebp.js";
        private const string KHtmlFileName = "index.html";

        /// <summary>
        /// Builds the provided project for the HTML5 platform.
        /// </summary>
        public void Build(TinyBuildOptions options, TinyBuildResults results)
        {
            using (var progress = new TinyEditorUtility.ProgressBarScope())
            {
                progress.Update(TinyConstants.ApplicationName + " Build", "Preparing output folder", 0.0f);

                // Final output directory
                results.BinaryFolder = new DirectoryInfo(Path.Combine(options.BuildFolder.FullName, "bin"));
                results.BinaryFolder.Create();

                // Package and export all data
                progress.Update("Packaging runtime", 0.1f);
                PackageSettings(options, results);
                PackageRuntime(options, results);

                progress.Update("Packaging assets", 0.2f);
                PackageAssets(options, results);

                // Generate and write all application code
                progress.Update("Generating entity groups", 0.3f);
                GenerateEntityGroups(options, results);

                progress.Update("Generating bindings", 0.4f);
                GenerateBindings(options, results);

                progress.Update("Generating scripts", 0.5f);
                GenerateScripts(options, results);

                progress.Update("Generating main", 0.6f);
                GenerateMain(options, results);

                // Generate additional appended code
                GenerateWebSocketClient(options, results);
                GenerateWebPDecompressor(options, results);

                // Generate final HTML file
                progress.Update("Generating HTML", 0.7f);
                GenerateHTML(options, results);

                // Generate build report
                progress.Update("Generating build report", 0.8f);
                GenerateBuildReport(results);
            }
        }

        public static string GetJsTypeName(TinyRegistryObjectBase @object)
        {
            Assert.IsNotNull(@object);
            return GetJsTypeName(TinyUtility.GetModules(@object).FirstOrDefault(), @object);
        }

        private static string GetJsTypeName(TinyModule module, TinyRegistryObjectBase @object)
        {
            var name = @object.Name;

            if (!string.IsNullOrEmpty(module?.Namespace))
            {
                name = module.Namespace + "." + name;
            }

            var type = @object as TinyType;
            if (type != null)
            {
                switch (type.TypeCode)
                {
                    case TinyTypeCode.Unknown:
                        break;
                    case TinyTypeCode.Int8:
                    case TinyTypeCode.Int16:
                    case TinyTypeCode.Int32:
                    case TinyTypeCode.Int64:
                    case TinyTypeCode.UInt8:
                    case TinyTypeCode.UInt16:
                    case TinyTypeCode.UInt32:
                    case TinyTypeCode.UInt64:
                    case TinyTypeCode.Float32:
                    case TinyTypeCode.Float64:
                    case TinyTypeCode.Boolean:
                    case TinyTypeCode.String:
                        return name.ToLower();
                    case TinyTypeCode.EntityReference:
                        // @TODO remove the magic value
                        return "ut.Entity";
                    case TinyTypeCode.Configuration:
                    case TinyTypeCode.Component:
                    case TinyTypeCode.Struct:
                    case TinyTypeCode.Enum:
                    case TinyTypeCode.UnityObject:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return name;
        }

        /// <summary>
        /// Packages settings to `settings.js`
        /// </summary>
        private static void PackageSettings(TinyBuildOptions options, TinyBuildResults results)
        {
            var writer = new TinyCodeWriter(CodeStyle.JavaScript);

            var file = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KSettingsFileName));

            var settings = options.Project.Settings;

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
                return;
            }

            PrependGeneratedHeader(writer, options.Project.Name);
            File.WriteAllText(file.FullName, writer.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// Packages the runtime to `runtime.js`
        /// </summary>
        private static void PackageRuntime(TinyBuildOptions options, TinyBuildResults results)
        {
            var runtimeVariant = TinyRuntimeInstaller.GetJsRuntimeVariant(options);
            var runtimePath = TinyRuntimeInstaller.GetRuntimeDirectory(options.Platform, options.Configuration);
            var reportRuntime = results.BuildReport.AddChild(TinyBuildReport.RuntimeNode);

            // special case the modularizable build for release builds with symbols disabled
            if (options.Configuration == TinyBuildConfiguration.Release &&
                options.Platform == TinyPlatform.Html5 &&
                options.ProjectSettings.SymbolsInReleaseBuild == false)
            {
                var runtimeFile = Path.Combine(runtimePath, "RuntimeGemini.js");
                var destPath = Path.Combine(results.BinaryFolder.FullName, "runtime.js");

                var dependencies = options.Project.Module.Dereference(options.Project.Registry).EnumerateDependencies();
                var regex = new System.Text.RegularExpressions.Regex(@"\/\*if\(([\s\S]*?)\)\*\/([\s\S]*?)\/\*endif\(([\s\S]*?)\)\*\/");
                var runtime = File.ReadAllText(runtimeFile);
                runtime = regex.Replace(runtime, match => match.Groups[match.Groups[1].Value.Split('|').Any(module => dependencies.WithName("UTiny." + module).Any() || module == "RendererGLWebGL") ? 2 : 3].Value);
                File.WriteAllText(destPath, runtime);
                reportRuntime.AddChild(new FileInfo(destPath));
                return;
            }

            var runtimeFiles = new DirectoryInfo(runtimePath).GetFiles(runtimeVariant + "*", SearchOption.TopDirectoryOnly);
            foreach (var runtimeFile in runtimeFiles)
            {
                if (runtimeFile.Name.EndsWith(".js.symbols") || runtimeFile.Name.EndsWith(".js.map") || runtimeFile.Name.EndsWith(".dll"))
                {
                    continue;
                }
                var destPath = Path.Combine(results.BinaryFolder.FullName, $"runtime{runtimeFile.Extension}");
                reportRuntime.AddChild(runtimeFile.CopyTo(destPath));
            }
        }

        /// <summary>
        /// Packages assets to `assets.js` or `Assets/*.*`
        /// </summary>
        private static void PackageAssets(TinyBuildOptions options, TinyBuildResults results)
        {
            var buildFolder = options.ExportFolder;
            var binFolder = results.BinaryFolder;

            // Export assets to the build directory
            var buildAssetsFolder = new DirectoryInfo(Path.Combine(buildFolder.FullName, "Assets"));
            buildAssetsFolder.Create();
            var exportInfos = TinyAssetExporter.Export(options, buildAssetsFolder);

            // copy assets to bin AND/OR encode assets to 'assets.js'
            var binAssetsFolder = new DirectoryInfo(Path.Combine(binFolder.FullName, "Assets"));
            binAssetsFolder.Create();

            var assetsFile = new FileInfo(Path.Combine(binFolder.FullName, KAssetsFileName));

            var writer = new TinyCodeWriter();

            PrependGeneratedHeader(writer, options.Project.Name);

            var reportAssets = results.BuildReport.AddChild(TinyBuildReport.AssetsNode);
            var reportJavaScript = reportAssets.AddChild("JavaScript");

            using (var jsdoc = new TinyJsdoc.Writer(writer))
            {
                jsdoc.Type("object");
                jsdoc.Desc("Map containing URLs for all assets.  If assets are included as base64 blobs, these will be data URLs.");
                jsdoc.Line("@example var assetUrl = UT_ASSETS[\"MyCustomAsset\"]");
            }

            long totalBase64Size = 0;
            using (writer.Scope($"var {KGlobalAssetsName} ="))
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
                            writer.Line($"{comma}\"{Path.GetFileNameWithoutExtension(file.Name)}\": \"Assets/{file.Name}\"");
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
            File.WriteAllText(assetsFile.FullName, writer.ToString());

            reportJavaScript.Item.Size = assetsFile.Length - totalBase64Size;

            // Remaining assets are binplaced
            foreach (var exportInfo in exportInfos)
            {
                foreach (var file in exportInfo.ExportedFiles)
                {
                    if (!file.Exists)
                    {
                        // this asset has been packaged already
                        continue;
                    }

                    file.MoveTo(Path.Combine(binAssetsFolder.FullName, file.Name));
                }
            }

            // Clean up the build directory
            buildAssetsFolder.Delete(true);

            // if we have no standalone assets, cleanup
            if (binAssetsFolder.GetFiles().Length <= 0)
            {
                binAssetsFolder.Delete();
            }
        }

        /// <summary>
        /// Writes components, structs and enums `bindings.js`
        /// </summary>
        private static void GenerateBindings(TinyBuildOptions options, TinyBuildResults results)
        {
            var file = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KBindingsFileName));

            // @NOTE `bind-generated.js` is the exported name from the `BindGen.exe` application
            File.Copy(Path.Combine(results.OutputFolder.FullName, "bind-generated.js"), file.FullName, true);

            using (var writer = File.AppendText(file.FullName))
            {
                writer.Write(File.ReadAllText(
                    Path.GetFullPath(TinyConstants.PackagePath + "/RuntimeExtensions/bindings.js.txt")));
            }

            results.BuildReport.GetOrAddChild("Code").AddChild(file);
        }

        /// <summary>
        /// Packages entity group objects to `entities.js`
        /// 
        /// Since we don't have a scene format, groups are written as setup functions
        /// </summary>
        private static void GenerateEntityGroups(TinyBuildOptions options, TinyBuildResults results)
        {
            var writer = new TinyCodeWriter(CodeStyle.JavaScript);
            var report = results.BuildReport.GetOrAddChild(TinyBuildReport.CodeNode).AddChild();

            PrependGeneratedHeader(writer, options.Project.Name);

            // @NOTE Namespaces are generated through through `BindGen.exe`
            // e.g. `{ENTITY_GROUPS}.{PROJECT_NAMESPACE}.{GROUP_NAME}` will already exist as a component

            using (var visitor = new EntityGroupSetupVisitor { Writer = writer, Report = report })
            {
                options.Project.Visit(visitor);
            }

            var file = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KEntityGroupsFileName));
            File.WriteAllText(file.FullName, writer.ToString(), Encoding.UTF8);
            report.Reset(file);
        }

        /// <summary>
        /// Writes user code to `code.js`
        /// 
        /// Any free standing code written by users is written to this file
        /// </summary>
        private static void GenerateScripts(TinyBuildOptions options, TinyBuildResults results)
        {
            var project = options.Project;
            var registry = project.Registry;
            var module = project.Module.Dereference(registry);
            var report = results.BuildReport.GetOrAddChild(TinyBuildReport.CodeNode).AddChild();

            // Install modules as needed
            var isSourceCodeLinked = options.Configuration != TinyBuildConfiguration.Release &&
                project.Settings.LinkToSource;

            if (isSourceCodeLinked)
            {
                var tsoutPath = TinyScriptUtility.MakeTsOutPath(results.OutputFolder);
                if (File.Exists(tsoutPath))
                {
                    report.AddChild(tsoutPath, Encoding.ASCII.GetBytes(File.ReadAllText(tsoutPath)));
                }
                report.Reset("Linked Code Files");
            }
            else
            {
                var writer = new TinyCodeWriter(CodeStyle.JavaScript);

                PrependGeneratedHeader(writer, options.Project.Name);

                var tsoutPath = TinyScriptUtility.MakeTsOutPath(results.OutputFolder);
                if (File.Exists(tsoutPath))
                {
                    var reportSystemPos = writer.Length;
                    writer.WriteRaw(File.ReadAllText(tsoutPath))
                        .Line();
                    report.AddChild(tsoutPath,
                        Encoding.ASCII.GetBytes(writer.Substring(reportSystemPos)));
                }

                var file = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KCodeFileName));
                File.WriteAllText(file.FullName, writer.ToString(), Encoding.UTF8);
                report.Reset(file);
            }
        }

        /// <summary>
        /// Generates entry point for the applicaton `main.js`
        /// This script will contain the system scheduling, window setup and initial group loading
        /// </summary>
        private static void GenerateMain(TinyBuildOptions options, TinyBuildResults results)
        {
            var scripting = options.Context.GetScriptMetadata();
            var project = options.Project;
            var registry = project.Registry;
            var module = project.Module.Dereference(registry);

            var file = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KMainFileName));

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
                var context = new EntityGroupSetupVisitor.VisitorContext
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
                    var componentIndex = context.ComponentIndexMap.GetOrAddValue(component);
                    writer.Line($"var c{componentIndex} = world.getConfigData({TinyScriptUtility.GetJsTypeName(type)});");
                    component.Properties.Visit(new EntityGroupSetupVisitor.ComponentVisitor
                    {
                        VisitorContext = context,
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

            File.WriteAllText(file.FullName, writer.ToString(), Encoding.UTF8);
            results.BuildReport.GetOrAddChild(TinyBuildReport.CodeNode).AddChild(file);
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

        /// <summary>
        /// Generates `wsclient.js` to handle live-linking
        /// </summary>
        private static void GenerateWebSocketClient(TinyBuildOptions options, TinyBuildResults results)
        {
            if (options.Configuration == TinyBuildConfiguration.Release)
            {
                return;
            }

            // Write wsclient to binary dir
            var source = new FileInfo(Path.Combine(TinyRuntimeInstaller.GetToolDirectory("wsclient"), KWebSocketClientFileName));
            if (source.Exists)
            {
                var destination = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KWebSocketClientFileName));
                source.CopyTo(destination.FullName, true);
                results.BuildReport.GetOrAddChild(TinyBuildReport.CodeNode).AddChild(destination);
            }
        }

        /// <summary>
        /// Generates `libwebp.js` to handle WebP decompressor
        /// </summary>
        private static void GenerateWebPDecompressor(TinyBuildOptions options, TinyBuildResults results)
        {
            // Check if project use WebP texture format
            var module = options.Project.Module.Dereference(options.Registry);
            var webpUsed = AssetIterator.EnumerateRootAssets(module)
                .Select(a => a.Object)
                .OfType<Texture2D>()
                .Select(t => TinyUtility.GetAssetExportSettings(options.Project, t))
                .OfType<TinyTextureSettings>()
                .Any(s => s.FormatType == TextureFormatType.WebP);

            // Warn about WebP usages
            if (options.Project.Settings.IncludeWebPDecompressor)
            {
                if (!webpUsed)
                {
                    Debug.LogWarning("This project does not uses the WebP texture format, but includes the WebP decompressor code. To reduce build size, it is recommended to disable \"Include WebP Decompressor\" in project settings.");
                }
            }
            else // WebP decompressor not included, do not copy to binary dir
            {
                if (webpUsed)
                {
                    Debug.LogWarning("This project uses the WebP texture format, but does not include the WebP decompressor code. The content will not load in browsers that do not natively support the WebP format. To ensure maximum compatibility, enable \"Include WebP Decompressor\" in project settings.");
                }
                return;
            }

            // Copy libwebp to binary dir
            var srcFile = Path.Combine(TinyRuntimeInstaller.GetToolDirectory("libwebp"), KWebPDecompressorFileName);
            var dstFile = Path.Combine(results.BinaryFolder.FullName, KWebPDecompressorFileName);
            File.Copy(srcFile, dstFile);

            results.BuildReport.GetOrAddChild(TinyBuildReport.CodeNode).AddChild(new FileInfo(dstFile));
        }

        /// <summary>
        /// Outputs the final `index.html` file
        /// </summary>
        private static void GenerateHTML(TinyBuildOptions options, TinyBuildResults results)
        {
            var project = options.Project;
            var registry = project.Registry;

            var isSourceCodeLinked = options.Configuration != TinyBuildConfiguration.Release &&
                project.Settings.LinkToSource;

            var settingsFile = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KSettingsFileName));
            var runtimeFile = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KRuntimeFileName));
            var bindingsFile = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KBindingsFileName));
            var assetsFile = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KAssetsFileName));
            var entityGroupsFile = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KEntityGroupsFileName));
            //var systemsFile = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KSystemsFileName));
            var codeFile = isSourceCodeLinked ? null : new FileInfo(Path.Combine(results.BinaryFolder.FullName, KCodeFileName));
            var mainFile = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KMainFileName));
            var webSocketClientFile = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KWebSocketClientFileName));
            var webpDecompressorFile = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KWebPDecompressorFileName));

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
                        var tsoutPath = Path.Combine(results.OutputFolder.ToString(), TinyBuildUtilities.ScriptAssembliesDirectory, TinyBuildUtilities.TypeScriptOutputFile);
                        if (File.Exists(tsoutPath))
                            files.Add(new FileInfo(tsoutPath));
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
                        var gameFile = new FileInfo(Path.Combine(results.BinaryFolder.FullName, "game.js"));
                        if (project.Settings.MinifyJavaScript)
                        {
                            // Minify JavaScript
                            using (new TinyEditorUtility.ProgressBarScope($"{TinyConstants.ApplicationName} Build",
                                "Minifying JavaScript code..."))
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
                        if (project.Settings.SingleFileHtml)
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

                                    var tempPath = new FileInfo(Path.Combine(results.BinaryFolder.FullName, path))
                                        .FullName;

                                    File.WriteAllText(tempPath, File.ReadAllText(fullPath, Encoding.UTF8), Encoding.UTF8);
                                }
                                else if (!fullPath.Contains(options.ExportFolder.FullName) || fullPath.Contains(TinyBuildUtilities.ScriptAssembliesDirectory))
                                {
                                    // heuristic: within the project root, but not in TinyExport -> direct link
                                    path = "/~project" + fullPath.Substring(projectDir.Length).ToForwardSlash();
                                }
                            }
                            writer.LineFormat("<script src=\"{0}\"></script>", path);
                        }
                    }
                    writer.Line("<script>window.addEventListener('load',function(){ut._HTML.main()})</script>"); // Kick off Tiny app launch once all scripts have loaded
                    writer.LineFormat("<title>{0}</title>", project.Name);
                    writer.CodeStyle.EndBrace = "</head>";
                }
                using (writer.Scope("<body>"))
                {
                    writer.CodeStyle.EndBrace = "</body>";
                }
                writer.CodeStyle.EndBrace = "</html>";
            }

            // Write final index.html file
            var htmlFile = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KHtmlFileName));
            File.WriteAllText(htmlFile.FullName, writer.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// Generates the `build-report.json` file
        /// </summary>
        private static void GenerateBuildReport(TinyBuildResults results)
        {
            // Output build report data as json
            File.WriteAllText(Path.Combine(results.OutputFolder.FullName, "build-report.json"), results.BuildReport.ToString(), Encoding.UTF8);
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

        private static bool UsesAdSupport(TinyProject project)
        {
            var registry = project.Registry;
            var module = project.Module.Dereference(registry);
            foreach (var m in module.EnumerateDependencies())
            {
                if (m.Namespace == "ut.AdSupport")
                    return true;
            }

            return false;
        }
    }
}
