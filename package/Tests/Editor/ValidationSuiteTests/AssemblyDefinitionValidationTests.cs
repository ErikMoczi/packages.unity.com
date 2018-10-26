using System;
using System.IO;
using NUnit.Framework;
using UnityEditor.PackageManager.ValidationSuite.ValidationTests;
using UnityEngine;
using UnityEditor.PackageManager.ValidationSuite;


namespace UnityEditor.PackageManager.ValidationSuite.Tests
{
    internal class AssemblyDefinitionValidationTests
    {
        private string testDirectory;
        private const string name = "com.unity.mypackage";

        [SetUp]
        public void Setup()
        {
            testDirectory = Path.Combine(Path.GetTempPath(), "tempAssemblyDefinitionValidationTests");
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
            
            Directory.CreateDirectory(testDirectory);
        }

        [TearDown]
        public void TearDown()
        {
        }

        private void CreatePackageJsonFile(string name)
        {
            var packageJsonPath = Path.Combine(testDirectory, "package.json");
            File.WriteAllText(packageJsonPath, "{\"name\":\"" + name + "\"}");
        }

        private void CreateFolderAndAssembly(string packageName, AssemblyDefinition content, bool isEditor, bool isTest, string csharpScriptName = "script.cs")
        {
            var folderPath = Path.Combine(testDirectory, Path.Combine(isTest?"Tests":"", isEditor?"Editor":"Runtime"));
            Directory.CreateDirectory(folderPath);

            if(content != null)
            {
                var assemblyFileName = packageName;
                assemblyFileName += isEditor ? ".Editor" : ".Runtime";
                assemblyFileName += isTest ? "Tests.asmdef" : ".asmdef";

                File.WriteAllText(Path.Combine(folderPath, assemblyFileName), JsonUtility.ToJson(content));
            }

            if (csharpScriptName != null)
                File.WriteAllText(Path.Combine(folderPath, csharpScriptName), "");
        }

        [Test]
        public void When_FolderEditor_IsPresent_But_No_CSharp_Script_And_AssemblyDef_AreMissing_Validation_Succeeds()
        {
            CreatePackageJsonFile(name);
            CreateFolderAndAssembly(name, null, true, false, null);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.RunTest();

            Assert.AreEqual(TestState.Succeeded, assemblyDefinitionValidation.TestState);
        }

        [Test]
        public void When_FolderEditor_IsPresent_But_AssemblyDef_AreMissing_Validation_Fails()
        {
            CreatePackageJsonFile(name);
            CreateFolderAndAssembly(name, null, true, false);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.RunTest();

            Assert.AreEqual(TestState.Failed, assemblyDefinitionValidation.TestState);
            Assert.Greater(assemblyDefinitionValidation.TestOutput.Count, 0);
        }

        [Test]
        public void When_FolderEditor_IsPresent_But_AssemblyDefTests_IsMissing_Validation_Succeeds()
        {
            CreatePackageJsonFile(name);
            
            AssemblyDefinition assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".Editor";
            assemblyDefinition.includePlatforms = new string [1] {"Editor"};

            CreateFolderAndAssembly(name, assemblyDefinition, true, false);

            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.RunTest();

            Assert.AreEqual(TestState.Succeeded, assemblyDefinitionValidation.TestState);
        }

        [Test]
        public void When_FolderTestsEditor_IsPresent_AssemblyDefTests_IsPresent_But_AssemblyDef_IsMissing_Validation_Succeeds()
        {
            CreatePackageJsonFile(name);
            
            AssemblyDefinition assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".EditorTests";
            assemblyDefinition.includePlatforms = new string [1] {"Editor"};
            assemblyDefinition.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinition, true, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.RunTest();

            Assert.AreEqual(TestState.Succeeded, assemblyDefinitionValidation.TestState);
        }

        [Test]
        public void When_FolderTestsEditor_IsPresent_But_AssemblyDefTests_IsMissing_Validation_Fails()
        {
            CreatePackageJsonFile(name);
            CreateFolderAndAssembly(name, null, true, true);

            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.RunTest();

            Assert.AreEqual(TestState.Failed, assemblyDefinitionValidation.TestState);
            Assert.Greater(assemblyDefinitionValidation.TestOutput.Count, 0);
        }

        [Test]
        public void When_FolderEditorAndTests_And_AssemblyDefs_ArePresent_But_MissingTestAssemblies_Validation_Fails()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinition assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".Editor";
            assemblyDefinition.includePlatforms = new string [1] {"Editor"};

            CreateFolderAndAssembly(name, assemblyDefinition, true, false);

            assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".EditorTests";
            assemblyDefinition.includePlatforms = new string [1] {"Editor"};

