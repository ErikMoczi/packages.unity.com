using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using UnityEditor.Compilation;
using UnityEngine;

namespace UnityEditor.Networking
{
    internal class WeaverRunner
    {
        [InitializeOnLoadMethod]
        static void OnInitializeOnLoad()
        {
            CompilationPipeline.assemblyCompilationFinished += OnCompilationFinished;
        }

        static void OnCompilationFinished(string targetAssembly, CompilerMessage[] messages)
        {
            const string k_HlapiRuntimeAssemblyName = "com.unity.multiplayer-hlapi.Runtime";

            // Do nothing if there were compile errors on the target
            if (messages.Length > 0)
            {
                Console.WriteLine("Compile messages present");
                foreach (var msg in messages)
                {
                    if (msg.type == CompilerMessageType.Error)
                    {
                        Console.WriteLine("Compile error, aborting: " + msg.message);
                        return;
                    }
                }
            }

            // Should not run on the editor only assemblies
            if (targetAssembly.Contains("-Editor") || targetAssembly.Contains(".Editor"))
            {
                return;
            }
            
            // Should not run on own assembly
            if (targetAssembly.Contains(k_HlapiRuntimeAssemblyName))
            {
                return;
            }

            var scriptAssembliesPath = Application.dataPath + "/../" + Path.GetDirectoryName(targetAssembly);

            string unityEngine = "";
            string unetAssemblyPath = "";
            var outputDirectory = scriptAssembliesPath;
            var assemblyPath = targetAssembly;

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            bool buildingForMetro = false;
            bool usesUnet = false;
            bool foundThisAssembly = false;
            foreach (var assembly in assemblies)
            {
                // Find the assembly currently being compiled from domain assembly list and check if it's using unet
                if (assembly.GetName().Name == Path.GetFileNameWithoutExtension(targetAssembly))
                {
                    foundThisAssembly = true;
                    foreach (var dependency in assembly.GetReferencedAssemblies())
                    {
                        if (dependency.Name.Contains(k_HlapiRuntimeAssemblyName))
                        {
                            usesUnet = true;
                        }
                    }
                }
                try
                {
                    if (assembly.Location.Contains("UnityEngine.CoreModule"))
                    {
                        unityEngine = assembly.Location;
                    }
                    if (assembly.Location.Contains(k_HlapiRuntimeAssemblyName))
                    {
                        unetAssemblyPath = assembly.Location;
                    }
                    if (assembly.Location.Contains("MetroSupport"))
                    {
                        buildingForMetro = true;
                    }
                }
                #pragma warning disable 168
                catch (NotSupportedException notSupported)
                {
                    // This assembly is in memory and has no location (there is no assembly.Location), so we'll ignore it
                    // Seems this only happens on UWP .net target and with the Microsoft.GeneratedCode assembly
                }
                #pragma warning restore 168
            }

            if (!foundThisAssembly)
            {
                // Target assembly not found in current domain, trying to load it to check references 
                // will lead to trouble in the build pipeline, so lets assume it should go to weaver
                // (only saw this happen in runtime test framework on editor platform)
                usesUnet = true;
            }

            if (!usesUnet)
            {
                return;
            }

            if (string.IsNullOrEmpty(unityEngine))
            {
                Debug.LogError("Failed to find UnityEngine assembly");
                return;
            }

            if (string.IsNullOrEmpty(unetAssemblyPath))
            {
                Debug.LogError("Failed to find hlapi runtime assembly");
                return;
            }

            IAssemblyResolver assemblyResolver = null;
            if (buildingForMetro)
            {
                assemblyResolver = GetAssemblyResolver(targetAssembly, null);
            }

            //Debug.Log("Package invoking weaver with " + unityEngine + " " + unetAssemblyPath + " " + outputDirectory + " " + assemblyPath + " " + assemblyResolver);

            Unity.UNetWeaver.Program.Process(unityEngine, unetAssemblyPath, outputDirectory, new[] { assemblyPath }, new string[] { }, assemblyResolver, (value) => { Debug.LogWarning(value); }, (value) => { Debug.LogError(value); });
        }

