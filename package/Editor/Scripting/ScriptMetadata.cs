

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Properties;
using Unity.Properties.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Tiny
{    
    internal enum DiagnosticCategory 
    {
        Warning = 0,
        Error = 1,
        Suggestion = 2,
        Message = 3
    }

    internal interface IScriptObject : INamed
    {
        ScriptSource Source { get; }
        string QualifiedName { get; }
    }

    internal sealed class ResolvedScriptComponentSystem<TSchedulable>
        where TSchedulable : class, IPropertyContainer, ISchedulable<TSchedulable>
    {
        public readonly List<ScriptEntityFilter> Filters = new List<ScriptEntityFilter>();
        public readonly List<TSchedulable> ExecuteAfter = new List<TSchedulable>();
        public readonly List<TSchedulable> ExecuteBefore = new List<TSchedulable>();
        public readonly List<TinyType.Reference> RequiredComponents = new List<TinyType.Reference>();
        public readonly List<TinyType.Reference> OptionalComponents = new List<TinyType.Reference>();
        
        public void Clear()
        {
            Filters.Clear();
            ExecuteAfter.Clear();
            ExecuteBefore.Clear();
            RequiredComponents.Clear();
            OptionalComponents.Clear();
        }
    }

    internal interface INamedDeclaration
    {
        string Name { get; }
        string QualifiedName { get; }
        string Description { get; }
        
        ScriptSource Source { get; }
    }

    internal interface ISchedulable<T> : INamedDeclaration
        where T : class, IPropertyContainer
    {
        PropertyList<T, string> Filters { get; }
        PropertyList<T, string> ExecuteAfter { get; }
        PropertyList<T, string> ExecuteBefore { get; }
        PropertyList<T, string> RequiredComponents { get; }
        PropertyList<T, string> OptionalComponents { get; }

        IEnumerable<T> GetSchedulables(ScriptMetadata source);
    }

    internal abstract class ScriptSchedulableBase<TAgainst>
        where TAgainst : class, IPropertyContainer, ISchedulable<TAgainst>
    {
        public readonly ResolvedScriptComponentSystem<TAgainst> Resolved =
            new ResolvedScriptComponentSystem<TAgainst>();
        
        internal void Resolve(TinyContext context, TAgainst source)
        {
            Resolved.Clear();
            
            foreach (var filter in source.Filters)
            {
                var resolved = context.GetScriptMetadata().Filters.FirstOrDefault(f =>
                    string.Equals(f.QualifiedName, filter, StringComparison.Ordinal));
                
                if (resolved == null)
                {
                    context.GetScriptMetadata().ResolutionErrors.Add(new TypeScriptParseError($"Could not resolve EntityFilter '{filter}'", source.Source));
                }
                Resolved.Filters.Add(resolved);
            }
            
            foreach (var executeAfter in source.ExecuteAfter)
            {
                var resolved = source.GetSchedulables(context.GetScriptMetadata()).FirstOrDefault(f =>
                    string.Equals(f.QualifiedName, executeAfter, StringComparison.Ordinal));

                if (resolved == null)
                {
                    context.GetScriptMetadata().ResolutionErrors.Add(new TypeScriptParseError($"Could not schedule '{source.QualifiedName}' after '{executeAfter}'", source.Source));
                }
                Resolved.ExecuteAfter.Add(resolved);
            }

            foreach (var executeBefore in source.ExecuteBefore)
            {
                var resolved = source.GetSchedulables(context.GetScriptMetadata()).FirstOrDefault(f =>
                    string.Equals(f.QualifiedName, executeBefore, StringComparison.Ordinal));

                if (resolved == null)
                {
                    context.GetScriptMetadata().ResolutionErrors.Add(new TypeScriptParseError($"Could not schedule '{source.QualifiedName}' before '{executeBefore}'", source.Source));
                }
                Resolved.ExecuteBefore.Add(resolved);
            }
            
            foreach (var requiredComponent in source.RequiredComponents)
            {
                var resolved = context.Registry.FindComponentByQualifiedName(requiredComponent);

                if (resolved == null)
                {
                    context.GetScriptMetadata().ResolutionErrors.Add(new TypeScriptParseError($"Could not resolve Component '{requiredComponent}'", source.Source));
                }
                Resolved.RequiredComponents.Add(resolved?.Ref ?? TinyType.Reference.None);
            }
            
            foreach (var optionalComponent in source.OptionalComponents)
            {
                var resolved = context.Registry.FindComponentByQualifiedName(optionalComponent);

                if (resolved == null)
                {
                    context.GetScriptMetadata().ResolutionErrors.Add(new TypeScriptParseError($"Could not resolve Component '{optionalComponent}'", source.Source));
                }
                Resolved.OptionalComponents.Add(resolved?.Ref ?? TinyType.Reference.None);
            }
        }
    }

    internal static class NamedDeclarationExtensions
    {
        public static string GetNamespace(this INamedDeclaration decl)
        {
            var dotIndex = decl.QualifiedName.LastIndexOf('.');
            if (dotIndex <= 0)
            {
                return string.Empty;
            }

            return decl.QualifiedName.Substring(0, dotIndex);
        }

        public static string GetIdlNamespace(this INamedDeclaration decl)
        {
            var ns = decl.GetNamespace();
            return ns.StartsWith("ut.", StringComparison.Ordinal) ? "UTiny." + ns.Substring(3) : ns;
        }

        public static string GetIdlName(this ScriptComponentSystem decl)
        {
            if (decl.IsRuntime)
            {
                return decl.Name;
            }

            return decl.Name + "JS";
        }

        public static string GetIdlQualifiedName(this ScriptComponentSystem decl)
        {
            var ns = decl.GetIdlNamespace();
            var name = decl.GetIdlName();
            return ns.Length == 0 ? name : ns + "." + name;
        }
    }
    
    internal partial class ScriptComponentSystem : ScriptSchedulableBase<ScriptComponentSystem>, ISchedulable<ScriptComponentSystem>, INamed, IScriptObject
    {
        public bool IsRuntime { get; set; }
        public bool IsBehaviour { get; set; }
        public ScriptComponentBehaviour Behaviour { get; set; }
        public string BehaviourMethod { get; set; }

        public IEnumerable<ScriptComponentSystem> GetSchedulables(ScriptMetadata source)
        {
            // systems can be scheduled against other systems, but not behaviours, as they represent multiple systems
            return source.Systems.Where(s => s.IsBehaviour == false);
        }

        public void Resolve(TinyContext context)
        {
            Resolve(context, this);
        }
    }
    
    internal partial class ScriptComponentBehaviour : ScriptSchedulableBase<ScriptComponentBehaviour>, ISchedulable<ScriptComponentBehaviour>, INamed, IScriptObject
    {
        public IEnumerable<ScriptComponentBehaviour> GetSchedulables(ScriptMetadata source)
        {
            // ComponentBehaviour can only be scheduled against other behaviours
            // individual systems are already scheduled between fences
            return source.Behaviours;
        }
        
        public ScriptField MainFilterField { get; private set; }
        public ScriptEntityFilter MainFilter { get; private set; }
        
        public string MakeName(string method)
        {
            return Name + "_" + method;
        }

        public string MakeQualifiedName(string method)
        {
            return QualifiedName + "_" + method;
        }
        
        public void Resolve(TinyContext context)
        {
            Resolve(context, this);

            foreach (var field in Fields)
            {
                field.Resolve(context);

                var filter = context.GetScriptMetadata().Filters.FirstOrDefault(f => f.QualifiedName == field.QualifiedName);
                if (filter != null)
                {
                    if (MainFilter != null)
                    {
                        context.GetScriptMetadata().ResolutionErrors.Add(
                            new TypeScriptParseError(
                                $"ComponentBehaviour support at most one EntityFilter field", field.Source));
                    }
                    else
                    {
                        MainFilter = filter;
                        MainFilterField = field;
                    }
                }
            }

            if (MainFilter == null)
            {
                context.GetScriptMetadata().ResolutionErrors.Add(
                    new TypeScriptParseError(
                        $"ComponentBehaviour must declare one EntityFilter field", Source));
            }
        }
    }

    internal enum ScriptFieldType
    {
        Unsupported = 0,
        /// <summary>
        /// Maps to a ut.World field type.
        /// </summary>
        World,
        /// <summary>
        /// Maps to a ut.Entity field type.
        /// </summary>
        Entity,
        /// <summary>
        /// Maps to any known ut.Component type
        /// </summary>
        Component,
        /// <summary>
        /// Maps to any known ut.EntityFilter type
        /// </summary>
        Filter
    }

    internal partial class ScriptEntityFilter : INamedDeclaration, INamed, IScriptObject
    {
        public void Resolve(TinyContext context)
        {
            foreach (var field in Fields)
            {
                field.Resolve(context);
            }
        }
    }

    internal partial class ScriptField
    {
        public ScriptFieldType Type { get; private set; }
        public TinyType.Reference ComponentType { get; private set; }

        public void Resolve(TinyContext context)
        {
            Type = ScriptFieldType.Unsupported;
            ComponentType = TinyType.Reference.None;
            
            switch (QualifiedName)
            {
                case "ut.World":
                    Type = ScriptFieldType.World;
                    break;
                case "ut.Entity":
                    Type = ScriptFieldType.Entity;
                    break;
                default:
                    var resolvedFilter = context.GetScriptMetadata().Filters.FirstOrDefault(f =>
                        string.Equals(f.QualifiedName, QualifiedName, StringComparison.Ordinal));

                    if (resolvedFilter != null)
                    {
                        Type = ScriptFieldType.Filter;
                    }
                    else
                    {
                        var resolvedType = context.Registry.FindComponentByQualifiedName(QualifiedName);
                        if (resolvedType != null)
                        {
                            Type = ScriptFieldType.Component;
                            ComponentType = resolvedType.Ref;
                        }
                    }
                    break;
            }
        }
    }

    internal static class RegistryScriptExtensions
    {
        public static T FindTypeByQualifiedName<T>(this IRegistry registry, string qualifiedName)
            where T : TinyRegistryObjectBase
        {
            if (string.IsNullOrEmpty(qualifiedName))
            {
                return null;
            }

            var lastDot = qualifiedName.LastIndexOf('.');
            if (lastDot == -1 || lastDot == qualifiedName.Length - 1)
            {
                return null;
            }

            var moduleNamespace = qualifiedName.Substring(0, lastDot);
            if (string.IsNullOrEmpty(moduleNamespace))
            {
                return null;
            }

            var typeName = qualifiedName.Substring(lastDot + 1);

            foreach (var module in registry.FindAllByType<TinyModule>())
            {
                if (module.Namespace == moduleNamespace)
                {
                    foreach (var typeRef in module.Types)
                    {
                        var type = typeRef.Dereference(registry);
                        if (type != null && type.Name == typeName)
                        {
                            return type as T;
                        }
                    }
                }
            }

            return null;
        }

        public static TinyType FindComponentByQualifiedName(this IRegistry registry, string qualifiedName)
        {
            var result = registry.FindTypeByQualifiedName<TinyType>(qualifiedName);
            return (result != null && result.IsComponent) ? result : null;
        }
    }

    internal class TypeScriptParseError : TypeScriptError
    {
        public TypeScriptParseError(string message, ScriptSource source) : base(message, source)
        {
        }
    }
    
    internal partial class ScriptMetadata
    {
        public readonly List<TypeScriptParseError> ResolutionErrors = new List<TypeScriptParseError>();

        public IEnumerable<INamedDeclaration> AllDeclarations =>
            Filters.Concat<INamedDeclaration>(Systems).Concat(Behaviours);

        /// <summary>
        /// Includes: built-in, legacy, user-defined, and behaviour-generated system.
        /// </summary>
        public IEnumerable<ScriptComponentSystem> AllSystems => Systems;

        public IEnumerable<IScriptObject> AllObjects => Systems.Where(s => !s.IsRuntime && !s.IsBehaviour)
            .Concat<IScriptObject>(Filters).Concat(Behaviours);

        private const string k_OnEnableMethod = "OnEntityEnable";
        private const string k_OnUpdateMethod = "OnEntityUpdate";
        private const string k_OnDisableMethod = "OnEntityDisable";

        private static readonly HashSet<string> k_SupportedMethods = new HashSet<string>()
        {
            k_OnEnableMethod,
            k_OnUpdateMethod,
            k_OnDisableMethod
        };

        private const string k_InputFenceName = "ut.Shared.InputFence";
        private const string k_UserCodeStartName = "ut.Shared.UserCodeStart";
        private const string k_UserCodeEndName = "ut.Shared.UserCodeEnd";
        private const string k_PlatformRenderingFenceName = "ut.Shared.PlatformRenderingFence";
        
        /// <summary>
        /// Resolves the qualified type names to object references.
        /// </summary>
        public bool Resolve(TinyContext context, TinyModule mainModule)
        {
            var requiredFences = new Dictionary<string, ScriptComponentSystem>()
            { 
                {k_InputFenceName, null},
                {k_UserCodeStartName, null},
                {k_UserCodeEndName, null},
                {k_PlatformRenderingFenceName, null}
            };
            
            foreach (var module in mainModule.EnumerateDependencies())
            {
                // look for runtime systems
                if (false == string.IsNullOrEmpty(module.MetadataFileGUID))
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(module.MetadataFileGUID);
                    if (false == string.IsNullOrEmpty(assetPath))
                    {
                        var resolvedPath = Path.GetFullPath(assetPath);
                        var runtimeMeta =
                            JsonSerializer.Deserialize<ScriptMetadata>(File.ReadAllText(resolvedPath));
                        
                        foreach (var scriptSystem in runtimeMeta.Systems)
                        {
                            scriptSystem.IsRuntime = true;
                            
                            Systems.Add(scriptSystem);
                        }
                    }
                }
            }

            foreach (var system in Systems)
            {      
                if (requiredFences.ContainsKey(system.QualifiedName))
                {
                    requiredFences[system.QualifiedName] = system;
                }
            }

            foreach (var kvp in requiredFences)
            {
                Assert.IsNotNull(kvp.Value, $"Could not find expected system: '{kvp.Key}'");
            }

            if (requiredFences.Any(f => f.Value == null))
            {
                return false;
            }
            
            // Resolve qualified names to actual objects

            foreach (var filter in Filters)
            {
                filter.Resolve(context);
            }

            foreach (var system in AllSystems)
            {
                system.Resolve(context);
            }
            
            foreach (var behaviour in Behaviours)
            {
                behaviour.Resolve(context);
            }
            
            var behaviourSystemMap = new Dictionary<string, ScriptComponentSystem>();
            
            // create behaviour systems based on their methods
            foreach (var behaviour in Behaviours)
            {
                foreach (var method in behaviour.Methods)
                {
                    if (k_SupportedMethods.Contains(method))
                    {
                        var system = new ScriptComponentSystem()
                        {
                            Name = behaviour.MakeName(method),
                            QualifiedName = behaviour.MakeQualifiedName(method),
                            IsBehaviour = true,
                            Behaviour = behaviour,
                            BehaviourMethod = method
                        };
                        behaviourSystemMap[system.QualifiedName] = system;
                        Systems.Add(system);
                    }
                }
            }
            
            // fix-up scheduling for the created behaviour systems
            foreach (var behaviour in Behaviours)
            {
                foreach (var method in behaviour.Methods)
                {
                    if (!k_SupportedMethods.Contains(method))
                    {
                        continue;
                    }

                    var sourceName = behaviour.MakeQualifiedName(method);
                    var sourceSystem = behaviourSystemMap[sourceName]; // must exist at this point

                    // group by method
                    switch (method)
                    {
                        case k_OnEnableMethod:
                            sourceSystem.Resolved.ExecuteBefore.Add(requiredFences[k_InputFenceName]);
                            break;
                        case k_OnUpdateMethod:
                            sourceSystem.Resolved.ExecuteAfter.Add(requiredFences[k_UserCodeStartName]);
                            sourceSystem.Resolved.ExecuteBefore.Add(requiredFences[k_UserCodeEndName]);
                            break;
                        case k_OnDisableMethod:
                            sourceSystem.Resolved.ExecuteAfter.Add(requiredFences[k_PlatformRenderingFenceName]);
                            break;
                    }
                    
                    // schedule against each other
                    foreach (var executeAfter in behaviour.Resolved.ExecuteAfter)
                    {
                        var name = executeAfter.MakeQualifiedName(method);
                        if (behaviourSystemMap.TryGetValue(name, out var matchingSystem))
                        {
                            sourceSystem.Resolved.ExecuteAfter.Add(matchingSystem);
                        }
                    }
                    
                    foreach (var executeBefore in behaviour.Resolved.ExecuteBefore)
                    {
                        var name = executeBefore.MakeQualifiedName(method);
                        if (behaviourSystemMap.TryGetValue(name, out var matchingSystem))
                        {
                            sourceSystem.Resolved.ExecuteBefore.Add(matchingSystem);
                        }
                    }
                }
            }

            return ResolutionErrors.Count == 0;
        }
        
        
        /// <summary>
        /// Returns a depth first search of the System execution graph
        /// </summary>
        /// <returns></returns>
        public IList<ScriptComponentSystem> GetSystemExecutionOrder()
        {
            var systems = new List<ScriptComponentSystem>();

            var graph = GetSystemGraph();

            DetectCycle(graph);

            AcyclicGraphIterator.DepthFirst.Execute(graph, system => systems.Add(system));

            return systems;
        }

        private static void DetectCycle(AcyclicGraph<ScriptComponentSystem> graph)
        {
            var count = 0;
            var error = string.Empty;

            AcyclicGraphIterator.DetectCycle.Execute(graph,
                () => { error += $"[{TinyConstants.ApplicationName}] SystemExecutionGraph detected cyclic reference ("; },
                () => { error += ")"; },
                system =>
                {
                    if (count != 0)
                    {
                        error += ", ";
                    }

                    error += system.Name;

                    count++;
                });

            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError(error);
            }
        }

        /// <summary>
        /// Builds an AcyclicGraph from the included systems
        /// </summary>
        private AcyclicGraph<ScriptComponentSystem> GetSystemGraph()
        {
            var graph = new AcyclicGraph<ScriptComponentSystem>();

            // Add all systems
            foreach (var system in AllSystems)
            {
                graph.Add(system);
            }

            // Connect all first level dependencies
            foreach (var node in graph.Nodes.ToList())
            {
                foreach (var executeAfter in node.Data.Resolved.ExecuteAfter)
                {
                    var depedencyNode = graph.GetOrAdd(executeAfter);
                    graph.AddDirectedConnection(node, depedencyNode);
                }

                foreach (var dependencyRef in node.Data.Resolved.ExecuteBefore)
                {
                    var depedencyNode = graph.GetOrAdd(dependencyRef);
                    graph.AddDirectedConnection(depedencyNode, node);
                }
            }

            return graph;
        }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            TypeConversion.Register<IPropertyContainer, ScriptSource>(v =>
            {
                var instance = new ScriptSource();
                PropertyContainer.Transfer(v, instance);
                return instance;
            });
        }
    }

    internal class TypeScriptError : Exception
    {
        private ScriptSource m_Source;

        // HACK to open the right file when the error is double-clicked in the Console window
        public override string StackTrace
        {
            get
            {
                return (m_Source == null || String.IsNullOrEmpty(m_Source.File)) ? base.StackTrace : $"Scripting error (at {m_Source.File}:{m_Source.Line})";
            }   
        }

        public TypeScriptError(string message, ScriptSource source)
            : base(message)
        {
            m_Source = source;
        }
    }

    internal partial class ScriptDiagnostic
    {
        public override string ToString()
        {
            return null != Source ? $"{Message}\r\n\r\n{Source}" : Message;
        }
    }

    internal partial class ScriptSource
    {
        public override string ToString()
        {
            return $"{File}:{Line}";
        }
    }
}