            CreateFolderAndAssembly(name, assemblyDefinition, true, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.RunTest();

            Assert.AreEqual(TestState.Failed, assemblyDefinitionValidation.TestState);
            Assert.Greater(assemblyDefinitionValidation.TestOutput.Count, 0);
        }

        [Test]
        public void When_FolderRuntimeAndTests_And_AssemblyDefs_ArePresent_But_MissingTestAssemblies_Validation_Fails()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinition assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".Runtime";

            CreateFolderAndAssembly(name, assemblyDefinition, false, false);

            assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".RuntimeTests";

            CreateFolderAndAssembly(name, assemblyDefinition, false, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.RunTest();

            Assert.AreEqual(TestState.Failed, assemblyDefinitionValidation.TestState);
            Assert.Greater(assemblyDefinitionValidation.TestOutput.Count, 0);
        }

        [Test]
        public void When_FolderEditorAndTests_And_AssemblyDefs_ArePresent_But_TestAssembliesAddedToEditor_Validation_Fails()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinition assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".Editor";
            assemblyDefinition.includePlatforms = new string [1] {"Editor"};
            assemblyDefinition.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinition, true, false);

            assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".EditorTests";
            assemblyDefinition.includePlatforms = new string [1] {"Editor"};
            assemblyDefinition.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinition, true, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.RunTest();

