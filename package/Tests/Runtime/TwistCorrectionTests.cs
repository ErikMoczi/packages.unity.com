using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Animations.Rigging;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;

using RigTestData = RuntimeRiggingTestFixture.RigTestData;

class TwistCorrectionTests
{
    const float k_Epsilon = 0.005f;

    struct ConstraintTestData
    {
        public RigTestData rigData;
        public TwistCorrection constraint;

        public Quaternion restLocalRotation;
    }

    private ConstraintTestData SetupConstraintRig()
    {
        var data = new ConstraintTestData();

        data.rigData = RuntimeRiggingTestFixture.SetupRigHierarchy();

        var twistCorrectionGO = new GameObject("twistCorrection");
        var twistCorrection = twistCorrectionGO.AddComponent<TwistCorrection>();
        twistCorrection.Reset();

        twistCorrectionGO.transform.parent = data.rigData.rigGO.transform;

        var leftArm = data.rigData.hipsGO.transform.Find("Chest/LeftArm");
        var leftForeArm = leftArm.Find("LeftForeArm");
        var leftHand = leftForeArm.Find("LeftHand");

        // Force zero rotation to simplify testing
        leftHand.rotation = Quaternion.identity;
        leftForeArm.rotation = Quaternion.identity;
        leftArm.rotation = Quaternion.identity;

        twistCorrection.data.sourceObject = new JobTransform(leftHand, true);
        twistCorrection.data.twistAxis = TwistCorrectionData.Axis.X;
        data.restLocalRotation = leftHand.localRotation;

        List<WeightedJobTransform> twistNodes = new List<WeightedJobTransform>(2);
        var twistNode0GO = new GameObject("twistNode0");
        var twistNode1GO = new GameObject("twistNode1");
        twistNode0GO.transform.parent = leftForeArm;
        twistNode1GO.transform.parent = leftForeArm;
        twistNode0GO.transform.SetPositionAndRotation(Vector3.Lerp(leftForeArm.position, leftHand.position, 0.25f), leftHand.rotation);
        twistNode1GO.transform.SetPositionAndRotation(Vector3.Lerp(leftForeArm.position, leftHand.position, 0.75f), leftHand.rotation);
        twistNodes.Add(new WeightedJobTransform(twistNode0GO.transform, false, 0.5f));
        twistNodes.Add(new WeightedJobTransform(twistNode1GO.transform, false, 0.5f));
        twistCorrection.data.twistNodes = twistNodes;

        data.rigData.rootGO.GetComponent<RigBuilder>().Build();

        data.constraint = twistCorrection;

        return data;
    }

    [UnityTest]
    public IEnumerator TwistCorrection_FollowsSourceObject()
    {
        var data = SetupConstraintRig();
        var constraint = data.constraint;

        var sourceObject = constraint.data.sourceObject;
        var twistNodes = constraint.data.twistNodes;

        // Apply rotation to source object
        sourceObject.transform.localRotation = sourceObject.transform.localRotation * Quaternion.AngleAxis(90, Vector3.left);

        // twistNode0.w = 0.5f, twistNode1.w = 0.5f [does not influence twist nodes]
        Assert.AreEqual(twistNodes[0].weight, 0.5f);
        Assert.AreEqual(twistNodes[1].weight, 0.5f);
        yield return RuntimeRiggingTestFixture.YieldTwoFrames();

        Assert.AreNotEqual(sourceObject.transform.localRotation, data.restLocalRotation);
        Assert.AreEqual(twistNodes[0].transform.localRotation, Quaternion.identity);
        Assert.AreEqual(twistNodes[1].transform.localRotation, Quaternion.identity);

        // twistNode0.w = 1f, twistNode1.w = 1f [twist nodes should be equal to source]
        twistNodes[0].weight = 1f;
        twistNodes[1].weight = 1f;
        constraint.data.MarkTwistNodeWeightsDirty();
        yield return RuntimeRiggingTestFixture.YieldTwoFrames();

        // Verify twist on X axis
        Assert.AreEqual(twistNodes[0].transform.localRotation.w, sourceObject.transform.localRotation.w, k_Epsilon);
        Assert.AreEqual(twistNodes[0].transform.localRotation.x, sourceObject.transform.localRotation.x, k_Epsilon);
        Assert.AreEqual(twistNodes[1].transform.localRotation.w, sourceObject.transform.localRotation.w, k_Epsilon);
        Assert.AreEqual(twistNodes[1].transform.localRotation.x, sourceObject.transform.localRotation.x, k_Epsilon);

        // twistNode0.w = 0f, twistNode1.w = 0f [twist nodes should be inverse to source]
        twistNodes[0].weight = 0f;
        twistNodes[1].weight = 0f;
        constraint.data.MarkTwistNodeWeightsDirty();
        yield return RuntimeRiggingTestFixture.YieldTwoFrames();

        var invTwist = Quaternion.Inverse(sourceObject.transform.localRotation);
        // Verify twist on X axis
        Assert.AreEqual(twistNodes[0].transform.localRotation.w, invTwist.w, k_Epsilon);
        Assert.AreEqual(twistNodes[0].transform.localRotation.x, invTwist.x, k_Epsilon);
        Assert.AreEqual(twistNodes[1].transform.localRotation.w, invTwist.w, k_Epsilon);
        Assert.AreEqual(twistNodes[1].transform.localRotation.x, invTwist.x, k_Epsilon);
    }

    [UnityTest]
    public IEnumerator TwistCorrection_ApplyWeight()
    {
        var data = SetupConstraintRig();
        var constraint = data.constraint;
        
        var sourceObject = constraint.data.sourceObject;
        var twistNodes = constraint.data.twistNodes;

        // Apply rotation to source object
        sourceObject.transform.localRotation = sourceObject.transform.localRotation * Quaternion.AngleAxis(90, Vector3.left);
        twistNodes[0].weight = 1f;
        constraint.data.MarkTwistNodeWeightsDirty();

        for (int i = 0; i <= 5; ++i)
        {
            float w = i / 5.0f;

            data.constraint.weight = w;
            yield return RuntimeRiggingTestFixture.YieldTwoFrames();

            var weightedRot = Quaternion.Lerp(data.restLocalRotation, sourceObject.transform.localRotation, w);
            Assert.AreEqual(twistNodes[0].transform.localRotation.w, weightedRot.w, k_Epsilon);
            Assert.AreEqual(twistNodes[0].transform.localRotation.x, weightedRot.x, k_Epsilon);
        }
    }
}