        public static IAssemblyResolver GetAssemblyResolver(string assemblyPath, string[] searchDirectories)
        {
            var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
            var targetFrameworkAttribute = assembly.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute");
            if (targetFrameworkAttribute != null)
            {
                var frameworkName = (string)targetFrameworkAttribute.ConstructorArguments[0].Value;
                if (frameworkName == ".NETCore,Version=v5.0" && PlayerSettings.GetScriptingBackend(BuildTargetGroup.WSA) == ScriptingImplementation.WinRTDotNET)
                {
                    var resolver = new NuGetAssemblyResolver(@"UWP\project.lock.json");
                    if (searchDirectories != null)
                    {
                        foreach (var dir in searchDirectories)
                        {
                            resolver.AddSearchDirectory(dir);
                        }
                    }
                    return resolver;
                }
            }
            return null;
        }
    }

    internal sealed class NuGetPackageResolver
    {
        public string PackagesDirectory
        {
            get;
            set;
        }

        public string ProjectLockFile
        {
            get;
            set;
        }

        public string TargetMoniker
        {
            get;
            set;
        }

        public string[] ResolvedReferences
        {
            get;
            private set;
        }

        public NuGetPackageResolver()
        {
            TargetMoniker = "UAP,Version=v10.0";
        }

        private string ConvertToWindowsPath(string path)
        {
            return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        public string[] Resolve()
        {
            var text = File.ReadAllText(ProjectLockFile);
            var lockFile = (Dictionary<string, object>)Json.Deserialize(text);
            var targets = (Dictionary<string, object>)lockFile["targets"];
            var target = FindUWPTarget(targets);

            var references = new List<string>();
            var packagesPath = ConvertToWindowsPath(GetPackagesPath());

            foreach (var packagePair in target)
            {
                var package = (Dictionary<string, object>)packagePair.Value;

                object compileObject;
                if (!package.TryGetValue("compile", out compileObject))
                    continue;
                var compile = (Dictionary<string, object>)compileObject;

                var parts = packagePair.Key.Split('/');
                var packageId = parts[0];
                var packageVersion = parts[1];
                var packagePath = Path.Combine(Path.Combine(packagesPath, packageId), packageVersion);
                if (!Directory.Exists(packagePath))
                    throw new Exception(string.Format("Package directory not found: \"{0}\".", packagePath));

                foreach (var name in compile.Keys)
                {
                    const string emptyFolder = "_._";
                    if (string.Equals(Path.GetFileName(name), emptyFolder, StringComparison.InvariantCultureIgnoreCase))
                        continue;
                    var reference = Path.Combine(packagePath, ConvertToWindowsPath(name));
                    if (!File.Exists(reference))
                        throw new Exception(string.Format("Reference not found: \"{0}\".", reference));
                    references.Add(reference);
                }

                if (package.ContainsKey("frameworkAssemblies"))
                    throw new NotImplementedException("Support for \"frameworkAssemblies\" property has not been implemented yet.");
            }

            ResolvedReferences = references.ToArray();
            return ResolvedReferences;
        }

        private Dictionary<string, object> FindUWPTarget(Dictionary<string, object> targets)
        {
            foreach (var target in targets)
            {
                if (target.Key.StartsWith(TargetMoniker) && !target.Key.Contains("/"))
                    return (Dictionary<string, object>)target.Value;
            }

            throw new InvalidOperationException("Could not find suitable target for " + TargetMoniker + " in project.lock.json file.");
        }

        private string GetPackagesPath()
        {
            var value = PackagesDirectory;
            if (!string.IsNullOrEmpty(value))
                return value;
            value = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
            if (!string.IsNullOrEmpty(value))
                return value;
#if NETFX_CORE
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
#else
            var userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
#endif
            return Path.Combine(Path.Combine(userProfile, ".nuget"), "packages");
        }
    }
    
