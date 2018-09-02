using NUnit.Framework;
using static Unity.Mathematics.math;

namespace Unity.Mathematics.Tests
{
    [TestFixture]
    class TestRigidTransform
    {
        [Test]
        public void rigid_transform_construct_from_matrix()
        {
            float3x3 m3x3 = TestMatrix.test3x3_xyz;
            float4x4 m4x4 = TestMatrix.test4x4_zyx;

            RigidTransform q4x4 = RigidTransform(m4x4);

            float4x4 mq4x4 = float4x4(q4x4);

            TestUtils.AreEqual(mq4x4, m4x4, 0.0001f);
        }

        [Test]
        public void rigid_transform_axisAngle()
        {
            RigidTransform q = RigidTransform.axisAngle(normalize(float3(1.0f, 2.0f, 3.0f)), 10.0f);

            RigidTransform r = RigidTransform(quaternion(-0.2562833f, -0.5125666f, -0.76885f, 0.2836622f), float3.zero);
            TestUtils.AreEqual(q, r, 0.0001f);
        }

        [Test]
        public void rigid_transform_euler()
        {
            float3 test_angles = TestMatrix.test_angles;
            RigidTransform q0 = RigidTransform.euler(test_angles);
            RigidTransform q0_xyz = RigidTransform.euler(test_angles, RotationOrder.XYZ);
            RigidTransform q0_xzy = RigidTransform.euler(test_angles, RotationOrder.XZY);
            RigidTransform q0_yxz = RigidTransform.euler(test_angles, RotationOrder.YXZ);
            RigidTransform q0_yzx = RigidTransform.euler(test_angles, RotationOrder.YZX);
            RigidTransform q0_zxy = RigidTransform.euler(test_angles, RotationOrder.ZXY);
            RigidTransform q0_zyx = RigidTransform.euler(test_angles, RotationOrder.ZYX);

            RigidTransform q1 = RigidTransform.euler(test_angles.x, test_angles.y, test_angles.z);
            RigidTransform q1_xyz = RigidTransform.euler(test_angles.x, test_angles.y, test_angles.z, RotationOrder.XYZ);
            RigidTransform q1_xzy = RigidTransform.euler(test_angles.x, test_angles.y, test_angles.z, RotationOrder.XZY);
            RigidTransform q1_yxz = RigidTransform.euler(test_angles.x, test_angles.y, test_angles.z, RotationOrder.YXZ);
            RigidTransform q1_yzx = RigidTransform.euler(test_angles.x, test_angles.y, test_angles.z, RotationOrder.YZX);
            RigidTransform q1_zxy = RigidTransform.euler(test_angles.x, test_angles.y, test_angles.z, RotationOrder.ZXY);
            RigidTransform q1_zyx = RigidTransform.euler(test_angles.x, test_angles.y, test_angles.z, RotationOrder.ZYX);

            float epsilon = 0.0001f;
            TestUtils.AreEqual(q0, RigidTransform(quaternion(-0.3133549f, 0.3435619f, 0.3899215f, 0.7948176f), float3.zero), epsilon);
            TestUtils.AreEqual(q0_xyz, RigidTransform(quaternion(-0.4597331f, 0.06979711f, 0.3899215f, 0.7948176f), float3.zero), epsilon);
            TestUtils.AreEqual(q0_xzy, RigidTransform(quaternion(-0.3133549f, 0.06979711f, 0.3899215f, 0.8630749f), float3.zero), epsilon);
            TestUtils.AreEqual(q0_yxz, RigidTransform(quaternion(-0.4597331f, 0.06979711f, 0.1971690f, 0.8630748f), float3.zero), epsilon);
            TestUtils.AreEqual(q0_yzx, RigidTransform(quaternion(-0.4597331f, 0.34356190f, 0.1971690f, 0.7948176f), float3.zero), epsilon);
            TestUtils.AreEqual(q0_zxy, RigidTransform(quaternion(-0.3133549f, 0.34356190f, 0.3899215f, 0.7948176f), float3.zero), epsilon);
            TestUtils.AreEqual(q0_zyx, RigidTransform(quaternion(-0.3133549f, 0.34356190f, 0.1971690f, 0.8630749f), float3.zero), epsilon);

            TestUtils.AreEqual(q1, RigidTransform(quaternion(-0.3133549f, 0.3435619f, 0.3899215f, 0.7948176f), float3.zero), epsilon);
            TestUtils.AreEqual(q1_xyz, RigidTransform(quaternion(-0.4597331f, 0.06979711f, 0.3899215f, 0.7948176f), float3.zero), epsilon);
            TestUtils.AreEqual(q1_xzy, RigidTransform(quaternion(-0.3133549f, 0.06979711f, 0.3899215f, 0.8630749f), float3.zero), epsilon);
            TestUtils.AreEqual(q1_yxz, RigidTransform(quaternion(-0.4597331f, 0.06979711f, 0.1971690f, 0.8630748f), float3.zero), epsilon);
            TestUtils.AreEqual(q1_yzx, RigidTransform(quaternion(-0.4597331f, 0.34356190f, 0.1971690f, 0.7948176f), float3.zero), epsilon);
            TestUtils.AreEqual(q1_zxy, RigidTransform(quaternion(-0.3133549f, 0.34356190f, 0.3899215f, 0.7948176f), float3.zero), epsilon);
            TestUtils.AreEqual(q1_zyx, RigidTransform(quaternion(-0.3133549f, 0.34356190f, 0.1971690f, 0.8630749f), float3.zero), epsilon);

            float3x3 m0 = float3x3(q0.rot);
            float3x3 m0_xyz = float3x3(q0_xyz.rot);
            float3x3 m0_xzy = float3x3(q0_xzy.rot);
            float3x3 m0_yxz = float3x3(q0_yxz.rot);
            float3x3 m0_yzx = float3x3(q0_yzx.rot);
            float3x3 m0_zxy = float3x3(q0_zxy.rot);
            float3x3 m0_zyx = float3x3(q0_zyx.rot);

            float3x3 m1 = float3x3(q1.rot);
            float3x3 m1_xyz = float3x3(q1_xyz.rot);
            float3x3 m1_xzy = float3x3(q1_xzy.rot);
            float3x3 m1_yxz = float3x3(q1_yxz.rot);
            float3x3 m1_yzx = float3x3(q1_yzx.rot);
            float3x3 m1_zxy = float3x3(q1_zxy.rot);
            float3x3 m1_zyx = float3x3(q1_zyx.rot);

            TestUtils.AreEqual(m0, TestMatrix.test3x3_zxy, epsilon);
            TestUtils.AreEqual(m0_xyz, TestMatrix.test3x3_xyz, epsilon);
            TestUtils.AreEqual(m0_yzx, TestMatrix.test3x3_yzx, epsilon);
            TestUtils.AreEqual(m0_zxy, TestMatrix.test3x3_zxy, epsilon);
            TestUtils.AreEqual(m0_xzy, TestMatrix.test3x3_xzy, epsilon);
            TestUtils.AreEqual(m0_yxz, TestMatrix.test3x3_yxz, epsilon);
            TestUtils.AreEqual(m0_zyx, TestMatrix.test3x3_zyx, 0.0001f);

            TestUtils.AreEqual(m1, TestMatrix.test3x3_zxy, epsilon);
            TestUtils.AreEqual(m1_xyz, TestMatrix.test3x3_xyz, epsilon);
            TestUtils.AreEqual(m1_yzx, TestMatrix.test3x3_yzx, epsilon);
            TestUtils.AreEqual(m1_zxy, TestMatrix.test3x3_zxy, epsilon);
            TestUtils.AreEqual(m1_xzy, TestMatrix.test3x3_xzy, epsilon);
            TestUtils.AreEqual(m1_yxz, TestMatrix.test3x3_yxz, epsilon);
            TestUtils.AreEqual(m1_zyx, TestMatrix.test3x3_zyx, epsilon);
        }

