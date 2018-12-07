using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using UnityEditor;
using UnityEngine.Assertions;
using Unity.Properties;
using Unity.Properties.Serialization;
using Debug = UnityEngine.Debug;

namespace Unity.Tiny
{
    internal static class TinyBuildUtilities
    {
        #region Fields

        public const string ScriptAssembliesDirectory = "ScriptAssemblies";
        public const string TypeScriptOutputFile = "tsc-emit.js";
        public const string TypeScriptOutputMetaFile = "tsc-meta.json";
        private const string s_TSConfigName = "tsconfig.json";

        #endregion

        #region BindGem
        public static void RunBindGem(TinyBuildOptions options)
        {
            var exportFolder = options.BuildFolder;
            var idlFile = new FileInfo(Path.Combine(exportFolder.FullName, "generated.cs"));
            TinyIDLGenerator.GenerateIDL(options, idlFile);

            var bindGem = new FileInfo(Path.Combine(TinyRuntimeInstaller.GetBindgemDirectory(), "bindgem.exe"));
            var exeName = StringExtensions.DoubleQuoted(bindGem.FullName);

            // always call bindgem with mono for consistency
            exeName = "mono " + exeName;

            // reference the core runtime file
            var bindReferences = $"-r \"{TinyRuntimeInstaller.GetRuntimeDefsAssemblyPath(options).FullName}\"";

            RunInShell(
                    $"{exeName} -j {bindReferences} {(options.Configuration != TinyBuildConfiguration.Release ? "-d" : "")} -o bind-generated {idlFile.Name}",
                    new ShellProcessArgs()
                    {
                        WorkingDirectory = exportFolder,
                        ExtraPaths = TinyPreferences.MonoDirectory.AsEnumerable()
                    });
        }
        #endregion

