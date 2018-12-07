using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

using UnityObject = UnityEngine.Object;

namespace Unity.InteractiveTutorials.Tests
{
    public class PropertyModificationCriterionTests : CriterionTestBase<PropertyModificationCriterion>
    {
        GameObject m_GameObject;

        [SetUp]
        public void Setup()
        {
            EditorSceneManager.OpenScene(GetTestAssetPath("EmptyTestScene.unity"));
            SaveActiveScene();

            m_GameObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        }

        [TearDown]
        public void Teardown2_TheTearDownening()
        {
            UnityObject.DestroyImmediate(m_GameObject);
        }

        [UnityTest]
        public IEnumerator WhenTargetIsEmpty_IsNotCompleted()
        {
            m_Criterion.target = null;
            yield return null;

            Assert.IsFalse(m_Criterion.completed);
        }

        [UnityTest]
        public IEnumerator WhenPropertyPathIsEmpty_IsNotCompleted()
        {
            m_Criterion.target.Update(Camera.main);
            yield return null;

            Assert.IsFalse(m_Criterion.completed);
        }

        [UnityTest]
        public IEnumerator WhenNoPropertyIsModified_IsNotCompleted()
        {
            m_Criterion.target.Update(Camera.main);
            m_Criterion.propertyPath = "m_LocalPosition.x";
            yield return null;

            Assert.IsFalse(m_Criterion.completed);
        }

        [UnityTest]
        public IEnumerator WhenDifferentPropertyIsModified_IsNotCompleted()
        {
            m_Criterion.target.Update(m_GameObject);
            m_Criterion.propertyPath = "m_LocalRotation.x";
            var so = new SerializedObject(m_GameObject.transform);
            SerializedProperty property = so.FindProperty("m_LocalPosition");
            property.vector3Value = new Vector3(1, 1, 1);
            so.ApplyModifiedProperties();
            yield return null;

            Assert.IsFalse(m_Criterion.completed);
        }

        [UnityTest]
        public IEnumerator WhenPropertyIsModifiedOnDifferentObject_IsNotCompleted()
        {
            m_GameObject.transform.position = Vector3.zero;
            m_Criterion.target.Update(Camera.main);
            m_Criterion.propertyPath = "m_LocalPosition.x";
            var so = new SerializedObject(m_GameObject.transform);
            SerializedProperty property = so.FindProperty("m_LocalPosition");
            property.vector3Value = new Vector3(1, 1, 1);
            so.ApplyModifiedProperties();
            yield return null;

            Assert.IsFalse(m_Criterion.completed);
        }

        [UnityTest]
        public IEnumerator WhenTargetPropertyIsModified_IsCompleted()
        {
            m_GameObject.transform.position = Vector3.zero;
            m_Criterion.target.Update(m_GameObject.transform);
            m_Criterion.propertyPath = "m_LocalPosition.x";
            var so = new SerializedObject(m_GameObject.transform);
            SerializedProperty property = so.FindProperty("m_LocalPosition");
            property.vector3Value = new Vector3(1, 1, 1);
            so.ApplyModifiedProperties();
            yield return null;

            Assert.IsTrue(m_Criterion.completed);
        }

        [UnityTest]
        public IEnumerator WhenTargetPropertyIsModifiedToIncorrectValue_IsNotCompleted()
        {
            m_GameObject.transform.position = Vector3.zero;
            m_Criterion.target.Update(m_GameObject.transform);
            m_Criterion.propertyPath = "m_LocalPosition.x";
            m_Criterion.targetValue = "1.1";
            m_Criterion.targetValueType = PropertyModificationCriterion.TargetValueType.Decimal;
            var so = new SerializedObject(m_GameObject.transform);
            SerializedProperty property = so.FindProperty("m_LocalPosition");
            property.vector3Value = new Vector3(1, 1, 1);
            so.ApplyModifiedProperties();
            yield return null;

            Assert.IsFalse(m_Criterion.completed);
        }

