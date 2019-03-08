using System;
using NUnit.Framework;
using UnityEditor;

namespace Unity.Tiny.Test
{
    [TestFixture]
    public class EditorApplicationAPITest
    {

        [Test]
        [TestCase("Assets/NewProject3/Nested")]
        public void CreateNewProject(string path)
        {
            new FlowTestHelper.ProjectContext(path).Dispose();
        }
        
        [Test]
        public void CreateProjectAtTheSamePath()
        {
            ValidateDuplication("Assets/NewProject1", TinyEditorApplication.NewProject);
        }
        
        [Test]
        [TestCase(" ")]
        [TestCase("Assets/")]
        [TestCase("D:/NewProject")]
        [TestCase("Assets/NewProject1.utproject")]
        [TestCase("NewProject")]
        [TestCase("/Assets/NewProject2")]
        [TestCase("Assets/NewProject2/")]
        [TestCase("Assets/NewProject3\\Nested")]
        [TestCase("Assets/New%")]
        public void CreateProjectInvalid(string path)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                TinyEditorApplication.NewProject(path);
            });
        }
        
        [Test]
        [TestCase("Assets/NewModule3/Nested")]
        public void CreateNewModule(string path)
        {
            new FlowTestHelper.ModuleContext(path).Dispose();
        }

        [Test]
        public void CreateModuleAtTheSamePath()
        {
            ValidateDuplication("Assets/NewModule1", TinyEditorApplication.NewModule);
        }
        
        [Test]
        [TestCase(" ")]
        [TestCase("Assets/")]
        [TestCase("D:/NewModule")]
        [TestCase("Assets/NewModule1.utproject")]
        [TestCase("NewModule")]
        [TestCase("/Assets/NewModule2")]
        [TestCase("Assets/NewModule2/")]
        [TestCase("Assets/NewModule3\\Nested")]
        [TestCase("Assets/New%")]
        public void CreateModuleInvalid(string path)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                TinyEditorApplication.NewModule(path);
            });
        }
        
        private void ValidateDuplication(string path, Func<string, TinyRegistryObjectBase> createCallback)
        {
            createCallback.Invoke(path);
            TinyEditorApplication.Close();
           
            Assert.Throws<ArgumentException>(() =>
            {
                createCallback.Invoke(path);
            });
            
            AssetDatabase.DeleteAsset(path);
        }
        
       
    }
}