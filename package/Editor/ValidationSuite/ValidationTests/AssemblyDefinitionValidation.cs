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
            var isEditor = assemblyDefinitionPath.IndexOf("Editor") >= 0;
            var isTest = assemblyDefinitionPath.IndexOf("Test") >= 0;

            try{
                var assemblyDefinitionData = Utilities.GetDataFromJson<AssemblyDefinition>(assemblyDefinitionPath);
                
                if(isEditor && assemblyDefinitionData.includePlatforms.Length != 1)
                {
                    TestState = TestState.Failed;
                    TestOutput.Add(string.Format("Only 'Editor' should be present in 'includePlatform' in: [{0}]", assemblyDefinitionPath));
                }
                
                if(FindValueInArray(assemblyDefinitionData.includePlatforms, "Editor") != isEditor)
                {
                    TestState = TestState.Failed;
                    TestOutput.Add(string.Format("'Editor' should{0} be present in includePlatform in: [{1}]", isEditor ? "":" not", assemblyDefinitionPath));
                }

                if(FindValueInArray(assemblyDefinitionData.optionalUnityReferences, "TestAssemblies") != isTest)
                {
                    TestState = TestState.Failed;
                    TestOutput.Add(string.Format("'TestAssemblies'{0} should be present in 'optionalUnityReferences' in: [{1}]", isTest? "" : " not", assemblyDefinitionPath));
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
            var manifestFilePath = Path.Combine(Context.PublishPackageInfo.path, Utilities.PackageJsonFilename);
            
            if (!File.Exists(manifestFilePath))
            {
                TestState = TestState.Failed;
                TestOutput.Add("Can't find manifest: " + manifestFilePath);
                return;
            }

            var packagePath = Context.PublishPackageInfo.path;

            foreach(var asmdef in Directory.GetFiles(packagePath, AssemblyFileDefinitionExtension, SearchOption.AllDirectories))
            {
                TestOutput.Add("Checking: " + asmdef);
                CheckAssemblyDefinitionContent(asmdef);
            }
        }
    }
}