        [UnityTest]
        public IEnumerator WhenTargetPropertyIsModifiedToDecimalTarget_IsCompleted()
        {
            m_GameObject.transform.position = Vector3.zero;
            m_Criterion.target.Update(m_GameObject.transform);
            m_Criterion.propertyPath = "m_LocalPosition.x";
            m_Criterion.targetValue = "1";
            m_Criterion.targetValueType = PropertyModificationCriterion.TargetValueType.Decimal;
            var so = new SerializedObject(m_GameObject.transform);
            SerializedProperty property = so.FindProperty("m_LocalPosition");
            property.vector3Value = new Vector3(1, 1, 1);
            so.ApplyModifiedProperties();
            yield return null;

            Assert.IsTrue(m_Criterion.completed);
        }

        [UnityTest]
        public IEnumerator WhenTargetPropertyIsModifiedToTextTarget_IsCompleted()
        {
            m_Criterion.target.Update((UnityObject)m_GameObject);
            m_Criterion.propertyPath = "m_Name";
            m_Criterion.targetValue = "Cube";
            m_Criterion.targetValueType = PropertyModificationCriterion.TargetValueType.Text;
            var so = new SerializedObject(m_GameObject);
            SerializedProperty property = so.FindProperty("m_Name");
            property.stringValue = "Cube";
            so.ApplyModifiedProperties();
            yield return null;

            Assert.IsTrue(m_Criterion.completed);
        }

        [UnityTest]
        public IEnumerator WhenTargetPropertyIsModifiedToIntegerTarget_IsCompleted()
        {
            m_Criterion.target.Update((UnityObject)m_GameObject);
            m_Criterion.propertyPath = "m_Layer";
            m_Criterion.targetValue = "1";
            m_Criterion.targetValueType = PropertyModificationCriterion.TargetValueType.Integer;
            var so = new SerializedObject(m_GameObject);
            SerializedProperty property = so.FindProperty("m_Layer");
            property.intValue = 1;
            so.ApplyModifiedProperties();
            yield return null;

            Assert.IsTrue(m_Criterion.completed);
        }

        [UnityTest]
        public IEnumerator WhenTargetPropertyIsModifiedToBooleanTarget_IsCompleted()
        {
            m_Criterion.target.Update((UnityObject)m_GameObject);
            m_Criterion.propertyPath = "m_IsActive";
            m_Criterion.targetValue = "False";
            m_Criterion.targetValueType = PropertyModificationCriterion.TargetValueType.Boolean;
            var so = new SerializedObject(m_GameObject);
            SerializedProperty property = so.FindProperty("m_IsActive");
            property.boolValue = false;
            so.ApplyModifiedProperties();
            yield return null;

            Assert.IsTrue(m_Criterion.completed);
            m_GameObject.SetActive(true);
        }

        [UnityTest]
        public IEnumerator WhenTargetPropertyIsSatisfiedAndThenUndone_IsNotCompleted()
        {
            m_Criterion.target.Update((UnityObject)m_GameObject);
            m_Criterion.propertyPath = "m_Layer";
            m_Criterion.targetValue = "1";
            m_Criterion.targetValueType = PropertyModificationCriterion.TargetValueType.Integer;
            var so = new SerializedObject(m_GameObject);
            SerializedProperty property = so.FindProperty("m_Layer");
            property.intValue = 1;
            so.ApplyModifiedProperties();
            Undo.PerformUndo();
            yield return null;

            Assert.IsFalse(m_Criterion.completed);
        }

        [UnityTest]
        public IEnumerator WhenTargetPropertyIsSatisfiedAndThenAnotherPropertyIsModified_IsCompleted()
        {
            m_Criterion.target.Update((UnityObject)m_GameObject);
            m_Criterion.propertyPath = "m_Layer";
            m_Criterion.targetValue = "1";
            m_Criterion.targetValueType = PropertyModificationCriterion.TargetValueType.Integer;
            var so = new SerializedObject(m_GameObject);
            SerializedProperty property = so.FindProperty("m_Layer");
            property.intValue = 1;
            so.ApplyModifiedProperties();
            yield return null;

            property = so.FindProperty("m_Name");
            property.stringValue = "Cube";
            so.ApplyModifiedProperties();
            yield return null;

            Assert.IsTrue(m_Criterion.completed);
        }

