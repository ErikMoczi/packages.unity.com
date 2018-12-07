using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.Experimental.U2D.Animation.Test.SkinningModuleTests
{
    public class TransformCacheTest
    {
        private class QuaternionCompare : IEqualityComparer<Quaternion>
        {
            public bool Equals(Quaternion a, Quaternion b)
            {
                return Quaternion.Dot(a, b) > 1f - Epsilon;
            }

            public int GetHashCode(Quaternion v)
            {
                return v.GetHashCode();
            }

            private static readonly float Epsilon = 0.001f;
        }

        private class Vector3Compare : IEqualityComparer<Vector3>
        {
            public bool Equals(Vector3 a, Vector3 b)
            {
                return Vector3.Distance(a, b) < Epsilon;
            }

            public int GetHashCode(Vector3 v)
            {
                return v.GetHashCode();
            }

            private static readonly float Epsilon = 0.001f;
        }

        private class MatrixCompare : IEqualityComparer<Matrix4x4>
        {
            public bool Equals(Matrix4x4 a, Matrix4x4 b)
            {
                return Vector4.Distance(new Vector4(a.m00, a.m01, a.m02, a.m03), new Vector4(b.m00, b.m01, b.m02, b.m03)) < Epsilon &&
                        Vector4.Distance(new Vector4(a.m10, a.m11, a.m12, a.m13), new Vector4(b.m10, b.m11, b.m12, b.m13)) < Epsilon &&
                        Vector4.Distance(new Vector4(a.m20, a.m21, a.m22, a.m23), new Vector4(b.m20, b.m21, b.m22, b.m23)) < Epsilon &&
                        Vector4.Distance(new Vector4(a.m30, a.m31, a.m32, a.m33), new Vector4(b.m30, b.m31, b.m32, b.m33)) < Epsilon;
            }

            public int GetHashCode(Matrix4x4 v)
            {
                return v.GetHashCode();
            }

            private static readonly float Epsilon = 0.001f;
        }

        private QuaternionCompare quatCompare = new QuaternionCompare();
        private Vector3Compare vec3Compare = new Vector3Compare();
        private MatrixCompare matrixCompare = new MatrixCompare();

        private TransformCache m_Transform0;
        private TransformCache m_Transform1;
        private TransformCache m_Transform2;

        [SetUp]
        public void Setup()
        {
            m_Transform0 = CacheObject.Create<TransformCache>(null);
            m_Transform1 = CacheObject.Create<TransformCache>(null);
            m_Transform2 = CacheObject.Create<TransformCache>(null);
        }

        [TearDown]
        public void TearDown()
        {
            BaseObject.DestroyImmediate(m_Transform0);
            BaseObject.DestroyImmediate(m_Transform1);
            BaseObject.DestroyImmediate(m_Transform2);
        }

        [Test]
        public void SetParent_TransformHasParent()
        {
            m_Transform1.SetParent(m_Transform0);

            Assert.AreEqual(m_Transform0, m_Transform1.parent, "Parent transform is incorrect");
        }

        [Test]
        public void SetParent_TransformBecomesChild()
        {
            m_Transform1.SetParent(m_Transform0);

            Assert.IsTrue(ArrayUtility.Contains(m_Transform0.children, m_Transform1), "Children should contain the transform");
        }

        [Test]
        public void SetParent_WorldPositionStays_PreservesWorldPosition()
        {
            m_Transform0.position = Vector3.right * 3f;

            m_Transform1.SetParent(m_Transform0, true);

            Assert.That(m_Transform1.position, Is.EqualTo(Vector3.zero).Using(vec3Compare));
        }

        [Test]
        public void SetParent_WorldPositionStays_PreservesWorldRotation()
        {
            m_Transform0.rotation = Quaternion.AngleAxis(45f, Vector3.forward);

            m_Transform1.SetParent(m_Transform0, true);

            Assert.That(m_Transform1.rotation, Is.EqualTo(Quaternion.identity).Using(quatCompare));
        }

        [Test]
        public void SetParent_WorldPositionStaysFalse_PreservesLocalPosition()
        {
            m_Transform0.position = Vector3.right * 3f;

            m_Transform1.SetParent(m_Transform0, false);

            Assert.That(m_Transform1.position, Is.EqualTo(new Vector3(3f, 0f, 0f)).Using(vec3Compare));
            Assert.That(m_Transform1.localPosition, Is.EqualTo(Vector3.zero).Using(vec3Compare));
        }

        [Test]
        public void SetParent_WorldPositionStaysFalse_PreservesLocalRotation()
        {
            var q = Quaternion.AngleAxis(45f, Vector3.forward);
            m_Transform0.rotation = q;

            m_Transform1.SetParent(m_Transform0, false);

            Assert.That(m_Transform1.rotation, Is.EqualTo(q).Using(quatCompare));
            Assert.That(m_Transform1.localRotation, Is.EqualTo(Quaternion.identity).Using(quatCompare));
        }

        [Test]
        public void ParentLocalScale_ScalesChildPosition()
        {
            m_Transform1.SetParent(m_Transform0);
            m_Transform0.localScale = new Vector3(3f, 2f, 1f);
            m_Transform1.localPosition = Vector3.right;

            Assert.That(m_Transform1.position, Is.EqualTo(new Vector3(3f, 0f, 0f)).Using(vec3Compare));
        }

        [Test]
        public void ParentScale_ScalesChildRightDirection()
        {
            var q = Quaternion.AngleAxis(45f, Vector3.forward);

            m_Transform1.SetParent(m_Transform0);
            m_Transform0.localScale = new Vector3(3f, 2f, 1f);
            m_Transform1.localRotation = q;

            Assert.That(m_Transform1.right, Is.EqualTo(new Vector3(0.832f, 0.5547f, 0f)).Using(vec3Compare));
        }

        [Test]
        public void ParentScale_ChildRightIsNormalized()
        {
            var q = Quaternion.AngleAxis(45f, Vector3.forward);

            m_Transform1.SetParent(m_Transform0);
            m_Transform0.localScale = new Vector3(3f, 2f, 1f);
            m_Transform1.localRotation = q;

            Assert.That(m_Transform1.right.magnitude, Is.EqualTo(1f));
        }

        [Test]
        public void ParentScaleZero_ChildRightCantNormalize()
        {
            m_Transform1.SetParent(m_Transform0);
            m_Transform0.localScale = Vector3.zero;

            Assert.That(m_Transform1.right.magnitude, Is.EqualTo(0f));
        }

        [Test]
        public void ParentScale_ScalesChildUpDirection()
        {
            var q = Quaternion.AngleAxis(45f, Vector3.forward);

            m_Transform1.SetParent(m_Transform0);
            m_Transform0.localScale = new Vector3(3f, 2f, 1f);
            m_Transform1.localRotation = q;

            Assert.That(m_Transform1.up, Is.EqualTo(new Vector3(-0.832f, 0.5547f, 0f)).Using(vec3Compare));
        }

        [Test]
        public void ParentScale_ChildUpIsNormalized()
        {
            var q = Quaternion.AngleAxis(45f, Vector3.forward);

            m_Transform1.SetParent(m_Transform0);
            m_Transform0.localScale = new Vector3(3f, 2f, 1f);
            m_Transform1.localRotation = q;

            Assert.That(m_Transform1.up.magnitude, Is.EqualTo(1f));
        }

        [Test]
        public void ParentScaleZero_ChildUpCantNormalize()
        {
            m_Transform1.SetParent(m_Transform0);
            m_Transform0.localScale = Vector3.zero;

            Assert.That(m_Transform1.up.magnitude, Is.EqualTo(0f));
        }


        [Test]
        public void ParentScale_ScalesChildFwdDirection()
        {
            var q = Quaternion.AngleAxis(45f, Vector3.up);

            m_Transform1.SetParent(m_Transform0);
            m_Transform0.localScale = new Vector3(1f, 2f, 3f);
            m_Transform1.localRotation = q;

            Assert.That(m_Transform1.forward, Is.EqualTo(new Vector3(0.31622f, 0, 0.94868f)).Using(vec3Compare));
        }

        [Test]
        public void ParentScale_ChildFwdIsNormalized()
        {
            var q = Quaternion.AngleAxis(45f, Vector3.up);

            m_Transform1.SetParent(m_Transform0);
            m_Transform0.localScale = new Vector3(1f, 2f, 3f);
            m_Transform1.localRotation = q;

            Assert.That(m_Transform1.forward.magnitude, Is.EqualTo(1f));
        }

        [Test]
        public void ParentScaleZero_ChildFwdCantNormalize()
        {
            m_Transform1.SetParent(m_Transform0);
            m_Transform0.localScale = Vector3.zero;

            Assert.That(m_Transform1.forward.magnitude, Is.EqualTo(0f));
        }

        [Test]
        public void SetLocalPosition_UpdatesLocalToWorldMatrix()
        {
            m_Transform0.localPosition = Vector3.right * 3f;

            Assert.That(m_Transform0.localToWorldMatrix, Is.EqualTo(Matrix4x4.TRS(Vector3.right * 3f, Quaternion.identity, Vector3.one)).Using(matrixCompare));
        }

        [Test]
        public void SetPosition_UpdatesLocalToWorldMatrix()
        {
            m_Transform0.position = Vector3.right * 3f;

            Assert.That(m_Transform0.localToWorldMatrix, Is.EqualTo(Matrix4x4.TRS(Vector3.right * 3f, Quaternion.identity, Vector3.one)).Using(matrixCompare));
        }

        [Test]
        public void SetLocalRotation_UpdatesLocalToWorldMatrix()
        {
            var q = Quaternion.AngleAxis(45f, Vector3.forward);
            m_Transform0.localRotation = q;

            Assert.That(m_Transform0.localToWorldMatrix, Is.EqualTo(Matrix4x4.TRS(Vector3.zero, q, Vector3.one)).Using(matrixCompare));
        }

        [Test]
        public void SetRotation_UpdatesLocalToWorldMatrix()
        {
            var q = Quaternion.AngleAxis(45f, Vector3.forward);
            m_Transform0.rotation = q;

            Assert.That(m_Transform0.localToWorldMatrix, Is.EqualTo(Matrix4x4.TRS(Vector3.zero, q, Vector3.one)).Using(matrixCompare));
        }

        [Test]
        public void SetLocalPosition_UpdatesChildLocalToWorldMatrix()
        {
            m_Transform1.SetParent(m_Transform0);
            m_Transform0.localPosition = Vector3.right * 3f;

            Assert.That(m_Transform1.localToWorldMatrix, Is.EqualTo(Matrix4x4.TRS(Vector3.right * 3f, Quaternion.identity, Vector3.one)).Using(matrixCompare));
        }

        [Test]
        public void SetPosition_UpdatesChildLocalToWorldMatrix()
        {
            m_Transform1.SetParent(m_Transform0);
            m_Transform0.position = Vector3.right * 3f;

            Assert.That(m_Transform1.localToWorldMatrix, Is.EqualTo(Matrix4x4.TRS(Vector3.right * 3f, Quaternion.identity, Vector3.one)).Using(matrixCompare));
        }

        [Test]
        public void SetLocalRotation_UpdatesChildLocalToWorldMatrix()
        {
            var q = Quaternion.AngleAxis(45f, Vector3.forward);
            m_Transform1.SetParent(m_Transform0);
            m_Transform0.localRotation = q;

            Assert.That(m_Transform1.localToWorldMatrix, Is.EqualTo(Matrix4x4.TRS(Vector3.zero, q, Vector3.one)).Using(matrixCompare));
        }

        [Test]
        public void SetRotation_UpdatesChildLocalToWorldMatrix()
        {
            var q = Quaternion.AngleAxis(45f, Vector3.forward);
            m_Transform1.SetParent(m_Transform0);
            m_Transform0.rotation = q;

            Assert.That(m_Transform1.localToWorldMatrix, Is.EqualTo(Matrix4x4.TRS(Vector3.zero, q, Vector3.one)).Using(matrixCompare));
        }

        [Test]
        public void SetRight_UpdatesRotation()
        {
            m_Transform0.right = Vector2.one;

            Assert.That(m_Transform0.rotation.eulerAngles, Is.EqualTo(new Vector3(0f, 0f, 45f)).Using(vec3Compare));
        }

        [Test]
        public void SetRight_UpdatesChildRotation()
        {
            m_Transform1.SetParent(m_Transform0);
            m_Transform0.right = Vector2.one;

            Assert.That(m_Transform1.rotation.eulerAngles, Is.EqualTo(new Vector3(0f, 0f, 45f)).Using(vec3Compare));
        }

        [Test]
        public void SetUp_UpdatesRotation()
        {
            m_Transform0.up = Vector2.one;

            Assert.That(m_Transform0.rotation.eulerAngles, Is.EqualTo(new Vector3(0f, 0f, 315f)).Using(vec3Compare));
        }

        [Test]
        public void SetUp_UpdatesChildRotation()
        {
            m_Transform1.SetParent(m_Transform0);
            m_Transform0.up = Vector2.one;

            Assert.That(m_Transform1.rotation.eulerAngles, Is.EqualTo(new Vector3(0f, 0f, 315f)).Using(vec3Compare));
        }

        [Test]
        public void SetFwd_UpdatesRotation()
        {
            m_Transform0.forward = Vector2.one;

            Assert.That(m_Transform0.rotation.eulerAngles, Is.EqualTo(new Vector3(315f, 90f, 315f)).Using(vec3Compare));
        }

        [Test]
        public void SetFwd_UpdatesChildRotation()
        {
            m_Transform1.SetParent(m_Transform0);
            m_Transform0.forward = Vector2.one;

            Assert.That(m_Transform1.rotation.eulerAngles, Is.EqualTo(new Vector3(315f, 90f, 315f)).Using(vec3Compare));
        }

        [Test]
        public void IsDescendant_WithNullAncestor_IsFalse()
        {
            m_Transform1.SetParent(m_Transform0);

            TransformCache ancestor = null;

            Assert.IsFalse(m_Transform0.IsDescendant(ancestor), "Null ancestor should return false");
        }

        [Test]
        public void IsDescendant_WithSameTransformAsAncestor_IsFalse()
        {
            m_Transform1.SetParent(m_Transform0);

            Assert.IsFalse(m_Transform0.IsDescendant(m_Transform0), "Transform can't be descendant of itself");
        }

        [Test]
        public void IsDescendant_WithDirectParentAsAncestor_IsTrue()
        {
            m_Transform1.SetParent(m_Transform0);

            Assert.IsTrue(m_Transform1.IsDescendant(m_Transform0), "Direct parent should return true");
        }

        [Test]
        public void IsDescendant_WithGrandParentAsAncestor_IsTrue()
        {
            m_Transform1.SetParent(m_Transform0);
            m_Transform2.SetParent(m_Transform1);

            Assert.IsTrue(m_Transform2.IsDescendant(m_Transform0), "Grand parent should return true");
        }
    }
}