        #region TypeScript
        public static void RegenerateTSDefinitionFiles(TinyBuildOptions options)
        {
            var definitionFile = new StringBuilder();

            var indent = "    ";

            definitionFile.Append(File.ReadAllText(Path.GetFullPath(TinyConstants.PackagePath + "/RuntimeExtensions/bindings.d.ts")));
            definitionFile.AppendLine($"declare var {TinyHtml5Builder.KGlobalAssetsName}: Object;");

            var modules = options.Project.Module.Dereference(options.Registry).EnumerateDependencies().Where(m => m.IsRuntimeIncluded == false);
            foreach (var module in modules)
            {
                if (module.ExportFlags.HasFlag(TinyExportFlags.EditorExtension))
                {
                    continue;
                }

                definitionFile.AppendLine($"declare namespace {module.Namespace}{{");

                var types = module.Types;

                foreach (var type in types.Deref(module.Registry))
                {
                    switch (type.TypeCode)
                    {
                        case TinyTypeCode.Component:
                        case TinyTypeCode.Configuration:
                        {
                            definitionFile.AppendLine($"{indent}class {type.Name} extends ut.Component {{");
                            // TODO: constructors with arguments?
                            definitionFile.AppendLine($"{indent}{indent}constructor();");

                            WriteComponentFields(definitionFile, type, module);
                            WriteComponentInterface(definitionFile, indent, type);
                            break;
                        }
                        case TinyTypeCode.Struct:
                        {
                            definitionFile.AppendLine($"{indent}class {type.Name} {{");
                            WriteStructFields(definitionFile, type, module);
                            WriteStructInterface(definitionFile, indent, type);
                            break;
                        }
                        case TinyTypeCode.Enum:
                        {
                            definitionFile.AppendLine($"{indent}enum {type.Name} {{");
                            WriteEnumFields(definitionFile, type);
                            break;
                        }
                    }

                    definitionFile.AppendLine($"{indent}}}");
                }

                definitionFile.AppendLine($"}}");
            }

            definitionFile.AppendLine($"declare namespace ut{{");
            definitionFile.AppendLine($"{indent}class EntityGroupData extends Object{{");
            definitionFile.AppendLine($"{indent}{indent}Component: ut.ComponentClass<any>;");
            definitionFile.AppendLine($"{indent}{indent}load(world: ut.World): ut.Entity[];");
            definitionFile.AppendLine($"{indent}{indent}name: string;");
            definitionFile.AppendLine($"{indent}}}");
            definitionFile.AppendLine($"{indent}interface EntityGroups{{");
            definitionFile.AppendLine($"{indent}{indent}[module: string]: any;");

            foreach (var module in modules)
            {
                var entityGroups = module.EntityGroups;

                if (entityGroups.Count() == 0)
                    continue;

                var moduleNamespaceParts = module.Namespace.Split('.');
                for (var i = 0; i < moduleNamespaceParts.Length; ++i)
                {
                    definitionFile.AppendLine($"{indent}{indent}{moduleNamespaceParts[i]}: {{");
                    if (i != moduleNamespaceParts.Length - 1)
                        definitionFile.AppendLine($"{indent}{indent}[module: string]: any;");
                }
                definitionFile.AppendLine($"{indent}{indent}{indent}[data: string]: EntityGroupData;");

                foreach (var entityGroup in entityGroups.Deref(module.Registry))
                {
                    definitionFile.AppendLine($"{indent}{indent}{indent}{entityGroup.Name}: EntityGroupData;");
                }

                foreach (var part in moduleNamespaceParts)
                {
                    definitionFile.AppendLine($"{indent}{indent}}}");
                }
            }

            definitionFile.AppendLine($"{indent}}}");
            definitionFile.AppendLine($"}}");

            definitionFile.AppendLine($"declare let {TinyHtml5Builder.KEntityGroupNamespace}: ut.EntityGroups;");


            definitionFile.AppendLine($"declare namespace ut.Core2D.layers{{");
            foreach (var layer in Camera2DEditor.GetLayerNames())
            {
                definitionFile.AppendLine($"{indent}class {layer.Replace(" ", "")} extends ut.Component {{");
                definitionFile.AppendLine($"{indent}{indent}static _wrap(w: number, e: number): {layer.Replace(" ", "")};");
                definitionFile.AppendLine($"{indent}{indent}static readonly cid: number;");
                definitionFile.AppendLine($"{indent}}}");

            }
            definitionFile.AppendLine($"}}");


            var buildFolder = options.BuildFolder.FullName;

            Directory.CreateDirectory(buildFolder);
            File.WriteAllText(Path.Combine(buildFolder, "bind-generated.d.ts"), definitionFile.ToString());
        }

        private static void WriteComponentInterface(StringBuilder definitionFile, string indent, TinyType type)
        {
            definitionFile.AppendLine($"{indent}{indent}static readonly cid: number;");
            definitionFile.AppendLine($"{indent}{indent}static readonly _view: any;");
            definitionFile.AppendLine($"{indent}{indent}static readonly _isSharedComp: boolean;");
            definitionFile.AppendLine($"{indent}{indent}static _size: number;");
            definitionFile.AppendLine($"{indent}{indent}static _fromPtr(p: number, v?: {type.Name}): {type.Name};");
            definitionFile.AppendLine($"{indent}{indent}static _toPtr(p: number, v: {type.Name}): void;");
            definitionFile.AppendLine($"{indent}{indent}static _tempHeapPtr(v: {type.Name}): number;");
            definitionFile.AppendLine($"{indent}{indent}static _dtorFn(v: {type.Name}): void;");
        }

        private static void WriteStructInterface(StringBuilder definitionFile, string indent, TinyType type)
        {
            definitionFile.AppendLine($"{indent}{indent}static _size: number;");
            definitionFile.AppendLine($"{indent}{indent}static _fromPtr(p: number, v?: {type.Name}): {type.Name};");
            definitionFile.AppendLine($"{indent}{indent}static _toPtr(p: number, v: {type.Name}): void;");
            definitionFile.AppendLine($"{indent}{indent}static _tempHeapPtr(v: {type.Name}): number;");
        }

