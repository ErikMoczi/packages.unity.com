using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Linq;
using UnityEditor.PackageManager.ValidationSuite;
using UnityEngine;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class AssemblyDefinitionValidation : BaseValidation
    {
        private const string AssemblyFileDefinitionExtension = "*.asmdef";
        private const string CSharpScriptExtension = "*.cs";
    
        public AssemblyDefinitionValidation()
        {
            TestName = "Assembly Definition Validation";
            TestDescription = "Validate Presence and Contents of Assembly Definition Files.";
            TestCategory = TestCategory.ContentScan;
            SupportedValidations = new[] { ValidationType.CI, ValidationType.LocalDevelopment, ValidationType.Publishing, ValidationType.VerifiedSet };
        }

        bool FindValueInArray(string[] array, string value)
        {
            var foundValue = false;
            for(int i = 0; i < array.Length && !foundValue; ++i)
            {
                foundValue = array[i] == value;
            }

            return foundValue;
        }

        void CheckAssemblyDefinitionContent(string assemblyDefinitionPath)
        {
            var simplifiedPath = assemblyDefinitionPath.Replace(Context.PublishPackageInfo.path, "{Package-Root}");
            TestOutput.Add("Checking: " + simplifiedPath);

            var isRuntime = simplifiedPath.IndexOf("Runtime") >= 0;
            var isEditor = simplifiedPath.IndexOf("Editor") >= 0;
            var isTest = simplifiedPath.IndexOf("Test") >= 0;

            try{
                var assemblyDefinitionData = Utilities.GetDataFromJson<AssemblyDefinition>(assemblyDefinitionPath);
                var editorInIncludePlatforms = FindValueInArray(assemblyDefinitionData.includePlatforms, "Editor");

                if(isEditor && assemblyDefinitionData.includePlatforms.Length > 1)
                {
                    TestState = TestState.Failed;
                    TestOutput.Add(string.Format("For editor assemblies, only 'Editor' should be present in 'includePlatform' in: [{0}]", simplifiedPath));
                }
                
                if(isEditor && !editorInIncludePlatforms)
                {
                    TestState = TestState.Failed;
                    TestOutput.Add(string.Format("For editor assemblies, 'Editor' should be present in the includePlatform section in: [{0}]", simplifiedPath));
                }

                if(FindValueInArray(assemblyDefinitionData.optionalUnityReferences, "TestAssemblies") != isTest)
                {
                    TestState = TestState.Failed;
                    TestOutput.Add(string.Format("'TestAssemblies'{0} should be present in 'optionalUnityReferences' in: [{1}]", isTest? "" : " not", simplifiedPath));
                }
            }
            catch(Exception e)
            {
                TestState = TestState.Failed;
                TestOutput.Add("Can't read assembly definition: " + e.Message);
            }
        }

        protected override void Run()
        {
            // Start by declaring victory
            TestState = TestState.Succeeded;
            var packagePath = Context.PublishPackageInfo.path;
            var isValidationSuite = Context.PublishPackageInfo.name == "com.unity.package-validation-suite";
            var manifestFilePath = Path.Combine(packagePath, Utilities.PackageJsonFilename);
            
            if (!File.Exists(manifestFilePath))
            {
                TestState = TestState.Failed;
                TestOutput.Add("Can't find manifest: " + manifestFilePath);
                return;
            }

            // filter out `ApiValidationTestAssemblies` folder as the content of the folder is for testing only.
            Func<string, bool> filterTestAssemblies = f => !(isValidationSuite && f.IndexOf("ApiValidationTestAssemblies") >= 0);

            var asmdefFiles = Directory.GetFiles(packagePath, AssemblyFileDefinitionExtension, SearchOption.AllDirectories).Where(filterTestAssemblies);

            // check the existence of valid asmdef file if there are c# scripts in the Editor or Tests folder
            var foldersToCheck = new string[] {"Editor", "Tests"};
            foreach(var folder in foldersToCheck)
            {
                var folderPath = Path.Combine(packagePath, folder);
                if (!Directory.Exists(folderPath))
                    continue;
                
                var foldersWithAsmdefFile = asmdefFiles.Where(f => f.IndexOf(folderPath) >= 0).Select(f => Path.GetDirectoryName(f));
                var csFiles = Directory.GetFiles(folderPath, CSharpScriptExtension, SearchOption.AllDirectories).Where(filterTestAssemblies);
                foreach(var csFile in csFiles)
                {
                    // check if the cs file is not in any folder that has asmdef file
                    if(foldersWithAsmdefFile.All(f => csFile.IndexOf(f) < 0))
                    {
                        TestOutput.Add("C# script found in \"" + folder + "\" folder, but no corresponding asmdef file: " + csFile);
                        TestState = TestState.Failed;
                    }
                }
            }

            foreach(var asmdef in asmdefFiles)
                CheckAssemblyDefinitionContent(asmdef);
        }
    }
}