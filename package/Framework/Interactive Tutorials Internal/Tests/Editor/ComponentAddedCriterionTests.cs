using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

using UnityObject = UnityEngine.Object;

namespace Unity.InteractiveTutorials.Tests
{
    public class ComponentAddedCriterionTests : CriterionTestBase<ComponentAddedCriterion>
    {
        [SetUp]
        public void OpenSceneAndSetUpCriterion()
        {
            EditorSceneManager.OpenScene(GetTestAssetPath("EmptyTestScene.unity"));
            SaveActiveScene();
        }

        class ComponentA : MonoBehaviour
        {
        }

        class ComponentB : MonoBehaviour
        {
        }

        [UnityTest]
        public IEnumerator WhenTargetGameObjectIsNotSet_IsNotCompleted()
        {
            yield return null;

            Assert.IsFalse(m_Criterion.completed);
        }

        [UnityTest]
        public IEnumerator WhenTargetGameObjectIsSetAndRequiredComponentsIsEmpty_IsCompleted()
        {
            m_Criterion.targetGameObject = new GameObject();
            yield return null;

            Assert.IsTrue(m_Criterion.completed);
        }

        [UnityTest]
        public IEnumerator WhenTargetGameObjectHasNoneOfTheRequiredComponents_IsNotCompleted()
        {
            m_Criterion.requiredComponents = new[] { typeof(ComponentA), typeof(ComponentB) };
            m_Criterion.targetGameObject = new GameObject();
            yield return null;

            Assert.IsFalse(m_Criterion.completed);
        }

        [UnityTest]
        public IEnumerator WhenTargetGameObjectHasSomeButNotAllOfTheRequiredComponents_IsNotCompleted()
        {
            m_Criterion.requiredComponents = new[] { typeof(ComponentA), typeof(ComponentB) };
            m_Criterion.targetGameObject = new GameObject("GameObject", typeof(ComponentA));
            yield return null;

            Assert.IsFalse(m_Criterion.completed);
        }

        [UnityTest]
        public IEnumerator WhenTargetGameObjectHasAllOfTheRequiredComponents_IsCompleted()
        {
            m_Criterion.requiredComponents = new[] { typeof(ComponentA), typeof(ComponentB) };
            m_Criterion.targetGameObject = new GameObject("GameObject", typeof(ComponentA), typeof(ComponentB));
            yield return null;

            Assert.IsTrue(m_Criterion.completed);
        }

        [UnityTest]
        public IEnumerator WhenTargetGameObjectHasOneOfTwoIdenticalRequiredComponents_IsCompleted()
        {
            m_Criterion.requiredComponents = new[] { typeof(ComponentA), typeof(ComponentA) };
            m_Criterion.targetGameObject = new GameObject("GameObject", typeof(ComponentA));
            yield return null;

            Assert.IsTrue(m_Criterion.completed);
        }

        [Test]
        public void WhenRequiredComponentsAreAlreadySatisfied_IsCompleted()
        {
            m_Criterion.StopTesting();

            var gameObject = new GameObject("GameObject", typeof(ComponentA));
            m_Criterion.targetGameObject = gameObject;
            m_Criterion.requiredComponents =  new[] { typeof(ComponentA) };

            m_Criterion.StartTesting();

            Assert.IsTrue(m_Criterion.completed);
        }

        [Test]
        public void WhenRequiredComponentsIsSet_FutureReferencesAreCreated()
        {
            m_Criterion.requiredComponents = new[] { typeof(ComponentA), typeof(ComponentB), null, typeof(ComponentA) };

            CollectionAssert.AreEquivalent(new[] {
                "1: Unity.InteractiveTutorials.Tests.ComponentAddedCriterionTests+ComponentA",
                "2: Unity.InteractiveTutorials.Tests.ComponentAddedCriterionTests+ComponentB",
                "4: Unity.InteractiveTutorials.Tests.ComponentAddedCriterionTests+ComponentA"
            }, futureReferences.Select(f => f.referenceName));
        }

        [UnityTest]
        public IEnumerator OnCompletion_FutureReferencesAreUpdatedToAddedComponents()
        {
            var gameObject = new GameObject();

            m_Criterion.targetGameObject = gameObject;
            m_Criterion.requiredComponents = new[] { typeof(ComponentA), typeof(ComponentB) };

            var componentA = gameObject.AddComponent<ComponentA>();
            var componentB = gameObject.AddComponent<ComponentB>();

            yield return null;

            CollectionAssert.AreEquivalent(new Component[] { componentA, componentB },
                futureReferences.Select(f => f.sceneObjectReference.ReferencedObjectAsComponent));
        }

        [UnityTest]
        public IEnumerator AutoComplete_IsCompleted()
        {
            var gameObject = new GameObject();
            m_Criterion.targetGameObject = gameObject;
            m_Criterion.requiredComponents = new[] { typeof(ComponentA), typeof(ComponentB) };

            m_Criterion.AutoComplete();
            yield return null;

            Assert.IsTrue(m_Criterion.completed);
        }
    }
}