        [UnityTest]
        public IEnumerator AutoComplete_WhenTargetIsEmpty_ReturnsFalseAndIsNotCompleted()
        {
            m_Criterion.target = null;

            Assert.IsFalse(m_Criterion.AutoComplete());
            yield return null;

            Assert.IsFalse(m_Criterion.completed);
        }

        [UnityTest]
        public IEnumerator AutoComplete_WhenPropertyPathIsEmpty_ReturnsFalseAndIsNotCompleted()
        {
            m_Criterion.target.Update(Camera.main);

            Assert.IsFalse(m_Criterion.AutoComplete());
            yield return null;

            Assert.IsFalse(m_Criterion.completed);
        }

        [UnityTest]
        public IEnumerator AutoComplete_WhenTargetValueTypeIsInteger_ReturnsTrueAndIsCompleted()
        {
            m_Criterion.target.Update((UnityObject)m_GameObject);
            m_Criterion.propertyPath = "m_Layer";
            m_Criterion.targetValue = "1";
            m_Criterion.targetValueType = PropertyModificationCriterion.TargetValueType.Integer;

            Assert.IsTrue(m_Criterion.AutoComplete());
            yield return null;

            Assert.IsTrue(m_Criterion.completed);
        }

        [UnityTest]
        public IEnumerator AutoComplete_WhenTargetValueTypeIsDecimal_ReturnsTrueAndIsCompleted()
        {
            m_GameObject.transform.position = Vector3.zero;
            m_Criterion.target.Update(m_GameObject.transform);
            m_Criterion.propertyPath = "m_LocalPosition.x";
            m_Criterion.targetValue = "1";
            m_Criterion.targetValueType = PropertyModificationCriterion.TargetValueType.Decimal;

            Assert.IsTrue(m_Criterion.AutoComplete());
            yield return null;

            Assert.IsTrue(m_Criterion.completed);
        }

        [UnityTest]
        public IEnumerator AutoComplete_WhenTargetValueTypeIsText_ReturnsTrueAndIsCompleted()
        {
            m_Criterion.target.Update((UnityObject)m_GameObject);
            m_Criterion.propertyPath = "m_Name";
            m_Criterion.targetValue = "Cube";
            m_Criterion.targetValueType = PropertyModificationCriterion.TargetValueType.Text;

            Assert.IsTrue(m_Criterion.AutoComplete());
            yield return null;

            Assert.IsTrue(m_Criterion.completed);
        }

        [UnityTest]
        public IEnumerator AutoComplete_WhenTargetValueTypeIsBool_ReturnsTrueAndIsCompleted()
        {
            m_Criterion.target.Update((UnityObject)m_GameObject);
            m_Criterion.propertyPath = "m_IsActive";
            m_Criterion.targetValue = "False";
            m_Criterion.targetValueType = PropertyModificationCriterion.TargetValueType.Boolean;

            Assert.IsTrue(m_Criterion.AutoComplete());
            yield return null;

            Assert.IsTrue(m_Criterion.completed);
        }

        [UnityTest]
        public IEnumerator WhenTargetPropertyAlreadyHasTargetValue_IsCompleted()
        {
            m_Criterion.StopTesting();

            m_GameObject.transform.position = Vector3.zero;
            m_Criterion.target.Update(m_GameObject.transform);
            m_Criterion.propertyPath = "m_LocalPosition.x";
            m_Criterion.targetValue = "1";
            m_Criterion.targetValueType = PropertyModificationCriterion.TargetValueType.Decimal;
            var so = new SerializedObject(m_GameObject.transform);
            SerializedProperty property = so.FindProperty("m_LocalPosition");
            property.vector3Value = new Vector3(1, 1, 1);
            so.ApplyModifiedProperties();
            yield return null;

            m_Criterion.StartTesting();

            Assert.IsTrue(m_Criterion.completed);
        }
    }
}
