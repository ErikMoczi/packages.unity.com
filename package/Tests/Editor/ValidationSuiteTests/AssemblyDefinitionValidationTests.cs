using System.IO;
using NUnit.Framework;
using UnityEditor.PackageManager.ValidationSuite.ValidationTests;
using UnityEngine;
using UnityEditor.PackageManager.ValidationSuite;


namespace UnityEditor.PackageManager.ValidationSuite.Tests
{
    internal class AssemblyDefinitionValidationTests
    {
        private const string testDirectory = "tempAssemblyDefinitionValidationTest";
        private const string name = "com.unity.patate";

        [SetUp]
        public void Setup()
        {

            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
            
            Directory.CreateDirectory(testDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }

        private void CreatePackageJsonFile(string name)
        {
            var packageJsonPath = Path.Combine(testDirectory, "package.json");
            File.WriteAllText(packageJsonPath, "{\"name\":\"" + name + "\"}");
        }

        private void CreateFolderAndAssembly(string packageName, AssemblyDefinitionValidation.AssemblyDefinitionData content, bool isEditor, bool isTest)
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
        }

        [Test]
        public void When_FolderEditor_IsPresent_But_AssemblyDef_AreMissing_Validation_Fails()
        {
            CreatePackageJsonFile(name);
            CreateFolderAndAssembly(name, null, true, false);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.Run();

            Assert.AreEqual(TestState.Failed, assemblyDefinitionValidation.TestState);
            Assert.AreEqual(2, assemblyDefinitionValidation.TestOutput.Count);
        }

        [Test]
        public void When_FolderEditor_IsPresent_But_AssemblyDefTests_IsMissing_Validation_Fails()
        {
            CreatePackageJsonFile(name);
            
            AssemblyDefinitionValidation.AssemblyDefinitionData assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".Editor";
            assemblyDefinitionData.includePlatforms = new string [1] {"Editor"};

            CreateFolderAndAssembly(name, assemblyDefinitionData, true, false);

            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.Run();

            Assert.AreEqual(TestState.Failed, assemblyDefinitionValidation.TestState);
            Assert.AreEqual(1, assemblyDefinitionValidation.TestOutput.Count);
        }

        [Test]
        public void When_FolderTestsEditor_IsPresent_But_AssemblyDef_AreMissing_Validation_Fails()
        {
            CreatePackageJsonFile(name);
            
            AssemblyDefinitionValidation.AssemblyDefinitionData assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".EditorTests";
            assemblyDefinitionData.includePlatforms = new string [1] {"Editor"};
            assemblyDefinitionData.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinitionData, true, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.Run();

            Assert.AreEqual(TestState.Failed, assemblyDefinitionValidation.TestState);
            Assert.AreEqual(1, assemblyDefinitionValidation.TestOutput.Count);
        }

        [Test]
        public void When_FolderTestsEditor_IsPresent_But_AssemblyDefTests_IsMissing_Validation_Fails()
        {
            CreatePackageJsonFile(name);
            CreateFolderAndAssembly(name, null, true, true);

            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.Run();

            Assert.AreEqual(TestState.Failed, assemblyDefinitionValidation.TestState);
            Assert.AreEqual(2, assemblyDefinitionValidation.TestOutput.Count);
        }

        [Test]
        public void When_FolderEditorAndTests_And_AssemblyDefs_ArePresent_But_MissingTestAssemblies_Validation_Fails()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinitionValidation.AssemblyDefinitionData assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".Editor";
            assemblyDefinitionData.includePlatforms = new string [1] {"Editor"};

            CreateFolderAndAssembly(name, assemblyDefinitionData, true, false);

            assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".EditorTests";
            assemblyDefinitionData.includePlatforms = new string [1] {"Editor"};

            CreateFolderAndAssembly(name, assemblyDefinitionData, true, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.Run();

            Assert.AreEqual(TestState.Failed, assemblyDefinitionValidation.TestState);
            Assert.AreEqual(1, assemblyDefinitionValidation.TestOutput.Count);
        }

        [Test]
        public void When_FolderRuntimeAndTests_And_AssemblyDefs_ArePresent_But_MissingTestAssemblies_Validation_Fails()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinitionValidation.AssemblyDefinitionData assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".Runtime";

            CreateFolderAndAssembly(name, assemblyDefinitionData, false, false);

            assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".RuntimeTests";

            CreateFolderAndAssembly(name, assemblyDefinitionData, false, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.Run();

            Assert.AreEqual(TestState.Failed, assemblyDefinitionValidation.TestState);
            Assert.AreEqual(1, assemblyDefinitionValidation.TestOutput.Count);
        }

        [Test]
        public void When_FolderEditorAndTests_And_AssemblyDefs_ArePresent_But_TestAssembliesAddedToEditor_Validation_Fails()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinitionValidation.AssemblyDefinitionData assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".Editor";
            assemblyDefinitionData.includePlatforms = new string [1] {"Editor"};
            assemblyDefinitionData.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinitionData, true, false);

            assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".EditorTests";
            assemblyDefinitionData.includePlatforms = new string [1] {"Editor"};
            assemblyDefinitionData.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinitionData, true, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.Run();

            Assert.AreEqual(TestState.Failed, assemblyDefinitionValidation.TestState);
            Assert.AreEqual(1, assemblyDefinitionValidation.TestOutput.Count);
        }

        [Test]
        public void When_FolderRuntimeAndTests_And_AssemblyDefs_ArePresent_But_TestAssembliesAddedToRuntime_Validation_Fails()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinitionValidation.AssemblyDefinitionData assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".Runtime";
            assemblyDefinitionData.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinitionData, false, false);

            assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".RuntimeTests";
            assemblyDefinitionData.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinitionData, false, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.Run();

            Assert.AreEqual(TestState.Failed, assemblyDefinitionValidation.TestState);
            Assert.AreEqual(1, assemblyDefinitionValidation.TestOutput.Count);
        }

        [Test]
        public void When_FolderEditorAndTests_And_AssemblyDefs_ArePresent_But_MissingEditorInIncludePlatformInEditor_Validation_Fails()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinitionValidation.AssemblyDefinitionData assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".Editor";

            CreateFolderAndAssembly(name, assemblyDefinitionData, true, false);

            assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".EditorTests";
            assemblyDefinitionData.includePlatforms = new string [1] {"Editor"};
            assemblyDefinitionData.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinitionData, true, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.Run();

            Assert.AreEqual(TestState.Failed, assemblyDefinitionValidation.TestState);
            Assert.AreEqual(2, assemblyDefinitionValidation.TestOutput.Count);
        }

        [Test]
        public void When_FolderEditorAndTests_And_AssemblyDefs_ArePresent_But_MissingEditorInIncludePlatformInTests_Validation_Fails()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinitionValidation.AssemblyDefinitionData assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".Editor";
            assemblyDefinitionData.includePlatforms = new string [1] {"Editor"};
            CreateFolderAndAssembly(name, assemblyDefinitionData, true, false);

            assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".EditorTests";
            assemblyDefinitionData.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinitionData, true, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.Run();

            Assert.AreEqual(TestState.Failed, assemblyDefinitionValidation.TestState);
            Assert.AreEqual(2, assemblyDefinitionValidation.TestOutput.Count);
        }

        [Test]
        public void When_FolderRuntimeAndTests_And_AssemblyDefs_ArePresent_But_AddingEditorInIncludePlatform_Validation_Fails()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinitionValidation.AssemblyDefinitionData assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".Runtime";
            assemblyDefinitionData.includePlatforms = new string [1] {"Editor"};

            CreateFolderAndAssembly(name, assemblyDefinitionData, false, false);

            assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".RuntimeTests";
            assemblyDefinitionData.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinitionData, false, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.Run();

            Assert.AreEqual(TestState.Failed, assemblyDefinitionValidation.TestState);
            Assert.AreEqual(1, assemblyDefinitionValidation.TestOutput.Count);
        }

        [Test]
        public void When_FolderRuntimeAndTests_And_AssemblyDefs_ArePresent_But_AddingEditorInIncludePlatformTest_Validation_Fails()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinitionValidation.AssemblyDefinitionData assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".Runtime";

            CreateFolderAndAssembly(name, assemblyDefinitionData, false, false);

            assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".RuntimeTests";
            assemblyDefinitionData.optionalUnityReferences = new string [1] {"TestAssemblies"};
            assemblyDefinitionData.includePlatforms = new string [1] {"Editor"};

            CreateFolderAndAssembly(name, assemblyDefinitionData, false, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.Run();

            Assert.AreEqual(TestState.Failed, assemblyDefinitionValidation.TestState);
            Assert.AreEqual(1, assemblyDefinitionValidation.TestOutput.Count);
        }

        [Test]
        public void When_FolderEditorAndTests_And_AssemblyDefs_ArePresent_But_WrongName_Validation_Succeeds()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinitionValidation.AssemblyDefinitionData assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + "WRONG.Editor";
            assemblyDefinitionData.includePlatforms = new string [1] {"Editor"};

            CreateFolderAndAssembly(name, assemblyDefinitionData, true, false);

            assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".EditorTests";
            assemblyDefinitionData.includePlatforms = new string [1] {"Editor"};
            assemblyDefinitionData.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinitionData, true, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.Run();

            Assert.AreEqual(TestState.Failed, assemblyDefinitionValidation.TestState);
            Assert.AreEqual(1, assemblyDefinitionValidation.TestOutput.Count);
        }

        [Test]
        public void When_FolderEditorAndTests_And_AssemblyDefs_ArePresent_But_WrongNameInTests_Validation_Succeeds()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinitionValidation.AssemblyDefinitionData assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".Editor";
            assemblyDefinitionData.includePlatforms = new string [1] {"Editor"};

            CreateFolderAndAssembly(name, assemblyDefinitionData, true, false);

            assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + "Wrong.EditorTests";
            assemblyDefinitionData.includePlatforms = new string [1] {"Editor"};
            assemblyDefinitionData.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinitionData, true, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.Run();

            Assert.AreEqual(TestState.Failed, assemblyDefinitionValidation.TestState);
            Assert.AreEqual(1, assemblyDefinitionValidation.TestOutput.Count);
        }

        [Test]
        public void When_FolderRuntimeAndTests_And_AssemblyDefs_ArePresent_But_WrongName_Validation_Succeeds()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinitionValidation.AssemblyDefinitionData assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + "WRONG.Runtime";

            CreateFolderAndAssembly(name, assemblyDefinitionData, false, false);

            assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".RuntimeTests";
            assemblyDefinitionData.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinitionData, false, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.Run();

            Assert.AreEqual(TestState.Failed, assemblyDefinitionValidation.TestState);
            Assert.AreEqual(1, assemblyDefinitionValidation.TestOutput.Count);
        }

        [Test]
        public void When_FolderRuntimeAndTests_And_AssemblyDefs_ArePresent_But_WrongNameInTests_Validation_Succeeds()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinitionValidation.AssemblyDefinitionData assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".Runtime";

            CreateFolderAndAssembly(name, assemblyDefinitionData, false, false);

            assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + "WRONG.RuntimeTests";
            assemblyDefinitionData.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinitionData, false, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.Run();

            Assert.AreEqual(TestState.Failed, assemblyDefinitionValidation.TestState);
            Assert.AreEqual(1, assemblyDefinitionValidation.TestOutput.Count);
        }

        [Test]
        public void When_FolderEditorAndTests_And_AssemblyDefs_ArePresent_And_FilledProperly_Validation_Succeeds()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinitionValidation.AssemblyDefinitionData assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".Editor";
            assemblyDefinitionData.includePlatforms = new string [1] {"Editor"};

            CreateFolderAndAssembly(name, assemblyDefinitionData, true, false);

            assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".EditorTests";
            assemblyDefinitionData.includePlatforms = new string [1] {"Editor"};
            assemblyDefinitionData.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinitionData, true, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.Run();

            Assert.AreEqual(TestState.Succeeded, assemblyDefinitionValidation.TestState);
            Assert.AreEqual(0, assemblyDefinitionValidation.TestOutput.Count);
        }

        [Test]
        public void When_FolderRuntimeAndTests_And_AssemblyDefs_ArePresent_And_FilledProperly_Validation_Succeeds()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinitionValidation.AssemblyDefinitionData assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".Runtime";

            CreateFolderAndAssembly(name, assemblyDefinitionData, false, false);

            assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".RuntimeTests";
            assemblyDefinitionData.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinitionData, false, true);
            
            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.Run();

            Assert.AreEqual(TestState.Succeeded, assemblyDefinitionValidation.TestState);
            Assert.AreEqual(0, assemblyDefinitionValidation.TestOutput.Count);
        }

        [Test]
        public void When_FolderRuntimeAndEditorAndTests_And_AssemblyDefs_ArePresent_And_FilledProperly_Validation_Succeeds()
        {
            CreatePackageJsonFile(name);

            AssemblyDefinitionValidation.AssemblyDefinitionData assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".Runtime";

            CreateFolderAndAssembly(name, assemblyDefinitionData, false, false);

            assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".RuntimeTests";
            assemblyDefinitionData.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinitionData, false, true);
             
            assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".Editor";
            assemblyDefinitionData.includePlatforms = new string [1] {"Editor"};

            CreateFolderAndAssembly(name, assemblyDefinitionData, true, false);

            assemblyDefinitionData = new AssemblyDefinitionValidation.AssemblyDefinitionData();
            assemblyDefinitionData.name = name + ".EditorTests";
            assemblyDefinitionData.includePlatforms = new string [1] {"Editor"};
            assemblyDefinitionData.optionalUnityReferences = new string [1] {"TestAssemblies"};

            CreateFolderAndAssembly(name, assemblyDefinitionData, true, true);

            var assemblyDefinitionValidation = new AssemblyDefinitionValidation();
            assemblyDefinitionValidation.Context = PrepareVettingContext(testDirectory);
            assemblyDefinitionValidation.Run();

            Assert.AreEqual(TestState.Succeeded, assemblyDefinitionValidation.TestState);
            Assert.AreEqual(0, assemblyDefinitionValidation.TestOutput.Count);
        }

        private VettingContext PrepareVettingContext(string packagePath)
        {
            return new VettingContext()
            {
                ProjectPackageInfo = new VettingContext.ManifestData()
                {
                    Path = packagePath
                },
                PublishPackageInfo = new VettingContext.ManifestData()
                {
                    Path = packagePath
                },
                PreviousPackageInfo = new VettingContext.ManifestData()
                {
                    Path = packagePath
                }
            };
        }
    }
}