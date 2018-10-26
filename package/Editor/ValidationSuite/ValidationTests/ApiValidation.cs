#if UNITY_2018_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Unity.APIComparison.Framework.Changes;
using Unity.APIComparison.Framework.Collectors;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class ApiValidation : BaseValidation
    {

        private readonly ApiValidationAssemblyInformation assemblyInformation;

        public ApiValidation()
        {
            TestName = "API Validation";
            TestDescription = "Checks public API for style and changest that conflict with Semantic Versioning.";
            TestCategory = TestCategory.ApiValidation;
            assemblyInformation = new ApiValidationAssemblyInformation();
        }

        public ApiValidation(ApiValidationAssemblyInformation apiValidationAssemblyInformation)
            :this()
        {
            this.assemblyInformation = apiValidationAssemblyInformation;
        }

        public ApiValidationAssemblyInformation ApiValidationAssemblyInformation
        {
            get { return assemblyInformation; }
        }

        private void Error(string message)
        {
            this.TestOutput.Add("Error: " + message);
            TestState = TestState.Failed;
        }

        private void CheckAsmdefs(HashSet<string> files, Assembly[] assemblies, out List<string> assemblyDefinitionFilePaths)
        {
            assemblyDefinitionFilePaths = new List<string>();
            var previousAssemblyDefinitions = GetAssemblyDefinitionDataInFolder(Context.PreviousPackageInfo.path);
            var versionChangeType = DetermineVersionChangeType();
            foreach (var assembly in assemblies)
            {
                var assemblyDefinitionFilePath = Path.GetFullPath(CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assembly.name));
                if (!assemblyDefinitionFilePath.StartsWith(Context.ProjectPackageInfo.path))
                {
                    foreach (var file in assembly.sourceFiles.Where(files.Contains))
                        Error(string.Format("Script \"{0}\" is not included by any asmdefs in the package.", file));
                }
                else
                {
                    //assembly is in the package
                    assemblyDefinitionFilePaths.Add(assemblyDefinitionFilePath);

                    var previousAssemblyDefinition = previousAssemblyDefinitions.FirstOrDefault(ad => DoAssembliesMatch(ad, assembly));
                    if (previousAssemblyDefinition == null)
                        continue; //new asmdefs are fine

                    var projectAssemblyDefinition = Utilities.GetDataFromJson<AssemblyDefinition>(assemblyDefinitionFilePath);
                    var excludePlatformsDiff = string.Format("Was:\"{0}\" Now:\"{1}\"",
                        string.Join(", ", previousAssemblyDefinition.excludePlatforms),
                        string.Join(", ", projectAssemblyDefinition.excludePlatforms));
                    if (previousAssemblyDefinition.excludePlatforms.Any(p => !projectAssemblyDefinition.excludePlatforms.Contains(p)) &&
                        versionChangeType == VersionChangeType.Patch)
                        Error("Removing from excludePlatfoms requires a new minor or major version. " + excludePlatformsDiff);
                    else if (projectAssemblyDefinition.excludePlatforms.Any(p =>
                                 !previousAssemblyDefinition.excludePlatforms.Contains(p)) &&
                             (versionChangeType == VersionChangeType.Patch || versionChangeType == VersionChangeType.Minor))
                        Error("Adding to excludePlatforms requires a new major version. " + excludePlatformsDiff);

                    var includePlatformsDiff = string.Format("Was:\"{0}\" Now:\"{1}\"",
                        string.Join(", ", previousAssemblyDefinition.includePlatforms),
                        string.Join(", ", projectAssemblyDefinition.includePlatforms));
                    if (previousAssemblyDefinition.includePlatforms.Any(p => !projectAssemblyDefinition.includePlatforms.Contains(p)) &&
                        (versionChangeType == VersionChangeType.Patch || versionChangeType == VersionChangeType.Minor))
                        Error("Removing from includePlatfoms requires a new major version. " + includePlatformsDiff);
                    else if (projectAssemblyDefinition.includePlatforms.Any(p =>
                                 !previousAssemblyDefinition.includePlatforms.Contains(p)))
                    {
                        if (versionChangeType == VersionChangeType.Patch)
                            Error("Adding to includePlatforms requires a new minor or major version. " + includePlatformsDiff);
                        else if (previousAssemblyDefinition.includePlatforms.Length == 0 && versionChangeType == VersionChangeType.Minor)
                            Error("Adding the first entry in inlcudePlatforms requires a new major version. " + includePlatformsDiff);
                    }
                             
                }
            }

            CheckForAsmdefsNotIncludedInEditor();
        }

        private bool DoAssembliesMatch(AssemblyDefinition assemblyDefinition, Assembly assembly)
        {
            return assemblyInformation.GetAssemblyName(assemblyDefinition, true).Equals(assemblyInformation.GetAssemblyName(assembly, false));
        }

        private void CheckForAsmdefsNotIncludedInEditor()
        {
            var assemblyDefinitions = GetAssemblyDefinitionDataInFolder(this.Context.ProjectPackageInfo.path);
            foreach (var assemblyDefinition in assemblyDefinitions)
            {
                if (assemblyDefinition.includePlatforms.Any() &&
                    !assemblyDefinition.includePlatforms.Contains("Editor") ||
                    assemblyDefinition.excludePlatforms.Any() &&
                    assemblyDefinition.excludePlatforms.Contains("Editor"))
                {
                    Error(string.Format("Package Validation Suite does not support .asmdefs that are not built on the \"Editor\" platform. See \"{0}\"", assemblyDefinition.name));
                }
            }
        }

        private AssemblyDefinition[] GetAssemblyDefinitionDataInFolder(string directory)
        {
            return Directory.GetFiles(directory, "*.asmdef", SearchOption.AllDirectories)
                .Select(Utilities.GetDataFromJson<AssemblyDefinition>).ToArray();
        }

        protected override void Run()
        {
            TestState = TestState.Succeeded;
            var packagePath = this.Context.ProjectPackageInfo.path;
            var files = new HashSet<string>(Directory.GetFiles(packagePath, "*", SearchOption.AllDirectories).Select(Utilities.GetNormalizedRelativePath));

            //does it compile?
            if (EditorUtility.scriptCompilationFailed)
            {
                Error("Compilation failed. Please fix any compilation errors.");
                return;
            }

            if (EditorApplication.isCompiling)
            {
                Error("Compilation in progress. Please wait for compilation to finish.");
                return;
            }

            if (Context.PreviousPackageInfo == null)
            {
                this.TestOutput.Add("No previous package version. Skipping Semantic Versioning checks.");
                return;
            }

            //does it have asmdefs for all scripts?
            List<string> assemblyDefinitionFilePaths;
            var assemblies = GetRelevantAssemblies(files);
            CheckAsmdefs(files, assemblies, out assemblyDefinitionFilePaths);

            CheckApiDiff(assemblies, files);

            //Run analyzers
        }

        private static Assembly[] GetRelevantAssemblies(HashSet<string> files)
        {
            return CompilationPipeline.GetAssemblies().Where(a => a.sourceFiles.Any(files.Contains)).ToArray();
        }

        class AssemblyChange
        {
            public List<IEntityChange> apiChanges = new List<IEntityChange>();
        }

        class ApiDiff
        {
            public List<string> missingAssemblies;
            public Dictionary<string, ApiValidation.AssemblyChange> assemblyChanges;
            public int breakingChanges;
            public int additions;
            public int removedAssemblyCount;

            public ApiDiff()
            {
                missingAssemblies = new List<string>();
                assemblyChanges = new Dictionary<string, ApiValidation.AssemblyChange>();
            }
        }

        private void CheckApiDiff(Assembly[] assemblies, IEnumerable<string> filesInPackage)
        {
            ApiValidation.ApiDiff diff = new ApiValidation.ApiDiff();
            var assembliesForPackage = NonTestAssembliesForPackage(assemblies, filesInPackage).ToArray();
            if (Context.PreviousPackageBinaryDirectory == null)
            {
                Error("Previous package binaries must be present on artifactory to do API diff.");
                return;
            }

            var oldAssemblyPaths = Directory.GetFiles(Context.PreviousPackageBinaryDirectory, "*.dll");

            //Build diff
            foreach (var currentAssembly in assembliesForPackage)
            {
                var currentAssemblyFilename = Path.GetFileName(currentAssembly.outputPath);
                var oldAssemblyPath = oldAssemblyPaths.FirstOrDefault(p =>
                    Path.GetFileName(p) == currentAssemblyFilename);

                var assemblyChange = new ApiValidation.AssemblyChange
                {
                    apiChanges = APIChangesCollector.Collect(oldAssemblyPath, currentAssembly.outputPath).ToList()
                };

                diff.assemblyChanges[currentAssemblyFilename] = assemblyChange;
            }

            foreach (var oldAssemblyPath in oldAssemblyPaths)
            {
                var oldAssemblyFilename = Path.GetFileName(oldAssemblyPath);
                if (assembliesForPackage.All(a => Path.GetFileName(a.outputPath) != oldAssemblyFilename))
                    diff.missingAssemblies.Add(oldAssemblyFilename);
            }

            //separate changes
            var allChanges = diff.assemblyChanges.Values.SelectMany(v => v.apiChanges).SelectMany(c => c.Changes).ToList();
            diff.additions = allChanges.Count(c => c.IsAdd());
            diff.removedAssemblyCount = diff.missingAssemblies.Count;
            diff.breakingChanges = allChanges.Count - diff.additions;

            this.TestOutput.Add(String.Format("API Diff - Breaking changes: {0} Additions: {1} Missing Assemblies: {2}", 
                diff.breakingChanges,
                diff.additions,
                diff.removedAssemblyCount));

            var json = JsonUtility.ToJson(diff);
            Directory.CreateDirectory(ValidationSuiteReport.resultsPath);
            File.WriteAllText(Path.Combine(ValidationSuiteReport.resultsPath, "ApiValidationReport.json"), json);

            //Figure out type of version change (patch, minor, major)
            //Error if changes are not allowed
            var changeType = DetermineVersionChangeType();

            if (changeType == VersionChangeType.Invalid)
                return;
            if (diff.breakingChanges > 0 && changeType != VersionChangeType.Major)
                Error("Breaking changes require a new major version");
            if (diff.additions > 0 && changeType == VersionChangeType.Patch)
                Error("Additions require a new minor or major version");
            if (diff.removedAssemblyCount > 0 && changeType != VersionChangeType.Major)
            {
                foreach (var assembly in diff.missingAssemblies)
                {
                    Error(string.Format(
                        "Assembly \"{0}\" no longer exists or is no longer included in build. This requires a new major version.", assembly));
                }
            }
        }

        private IEnumerable<Assembly> NonTestAssembliesForPackage(Assembly[] assemblies, IEnumerable<string> filesInPackage)
        {
            return Utilities.AssembliesForPackage(assemblies, filesInPackage).Where(a => !ApiValidationAssemblyInformation.IsTestAssembly(a, false));
        }

        private VersionChangeType DetermineVersionChangeType()
        {
            var prevVersion = Semver.SemVersion.Parse(this.Context.PreviousPackageInfo.version);
            var curVersion = Semver.SemVersion.Parse(this.Context.ProjectPackageInfo.version);

            if (curVersion.Major > prevVersion.Major)
                return VersionChangeType.Major;
            if (curVersion.Major < prevVersion.Major)
                throw new ArgumentException("Previous version number comes after current version number");
            if (curVersion.Minor > prevVersion.Minor)
                return VersionChangeType.Minor;
            if (curVersion.Minor < prevVersion.Minor)
                throw new ArgumentException("Previous version number comes after current version number");
            if (curVersion.Patch > prevVersion.Patch)
                return VersionChangeType.Patch;
            if (curVersion.Patch < prevVersion.Patch)
                throw new ArgumentException("Previous version number comes after current version number");

            Error("Previous version number" + this.Context.PreviousPackageInfo.version + " is the same major/minor/patch version as the current package " + this.Context.ProjectPackageInfo.version);

            return VersionChangeType.Invalid;
        }
    }

    internal enum VersionChangeType
    {
        Patch,
        Minor,
        Major,
        Invalid
    }
}

#endif