using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Animations.Rigging;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

using RigTestData = RuntimeRiggingTestFixture.RigTestData;

class MultiParentConstraintTests
{
    const float k_Epsilon = 0.05f;

    struct ConstraintTestData
    {
        public RigTestData rigData;
        public MultiParentConstraint constraint;

        public AffineTransform constrainedObjectRestTx;
    }

    private ConstraintTestData SetupConstraintRig()
    {
        var data = new ConstraintTestData();

        data.rigData = RuntimeRiggingTestFixture.SetupRigHierarchy();

        var multiParentGO = new GameObject("multiParent");
        var multiParent = multiParentGO.AddComponent<MultiParentConstraint>();
        multiParent.Reset();

        multiParentGO.transform.parent = data.rigData.rigGO.transform;

        multiParent.data.constrainedObject = new JobTransform(data.rigData.hipsGO.transform, false);
        data.constrainedObjectRestTx = new AffineTransform(
            multiParent.data.constrainedObject.transform.position,
            multiParent.data.constrainedObject.transform.rotation
            );

        List<WeightedJobTransform> sources = new List<WeightedJobTransform>(2);
        var src0GO = new GameObject("source0");
        var src1GO = new GameObject("source1");
        src0GO.transform.parent = multiParentGO.transform;
        src1GO.transform.parent = multiParentGO.transform;
        sources.Add(new WeightedJobTransform(src0GO.transform, true, 0f));
        sources.Add(new WeightedJobTransform(src1GO.transform, true, 0f));
        multiParent.data.sourceObjects = sources;

        var pos = data.rigData.hipsGO.transform.position;
        var rot = data.rigData.hipsGO.transform.rotation;
        src0GO.transform.SetPositionAndRotation(pos, rot);
        src1GO.transform.SetPositionAndRotation(pos, rot);

        data.rigData.rootGO.GetComponent<RigBuilder>().Build();

        data.constraint = multiParent;

        return data;
    }

    [UnityTest]
    public IEnumerator MultiParentConstraint_FollowSourceObjects()
    {
        var data = SetupConstraintRig();
        var constraint = data.constraint;
        
        var constrainedObject = constraint.data.constrainedObject;
        var sources = constraint.data.sourceObjects;

        // src0.w = 0, src1.w = 0
        Assert.Zero(sources[0].weight);
        Assert.Zero(sources[1].weight);
        yield return RuntimeRiggingTestFixture.YieldTwoFrames();

        Assert.AreEqual(constrainedObject.transform.position, data.constrainedObjectRestTx.translation);
        Assert.AreEqual(constrainedObject.transform.rotation, data.constrainedObjectRestTx.rotation);

        // Add displacement to source objects
        sources[0].transform.position += Vector3.right;
        sources[0].transform.rotation *= Quaternion.AngleAxis(-90, Vector3.up);
        sources[1].transform.position += Vector3.left;
        sources[1].transform.rotation *= Quaternion.AngleAxis(90, Vector3.up);

        // src0.w = 1, src1.w = 0
        sources[0].weight = 1f;
        constraint.data.MarkSourceWeightsDirty();
        yield return RuntimeRiggingTestFixture.YieldTwoFrames();

        Assert.AreEqual(constrainedObject.transform.position, sources[0].transform.position);
        Assert.AreEqual(constrainedObject.transform.rotation, sources[0].transform.rotation);

        // src0.w = 0, src1.w = 1
        sources[0].weight = 0f;
        sources[1].weight = 1f;
        constraint.data.MarkSourceWeightsDirty();
        yield return RuntimeRiggingTestFixture.YieldTwoFrames();

        Assert.AreEqual(constrainedObject.transform.position, sources[1].transform.position);
        Assert.AreEqual(constrainedObject.transform.rotation, sources[1].transform.rotation);
    }

    [UnityTest]
    public IEnumerator MultiParentConstraint_ApplyWeight()
    {
        var data = SetupConstraintRig();
        var constraint = data.constraint;

        var constrainedObject = constraint.data.constrainedObject;
        var sources = constraint.data.sourceObjects;

        sources[0].transform.position += Vector3.right;
        sources[0].transform.rotation *= Quaternion.AngleAxis(-90, Vector3.up);
        sources[0].weight = 1f;
        constraint.data.MarkSourceWeightsDirty();

        for (int i = 0; i <= 5; ++i)
        {
            float w = i / 5.0f;

            data.constraint.weight = w;
            yield return RuntimeRiggingTestFixture.YieldTwoFrames();


            var weightedPos = Vector3.Lerp(data.constrainedObjectRestTx.translation, sources[0].transform.position, w);
            Assert.AreEqual(
                constrainedObject.transform.position,
                weightedPos,
                String.Format("Expected constrainedObject to be at {0} for a weight of {1}, but was {2}", weightedPos, w, constrainedObject.transform.position)
                );

            var weightedRot = Quaternion.Lerp(data.constrainedObjectRestTx.rotation, sources[0].transform.rotation, w);
             Assert.AreEqual(
                constrainedObject.transform.rotation,
                weightedRot,
                String.Format("Expected constrainedObject to be at {0} for a weight of {1}, but was {2}", weightedRot, w, constrainedObject.transform.rotation)
                );
        }
    }
}
