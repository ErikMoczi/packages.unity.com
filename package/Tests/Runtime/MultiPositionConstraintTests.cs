using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Animations.Rigging;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

using RigTestData = RuntimeRiggingTestFixture.RigTestData;

class MultiPositionConstraintTests
{
    const float k_Epsilon = 0.05f;

    struct ConstraintTestData
    {
        public RigTestData rigData;
        public MultiPositionConstraint constraint;

        public Vector3 constrainedObjectRestPosition;
    }

    private ConstraintTestData SetupConstraintRig()
    {
        var data = new ConstraintTestData();

        data.rigData = RuntimeRiggingTestFixture.SetupRigHierarchy();

        var multiPositionGO = new GameObject("multiPosition");
        var multiPosition = multiPositionGO.AddComponent<MultiPositionConstraint>();
        multiPosition.Reset();

        multiPositionGO.transform.parent = data.rigData.rigGO.transform;

        multiPosition.data.constrainedObject = new JobTransform(data.rigData.hipsGO.transform, false);
        data.constrainedObjectRestPosition = multiPosition.data.constrainedObject.transform.position;

        List<WeightedJobTransform> sources = new List<WeightedJobTransform>(2);
        var src0GO = new GameObject("source0");
        var src1GO = new GameObject("source1");
        src0GO.transform.parent = multiPositionGO.transform;
        src1GO.transform.parent = multiPositionGO.transform;
        sources.Add(new WeightedJobTransform(src0GO.transform, true, 0f));
        sources.Add(new WeightedJobTransform(src1GO.transform, true, 0f));
        multiPosition.data.sourceObjects = sources;

        src0GO.transform.position = data.rigData.hipsGO.transform.position;
        src1GO.transform.position = data.rigData.hipsGO.transform.position;

        data.rigData.rootGO.GetComponent<RigBuilder>().Build();

        data.constraint = multiPosition;

        return data;
    }

    [UnityTest]
    public IEnumerator MultiPositionConstraint_FollowSourceObjects()
    {
        var data = SetupConstraintRig();
        var constraint = data.constraint;
        
        var constrainedObject = constraint.data.constrainedObject;
        var sources = constraint.data.sourceObjects;

        // src0.w = 0, src1.w = 0
        Assert.Zero(sources[0].weight);
        Assert.Zero(sources[1].weight);
        yield return RuntimeRiggingTestFixture.YieldTwoFrames();

        Assert.AreEqual(constrainedObject.transform.position, data.constrainedObjectRestPosition);

        // Add displacement to source objects
        sources[0].transform.position += Vector3.right;
        sources[1].transform.position += Vector3.left;

        // src0.w = 1, src1.w = 0
        sources[0].weight = 1f;
        constraint.data.MarkSourceWeightsDirty();
        yield return RuntimeRiggingTestFixture.YieldTwoFrames();

        Assert.AreEqual(constrainedObject.transform.position, sources[0].transform.position);

        // src0.w = 0, src1.w = 1
        sources[0].weight = 0f;
        sources[1].weight = 1f;
        constraint.data.MarkSourceWeightsDirty();
        yield return RuntimeRiggingTestFixture.YieldTwoFrames();

        Assert.AreEqual(constrainedObject.transform.position, sources[1].transform.position);

        // src0.w = 1, src1.w = 1
        // since source object positions are mirrored, we should simply evaluate to the original rest pos.
        sources[0].weight = 1f;
        constraint.data.MarkSourceWeightsDirty();
        yield return RuntimeRiggingTestFixture.YieldTwoFrames();

        Assert.AreEqual(constrainedObject.transform.position, data.constrainedObjectRestPosition);
    }

    [UnityTest]
    public IEnumerator MultiPositionConstraint_ApplyWeight()
    {
        var data = SetupConstraintRig();
        var constraint = data.constraint;
        
        var constrainedObject = constraint.data.constrainedObject;
        var sources = constraint.data.sourceObjects;

        sources[0].transform.position += Vector3.forward;
        sources[0].weight = 1f;
        constraint.data.MarkSourceWeightsDirty();

        for (int i = 0; i <= 5; ++i)
        {
            float w = i / 5.0f;

            data.constraint.weight = w;
            yield return RuntimeRiggingTestFixture.YieldTwoFrames();

            Vector3 weightedPos = Vector3.Lerp(data.constrainedObjectRestPosition, sources[0].transform.position, w);
            Assert.AreEqual(
                constrainedObject.transform.position,
                weightedPos,
                String.Format("Expected constrainedObject to be at {0} for a weight of {1}, but was {2}", weightedPos, w, constrainedObject.transform.position)
                );
        }
    }
}
