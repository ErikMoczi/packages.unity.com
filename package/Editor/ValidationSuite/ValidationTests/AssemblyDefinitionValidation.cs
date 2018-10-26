using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;
using UnityEditor.PackageManager.ValidationSuite;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    /** Skip it for now
    internal class AssemblyDefinitionValidation : BaseValidation
    {
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

        void CheckAssemblyDefinitionContent(string assemblyDefinitionPath, string packageName, bool isEditor, bool isTest)
        {
            try{
                var assemblyDefinitionData = Utilities.GetDataFromJson<AssemblyDefinitionData>(assemblyDefinitionPath);
                
                var expectedName = packageName + (isEditor ? ".Editor" : ".Runtime") + (isTest ? "Tests": "");
                if(assemblyDefinitionData.name != expectedName)
                {
                    TestState = TestState.Failed;
                    TestOutput.Add(string.Format("Wrong Name: {0}, expected: {1} in: [{2}]", assemblyDefinitionData.name, expectedName, assemblyDefinitionPath));
                }

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
            
            if(!System.IO.File.Exists(manifestFilePath))
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

            if (System.IO.Directory.Exists(editorProjectPath) || System.IO.Directory.Exists(editorTestProjectPath))
            {
                var editorAssemblyDefinitionFilePath = Path.Combine(editorProjectPath, Context.PublishPackageInfo.name + Utilities.EditorAssemblyDefintionSuffix);
                var editorTestsAssemblyDefinitionFilePath = Path.Combine(editorTestProjectPath, Context.PublishPackageInfo.name + Utilities.EditorTestsAssemblyDefintionSuffix);

                if(!System.IO.File.Exists(editorAssemblyDefinitionFilePath))
                {
                    TestState = TestState.Failed;
                    TestOutput.Add(string.Format("Editor assembly definition is missing.  Expecting {0} to exist.", editorAssemblyDefinitionFilePath));
                }
                else
                {
                    CheckAssemblyDefinitionContent(editorAssemblyDefinitionFilePath, Context.PublishPackageInfo.name, true, false);
                }

                if(!System.IO.File.Exists(editorTestsAssemblyDefinitionFilePath))
                {
                    TestState = TestState.Failed;
                    TestOutput.Add(string.Format("Editor Tests assembly definition is missing.  Expecting {0} to exist.", editorTestsAssemblyDefinitionFilePath));
                }
                else
                {
                    CheckAssemblyDefinitionContent(editorTestsAssemblyDefinitionFilePath, Context.PublishPackageInfo.name, true, true);
                }    
            }
           
            if (System.IO.Directory.Exists(runtimeProjectPath) || System.IO.Directory.Exists(runtimeTestProjectPath))
            {
                var runtimeAssemblyDefinitionFilePath = Path.Combine(runtimeProjectPath, Context.PublishPackageInfo.name + Utilities.RuntimeAssemblyDefintionSuffix);
                var runtimeTestsAssemblyDefinitionFilePath = Path.Combine(runtimeTestProjectPath, Context.PublishPackageInfo.name + Utilities.RuntimeTestsAssemblyDefintionSuffix);

                if(!System.IO.File.Exists(runtimeAssemblyDefinitionFilePath))
                {
                    TestState = TestState.Failed;
                    TestOutput.Add(string.Format("Runtime assembly definition is missing.  Expecting {0} to exist.", runtimeAssemblyDefinitionFilePath));
                }
                else
                {
                    CheckAssemblyDefinitionContent(runtimeAssemblyDefinitionFilePath, Context.PublishPackageInfo.name, false, false);
                }

                if(!System.IO.File.Exists(runtimeTestsAssemblyDefinitionFilePath))
                {
                    TestState = TestState.Failed;
                    TestOutput.Add(string.Format("Runtime Tests assembly definition is missing.  Expecting {0} to exist.", runtimeTestsAssemblyDefinitionFilePath));
                }
                else
                {
                    CheckAssemblyDefinitionContent(runtimeTestsAssemblyDefinitionFilePath, Context.PublishPackageInfo.name, false, true);
                }
            }
        }
    }
    **/
}