            Assert.AreEqual(TestState.Failed, assemblyDefinitionValidation.TestState);
            Assert.Greater(assemblyDefinitionValidation.TestOutput.Count, 0);
        }

        [Test]
        public void When_FolderRuntimeAndTests_And_AssemblyDefs_ArePresent_But_TestAssembliesAddedToRuntime_Validation_Fails()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinition assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".Runtime";
            assemblyDefinition.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinition, false, false);

            assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".RuntimeTests";
            assemblyDefinition.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinition, false, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.RunTest();

            Assert.AreEqual(TestState.Failed, assemblyDefinitionValidation.TestState);
            Assert.Greater(assemblyDefinitionValidation.TestOutput.Count, 0);
        }

        [Test]
        public void When_FolderEditorAndTests_And_AssemblyDefs_ArePresent_But_MissingEditorInIncludePlatformInEditor_Validation_Fails()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinition assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".Editor";

            CreateFolderAndAssembly(name, assemblyDefinition, true, false);

            assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".EditorTests";
            assemblyDefinition.includePlatforms = new string [1] {"Editor"};
            assemblyDefinition.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinition, true, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.RunTest();

            Assert.AreEqual(TestState.Failed, assemblyDefinitionValidation.TestState);
            Assert.Greater(assemblyDefinitionValidation.TestOutput.Count, 0);
        }

        [Test]
        public void When_FolderEditorAndTests_And_AssemblyDefs_ArePresent_But_MissingEditorInIncludePlatformInTests_Validation_Fails()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinition assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".Editor";
            assemblyDefinition.includePlatforms = new string [1] {"Editor"};
            CreateFolderAndAssembly(name, assemblyDefinition, true, false);

            assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".EditorTests";
            assemblyDefinition.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinition, true, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.RunTest();

            Assert.AreEqual(TestState.Failed, assemblyDefinitionValidation.TestState);
            Assert.Greater(assemblyDefinitionValidation.TestOutput.Count, 0);
        }

        [Test]
        public void When_FolderRuntimeAndTests_And_AssemblyDefs_ArePresent_But_AddingEditorInIncludePlatform_Validation_Fails()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinition assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".Runtime";
            assemblyDefinition.includePlatforms = new string [1] {"Editor"};

            CreateFolderAndAssembly(name, assemblyDefinition, false, false);

            assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".RuntimeTests";
            assemblyDefinition.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinition, false, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.RunTest();

            Assert.AreEqual(TestState.Failed, assemblyDefinitionValidation.TestState);
            Assert.Greater(assemblyDefinitionValidation.TestOutput.Count, 0);
        }

        [Test]
        public void When_FolderRuntimeAndTests_And_AssemblyDefs_ArePresent_But_AddingEditorInIncludePlatformTest_Validation_Fails()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinition assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".Runtime";

            CreateFolderAndAssembly(name, assemblyDefinition, false, false);

            assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".RuntimeTests";
            assemblyDefinition.optionalUnityReferences = new string [1] {"TestAssemblies"};
            assemblyDefinition.includePlatforms = new string [1] {"Editor"};

            CreateFolderAndAssembly(name, assemblyDefinition, false, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.RunTest();

            Assert.AreEqual(TestState.Failed, assemblyDefinitionValidation.TestState);
            Assert.Greater(assemblyDefinitionValidation.TestOutput.Count, 0);
        }

        [Test]
        public void When_FolderEditorAndTests_And_AssemblyDefs_ArePresent_But_WrongName_Validation_Succeeds()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinition assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + "WRONG.Editor";
            assemblyDefinition.includePlatforms = new string [1] {"Editor"};

            CreateFolderAndAssembly(name, assemblyDefinition, true, false);

            assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".EditorTests";
            assemblyDefinition.includePlatforms = new string [1] {"Editor"};
            assemblyDefinition.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinition, true, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.RunTest();

            Assert.AreEqual(TestState.Succeeded, assemblyDefinitionValidation.TestState);
        }

        [Test]
        public void When_FolderEditorAndTests_And_AssemblyDefs_ArePresent_But_WrongNameInTests_Validation_Succeeds()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinition assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".Editor";
            assemblyDefinition.includePlatforms = new string [1] {"Editor"};

            CreateFolderAndAssembly(name, assemblyDefinition, true, false);

            assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + "Wrong.EditorTests";
            assemblyDefinition.includePlatforms = new string [1] {"Editor"};
            assemblyDefinition.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinition, true, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.RunTest();

            Assert.AreEqual(TestState.Succeeded, assemblyDefinitionValidation.TestState);
        }

        [Test]
        public void When_FolderRuntimeAndTests_And_AssemblyDefs_ArePresent_But_WrongName_Validation_Succeeds()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinition assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + "WRONG.Runtime";

            CreateFolderAndAssembly(name, assemblyDefinition, false, false);

            assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".RuntimeTests";
            assemblyDefinition.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinition, false, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.RunTest();

            Assert.AreEqual(TestState.Succeeded, assemblyDefinitionValidation.TestState);
        }

        [Test]
        public void When_FolderRuntimeAndTests_And_AssemblyDefs_ArePresent_But_WrongNameInTests_Validation_Succeeds()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinition assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".Runtime";

            CreateFolderAndAssembly(name, assemblyDefinition, false, false);

            assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + "WRONG.RuntimeTests";
            assemblyDefinition.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinition, false, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.RunTest();

            Assert.AreEqual(TestState.Succeeded, assemblyDefinitionValidation.TestState);
        }

        [Test]
        public void When_FolderEditorAndTests_And_AssemblyDefs_ArePresent_And_FilledProperly_Validation_Succeeds()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinition assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".Editor";
            assemblyDefinition.includePlatforms = new string [1] {"Editor"};

            CreateFolderAndAssembly(name, assemblyDefinition, true, false);

            assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".EditorTests";
            assemblyDefinition.includePlatforms = new string [1] {"Editor"};
            assemblyDefinition.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinition, true, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.RunTest();

            Assert.AreEqual(TestState.Succeeded, assemblyDefinitionValidation.TestState);
        }

        [Test]
        public void When_FolderRuntimeAndTests_And_AssemblyDefs_ArePresent_And_FilledProperly_Validation_Succeeds()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinition assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".Runtime";

            CreateFolderAndAssembly(name, assemblyDefinition, false, false);

            assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".RuntimeTests";
            assemblyDefinition.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinition, false, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.RunTest();

            Assert.AreEqual(TestState.Succeeded, assemblyDefinitionValidation.TestState);
        }

        [Test]
        public void When_FolderRuntimeAndEditorAndTests_And_AssemblyDefs_ArePresent_And_FilledProperly_Validation_Succeeds()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinition assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".Runtime";

            CreateFolderAndAssembly(name, assemblyDefinition, false, false);

            assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".RuntimeTests";
            assemblyDefinition.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinition, false, true);
             
            assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".Editor";
            assemblyDefinition.includePlatforms = new string [1] {"Editor"};

            CreateFolderAndAssembly(name, assemblyDefinition, true, false);

            assemblyDefinition = new AssemblyDefinition();
            assemblyDefinition.name = name + ".EditorTests";
            assemblyDefinition.includePlatforms = new string [1] {"Editor"};
            assemblyDefinition.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinition, true, true);

            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.RunTest();

            Assert.AreEqual(TestState.Succeeded, assemblyDefinitionValidation.TestState);
        }

        private VettingContext PrepareVettingContext(string packagePath)
        {
            var packageJson = File.ReadAllText(Path.Combine(packagePath, "package.json"));
            VettingContext.ManifestData manifestData = null;
            try
            {
                manifestData = JsonUtility.FromJson<VettingContext.ManifestData>(packageJson);
            }
            catch (Exception)
            {
            }
            
            var vettingContext = new VettingContext
            {
                ProjectPackageInfo = manifestData,
                PublishPackageInfo = manifestData,
                PreviousPackageInfo = manifestData
            };

            if (manifestData != null)
            {
                vettingContext.ProjectPackageInfo.path = packagePath;
                vettingContext.PublishPackageInfo.path = packagePath;
                vettingContext.PreviousPackageInfo.path = packagePath;
            }
            
            return vettingContext;
        }
    }
}