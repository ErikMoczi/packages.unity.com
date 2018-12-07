

using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny.Test
{
    /// <summary>
    /// These tests are to ensure that TinyModule value are preserved when passing through the serializaton pipeline
    /// </summary>
    [TestFixture]
    internal class ModuleSerializationTest
    {
        private IRegistry m_Registry;
        private TinyModule m_Module;
        private Texture2D m_Texture2D;
        
        [SetUp]
        public void SetUp()
        {
            m_Registry = new TinyRegistry();
            m_Module = m_Registry.CreateModule(TinyId.New(), "TestModule");
        }

        [Test]
        public void AssetSerializationTest()
        {
            // Create some asset on disc
            File.WriteAllBytes(Application.dataPath + "/TestTexture.png", new Texture2D(32, 32).EncodeToPNG());
            AssetDatabase.ImportAsset("Assets/TestTexture.png");
            m_Texture2D = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TestTexture.png");
            
            // Reference the asset in the module
            m_Module.AddAsset(m_Texture2D);
            
            Debug.Log(m_Module);

            var registry = new TinyRegistry();
            using (var json = new MemoryStream())
            using (var command = new MemoryStream())
            {
                // Write the module to the stream
                Serialization.Json.JsonBackEnd.Persist(json, m_Module);
                json.Position = 0;
                
                Serialization.Json.JsonFrontEnd.Accept(json, command);
                command.Position = 0;
                
                Serialization.CommandStream.CommandFrontEnd.Accept(command, registry);
            }

            var module = registry.FindById<TinyModule>(m_Module.Id);
            Debug.Log(module);
            
            var path = AssetDatabase.GetAssetPath(m_Texture2D);
            AssetDatabase.DeleteAsset(path);
        }
    }
}

