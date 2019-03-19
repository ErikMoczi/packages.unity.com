using System;
using System.IO;
using NUnit.Framework;
using UnityEditor.AI.Planner.DomainLanguage.TraitBased;
using UnityEngine;
using UnityEngine.AI.Planner.DomainLanguage.TraitBased;

namespace UnityEditor.AI.Planner.Tests
{
    class DomainDefinitionTestFixture
    {
        protected DomainDefinition m_DomainDefinition;

        protected const string k_AssetsPath = "Assets/Temp/";
        const string k_OutputPath = "Temp/Tests/";

        [OneTimeSetUp]
        public virtual void Setup()
        {
            m_DomainDefinition = ScriptableObject.CreateInstance<DomainDefinition>();
            m_DomainDefinition.EnumDefinitions = new[]
            {
                new EnumDefinition()
                {
                    Name = "ItemType",
                    Values = new[]
                    {
                        "Apple",
                        "Orange",
                        "Banana"
                    }
                }
            };

            m_DomainDefinition.TraitDefinitions = new[]
            {
                new TraitDefinition()
                {
                    Name = "Merchant",
                    Dynamic = true,
                    Fields = new []
                    {
                        new TraitDefinitionField()
                        {
                            Name = "Gold",
                            FieldType = typeof(int)
                        }
                    }
                },
                new TraitDefinition()
                {
                    Name = "Item",
                    Dynamic = true,
                    Fields = new []
                    {
                        new TraitDefinitionField()
                        {
                            Name = "Price",
                            FieldType = typeof(int)
                        },
                        new TraitDefinitionField()
                        {
                            Name = "ItemType",
                            Type = "ItemType"
                        }
                    }
                },
                new TraitDefinition()
                {
                    Name = "Consumer",
                    Dynamic = true,
                    Fields = new []
                    {
                        new TraitDefinitionField()
                        {
                            Name = "Gold",
                            FieldType = typeof(int)
                        }
                    }
                },
                new TraitDefinition()
                {
                    Name = "Agent",
                    Dynamic = true,
                }
            };

            m_DomainDefinition.AliasDefinitions = new[]
            {
                new AliasDefinition()
                {
                    Name = "Player",
                    TraitTypes = new [] { "Consumer", "Agent" }
                }
            };

            var path = $"{k_AssetsPath}MerchantDomain.asset";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(m_DomainDefinition, path);
            m_DomainDefinition.BaseDirectory = k_OutputPath;
            m_DomainDefinition.GenerateClasses();
        }

        [OneTimeTearDown]
        public virtual void TearDown()
        {
            CleanupFiles();
            AssetDatabase.Refresh();
        }

        static void CleanupFiles()
        {
            if (Directory.Exists(k_AssetsPath))
                Directory.Delete(k_AssetsPath, true);

            if (Directory.Exists(k_OutputPath))
                Directory.Delete(k_OutputPath, true);
        }
    }

    [TestFixture]
    class DomainDefinitionTests : DomainDefinitionTestFixture
    {
        [Test]
        public void EnumerationsAreGenerated()
        {
            foreach (var enumeration in m_DomainDefinition.EnumDefinitions)
            {
                var path = $"{m_DomainDefinition.GeneratedClassDirectory}{enumeration.Name}.cs";
                Assert.IsTrue(File.Exists(path));
            }
        }

        [Test]
        public void TraitsAreGenerated()
        {
            foreach (var trait in m_DomainDefinition.TraitDefinitions)
            {
                var path = $"{m_DomainDefinition.GeneratedClassDirectory}{trait.Name}.cs";
                Assert.IsTrue(File.Exists(path));
                Assert.IsTrue(File.Exists(Path.ChangeExtension(path, ".Extra.cs")));
            }
        }

        [Test]
        public void DomainSystemIsGenerated()
        {
            Assert.IsTrue(File.Exists($"{m_DomainDefinition.GeneratedClassDirectory}{m_DomainDefinition.name}.cs"));
        }

        [Test]
        public void EditorWindowOpens()
        {
            var window = DomainEditorWindow.ShowWindow(m_DomainDefinition);
            Assert.IsNotNull(window);
            window.Close();
        }

    }
}
