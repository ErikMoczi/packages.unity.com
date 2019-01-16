using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Animations.Rigging;
using NUnit.Framework;
using System.Collections;
using System;

using RigTestData = RuntimeRiggingTestFixture.RigTestData;

class TwoBoneIKConstraintTests {

    const float k_Epsilon = 0.05f;

    struct ConstraintTestData
    {
        public RigTestData rigData;
        public TwoBoneIKConstraint constraint;
    }

    private ConstraintTestData SetupConstraintRig()
    {
        var data = new ConstraintTestData();

        data.rigData = RuntimeRiggingTestFixture.SetupRigHierarchy();

        var twoBoneIKGO = new GameObject("twoBoneIK");
        var twoBoneIK = twoBoneIKGO.AddComponent<TwoBoneIKConstraint>();
        twoBoneIK.Reset();

        twoBoneIKGO.transform.parent = data.rigData.rigGO.transform;

        twoBoneIK.data.root = data.rigData.hipsGO.transform.Find("Chest/LeftArm");
        Assert.IsNotNull(twoBoneIK.data.root, "Could not find root transform");

        twoBoneIK.data.mid = twoBoneIK.data.root.transform.Find("LeftForeArm");
        Assert.IsNotNull(twoBoneIK.data.mid, "Could not find mid transform");

        twoBoneIK.data.tip = twoBoneIK.data.mid.transform.Find("LeftHand");
        Assert.IsNotNull(twoBoneIK.data.tip, "Could not find tip transform");

        var targetGO = new GameObject ("target");
        targetGO.transform.parent = twoBoneIKGO.transform;

        var hintGO = new GameObject ("hint");
        hintGO.transform.parent = twoBoneIKGO.transform;

        twoBoneIK.data.target = new JobTransform(targetGO.transform, true);
        twoBoneIK.data.hint = new JobTransform(hintGO.transform, true);

        data.rigData.rootGO.GetComponent<RigBuilder>().Build();

        targetGO.transform.position = twoBoneIK.data.tip.transform.position;

        data.constraint = twoBoneIK;

        return data;
    }


    [UnityTest]
    public IEnumerator TwoBoneIKConstraint_FollowsTarget()
    {
        var data = SetupConstraintRig();
        var constraint = data.constraint;

        var target = constraint.data.target.transform;
        var tip = constraint.data.target.transform;
        var root = constraint.data.root.transform;

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
    public IEnumerator TwoBoneIKConstraint_UsesHint()
    {
        var data = SetupConstraintRig();
        var constraint = data.constraint;

        var target = constraint.data.target.transform;
        var hint = constraint.data.hint.transform;
        var mid = constraint.data.mid.transform;

        Vector3 midPos1 = mid.position;

        // Make left arm flex.
        target.position += new Vector3(0.2f, 0.0f, 0f);

        hint.position = mid.position + new Vector3(0f, 1f, 0f);
        yield return null;

        Vector3 midPos2 = mid.position;
        Assert.Greater(midPos2.y, midPos1.y, String.Format("Expected mid2.y to be greater than mid1.y"));
        Assert.AreEqual(midPos1.z, midPos2.z, k_Epsilon, String.Format("Expected mid2.z to be {0}, but was {1}", midPos1.z, midPos2.z));

        hint.position = mid.position + new Vector3(0f, -1f, 0f);
        yield return null;

        midPos2 = mid.position;
        Assert.Less(midPos2.y, midPos1.y, String.Format("Expected mid2.y to be lower than mid1.y"));
        Assert.AreEqual(midPos1.z, midPos2.z, k_Epsilon, String.Format("Expected mid2.z to be {0}, but was {1}", midPos1.z, midPos2.z));

        hint.position = mid.position + new Vector3(0f, 0f, 1f);
        yield return null;

        midPos2 = mid.position;
        Assert.AreEqual(midPos1.y, midPos2.y, k_Epsilon, String.Format("Expected mid2.y to be {0}, but was {1}", midPos1.y, midPos2.y));
        Assert.Greater(midPos2.z, midPos1.z, String.Format("Expected mid2.y to be greater than mid1.y"));

        hint.position = mid.position + new Vector3(0f, 0f, -1f);
        yield return null;

        midPos2 = mid.position;
        Assert.AreEqual(midPos1.y, midPos2.y, k_Epsilon, String.Format("Expected mid2.y to be {0}, but was {1}", midPos1.y, midPos2.y));
        Assert.Less(midPos2.z, midPos1.z, String.Format("Expected mid2.y to be greater than mid1.y"));
    }

    [UnityTest]
    public IEnumerator TwoBoneIKConstraint_ApplyWeight()
    {
        var data = SetupConstraintRig();
        var constraint = data.constraint;

        var tip = constraint.data.tip.transform;
        var target = constraint.data.target.transform;

        Vector3 tipPos1 = tip.position;

        target.position += new Vector3(0f, 0.5f, 0f);
        yield return null;

        Vector3 tipPos2 = tip.position;

        for (int i = 0; i <= 5; ++i)
        {
            float w = i / 5.0f;

            data.constraint.weight = w;
            yield return RuntimeRiggingTestFixture.YieldTwoFrames();

            Vector3 weightedTipPos = Vector3.Lerp(tipPos1, tipPos2, w);
            Vector3 tipPos = tip.position;

            Assert.AreEqual(weightedTipPos.x, tipPos.x, k_Epsilon, String.Format("Expected tip.x to be {0} for a weight of {1}, but was {2}", weightedTipPos.x, w, tipPos.x));
            Assert.AreEqual(weightedTipPos.y, tipPos.y, k_Epsilon, String.Format("Expected tip.y to be {0} for a weight of {1}, but was {2}", weightedTipPos.y, w, tipPos.y));
            Assert.AreEqual(weightedTipPos.z, tipPos.z, k_Epsilon, String.Format("Expected tip.z to be {0} for a weight of {1}, but was {2}", weightedTipPos.z, w, tipPos.z));
        }
    }

}
