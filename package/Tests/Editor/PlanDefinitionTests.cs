using System;
using System.IO;
using NUnit.Framework;
using UnityEditor.AI.Planner.DomainLanguage.TraitBased;
using UnityEngine;
using UnityEngine.AI.Planner.DomainLanguage.TraitBased;

namespace UnityEditor.AI.Planner.Tests
{
    class PlanDefinitionTestFixture : DomainDefinitionTestFixture
    {
        protected PlanDefinition m_PlanDefinition;

        [OneTimeSetUp]
        public override void Setup()
        {
            base.Setup();

            m_PlanDefinition = ScriptableObject.CreateInstance<PlanDefinition>();

            m_PlanDefinition.DomainDefinition = m_DomainDefinition;
            m_PlanDefinition.ActionDefinitions = new[]
            {
                new ActionDefinition()
                {
                    Name = "Purchase",
                    Parameters = new []
                    {
                        new ParameterDefinition()
                        {
                            Name = "merchant",
                            IncludeTraitTypes = new [] { "Merchant" }
                        },
                        new ParameterDefinition()
                        {
                            Name = "item",
                            IncludeTraitTypes = new [] { "Item" }
                        },
                        new ParameterDefinition()
                        {
                            Name = "consumer",
                            IncludeTraitTypes = new [] { "Consumer" }
                        }
                    },
                    Preconditions = new []
                    {
                        new Operation()
                        {
                            OperandA = new [] { "consumer", "Consumer.Gold" },
                            Operator = ">=",
                            OperandB = new [] { "item", "Item.Price" }
                        }
                    },
                    CreatedObjects = new []
                    {
                        new ParameterDefinition()
                        {
                            Name = "itemPurchased",
                            IncludeTraitTypes = new [] { "Item"}
                        }
                    },
                    Effects = new []
                    {
                        new Operation()
                        {
                            OperandA = new [] { "itemPurchased", "Item.Price"},
                            Operator = "=",
                            OperandB = new [] { "item", "Item.Price" }
                        },
                        new Operation()
                        {
                            OperandA = new [] { "itemPurchased", "Item.ItemType"},
                            Operator = "=",
                            OperandB = new [] { "item", "Item.ItemType" }
                        },
                        new Operation()
                        {
                            OperandA = new [] { "merchant", "Merchant.Gold"},
                            Operator = "+=",
                            OperandB = new [] { "item", "Item.Price" }
                        },
                        new Operation()
                        {
                            OperandA = new [] { "consumer", "Consumer.Gold"},
                            Operator = "-=",
                            OperandB = new [] { "item", "Item.Price" }
                        },
                    }
                },
                new ActionDefinition()
                {
                    Name = "Sell",
                    Parameters = new []
                    {
                        new ParameterDefinition()
                        {
                            Name = "merchant",
                            IncludeTraitTypes = new [] { "Merchant" }
                        },
                        new ParameterDefinition()
                        {
                            Name = "itemSold",
                            IncludeTraitTypes = new [] { "Item" }
                        },
                        new ParameterDefinition()
                        {
                            Name = "seller",
                            IncludeTraitTypes = new [] { "Consumer" }
                        }
                    },
                    Preconditions = new []
                    {
                        new Operation()
                        {
                            OperandA = new [] { "merchant", "Merchant.Gold" },
                            Operator = ">=",
                            OperandB = new [] { "itemSold", "Item.Price" }
                        }
                    },
                    RemovedObjects = new [] { "itemSold" },
                    Effects = new []
                    {
                        new Operation()
                        {
                            OperandA = new [] { "merchant", "Merchant.Gold"},
                            Operator = "-=",
                            OperandB = new [] { "itemSold", "Item.Price" }
                        },
                        new Operation()
                        {
                            OperandA = new [] { "consumer", "Consumer.Gold"},
                            Operator = "+=",
                            OperandB = new [] { "itemSold", "Item.Price" }
                        },
                    }
                }
            };
            m_PlanDefinition.StateTerminationDefinitions = new[]
            {
                new StateTerminationDefinition()
                {
                    Name = "Broke",
                    ObjectParameters = new ParameterDefinition()
                    {
                        Name = "player",
                        IncludeTraitTypes = new [] { "Consumer", "Agent" }
                    },
                    Criteria = new []
                    {
                        new Operation()
                        {
                            OperandA = new [] { "player", "Consumer.Gold" },
                            Operator = "<=",
                            OperandB = new [] { "0" }
                        }
                    }
                }
            };

            var path = $"{k_AssetsPath}MerchantActions.asset";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(m_PlanDefinition, path);
            m_PlanDefinition.GenerateClasses();
        }

        [OneTimeTearDown]
        public override void TearDown()
        {
            base.TearDown();
        }
    }

    [TestFixture]
    class PlanDefinitionTests : PlanDefinitionTestFixture
    {
        [Test]
        public void ActionsAreGenerated()
        {
            foreach (var actions in m_PlanDefinition.ActionDefinitions)
            {
                var path = $"{m_PlanDefinition.GeneratedClassDirectory}{actions.Name}.cs";
                Assert.IsTrue(File.Exists(path));
            }
        }

        [Test]
        public void StateTerminationsAreGenerated()
        {
            foreach (var stateTermination in m_PlanDefinition.StateTerminationDefinitions)
            {
                var path = $"{m_PlanDefinition.GeneratedClassDirectory}{stateTermination.Name}.cs";
                Assert.IsTrue(File.Exists(path));
            }
        }

        [Test]
        public void EditorWindowOpens()
        {
            var window = PlanEditorWindow.ShowWindow(m_PlanDefinition);
            Assert.IsNotNull(window);
            window.Close();
        }
    }
}
