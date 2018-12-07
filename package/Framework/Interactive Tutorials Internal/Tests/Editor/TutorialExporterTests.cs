using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.InteractiveTutorials.InternalToolsTests
{
    public class TutorialExporterTests
    {
        static string k_TempFolderGUID;

        [SetUp]
        public void SetUp()
        {
            k_TempFolderGUID = AssetDatabase.CreateFolder("Packages/com.unity.learn.iet-framework/Framework/Interactive Tutorials Internal/Tests/Editor", "Temp");
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(k_TempFolderGUID));
        }

        [Test]
        public void CompileAssemblyWithScriptsUsingInternals_ReturnsTrue()
        {
            var assemblyPath = Path.GetFullPath(Path.Combine(AssetDatabase.GUIDToAssetPath(k_TempFolderGUID), TutorialExporter.assemblyName));
            Assert.IsTrue(TutorialExporter.CompileAssemblyWithScriptsUsingInternals(assemblyPath));
        }
    }
}
