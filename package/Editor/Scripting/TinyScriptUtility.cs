using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Tiny
{
    /// <summary>
    /// Helper class for managing Tiny Scripts (.ts and .js.txt files)
    /// </summary>
    internal static class TinyScriptUtility
    {
        private static readonly HashSet<string> s_CSharpReservedKeywords = new HashSet<string>
        {
            "abstract",
            "as",
            "base",
            "bool",
            "break",
            "byte",
            "case",
            "catch",
            "char",
            "checked",
            "class",
            "const",
            "continue",
            "decimal",
            "default",
            "delegate",
            "do",
            "double",
            "else",
            "enum",
            "event",
            "explicit",
            "extern",
            "false",
            "finally",
            "fixed",
            "float",
            "for",
            "foreach",
            "goto",
            "if",
            "implicit",
            "in",
            "int",
            "interface",
            "internal",
            "is",
            "lock",
            "long",
            "namespace",
            "new",
            "null",
            "object",
            "operator",
            "out",
            "override",
            "params",
            "private",
            "protected",
            "public",
            "readonly",
            "ref",
            "return",
            "sbyte",
            "sealed",
            "short",
            "sizeof",
            "stackalloc",
            "static",
            "string",
            "struct",
            "switch",
            "this",
            "throw",
            "true",
            "try",
            "typeof",
            "uint",
            "ulong",
            "unchecked",
            "unsafe",
            "ushort",
            "using",
            "using",
            "static",
            "virtual",
            "void",
            "volatile",
            "while"
        };
        
        public static bool IsReservedKeyword(string name)
        {
            return s_CSharpReservedKeywords.Contains(name);
        }
        
        public struct BuildArtifacts
        {
            public byte[] JsOutput;
            public byte[] MetaOutput;
        }
        
        public static BuildArtifacts ReadLastBuild(DirectoryInfo outputFolder)
        {
            var artifacts = new BuildArtifacts();
            var lastCompilationPath = MakeTsOutPath(outputFolder);
            if (File.Exists(lastCompilationPath))
            {
                artifacts.JsOutput = File.ReadAllBytes(lastCompilationPath);
            }
            lastCompilationPath = MakeTsOutMetaPath(outputFolder);
            if (File.Exists(lastCompilationPath))
            {
                artifacts.MetaOutput = File.ReadAllBytes(lastCompilationPath);
            }

            return artifacts;
        }
        
        public static void WriteLastBuild(DirectoryInfo outputFolder, BuildArtifacts lastBuild)
        {
            var buildFile = new FileInfo(MakeTsOutPath(outputFolder));
            buildFile.Directory?.Create();

            if (null != lastBuild.JsOutput)
            {
                File.WriteAllBytes(buildFile.FullName, lastBuild.JsOutput);
            }
            
            buildFile = new FileInfo(MakeTsOutMetaPath(outputFolder));
            buildFile.Directory?.Create();
            
            if (null != lastBuild.MetaOutput)
            {
                File.WriteAllBytes(buildFile.FullName, lastBuild.MetaOutput);
            }
        }
        
        public static string MakeTsOutPath(DirectoryInfo outputFolder)
        {
            return Path.Combine(outputFolder.FullName, TinyBuildUtilities.ScriptAssembliesDirectory, TinyBuildUtilities.TypeScriptOutputFile);
        }
        
        public static string MakeTsOutMetaPath(DirectoryInfo outputFolder)
        {
            return Path.Combine(outputFolder.FullName, TinyBuildUtilities.ScriptAssembliesDirectory, TinyBuildUtilities.TypeScriptOutputMetaFile);
        }
        
        /// <inheritdoc />
        /// <summary>
        /// Postproccessor to detect imports and refresh the implicit scripts
        /// </summary>
        internal class PostProcessor : AssetPostprocessor
        {
            private static readonly List<string> s_ChangedPaths = new List<string>();
    
            private static void OnPostprocessAllAssets(
                string[] importedAssets,
                string[] deletedAssets,
                string[] movedAssets,
                string[] movedFromAssetPaths)
            {
                // early exit when no Tiny context is loaded
                if (null == TinyEditorApplication.Module || string.IsNullOrEmpty(TinyEditorApplication.Module.GetDirectoryPath()))
                {
                    return;
                }
                
                s_ChangedPaths.Clear();
    
                foreach (var path in importedAssets)
                {
                    if (!EndsWithTinyScriptExtension(path))
                    {
                        continue;
                    }
    
                    s_ChangedPaths.Add(path);
                }
    
                foreach (var path in deletedAssets)
                {
                    if (!EndsWithTinyScriptExtension(path))
                    {
                        continue;
                    }
    
                    s_ChangedPaths.Add(path);
                }
    
                foreach (var path in movedAssets)
                {
                    if (!EndsWithTinyScriptExtension(path))
                    {
                        continue;
                    }
    
                    s_ChangedPaths.Add(path);
                }
    
                foreach (var path in movedFromAssetPaths)
                {
                    if (!EndsWithTinyScriptExtension(path))
                    {
                        continue;
                    }

                    s_ChangedPaths.Add(path);
                }

                if (s_ChangedPaths.Count == 0)
                {
                    return;
                }
                
                // now that we've updated script references, run the compiler to let the user know
                TinyBuildUtilities.CompileScripts();
            }
        }
            
        public const string TypeScriptExtension = ".ts";
        public const string JavaScriptExtension = ".js.txt";
        public const string JavaScriptExtensionWithoutTXT = ".js";

        public static bool EndsWithTinyScriptExtension(string path)
        {
            return path.EndsWith(TypeScriptExtension, StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(JavaScriptExtension, StringComparison.OrdinalIgnoreCase);
        }
        
        internal static class TinyScriptHandler
        {
            [OnOpenAsset(1)]
            public static bool OpenScript(int instanceId, int line)
            {
                // @TODO Try to avoid static state here
                if (TinyEditorApplication.Project == null)
                {
                    return false;
                }

                var path = AssetDatabase.GetAssetPath(instanceId);

                if (!EndsWithTinyScriptExtension(path))
                {
                    return false;
                }

                var options = TinyBuildPipeline.WorkspaceBuildOptions;
                TinyBuildUtilities.RegenerateTSDefinitionFiles(options);
                TinyBuildUtilities.RegenerateTsConfig(options);

                if (string.IsNullOrEmpty(TinyPreferences.IDEDirectory))
                {
                    return false;
                }

                string openFileCommand;

                if (line < 0)
                {
                    line = 0;
                }

                // Try to launch executable with code
                if (TinyPreferences.IDEDirectory.Contains("Sublime"))
                {
                    openFileCommand = $"\"{Application.dataPath}/..\" \"{path}:{line}\" -a";
                }
                else if (TinyPreferences.IDEDirectory.Contains("VS Code") ||
                    TinyPreferences.IDEDirectory.Contains("Visual Studio Code"))
                {
                    // see: https://code.visualstudio.com/docs/editor/command-line 
#if UNITY_EDITOR_OSX
                    var appPath = Path.Combine(TinyPreferences.IDEDirectory, "Contents/Resources/app/bin/code");
#else
                    var appPath = TinyPreferences.IDEDirectory;
#endif
                    Process.Start(new ProcessStartInfo(appPath)
                    {
                        Arguments = $"./ --reuse-window --goto \"{path}:{line}\"",
                        WorkingDirectory = Path.GetFullPath(".")
                    });
                    return true;
                }
                else if (TinyPreferences.IDEDirectory.Contains("WebStorm"))
                {
                    openFileCommand = $"\"{Application.dataPath}/..\" \"{path}\" --line {line}";
                }
                else
                {
                    openFileCommand = $"\"{Application.dataPath}/..\" \"{path}\"";
                }

                Process.Start(TinyPreferences.IDEDirectory, openFileCommand);
                return true;
            }
        }
        
        public static void LogDiagnostics(this ScriptMetadata meta)
        {
            foreach (var d in meta.Diagnostics)
            {
                UnityEngine.Object scriptAsset = null;
                if (d.Source != null && !string.IsNullOrEmpty(d.Source.File))
                {
                    var scriptAssetPath = Persistence.GetPathRelativeToProjectPath(d.Source.File);
                    scriptAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(scriptAssetPath);
                }
                var log = d.ToString();
                switch ((DiagnosticCategory)d.Category)
                {
                    case DiagnosticCategory.Error:
                        UnityEngine.Debug.LogException(new TypeScriptError(d.Message, d.Source), scriptAsset);
                        break;
                    case DiagnosticCategory.Warning:
                        UnityEngine.Debug.LogWarning("TypeScript: " + log, scriptAsset);
                        break;
                    case DiagnosticCategory.Suggestion:
                    case DiagnosticCategory.Message:
                        UnityEngine.Debug.Log("TypeScript: " + log, scriptAsset);
                        break;
                    default:
                        UnityEngine.Debug.LogError($"TypeScript: Unknown compiler diagnostic category - {d.Category}", scriptAsset);
                        UnityEngine.Debug.LogError("TypeScript: " + log, scriptAsset);
                        break;
                }
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
    }
}