        [Test]
        public void rigid_transform_rotateX()
        {
            float angle = 2.3f;
            RigidTransform q = RigidTransform.rotateX(angle);

            RigidTransform r = RigidTransform(quaternion(0.91276394f, 0.0f, 0.0f, 0.40848744f), float3.zero);
            TestUtils.AreEqual(q, r, 0.0001f);
        }

        [Test]
        public void rigid_transform_rotateY()
        {
            float angle = 2.3f;
            RigidTransform q = RigidTransform.rotateY(angle);

            RigidTransform r = RigidTransform(quaternion(0.0f, 0.91276394f, 0.0f, 0.40848744f), float3.zero);
            TestUtils.AreEqual(q, r, 0.0001f);
        }

        [Test]
        public void rigid_transform_rotateZ()
        {
            float angle = 2.3f;
            RigidTransform q = RigidTransform.rotateZ(angle);

            RigidTransform r = RigidTransform(quaternion(0.0f, 0.0f, 0.91276394f, 0.40848744f), float3.zero);
            TestUtils.AreEqual(q, r, 0.0001f);
        }

        static internal quaternion test_q0 = quaternion(0.3270836f, 0.8449658f, -0.1090279f, 0.4088545f);
        static internal quaternion test_q1 = quaternion(-0.05623216f, 0.731018f, -0.6747859f, -0.08434824f);
        static internal quaternion test_q2 = quaternion(-0.2316205f, -0.6022133f, -0.7411857f, -0.1852964f);
        static internal quaternion test_q3 = quaternion(0.3619499f, 0.8352691f, -0.1392115f, 0.3897922f);
        
        [Test]
        public void rigid_transform_inverse()
        {
            RigidTransform q = RigidTransform(quaternion(1.0f, -2.0f, 3.0f, -4.0f), float3(1,2,3));
            RigidTransform iq = inverse(q);
            RigidTransform r = RigidTransform(quaternion(-0.1825742f, 0.3651484f, -0.5477226f, -0.7302967f), float3(2.733333f, 0.6666664f, -2.466666f));

            TestUtils.AreEqual(iq, r, 0.00001f);
        }
        
        [Test]
        public void rigid_transform_mul_vector()
        {
            float4x4 m = TestMatrix.test4x4_xyz;
            RigidTransform q = RigidTransform(m);

            float3 vector = float3(1.1f, -2.2f, 3.5f);

            float4 mvector0 = mul(m, float4(vector, 0));
            float4 qvector0 = mul(q, float4(vector, 0));
            TestUtils.AreEqual(qvector0, mvector0, 0.0001f);

            float4 mvector1 = mul(m, float4(vector, 1));
            float4 qvector1 = mul(q, float4(vector, 1));
            TestUtils.AreEqual(qvector1, mvector1, 0.0001f);
        }
    }
}
