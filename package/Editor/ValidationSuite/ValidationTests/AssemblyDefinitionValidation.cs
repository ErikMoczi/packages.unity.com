using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;
using UnityEditor.PackageManager.ValidationSuite;
using UnityEngine;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class AssemblyDefinitionValidation : BaseValidation
    {
        private const string AssemblyFileDefinitionExtension = "*.asmdef";
    
        public AssemblyDefinitionValidation()
        {
            TestName = "Assembly Definition Validation";
            TestDescription = "Validate Presence and Contents of Assembly Definition Files.";
            TestCategory = TestCategory.ContentScan;
            SupportedValidations = new[] { ValidationType.PackageManager };
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

            var isEditor = simplifiedPath.IndexOf("Editor") >= 0;
            var isTest = simplifiedPath.IndexOf("Test") >= 0;

            try{
                var assemblyDefinitionData = Utilities.GetDataFromJson<AssemblyDefinition>(assemblyDefinitionPath);
                
                if(isEditor && assemblyDefinitionData.includePlatforms.Length > 1)
                {
                    TestState = TestState.Failed;
                    TestOutput.Add(string.Format("For editor assemblies, only 'Editor' should be present in 'includePlatform' in: [{0}]", simplifiedPath));
                }
                
                if(isEditor && !FindValueInArray(assemblyDefinitionData.includePlatforms, "Editor"))
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
            
            var manifestFilePath = Path.Combine(packagePath, Utilities.PackageJsonFilename);
            
            if (!File.Exists(manifestFilePath))
            {
                TestState = TestState.Failed;
                TestOutput.Add("Can't find manifest: " + manifestFilePath);
                return;
            }

            var asmdefFiles = Directory.GetFiles(packagePath, AssemblyFileDefinitionExtension, SearchOption.AllDirectories);
            if (asmdefFiles.Length == 0)
            {
                TestState = TestState.NotRun;
                TestOutput.Add("No assembly definition found. Skipping assembly definition validation.");
                return;
            }

            TestOutput.Add("Checking " + packagePath + ": " + asmdefFiles.Length + " assembly definition(s) found");
            foreach(var asmdef in asmdefFiles)
                CheckAssemblyDefinitionContent(asmdef);
        }
    }
}