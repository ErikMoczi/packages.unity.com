using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Animations.Rigging;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

using RigTestData = RuntimeRiggingTestFixture.RigTestData;

class MultiReferentialConstraintTests
{
    const float k_Epsilon = 0.05f;

    struct ConstraintTestData
    {
        public RigTestData rigData;
        public MultiReferentialConstraint constraint;
        public MultiReferentialConstraintData constraintData;

        public AffineTransform restPose;
    }

    private ConstraintTestData SetupConstraintRig()
    {
        var data = new ConstraintTestData();

        data.rigData = RuntimeRiggingTestFixture.SetupRigHierarchy();

        var multiRefGO = new GameObject("multiReferential");
        var multiRef = multiRefGO.AddComponent<MultiReferentialConstraint>();
        var multiRefData = multiRef.data;
        multiRefGO.transform.parent = data.rigData.rigGO.transform;

        Assert.IsNotNull(multiRefData);
        List<JobTransform> sources = new List<JobTransform>(3);
        var src0GO = new GameObject("source0");
        var src1GO = new GameObject("source1");
        src0GO.transform.parent = multiRefGO.transform;
        src1GO.transform.parent = multiRefGO.transform;
        sources.Add(new JobTransform(data.rigData.hipsGO.transform, false));
        sources.Add(new JobTransform(src0GO.transform, true));
        sources.Add(new JobTransform(src1GO.transform, true));
        multiRefData.sourceObjects = sources;
        multiRefData.driver = 0;

        var pos = data.rigData.hipsGO.transform.position;
        var rot = data.rigData.hipsGO.transform.rotation;
        src0GO.transform.SetPositionAndRotation(pos, rot);
        src1GO.transform.SetPositionAndRotation(pos, rot);
        data.restPose = new AffineTransform(pos, rot);

        data.rigData.rootGO.GetComponent<RigBuilder>().Build();

        data.constraint = multiRef;
        data.constraintData = multiRefData;

        return data;
    }

    [UnityTest]
    public IEnumerator MultiReferentialConstraint_FollowSourceObjects()
    {
        var data = SetupConstraintRig();
        var constraintData = data.constraintData;
        var sources = constraintData.sourceObjects;

        constraintData.driver = 0;
        var driver = sources[0];
        driver.transform.position += Vector3.forward;
        driver.transform.rotation *= Quaternion.AngleAxis(90, Vector3.up);
        yield return null;
        Assert.AreEqual(driver.transform.position, sources[1].transform.position);
        Assert.AreEqual(driver.transform.rotation, sources[1].transform.rotation);
        Assert.AreEqual(driver.transform.position, sources[2].transform.position);
        Assert.AreEqual(driver.transform.rotation, sources[2].transform.rotation);

        constraintData.driver = 1;
        driver = sources[1];
        driver.transform.position += Vector3.back;
        driver.transform.rotation *= Quaternion.AngleAxis(-90, Vector3.up);
        yield return null;
        Assert.AreEqual(driver.transform.position, sources[0].transform.position);
        Assert.AreEqual(driver.transform.rotation, sources[0].transform.rotation);
        Assert.AreEqual(driver.transform.position, sources[2].transform.position);
        Assert.AreEqual(driver.transform.rotation, sources[2].transform.rotation);

        constraintData.driver = 2;
        driver = sources[2];
        driver.transform.position += Vector3.up;
        driver.transform.rotation *= Quaternion.AngleAxis(90, Vector3.left);
        yield return null;
        Assert.AreEqual(driver.transform.position, sources[0].transform.position);
        Assert.AreEqual(driver.transform.rotation, sources[0].transform.rotation);
        Assert.AreEqual(driver.transform.position, sources[1].transform.position);
        Assert.AreEqual(driver.transform.rotation, sources[1].transform.rotation);
    }

    [UnityTest]
    public IEnumerator MultiReferentialConstraint_ApplyWeight()
    {
        var data = SetupConstraintRig();
        var constraintData = data.constraintData;
        var sources = constraintData.sourceObjects;

        constraintData.driver = 1;
        sources[1].transform.position += Vector3.right;
        sources[1].transform.rotation *= Quaternion.AngleAxis(-90, Vector3.up);

        for (int i = 0; i <= 5; ++i)
        {
            float w = i / 5.0f;

            data.constraint.weight = w;
            yield return null;
            yield return null;

            var weightedPos = Vector3.Lerp(data.restPose.translation, sources[1].transform.position, w);
            Assert.AreEqual(
                sources[0].transform.position,
                weightedPos,
                String.Format("Expected Source0 to be at {0} for a weight of {1}, but was {2}", weightedPos, w, sources[0].transform.position)
                );
            Assert.AreEqual(
                sources[2].transform.position,
                weightedPos,
                String.Format("Expected Source2 to be at {0} for a weight of {1}, but was {2}", weightedPos, w, sources[2].transform.position)
                );

            var weightedRot = Quaternion.Lerp(data.restPose.rotation, sources[1].transform.rotation, w);
            Assert.AreEqual(
                sources[0].transform.rotation,
                weightedRot,
                String.Format("Expected Source0 to be at {0} for a weight of {1}, but was {2}", weightedRot, w, sources[0].transform.rotation)
                );
            Assert.AreEqual(
                sources[2].transform.rotation,
                weightedRot,
                String.Format("Expected Source2 to be at {0} for a weight of {1}, but was {2}", weightedRot, w, sources[2].transform.rotation)
                );
        }
    }
}
