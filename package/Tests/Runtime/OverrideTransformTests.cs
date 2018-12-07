using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Animations.Rigging;
using NUnit.Framework;
using System.Collections;
using System;

using RigTestData = RuntimeRiggingTestFixture.RigTestData;

class OverrideTransformTests
{
    const float k_Epsilon = 0.05f;

    struct ConstraintTestData
    {
        public RigTestData rigData;
        public OverrideTransform constraint;
        public OverrideTransformData constraintData;
    }

    private ConstraintTestData SetupConstraintRig()
    {
        var data = new ConstraintTestData();

        data.rigData = RuntimeRiggingTestFixture.SetupRigHierarchy();

        var overrideTransformGO = new GameObject("overrideTransform");
        var overrideTransform = overrideTransformGO.AddComponent<OverrideTransform>();
        var overrideTransformData = overrideTransform.data;

        overrideTransformGO.transform.parent = data.rigData.rigGO.transform;

        Assert.IsNotNull(overrideTransformData);

        overrideTransformData.constrainedObject = new JobTransform(data.rigData.hipsGO.transform.Find("Chest"), false);

        var overrideSourceGO = new GameObject ("source");
        overrideSourceGO.transform.parent = overrideTransformGO.transform;

        overrideTransformData.sourceObject = new JobTransform(overrideSourceGO.transform, true);

        data.rigData.rootGO.GetComponent<RigBuilder>().Build();

        data.constraint = overrideTransform;
        data.constraintData = overrideTransformData;

        return data;
    }

    [UnityTest]
    public IEnumerator OverrideTransform_FollowsSource_WorldSpace()
    {
        var data = SetupConstraintRig();

        data.constraintData.space = OverrideTransformData.Space.World;
        yield return null;

        var constrainedTransform = data.constraintData.constrainedObject.transform;
        var sourceTransform = data.constraintData.sourceObject.transform;

        for (int i = 0; i < 5; ++i)
        {
            sourceTransform.position += new Vector3(0f, 0.1f, 0.0f);
            yield return null;

            Vector3 sourcePosition = sourceTransform.position;
            Vector3 constrainedPosition = constrainedTransform.position;

            Assert.AreEqual(sourcePosition.x, constrainedPosition.x, k_Epsilon, String.Format("Expected constrainedPosition.x to be {0}, but was {1}", sourcePosition.x, constrainedPosition.x));
            Assert.AreEqual(sourcePosition.y, constrainedPosition.y, k_Epsilon, String.Format("Expected constrainedPosition.y to be {0}, but was {1}", sourcePosition.y, constrainedPosition.y));
            Assert.AreEqual(sourcePosition.z, constrainedPosition.z, k_Epsilon, String.Format("Expected constrainedPosition.z to be {0}, but was {1}", sourcePosition.z, constrainedPosition.z));
        }
    }

    [UnityTest]
    public IEnumerator OverrideTransform_FollowsSource_PivotSpace()
    {
        var data = SetupConstraintRig();

        var constrainedTransform = data.constraintData.constrainedObject.transform;
        var sourceTransform = data.constraintData.sourceObject.transform;

        Vector3 originalPosition = constrainedTransform.position;

        data.constraintData.space = OverrideTransformData.Space.Pivot;
        yield return null;

        for (int i = 0; i < 5; ++i)
        {
            sourceTransform.position += new Vector3(0f, 0.1f, 0.0f);
            yield return null;

            Vector3 sourcePosition = sourceTransform.position;
            Vector3 constrainedPosition = constrainedTransform.position;
            Vector3 expectedPosition = sourcePosition + originalPosition;

            Assert.AreEqual(expectedPosition.x, constrainedPosition.x, k_Epsilon, String.Format("Expected constrainedPosition.x to be {0}, but was {1}", expectedPosition.x, constrainedPosition.x));
            Assert.AreEqual(expectedPosition.y, constrainedPosition.y, k_Epsilon, String.Format("Expected constrainedPosition.y to be {0}, but was {1}", expectedPosition.y, constrainedPosition.y));
            Assert.AreEqual(expectedPosition.z, constrainedPosition.z, k_Epsilon, String.Format("Expected constrainedPosition.z to be {0}, but was {1}", expectedPosition.z, constrainedPosition.z));
        }
    }

    [UnityTest]
    public IEnumerator OverrideTransform_FollowsSource_LocalSpace()
    {
        var data = SetupConstraintRig();

        var constrainedTransform = data.constraintData.constrainedObject.transform;
        var sourceTransform = data.constraintData.sourceObject.transform;

        Vector3 parentPosition = constrainedTransform.parent.position;

        data.constraintData.space = OverrideTransformData.Space.Local;
        yield return null;

        for (int i = 0; i < 5; ++i)
        {
            sourceTransform.position += new Vector3(0f, 0.1f, 0.0f);
            yield return null;

            Vector3 sourcePosition = sourceTransform.position;
            Vector3 constrainedPosition = constrainedTransform.position;
            Vector3 expectedPosition = sourcePosition + parentPosition;

            Assert.AreEqual(expectedPosition.x, constrainedPosition.x, k_Epsilon, String.Format("Expected constrainedPosition.x to be {0}, but was {1}", expectedPosition.x, constrainedPosition.x));
            Assert.AreEqual(expectedPosition.y, constrainedPosition.y, k_Epsilon, String.Format("Expected constrainedPosition.y to be {0}, but was {1}", expectedPosition.y, constrainedPosition.y));
            Assert.AreEqual(expectedPosition.z, constrainedPosition.z, k_Epsilon, String.Format("Expected constrainedPosition.z to be {0}, but was {1}", expectedPosition.z, constrainedPosition.z));
        }
    }

    [UnityTest]
    public IEnumerator OverrideTransform_ApplyWeight()
    {
        var data = SetupConstraintRig();

        var constrainedTransform = data.constraintData.constrainedObject.transform;
        var sourceTransform = data.constraintData.sourceObject.transform;

        Vector3 constrainedPos1 = constrainedTransform.position;

        data.constraintData.space = OverrideTransformData.Space.World;
        yield return null;

        sourceTransform.position = new Vector3(0f, 0.5f, 0f);
        yield return null;

        Vector3 constrainedPos2 = constrainedTransform.position;

        for (int i = 0; i <= 5; ++i)
        {
            float w = i / 5.0f;

            data.constraint.weight = w;
            yield return null;
            yield return null;

            Vector3 weightedConstrainedPos = Vector3.Lerp(constrainedPos1, constrainedPos2, w);
            Vector3 constrainedPos = constrainedTransform.position;

            Assert.AreEqual(weightedConstrainedPos.x, constrainedPos.x, k_Epsilon, String.Format("Expected constrainedPos.x to be {0} for a weight of {1}, but was {2}", weightedConstrainedPos.x, w, constrainedPos.x));
            Assert.AreEqual(weightedConstrainedPos.y, constrainedPos.y, k_Epsilon, String.Format("Expected constrainedPos.y to be {0} for a weight of {1}, but was {2}", weightedConstrainedPos.y, w, constrainedPos.y));
            Assert.AreEqual(weightedConstrainedPos.z, constrainedPos.z, k_Epsilon, String.Format("Expected constrainedPos.z to be {0} for a weight of {1}, but was {2}", weightedConstrainedPos.z, w, constrainedPos.z));
        }
    }

}