        private static void WriteEnumFields(StringBuilder definitionFile, TinyType enumeration)
        {
            Assert.IsTrue(enumeration.IsEnum);
            var fields = enumeration.Fields;
            var indent = "    ";

            for (var i = 0; i < fields.Count(); ++i)
            {
                definitionFile.AppendLine($"{indent}{indent}{fields[i].Name} = {i},");
            }
        }
        private static void WriteStructFields(StringBuilder definitionFile, TinyType structure, TinyModule module)
        {
            Assert.IsTrue(structure.IsStruct);
            var fields = structure.Fields;
            var indent = "    ";

            foreach (var field in fields)
            {
                var fieldType = GetFieldType(field, module);
                definitionFile.AppendLine($"{indent}{indent}{field.Name}: {fieldType};");
            }
        }
        private static void WriteComponentFields(StringBuilder definitionFile, TinyType component, TinyModule module)
        {
            Assert.IsTrue(component.IsComponent || component.IsConfiguration);
            var fields = component.Fields;
            var indent = "    ";

            var registry = module.Registry;
            foreach (var field in fields)
            {
                var fieldType = field.FieldType.Dereference(registry);

                if (null == fieldType)
                {
                    continue;
                }
            
                if (fieldType.ExportFlags.HasFlag(TinyExportFlags.EditorExtension))
                {
                    var exportedFieldType = TinyEditorExtensionsGenerator.GetExportedFieldType(field, module);
                    if (!string.IsNullOrEmpty(exportedFieldType))
                    {
                        definitionFile.AppendLine($"{indent}{indent}{field.Name}: {exportedFieldType};");
                    }

                    continue;
                }
                var tsFieldTypeName = GetFieldType(field, module);
                definitionFile.AppendLine($"{indent}{indent}{field.Name}: {tsFieldTypeName};");
            }
        }
        private static string GetFieldType(TinyField field, TinyModule module)
        {
            var returnVal = "";
            switch (field.FieldType.Dereference(module.Registry).TypeCode)
            {
                case TinyTypeCode.Float32:
                case TinyTypeCode.Float64:
                case TinyTypeCode.Int16:
                case TinyTypeCode.Int32:
                case TinyTypeCode.Int64:
                case TinyTypeCode.Int8:
                case TinyTypeCode.UInt16:
                case TinyTypeCode.UInt32:
                case TinyTypeCode.UInt64:
                case TinyTypeCode.UInt8:
                    returnVal = "number";
                    break;
                case TinyTypeCode.Boolean:
                    returnVal = "boolean";
                    break;
                case TinyTypeCode.String:
                    returnVal = "string";
                    break;
                case TinyTypeCode.EntityReference:
                    returnVal = "ut.Entity";
                    break;
                case TinyTypeCode.Component:
                case TinyTypeCode.Enum:
                case TinyTypeCode.Struct:
                case TinyTypeCode.UnityObject:
                case TinyTypeCode.Configuration:
                    var owningModule = module.EnumerateDependencies().FirstOrDefault(m =>
                        m.Components.Concat(m.Structs).Concat(m.Enums).Contains(field.FieldType));
                    if (owningModule == null)
                        returnVal = "ut.Entity";
                    else if (owningModule == module)
                        returnVal = field.FieldType.Name;
                    else
                        returnVal = $"{owningModule.Namespace}.{field.FieldType.Name}";
                    break;
                case TinyTypeCode.Unknown:
                    Debug.LogError($"Error in type detected for field : {field.Name}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (field.Array)
            {
                returnVal += "[]";
            }

            return returnVal;
        }

        public static FileInfo RegenerateTsConfig(TinyBuildOptions options)
        {
            var buildFolder = options.BuildFolder.FullName;

            const string typescriptRelativeRoot = "";

            var module = options.Project.Module.Dereference(options.Registry);

            var configGen = new TinyTypeScriptConfigGenerator
            {
                compileOnSave = true,
                compilerOptions = new TinyTypeScriptConfigGenerator.TinyTypeScriptCompilerOptions
                {
                    outFile = Path
                        .Combine(typescriptRelativeRoot, buildFolder, ScriptAssembliesDirectory, TypeScriptOutputFile)
                        .ToForwardSlash(),
                    target = "ES5",
                    sourceMap = true,
                    experimentalDecorators = true,
                    skipLibCheck = true // much faster (dangerous? maybe we should skip *known* decl files only)
                }
            };

            var runtimePath = TinyRuntimeInstaller.GetRuntimeDistDirectory();
            var runtimeVariantDts = TinyRuntimeInstaller.GetJsRuntimeVariant(options) + ".d.ts";
            configGen.files.Add(Path.Combine(typescriptRelativeRoot, buildFolder, "bind-generated.d.ts").ToForwardSlash());
            configGen.files.Add(Path.Combine(typescriptRelativeRoot, runtimePath, "runtimedll", runtimeVariantDts).ToForwardSlash());

            foreach (var m in module.EnumerateDependencies())
            {
                configGen.include.Add(m.GetDirectoryPath().ToForwardSlash() + "/**/*.ts");
            }

            var generatedTsConfig = new MigrationContainer(configGen);

            var customTsConfigDir = module.GetDirectoryPath();
            if (false == string.IsNullOrEmpty(customTsConfigDir))
            {
                if (File.Exists(Path.Combine(customTsConfigDir, s_TSConfigName)))
                {
                    Debug.LogWarning(
                        $"{TinyConstants.ApplicationName}: Using tsconfig.json in your projects will cause wrong autocomplete for typescript, use tsconfig.override.json for overriding purposes.");
                }

                var customTsConfigPath = Path.Combine(customTsConfigDir, "tsconfig.override.json");

                if (File.Exists(customTsConfigPath))
                {
                    var customTsConfigJson = JsonSerializer.Deserialize(File.ReadAllText(customTsConfigPath));
                    var customTsConfig = new MigrationContainer(customTsConfigJson);

                    customTsConfig.Visit(new TSConfigMergeVisitor(generatedTsConfig,
                        customTsConfigDir.Split(new string[] { Path.GetFileName(customTsConfigPath) },
                            StringSplitOptions.None)[0]));
                }
            }

            RemapPackagePaths(generatedTsConfig);
            var json = JsonSerializer.Serialize(generatedTsConfig);
            var tsconfigFile = new FileInfo(s_TSConfigName);
            File.WriteAllText(tsconfigFile.FullName, json);

            return tsconfigFile;
        }

        private static void RemapPackagePaths(IPropertyContainer container)
        {
            foreach (var property in container.PropertyBag.Properties)
            {
                if (property is IClassProperty)
                {
                    switch (property)
                    {
                        case IListTypedItemClassProperty<string> listTypedItemProperty:
                        {
                            for (var i = 0; i < listTypedItemProperty.Count(container); ++i)
                            {
                                var path = listTypedItemProperty.GetAt(container, i);
                                listTypedItemProperty.SetAt(container, i, TinyPackageUtility.GetUnityOrOSPath(path));
                            }
                            continue;
                        }
                        case IValueClassProperty valueClassProperty:
                        {
                            if (valueClassProperty.GetObjectValue(container) is string path)
                            {
                                valueClassProperty.SetObjectValue(container, TinyPackageUtility.GetUnityOrOSPath(path));
                            }
                            continue;
                        }
                    }
                }
            }
        }

        public static ScriptMetadata CompileTypeScript(FileInfo tsConfigFile, FileInfo outMetadataFile)
        {
            outMetadataFile.Directory?.Create();

            if (!TinyShell.RunTool("tscompile",
                $"-i {tsConfigFile.FullName.DoubleQuoted()}",
                $"-o {outMetadataFile.FullName.DoubleQuoted()}"))
            {
                return null;
            }

            ScriptMetadata metadata = null;
            outMetadataFile.Refresh();
            if (outMetadataFile.Exists)
            {
                var outJson = File.ReadAllText(outMetadataFile.FullName, Encoding.UTF8);
                metadata = JsonSerializer.Deserialize<ScriptMetadata>(outJson);
                metadata.LogDiagnostics();
            }

            if (metadata == null || !metadata.Success)
            {
                return null;
            }

            return metadata;
        }

        public static bool CompileScripts()
        {
            return CompileScripts(TinyBuildPipeline.WorkspaceBuildOptions);
        }

        public static bool CompileScripts(TinyBuildOptions buildOptions)
        {
            var context = buildOptions.Context;
            var manager = context.GetManager<IScriptingManager>();
            return manager.CompileScripts(buildOptions);
        }

        #endregion

        #region Zip Utilities

        private static FileInfo ZipProgramFile()
        {
            return new FileInfo(TinyPreferences.Default7zPath());
        }
        public static bool ZipFolder(DirectoryInfo folder, string zipPath)
        {
            File.Delete(zipPath);

            var zip = ZipProgramFile();
            return RunInShell($"\"{zip.FullName}\" a \"{zipPath}\" \"{folder.FullName}\"", ShellProcessArgs.Default);
        }

        public static bool ZipPaths(string[] toZip, string zipPath)
        {
            File.Delete(zipPath);

            var zip = ZipProgramFile();
            var paths = string.Join(" ", toZip.Select(path => $"\"{path}\""));
            return RunInShell($"\"{zip.FullName}\" a \"{zipPath}\" {paths}", ShellProcessArgs.Default);
        }

        public static bool UnzipFile(string zipPath, DirectoryInfo destFolder)
        {
            if (!destFolder.Exists)
            {
                destFolder.Create();
            }
            var zip = ZipProgramFile();
            return RunInShell($"\"{zip.FullName}\" x -y -o. \"{zipPath}\"", new ShellProcessArgs()
            {
                WorkingDirectory = destFolder
            });
        }
        #endregion

        #region Process Utilities

        /// <summary>
        /// Runs the given command in the OS shell (cmd on Windows, bash on Mac/Linux).
        /// </summary>
        public static bool RunInShell(string command, ShellProcessArgs processArgs, bool outputOnErrorOnly = true)
        {
            var output = TinyShell.RunInShell(command, processArgs);

            if (!output.Succeeded)
            {
                Debug.LogError($"{TinyConstants.ApplicationName}: {output.FullOutput}");
            }
            else if (!outputOnErrorOnly)
            {
                Debug.Log($"{TinyConstants.ApplicationName}: {output.FullOutput}");
            }

            return output.Succeeded;
        }

        #endregion

        /// <summary>
        /// Deletes a directory and all of its contents.
        /// This method will throw if a file or directory cannot be deleted.
        /// </summary>
        public static void PurgeDirectory(DirectoryInfo dir)
        {
            if (!dir.Exists)
            {
                return;
            }
            foreach (var d in dir.GetDirectories())
            {
                PurgeDirectory(d);
            }
            foreach (var f in dir.GetFiles())
            {
                f.Delete();
            }
            dir.Delete();
            Console.WriteLine("Purged " + dir.FullName);
        }

        public static void CopyDirectory(string from, string to, bool purge)
        {
            CopyDirectory(new DirectoryInfo(from), new DirectoryInfo(to), purge);
        }

        public static void CopyDirectory(DirectoryInfo from, DirectoryInfo to, bool purge)
        {
            if (false == from.Exists)
            {
                throw new Exception($"Source directory ({from.FullName}) does not exist.");
            }
            if (false == to.Exists)
            {
                Console.WriteLine("Created " + to.FullName);
                to.Create();
            }

            foreach (var dir in from.EnumerateDirectories())
            {
                var toDir = new DirectoryInfo(Path.Combine(to.FullName, dir.Name));
                CopyDirectory(dir, toDir, purge);
            }

            if (purge)
            {
                foreach (var dir in to.EnumerateDirectories())
                {
                    if (false == Directory.Exists(Path.Combine(from.FullName, dir.Name)))
                    {
                        PurgeDirectory(dir);
                    }
                }
                foreach (var file in to.EnumerateFiles())
                {
                    file.Delete();
                }
            }

            foreach (var file in from.EnumerateFiles())
            {
                var toPath = Path.Combine(to.FullName, file.Name);
                Console.WriteLine("Copied " + file.FullName + " to " + toPath);
                file.CopyTo(toPath, true);
            }
        }
    }
}
