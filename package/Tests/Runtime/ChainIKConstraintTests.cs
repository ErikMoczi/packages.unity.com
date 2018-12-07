using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Animations.Rigging;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

using RigTestData = RuntimeRiggingTestFixture.RigTestData;

class ChainIKConstraintTests {

    const float k_Epsilon = 0.05f;

    struct ConstraintTestData
    {
        public RigTestData rigData;
        public ChainIKConstraint constraint;
        public ChainIKConstraintData constraintData;
    }

    private ConstraintTestData SetupConstraintRig()
    {
        var data = new ConstraintTestData();

        data.rigData = RuntimeRiggingTestFixture.SetupRigHierarchy();

        var chainIKGO = new GameObject("chainIK");
        var chainIK = chainIKGO.AddComponent<ChainIKConstraint>();
        var chainIKData = chainIK.data;

        chainIKGO.transform.parent = data.rigData.rigGO.transform;

        Assert.IsNotNull(chainIKData);

        chainIKData.root = new JobTransform(data.rigData.hipsGO.transform.Find("Chest"), false);
        Assert.IsNotNull(chainIKData.root.transform, "Could not find root transform");

        chainIKData.tip = new JobTransform(chainIKData.root.transform.Find("LeftArm/LeftForeArm/LeftHand"), false);
        Assert.IsNotNull(chainIKData.tip.transform, "Could not find tip transform");

        var targetGO = new GameObject ("target");
        targetGO.transform.parent = chainIKGO.transform;

        chainIKData.target = new JobTransform(targetGO.transform, true);

        data.rigData.rootGO.GetComponent<RigBuilder>().Build();
        targetGO.transform.position = chainIKData.tip.transform.position;

        data.constraint = chainIK;
        data.constraintData = chainIKData;

        return data;
    }


    [UnityTest]
    public IEnumerator ChainIKConstraint_FollowsTarget()
    {
        var data = SetupConstraintRig();

        var target = data.constraintData.target.transform;
        var tip = data.constraintData.target.transform;
        var root = data.constraintData.root.transform;

        for (int i = 0; i < 5; ++i)
        {
            target.position += new Vector3(0f, 0.1f, 0f);
            yield return null;

            Vector3 rootToTip = (tip.position - root.position).normalized;
            Vector3 rootToTarget = (target.position - root.position).normalized;

            Assert.AreEqual(rootToTarget.x, rootToTip.x, k_Epsilon, String.Format("Expected rootToTip.x to be {0}, but was {1}", rootToTip.x, rootToTarget.x));
            Assert.AreEqual(rootToTarget.y, rootToTip.y, k_Epsilon, String.Format("Expected rootToTip.y to be {0}, but was {1}", rootToTip.y, rootToTarget.y));
            Assert.AreEqual(rootToTarget.z, rootToTip.z, k_Epsilon, String.Format("Expected rootToTip.z to be {0}, but was {1}", rootToTip.z, rootToTarget.z));
        }
    }

    [UnityTest]
    public IEnumerator ChainIKConstraint_ApplyWeight()
    {
        var data = SetupConstraintRig();

        List<Transform> chain = new List<Transform>();
        Transform tmp = data.constraintData.tip.transform;
        while (tmp != data.constraintData.root.transform)
        {
            chain.Add(tmp);
            tmp = tmp.parent;
        }
        chain.Add(data.constraintData.root.transform);
        chain.Reverse();

        // Chain with no constraint.
        Vector3[] bindPoseChain = chain.Select(transform => transform.position).ToArray();

        var target = data.constraintData.target.transform;
        target.position += new Vector3(0f, 0.5f, 0f);

        yield return null;

        // Chain with ChainIK constraint.
        Vector3[] weightedChain = chain.Select(transform => transform.position).ToArray();

        // In-between chains.
        List<Vector3[]> inBetweenChains = new List<Vector3[]>();
        for (int i = 0; i <= 5; ++i)
        {
            float w = i / 5.0f;

            data.constraint.weight = w;
            yield return null;
            yield return null;

            inBetweenChains.Add(chain.Select(transform => transform.position).ToArray());
        }

        for (int i = 0; i <= 5; ++i)
        {
            Vector3[] prevChain = (i > 0) ? inBetweenChains[i - 1] : bindPoseChain;
            Vector3[] currentChain = inBetweenChains[i];
            Vector3[] nextChain = (i < 5) ? inBetweenChains[i + 1] : weightedChain;

            for (int j = 0; j < bindPoseChain.Length - 1; ++j)
            {
                Vector2 dir1 = prevChain[j + 1] - prevChain[j];
                Vector2 dir2 = currentChain[j + 1] - currentChain[j];
                Vector2 dir3 = nextChain[j + 1] - nextChain[j];

                float maxAngle = Vector2.Angle(dir1, dir3);
                float angle = Vector2.Angle(dir1, dir3);

                Assert.GreaterOrEqual(angle, 0f);
                Assert.LessOrEqual(angle, maxAngle);
            }
        }
    }
}
