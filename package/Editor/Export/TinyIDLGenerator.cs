

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Unity.Tiny
{
    internal static class TinyIDLGenerator
    {
        /// <summary>
        /// Generates the platform-agnostic IDL file from the given TinyProject.
        /// </summary>
        public static void GenerateIDL(TinyBuildOptions options, FileInfo destination)
        {
            var writer = new TinyCodeWriter
            {
                CodeStyle = CodeStyle.CSharp
            };

            var project = options.Project;
            var scripting = options.Context.GetScriptMetadata();
            
            writer.Line("using UTiny;");
            //writer.Line("using UTiny.Shared;");

            var mainModule = project.Module.Dereference(project.Registry);
            var usingClauses = new HashSet<string>(); 
            foreach (var dep in mainModule.EnumerateDependencies())
            {
                if (dep.Equals(mainModule))
                    continue;
                if (dep.Name == "UTiny.Core") // Hack: The Core module does not actually generate a Core namespace, but Core namespace is the UTiny/ut namespace.
                    continue;
                if (dep.ExportFlags.HasFlag(TinyExportFlags.EditorExtension))
                {
                    continue;
                }
                if (dep.IsRuntimeIncluded)
                {
                    usingClauses.Add($"using {dep.Namespace.Replace("ut.", "UTiny.")};"); // Core namespaces are of form "UTiny.Core2D".
                }
                else
                {
                    usingClauses.Add($"using {dep.Namespace.Replace("UTiny.", "ut.")};"); // Runtime generated namespaces are of form "ut.tween".
                }
            }

            foreach (var usingClause in usingClauses)
            {
                writer.Line(usingClause);
            }

            project.Visit(new GenerateIDLVisitor {Writer = writer});
            
            foreach (var behaviour in scripting.Behaviours)
            {
                WriteBehaviour(writer, behaviour);
            }

            foreach (var system in scripting.AllSystems)
            {
                WriteSystem(writer, system);
            }
            
            File.WriteAllText(destination.FullName, writer.ToString(), Encoding.UTF8);
        }

        private static void WriteBehaviour(TinyCodeWriter writer, ScriptComponentBehaviour behaviour)
        {
            using (writer.Scope($"namespace {behaviour.GetIdlNamespace()}"))
            {
                using (writer.Scope($"public struct {behaviour.Name}_State : IComponentData"))
                {
                    writer.Line("public bool initialized;");
                    writer.Line("public bool enabled;");
                    writer.Line("public bool onEnableCalled;");
                    writer.Line("public bool onDisableCalled;");
                }
            }
        }
        
        private static void WriteSystem(TinyCodeWriter writer, ScriptComponentSystem system)
        {
            if (system.IsRuntime)
            {
                return;
            }

            using (writer.Scope($"namespace {system.GetIdlNamespace()}"))
            {
                foreach (var d in system.Resolved.ExecuteBefore)
                {
                    writer.Line($"[UpdateBefore(typeof({d.GetIdlQualifiedName()}))]");
                }

                foreach (var d in system.Resolved.ExecuteAfter)
                {
                    writer.Line($"[UpdateAfter(typeof({d.GetIdlQualifiedName()}))]");
                }

                using (writer.Scope($"public class {system.GetIdlName()} : IComponentSystem"))
                {
                }
            }
        }

        private class GenerateIDLVisitor : TinyProject.Visitor
        {
            public TinyCodeWriter Writer { private get; set; }

            private TinyModule m_Module;

            private CodeWriterScope m_NamespaceCodeWriterScope;

            public override void BeginModule(TinyModule module)
            {
                if (module.ExportFlags.HasFlag(TinyExportFlags.EditorExtension))
                {
                    return;
                }
                m_Module = module;
                
                if (module.EntityGroups.Count > 0)
                {
                    // The idea here is we want to pack the component within the runtime `entityGroup` javascript object
                    // Each group resolves to a javascript object like so `{ENTITY_GROUPS}.{NAMESPACE}.{GROUP_NAME}`
                    //
                    // The group component lives in this object with the name `Component`
                    //
                    // e.g.
                    //
                    // - "MyGroup" becomes the runtime object `entities.game.MyGroup = {}`
                    // - The generated component becomes `entities.game.MyGroup.Component = function(w, e) {...}`
                    Writer.Line();
                    Writer.Line($"/*");
                    Writer.Line($" * !!! TEMP UNITL PROPER SCENE FORMAT !!!");
                    Writer.Line($" */");
                    using (Writer.Scope($"namespace {TinyHtml5Builder.KEntityGroupNamespace}.{module.Namespace}"))
                    {
                        foreach (var entityGroupRef in module.EntityGroups)
                        {
                            var entityGroup = entityGroupRef.Dereference(module.Registry);

                            if (null == entityGroup)
                            {
                                // @TODO We need to notify the user at this point
                                continue;
                            }
                            
                            using (Writer.Scope($"namespace {entityGroup.Name}"))
                            {
                                VisitType(new TinyType(null, null)
                                {
                                    TypeCode = TinyTypeCode.Component,
                                    Name = "Component"
                                });
                            }
                        }
                    }

                }
                
                Writer.Line();
                m_NamespaceCodeWriterScope = Writer.Scope($"namespace {module.Namespace}");
            }

            public override void EndModule(TinyModule module)
            {
                if (module.ExportFlags.HasFlag(TinyExportFlags.EditorExtension))
                {
                    return;
                }
                
                m_NamespaceCodeWriterScope.Dispose();
                m_NamespaceCodeWriterScope = null;
            }

            public override void VisitType(TinyType type)
            {
                if (type.ExportFlags.HasFlag(TinyExportFlags.EditorExtension))
                {
                    return;
                }
                
                if (type.IsRuntimeIncluded)
                {
                    // @HACK
                    if (type.Name == "Camera2D")
                    {
                        GenerateLayerComponents();
                    }
                    
                    return;
                }

                if (type.IsEnum)
                {
                    // @TODO Handle underlying type - for now we assume C#-friendly types
                    using (Writer.Scope($"public enum {type.Name}"))
                    {
                        var defaultValue = type.DefaultValue as TinyObject;
                        var first = true;
                        foreach (var field in type.Fields)
                        {
                            var value = defaultValue[field.Name];
                            Writer.Line($"{(first ? "" : ", ")}{field.Name} = {value}");
                            first = false;
                        }
                    }
                }
                else
                {
                    WriteType(type);
                }
            }
            
            private void GenerateLayerComponents()
            {
                using (Writer.Scope("namespace layers"))
                {
                    
                    for (var i = 0; i < 32; ++i)
                    {
                        var name = UnityEngine.LayerMask.LayerToName(i);
                        if (string.IsNullOrEmpty(name))
                        {
                            continue;
                        }

                        VisitType(new TinyType(null, null)
                        {
                            TypeCode = TinyTypeCode.Component,
                            Name = name.Replace(" ", "")
                        });
                    }
                }
            }

            public override void VisitEntityGroup(TinyEntityGroup entityGroup)
            {
                
            }

            private void WriteType(TinyType type)
            {
                var typePost = "";
                if (type.TypeCode == TinyTypeCode.Component || type.TypeCode == TinyTypeCode.Configuration) {
                    typePost = " : IComponentData";
                }


                if (type.TypeCode == TinyTypeCode.Configuration)
                {
                    Writer.Line("[Configuration]");
                }
                using (Writer.Scope($"public struct {type.Name}{typePost}"))
                {
                    foreach (var field in type.Fields)
                    {
                        var idlName = string.Empty;
                        var fieldType = field.FieldType.Dereference(type.Registry);
                        
                        if (fieldType.ExportFlags.HasFlag(TinyExportFlags.EditorExtension))
                        {
                            idlName = TinyEditorExtensionsGenerator.GetFieldTypeToIDL(type.Registry, field);
                        }
                        else
                        {
                            idlName = FieldTypeToIDL(type.Registry, field);
                        }

                        if (!string.IsNullOrEmpty(idlName))
                        {
                            Writer.LineFormat(field.Array ? "public DynamicArray<{0}> {1};" : "public {0} {1};",
                                idlName, field.Name);
                        }
                    }
                }
            }

            /// <summary>
            /// Returns the module that this enum type belongs to
            /// </summary>
            private static TinyModule GetEnumModule(IRegistry registry, TinyType.Reference type)
            {
                // @TODO Optimization/direct lookup... this is really bad
                var modules = registry.FindAllByType<TinyModule>();
                return modules.FirstOrDefault(module => module.Enums.Contains(type));
            }

            private static string FieldTypeToIDL(IRegistry registry, TinyField field)
            {
                // @TODO Use TinyBuildUtility.GetCSharpTypeName
                var code = field.FieldType.Dereference(registry).TypeCode;
                
                switch (code)
                {
                    case TinyTypeCode.Boolean: return "bool";
                    case TinyTypeCode.Int8: return "sbyte";
                    case TinyTypeCode.Int16: return "short";
                    case TinyTypeCode.Int32: return "int";
                    case TinyTypeCode.Int64: return "long";
                    case TinyTypeCode.UInt8: return "byte";
                    case TinyTypeCode.UInt16: return "ushort";
                    case TinyTypeCode.UInt32: return "uint";
                    case TinyTypeCode.UInt64: return "ulong";
                    case TinyTypeCode.Float32: return "float";
                    case TinyTypeCode.Float64: return "double";
                    case TinyTypeCode.String: return "string";
                    case TinyTypeCode.EntityReference: return "Entity";
                    case TinyTypeCode.UnityObject: return "Entity";
                    case TinyTypeCode.Struct:
                    case TinyTypeCode.Enum:
                        var name = field.FieldType.Name;
                        var module = GetEnumModule(registry, field.FieldType);
                        if (!string.IsNullOrEmpty(module?.Namespace) && !module.ExportFlags.HasFlag(TinyExportFlags.RuntimeIncluded))
                        {
                            name = $"{module.Namespace}.{name}";
                        }
                        return name;
                    default:
                        throw new NotSupportedException($"TinyTypeCode '{code.ToString()}' is not supported in IDL at the moment");
                }
            }
        }
    }
}

