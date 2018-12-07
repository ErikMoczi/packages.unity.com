using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Animations.Rigging;
using NUnit.Framework;
using System.Collections;

using RigTestData = RuntimeRiggingTestFixture.RigTestData;

class BlendConstraintTests
{
    const float k_Epsilon = 1e-5f;

    struct ConstraintTestData
    {
        public RigTestData rigData;
        public BlendConstraint constraint;
        public AffineTransform restPose;
    }

    private ConstraintTestData SetupConstraintRig()
    {
        var data = new ConstraintTestData();

        data.rigData = RuntimeRiggingTestFixture.SetupRigHierarchy();

        var blendConstraintGO = new GameObject("blendConstraint");
        var blendConstraint = blendConstraintGO.AddComponent<BlendConstraint>();
        var blendConstraintData = blendConstraint.data;
        blendConstraintGO.transform.parent = data.rigData.rigGO.transform;

        Assert.IsNotNull(blendConstraintData);
        var leftForeArm = data.rigData.hipsGO.transform.Find("Chest/LeftArm/LeftForeArm");
        var leftHand = leftForeArm.Find("LeftHand");

        blendConstraintData.sourceObjectA = new JobTransform(leftForeArm, true);
        blendConstraintData.sourceObjectB = new JobTransform(leftHand, true);

        var constrainedObject = new GameObject("constrainedBlendObj");
        constrainedObject.transform.parent = blendConstraintGO.transform;
        blendConstraintData.constrainedObject = new JobTransform(constrainedObject.transform, false);
        data.restPose = new AffineTransform(constrainedObject.transform.position, constrainedObject.transform.rotation);

        blendConstraintData.positionWeight = blendConstraintData.rotationWeight = 0.5f;
        blendConstraintData.blendPosition = blendConstraintData.blendRotation = true;

        data.rigData.rootGO.GetComponent<RigBuilder>().Build();
        data.constraint = blendConstraint;

        return data;
    }

    [UnityTest]
    public IEnumerator BlendConstraint_FollowsSourceObjects()
    {
        var data = SetupConstraintRig();
        var constraintData = data.constraint.data;
        var srcObjA = constraintData.sourceObjectA;
        var srcObjB = constraintData.sourceObjectB;
        var constrainedObj = constraintData.constrainedObject;

        // Apply rotation on sourceB
        srcObjB.transform.rotation *= Quaternion.AngleAxis(90, Vector3.right);
        yield return null;

        // SourceA has full influence
        constraintData.positionWeight = 0f;
        constraintData.rotationWeight = 0f;
        yield return null;
        yield return null;
        Assert.AreEqual(constrainedObj.transform.position, srcObjA.transform.position);
        RotationsAreEqual(constrainedObj.transform.rotation, srcObjA.transform.rotation);

        // SourceB has full influence
        constraintData.positionWeight = 1f;
        constraintData.rotationWeight = 1f;
        yield return null;
        yield return null;
        Assert.AreEqual(constrainedObj.transform.position, srcObjB.transform.position);
        RotationsAreEqual(constrainedObj.transform.rotation, srcObjB.transform.rotation);

        // Translation/Rotation blending between sources is disabled
        constraintData.blendPosition = false;
        constraintData.blendRotation = false;
        yield return null;
        yield return null;
        Assert.AreEqual(constrainedObj.transform.position, data.restPose.translation);
        RotationsAreEqual(constrainedObj.transform.rotation, data.restPose.rotation);
    }

    [UnityTest]
    public IEnumerator BlendConstraint_ApplyWeight()
    {
        var data = SetupConstraintRig();
        var constraintData = data.constraint.data;
        var srcObjB = constraintData.sourceObjectB;
        var constrainedObj = constraintData.constrainedObject;

        // SourceB has full influence
        constraintData.positionWeight = 1f;
        constraintData.rotationWeight = 1f;
        srcObjB.transform.rotation *= Quaternion.AngleAxis(90, Vector3.right);
        yield return null;

        for (int i = 0; i <= 5; ++i)
        {
            float w = i / 5.0f;

            data.constraint.weight = w;
            yield return null;
            yield return null;

            var weightedPos = Vector3.Lerp(data.restPose.translation, srcObjB.transform.position, w);
            var weightedRot = Quaternion.Lerp(data.restPose.rotation, srcObjB.transform.rotation, w);
            Assert.AreEqual(constrainedObj.transform.position, weightedPos);
            RotationsAreEqual(constrainedObj.transform.rotation, weightedRot);
        }
    }

    static void RotationsAreEqual(Quaternion lhs, Quaternion rhs)
    {
        var dot = Quaternion.Dot(lhs, rhs);
        Assert.AreEqual(Mathf.Abs(dot), 1f, k_Epsilon);
    }
}
