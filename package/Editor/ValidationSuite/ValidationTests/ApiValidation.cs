#if UNITY_2018_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Semver;
using Unity.APIComparison.Framework.Changes;
using Unity.APIComparison.Framework.Collectors;
using UnityEngine;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class ApiValidation : BaseAssemblyValidation
    {
        private readonly ApiValidationAssemblyInformation assemblyInformation;

        public ApiValidation()
        {
            TestName = "API Validation";
            TestDescription = "Checks public API for style and changest that conflict with Semantic Versioning.";
            TestCategory = TestCategory.ApiValidation;
            assemblyInformation = new ApiValidationAssemblyInformation();
            SupportedValidations = new[] { ValidationType.CI, ValidationType.LocalDevelopment, ValidationType.Publishing };
        }

        public ApiValidation(ApiValidationAssemblyInformation apiValidationAssemblyInformation)
            : this()
        {
            assemblyInformation = apiValidationAssemblyInformation;
        }

        public ApiValidationAssemblyInformation ApiValidationAssemblyInformation
        {
            get { return assemblyInformation; }
        }

        private AssemblyInfo[] GetAndCheckAsmdefs()
        {
            var relevantAssemblyInfo = GetRelevantAssemblyInfo();

            var previousAssemblyDefinitions = GetAssemblyDefinitionDataInFolder(Context.PreviousPackageInfo.path);
            var versionChangeType = Context.VersionChangeType;

            foreach (var assemblyInfo in relevantAssemblyInfo)
            {
                //assembly is in the package
                var previousAssemblyDefinition = previousAssemblyDefinitions.FirstOrDefault(ad => DoAssembliesMatch(ad, assemblyInfo.assemblyDefinition));
                if (previousAssemblyDefinition == null)
                    continue; //new asmdefs are fine
                
                var excludePlatformsDiff = string.Format("Was:\"{0}\" Now:\"{1}\"",
                    string.Join(", ", previousAssemblyDefinition.excludePlatforms),
                    string.Join(", ", assemblyInfo.assemblyDefinition.excludePlatforms));
                if (previousAssemblyDefinition.excludePlatforms.Any(p => !assemblyInfo.assemblyDefinition.excludePlatforms.Contains(p)) &&
                    versionChangeType == VersionChangeType.Patch)
                    Error("Removing from excludePlatfoms requires a new minor or major version. " + excludePlatformsDiff);
                else if (assemblyInfo.assemblyDefinition.excludePlatforms.Any(p =>
                                !previousAssemblyDefinition.excludePlatforms.Contains(p)) &&
                            (versionChangeType == VersionChangeType.Patch || versionChangeType == VersionChangeType.Minor))
                    Error("Adding to excludePlatforms requires a new major version. " + excludePlatformsDiff);

                var includePlatformsDiff = string.Format("Was:\"{0}\" Now:\"{1}\"",
                    string.Join(", ", previousAssemblyDefinition.includePlatforms),
                    string.Join(", ", assemblyInfo.assemblyDefinition.includePlatforms));
                if (previousAssemblyDefinition.includePlatforms.Any(p => !assemblyInfo.assemblyDefinition.includePlatforms.Contains(p)) &&
                    (versionChangeType == VersionChangeType.Patch || versionChangeType == VersionChangeType.Minor))
                    Error("Removing from includePlatfoms requires a new major version. " + includePlatformsDiff);
                else if (assemblyInfo.assemblyDefinition.includePlatforms.Any(p => !previousAssemblyDefinition.includePlatforms.Contains(p)))
                {
                    if (previousAssemblyDefinition.includePlatforms.Length == 0 && 
                        (versionChangeType == VersionChangeType.Minor || versionChangeType == VersionChangeType.Patch))
                        Error("Adding the first entry in inlcudePlatforms requires a new major version. " + includePlatformsDiff);
                    else if (versionChangeType == VersionChangeType.Patch)
                        Error("Adding to includePlatforms requires a new minor or major version. " + includePlatformsDiff);
                }
            }

            CheckForAsmdefsNotIncludedInEditor();
            return relevantAssemblyInfo;
        }

        private bool DoAssembliesMatch(AssemblyDefinition assemblyDefinition1, AssemblyDefinition assemblyDefinition2)
        {
            return assemblyInformation.GetAssemblyName(assemblyDefinition1, true).Equals(assemblyInformation.GetAssemblyName(assemblyDefinition2, false));
        }

        private void CheckForAsmdefsNotIncludedInEditor()
        {
            var assemblyDefinitions = GetAssemblyDefinitionDataInFolder(Context.ProjectPackageInfo.path);
            foreach (var assemblyDefinition in assemblyDefinitions)
            {
                if (assemblyDefinition.includePlatforms.Any() &&
                    !assemblyDefinition.includePlatforms.Contains("Editor") ||
                    assemblyDefinition.excludePlatforms.Any() &&
                    assemblyDefinition.excludePlatforms.Contains("Editor"))
                {
                    Error("Package Validation Suite does not support .asmdefs that are not built on the \"Editor\" platform. See \"{0}\"", assemblyDefinition.name);
                }
            }
        }

        private AssemblyDefinition[] GetAssemblyDefinitionDataInFolder(string directory)
        {
            return Directory.GetFiles(directory, "*.asmdef", SearchOption.AllDirectories)
                .Select(Utilities.GetDataFromJson<AssemblyDefinition>).ToArray();
        }

        protected override void Run(AssemblyInfo[] info)
        {
            TestState = TestState.Succeeded;
            var packagePath = Context.ProjectPackageInfo.path;
            var files = new HashSet<string>(Directory.GetFiles(packagePath, "*", SearchOption.AllDirectories).Select(Path.GetFullPath));

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
                TestOutput.Add("No previous package version. Skipping Semantic Versioning checks.");
                TestState = TestState.NotRun;
                return;
            }

            //does it have asmdefs for all scripts?
            var assemblies = GetAndCheckAsmdefs();

            CheckApiDiff(assemblies);

            //Run analyzers
        }

        [Serializable]
        class AssemblyChange
        {
            public string assemblyName;
            public List<string> additions = new List<string>();
            public List<string> breakingChanges = new List<string>();

            public AssemblyChange(string assemblyName)
            {
                this.assemblyName = assemblyName;
            }
        }

        [Serializable]
        class ApiDiff
        {
            public List<string> missingAssemblies = new List<string>();
            public List<string> newAssemblies = new List<string>();
            public List<AssemblyChange> assemblyChanges = new List<AssemblyChange>();
            public int breakingChanges;
            public int additions;
            public int removedAssemblyCount;
        }

        private void CheckApiDiff(AssemblyInfo[] assemblyInfo)
        {
            var diff = new ApiDiff();
            var assembliesForPackage = assemblyInfo.Where(a => !assemblyInformation.IsTestAssembly(a)).ToArray();
            if (Context.PreviousPackageBinaryDirectory == null)
            {
                TestState = TestState.NotRun;
                TestOutput.Add("Previous package binaries must be present on artifactory to do API diff.");
                return;
            }

            var oldAssemblyPaths = Directory.GetFiles(Context.PreviousPackageBinaryDirectory, "*.dll");

            //Build diff
            foreach (var info in assembliesForPackage)
            {
                var assemblyDefinition = info.assemblyDefinition;
                var oldAssemblyPath = oldAssemblyPaths.FirstOrDefault(p =>
                    Path.GetFileNameWithoutExtension(p) == assemblyDefinition.name);

                if (info.assembly != null)
                {
                    var entityChanges = APIChangesCollector.Collect(oldAssemblyPath, info.assembly.outputPath).SelectMany(c => c.Changes).ToList();
                    var assemblyChange = new AssemblyChange(info.assembly.name)
                    {
                        additions = entityChanges.Where(c => c.IsAdd()).Select(c => c.ToString()).ToList(),
                        breakingChanges = entityChanges.Where(c => !c.IsAdd()).Select(c => c.ToString()).ToList()
                    };

                    if (entityChanges.Count > 0)
                        diff.assemblyChanges.Add(assemblyChange);
                }

                if (oldAssemblyPath == null)
                    diff.newAssemblies.Add(assemblyDefinition.name);
            }

            foreach (var oldAssemblyPath in oldAssemblyPaths)
            {
                var oldAssemblyName = Path.GetFileNameWithoutExtension(oldAssemblyPath);
                if (assembliesForPackage.All(a => a.assemblyDefinition.name != oldAssemblyName))
                    diff.missingAssemblies.Add(oldAssemblyName);
            }

            //separate changes
            diff.additions = diff.assemblyChanges.Sum(v => v.additions.Count);
            diff.removedAssemblyCount = diff.missingAssemblies.Count;
            diff.breakingChanges = diff.assemblyChanges.Sum(v => v.breakingChanges.Count);

            TestOutput.Add(String.Format("API Diff - Breaking changes: {0} Additions: {1} Missing Assemblies: {2}",
                diff.breakingChanges,
                diff.additions,
                diff.removedAssemblyCount));

            if (diff.breakingChanges > 0 || diff.additions > 0)
            {
                TestOutput.AddRange(diff.assemblyChanges.Select(c => JsonUtility.ToJson(c, true)));
            }


            string json = JsonUtility.ToJson(diff, true);
            Directory.CreateDirectory(ValidationSuiteReport.resultsPath);
            File.WriteAllText(Path.Combine(ValidationSuiteReport.resultsPath, "ApiValidationReport.json"), json);

            //Figure out type of version change (patch, minor, major)
            //Error if changes are not allowed
            var changeType = Context.VersionChangeType;

            if (changeType == VersionChangeType.Unknown)
                return;

            if (diff.breakingChanges > 0 && changeType != VersionChangeType.Major)
                Error("Breaking changes require a new major version.");
            if (diff.additions > 0 && changeType == VersionChangeType.Patch)
                Error("Additions require a new minor or major version.");
            if (changeType != VersionChangeType.Major)
            {
                foreach (var assembly in diff.missingAssemblies)
                {
                    Error(
                        "Assembly \"{0}\" no longer exists or is no longer included in build. This requires a new major version.", assembly);
                }
            }
            if (changeType == VersionChangeType.Patch)
            {
                foreach (var assembly in diff.newAssemblies)
                {
                    Error(
                        "New assembly \"{0}\" may only be added in a new minor or major version.", assembly);
                }
            }
        }
    }
}

#endif
