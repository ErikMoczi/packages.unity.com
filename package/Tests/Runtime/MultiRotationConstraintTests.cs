using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Animations.Rigging;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

using RigTestData = RuntimeRiggingTestFixture.RigTestData;

class MultiRotationConstraintTests
{
    const float k_Epsilon = 0.05f;

    struct ConstraintTestData
    {
        public RigTestData rigData;
        public MultiRotationConstraint constraint;
        public MultiRotationConstraintData constraintData;

        public Quaternion constrainedObjectRestRotation;
    }

    private ConstraintTestData SetupConstraintRig()
    {
        var data = new ConstraintTestData();

        data.rigData = RuntimeRiggingTestFixture.SetupRigHierarchy();

        var multiRotationGO = new GameObject("multiPosition");
        var multiRotation = multiRotationGO.AddComponent<MultiRotationConstraint>();
        var multiRotationData = multiRotation.data;
        multiRotationGO.transform.parent = data.rigData.rigGO.transform;

        Assert.IsNotNull(multiRotationData);
        multiRotationData.constrainedObject = new JobTransform(data.rigData.hipsGO.transform, false);
        data.constrainedObjectRestRotation = multiRotationData.constrainedObject.transform.rotation;

        List<WeightedJobTransform> sources = new List<WeightedJobTransform>(2);
        var src0GO = new GameObject("source0");
        var src1GO = new GameObject("source1");
        src0GO.transform.parent = multiRotationGO.transform;
        src1GO.transform.parent = multiRotationGO.transform;
        sources.Add(new WeightedJobTransform(src0GO.transform, true, 0f));
        sources.Add(new WeightedJobTransform(src1GO.transform, true, 0f));
        multiRotationData.sourceObjects = sources;

        src0GO.transform.rotation = data.rigData.hipsGO.transform.rotation;
        src1GO.transform.rotation = data.rigData.hipsGO.transform.rotation;

        data.rigData.rootGO.GetComponent<RigBuilder>().Build();

        data.constraint = multiRotation;
        data.constraintData = multiRotationData;

        return data;
    }

    [UnityTest]
    public IEnumerator MultiRotationConstraint_FollowSourceObjects()
    {
        var data = SetupConstraintRig();
        var constraintData = data.constraintData;
        var constrainedObject = constraintData.constrainedObject;
        var sources = constraintData.sourceObjects;

        // src0.w = 0, src1.w = 0
        Assert.Zero(sources[0].weight);
        Assert.Zero(sources[1].weight);
        yield return null;
        yield return null;
        Assert.AreEqual(constrainedObject.transform.rotation, data.constrainedObjectRestRotation);

        // Add rotation to source objects
        sources[0].transform.rotation *= Quaternion.AngleAxis(90, Vector3.up);
        sources[1].transform.rotation *= Quaternion.AngleAxis(-90, Vector3.up);

        // src0.w = 1, src1.w = 0
        sources[0].weight = 1f;
        constraintData.MarkSourceWeightsDirty();
        yield return null;
        yield return null;
        Assert.AreEqual(constrainedObject.transform.rotation, sources[0].transform.rotation);

        // src0.w = 0, src1.w = 1
        sources[0].weight = 0f;
        sources[1].weight = 1f;
        constraintData.MarkSourceWeightsDirty();
        yield return null;
        yield return null;
        Assert.AreEqual(constrainedObject.transform.rotation, sources[1].transform.rotation);
    }

    [UnityTest]
    public IEnumerator MultiRotationConstraint_ApplyWeight()
    {
        var data = SetupConstraintRig();
        var constraintData = data.constraintData;
        var constrainedObject = constraintData.constrainedObject;
        var sources = constraintData.sourceObjects;

        sources[0].transform.rotation *= Quaternion.AngleAxis(90, Vector3.up);
        sources[0].weight = 1f;
        constraintData.MarkSourceWeightsDirty();

        for (int i = 0; i <= 5; ++i)
        {
            float w = i / 5.0f;

            data.constraint.weight = w;
            yield return null;
            yield return null;

            Quaternion weightedRot = Quaternion.Lerp(data.constrainedObjectRestRotation, sources[0].transform.rotation, w);
            Assert.AreEqual(
                constrainedObject.transform.rotation,
                weightedRot,
                String.Format("Expected constrainedObject rotation to be {0} for a weight of {1}, but was {2}", weightedRot, w, constrainedObject.transform.rotation)
                );
        }
    }
}
