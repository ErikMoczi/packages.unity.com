using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;
using UnityEditor.PackageManager.ValidationSuite;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class AssemblyDefinitionValidation : BaseValidation
    {
        private const string AssemblyFileDefinitionExtension = "*.asmdef";
        
        internal class AssemblyDefinitionData {
            public string name = "";
            public string [] references = new string[0];
            public string [] optionalUnityReferences = new string[0];
            public string [] includePlatforms = new string[0];
            public string [] excludePlatforms = new string[0];
        }
    
        public AssemblyDefinitionValidation()
        {
            TestName = "Assembly Definition Validation";
            TestDescription = "Validate Presence and Contents of Assembly Definition Files.";
            TestCategory = TestCategory.ContentScan;
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

        void CheckAssemblyDefinitionContent(string assemblyDefinitionPath, bool isEditor, bool isTest)
        {
            try{
                var assemblyDefinitionData = Utilities.GetDataFromJson<AssemblyDefinitionData>(assemblyDefinitionPath);
                
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

        public override void Run()
        {
            // Start by declaring victory
            TestState = TestState.Succeeded;
            var manifestFilePath = Path.Combine(Context.PublishPackageInfo.Path, Utilities.PackageJsonFilename);
            
            if(!File.Exists(manifestFilePath))
            {
                TestState = TestState.Failed;
                TestOutput.Add("Can't find manifest: " + manifestFilePath);
                return;
            }

            var packagePath = Context.PublishPackageInfo.Path;
            var editorProjectPath = Path.Combine(packagePath, "Editor");
            var runtimeProjectPath =  Path.Combine(packagePath, "Runtime");
            var editorTestProjectPath = Path.Combine(packagePath, Path.Combine("Tests", "Editor"));
            var runtimeTestProjectPath = Path.Combine(packagePath, Path.Combine("Tests", "Runtime"));

            if (Directory.Exists(editorProjectPath))
            {
                var assemblyDefinitionFiles = Directory.GetFiles(editorProjectPath, AssemblyFileDefinitionExtension);
                if (assemblyDefinitionFiles.Length != 1)
                {
                    TestState = TestState.Failed;
                    TestOutput.Add("Editor assembly definition is missing.");
                }
                else
                {
                    var editorAssemblyDefinitionFilePath =
                        Path.Combine(editorTestProjectPath, assemblyDefinitionFiles[0]);
                    CheckAssemblyDefinitionContent(editorAssemblyDefinitionFilePath, true, false);
                }
                
                if (Directory.Exists(editorTestProjectPath))
                {
                    assemblyDefinitionFiles = Directory.GetFiles(editorTestProjectPath, AssemblyFileDefinitionExtension);
                    if (assemblyDefinitionFiles.Length != 1)
                    {
                        TestState = TestState.Failed;
                        TestOutput.Add("Editor assembly definition is missing.");
                    }
                    else
                    {
                        var editorTestAssemblyDefinitionFilePath =
                            Path.Combine(editorTestProjectPath, assemblyDefinitionFiles[0]);
                        CheckAssemblyDefinitionContent(editorTestAssemblyDefinitionFilePath, true, true);
                    }
                }
            }

            if (Directory.Exists(runtimeProjectPath))
            {
                var assemblyDefinitionFiles = Directory.GetFiles(runtimeProjectPath, AssemblyFileDefinitionExtension);
                if (assemblyDefinitionFiles.Length != 1)
                {
                    TestState = TestState.Failed;
                    TestOutput.Add("Editor assembly definition is missing.");
                }
                else
                {
                    var runtimeAssemblyDefinitionFilePath =
                        Path.Combine(editorTestProjectPath, assemblyDefinitionFiles[0]);
                    CheckAssemblyDefinitionContent(runtimeAssemblyDefinitionFilePath, false, false);
                }
                
                if (Directory.Exists(runtimeTestProjectPath))
                {
                    assemblyDefinitionFiles = Directory.GetFiles(runtimeTestProjectPath, AssemblyFileDefinitionExtension);
                    if (assemblyDefinitionFiles.Length != 1)
                    {
                        TestState = TestState.Failed;
                        TestOutput.Add("Editor assembly definition is missing.");
                    }
                    else
                    {
                        var runtimeTestAssemblyDefinitionFilePath =
                            Path.Combine(runtimeTestProjectPath, assemblyDefinitionFiles[0]);
                        CheckAssemblyDefinitionContent(runtimeTestAssemblyDefinitionFilePath, false, true);
                    }
                }
            }
        }
    }
}