    public class SearchPathAssemblyResolver : IAssemblyResolver
    {
        private readonly Dictionary<string, AssemblyDefinition> _assemblies = new Dictionary<string, AssemblyDefinition>(StringComparer.InvariantCulture);
        private readonly List<string> _searchPaths = new List<string>();

        public void AddAssembly(AssemblyDefinition assembly)
        {
            var name = assembly.Name.Name;
            if (_assemblies.ContainsKey(name))
                throw new Exception(string.Format("Assembly \"{0}\" is already registered.", name));
            _assemblies.Add(name, assembly);
        }

        public void AddSearchDirectory(string path)
        {
            if (_searchPaths.Any(p => string.Equals(p, path, StringComparison.InvariantCultureIgnoreCase)))
                return;
            _searchPaths.Add(path);
        }

        public AssemblyDefinition Resolve(string fullName)
        {
            return Resolve(fullName, new ReaderParameters() { AssemblyResolver = this });
        }

        public AssemblyDefinition Resolve(string fullName, ReaderParameters parameters)
        {
            return Resolve(AssemblyNameReference.Parse(fullName), parameters);
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            ReaderParameters p = new ReaderParameters();
            p.AssemblyResolver = this;
            return Resolve(name, p);
        }

        public virtual AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            AssemblyDefinition assembly;
            if (_assemblies.TryGetValue(name.Name, out assembly))
                return assembly;

            foreach (var searchPath in _searchPaths)
            {
                var fileName = name.Name + (name.IsWindowsRuntime ? ".winmd" : ".dll");
                var filePath = Path.Combine(searchPath, fileName);
                if (!File.Exists(filePath))
                    continue;

                assembly = AssemblyDefinition.ReadAssembly(filePath, parameters);
                if (!string.Equals(assembly.Name.Name, name.Name, name.IsWindowsRuntime ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture))
                    continue;
                _assemblies.Add(name.Name, assembly);
                return assembly;
            }

            throw new AssemblyResolutionException(name);
        }

        public void Dispose()
        {
            _assemblies.Clear();
            _searchPaths.Clear();
        }
    }
    
    class NuGetAssemblyResolver : SearchPathAssemblyResolver
    {
        private readonly Dictionary<string, string>  _references;
        private readonly Dictionary<string, AssemblyDefinition> _assemblies = new Dictionary<string, AssemblyDefinition>(StringComparer.InvariantCulture);

        public NuGetAssemblyResolver(string projectLockFile)
        {
            var resolver = new NuGetPackageResolver
            {
                ProjectLockFile = projectLockFile,
            };
            resolver.Resolve();
            var references = resolver.ResolvedReferences;

            _references = new Dictionary<string, string>(references.Length, StringComparer.InvariantCultureIgnoreCase);
            foreach (var reference in references)
            {
                var fileName = Path.GetFileName(reference);
                string existingReference;
                if (_references.TryGetValue(fileName, out existingReference))
                    throw new Exception(string.Format("Reference \"{0}\" already added as \"{1}\".", reference, existingReference));
                _references.Add(fileName, reference);
            }
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            AssemblyDefinition assembly;
            if (_assemblies.TryGetValue(name.Name, out assembly))
                return assembly;

            var fileName = name.Name + (name.IsWindowsRuntime ? ".winmd" : ".dll");
            string reference;
            if (_references.TryGetValue(fileName, out reference))
            {
                assembly = AssemblyDefinition.ReadAssembly(reference, parameters);
                if (string.Equals(assembly.Name.Name, name.Name, StringComparison.InvariantCulture))
                {
                    _assemblies.Add(name.Name, assembly);
                    return assembly;
                }
            }

            return base.Resolve(name, parameters);
        }

        public bool IsFrameworkAssembly(AssemblyNameReference name)
        {
            string reference;
            var fileName = name.Name + (name.IsWindowsRuntime ? ".winmd" : ".dll");
            return _references.TryGetValue(fileName, out reference);
        }
    }
}
