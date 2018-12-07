using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using UnityEngine.Experimental.U2D.Animation;
using UnityEditor;
using UnityEditor.Experimental.U2D.Animation;

namespace UnityEditor.Experimental.U2D.Animation.Test.BoneGizmo
{
    using Object = UnityEngine.Object;

    [TestFixture]
    public class BoneGizmoControllerTests
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

        private QuaternionCompare quatCompare = new QuaternionCompare();
        private Vector3Compare vec3Compare = new Vector3Compare();

        private ISkeletonView m_SkeletonView;
        private IBoneGizmoToggle m_BoneGizmoToggle;
        private IUndo m_Undo;
        private BoneGizmoController m_BoneGizmoController;
        private Sprite m_SkinnedSprite;
        private SpriteSkin m_SpriteSkin;
        private SkeletonAction m_HotAction;
        private int m_HotBoneID;
        private int m_HoveredBoneID;
        private int m_HoveredBodyID;
        private int m_HoveredJointID;
        private int m_HoveredTailID;

        [SetUp]
        public void Setup()
        {
            var go = new GameObject("TestObject");
            m_SkinnedSprite = Resources.Load<Sprite>("bird");
            m_SpriteSkin = go.AddComponent<SpriteSkin>();
            m_SpriteSkin.spriteRenderer.sprite = m_SkinnedSprite;
            m_SpriteSkin.CreateBoneHierarchy();

            m_SkeletonView = Substitute.For<ISkeletonView>();
            m_SkeletonView.hotBoneID.Returns( x => m_HotBoneID );
            m_SkeletonView.hoveredBoneID.Returns( x => m_HoveredBoneID );
            m_SkeletonView.hoveredBodyID.Returns( x => m_HoveredBodyID );
            m_SkeletonView.hoveredJointID.Returns( x => m_HoveredJointID );
            m_SkeletonView.hoveredTailID.Returns( x => m_HoveredTailID );
            m_SkeletonView.IsActionHot(Arg.Any<SkeletonAction>()).Returns(x => m_HotAction == (SkeletonAction)x[0]);
            m_SkeletonView.CanLayout().Returns(x => true);

            m_Undo = Substitute.For<IUndo>();
            m_BoneGizmoToggle = Substitute.For<IBoneGizmoToggle>();
            m_BoneGizmoToggle.enableGizmos.Returns(x => true);

            m_BoneGizmoController = new BoneGizmoController(m_SkeletonView, m_Undo, m_BoneGizmoToggle);

            m_HotBoneID = 0;
            m_HoveredBodyID = 0;
            m_HoveredBoneID = 0;
            m_HoveredJointID = 0;
            m_HoveredTailID = 0;
            m_HotAction = SkeletonAction.None;
        }

        [TearDown]
        public void TearDown()
        {
            Selection.activeGameObject = null;

            foreach (var transform in m_SpriteSkin.boneTransforms)
                transform.parent = null;

            foreach (var transform in m_SpriteSkin.boneTransforms)
                GameObject.DestroyImmediate(transform.gameObject);

            GameObject.DestroyImmediate(m_SpriteSkin.gameObject);
        }

        [Test]
        public void RotateBone_SingleSelection()
        {
            m_HotAction = SkeletonAction.RotateBone;
            m_HotBoneID = m_SpriteSkin.boneTransforms[0].GetInstanceID();

            float deltaAngle;
            m_SkeletonView.DoRotateBone(Arg.Any<Vector3>(), Arg.Any<Vector3>(), out deltaAngle).Returns(x =>
                {
                    x[2] = 90f;
                    return true; 
                });
            
            Selection.activeGameObject = m_SpriteSkin.boneTransforms[0].gameObject;
            m_BoneGizmoController.OnSelectionChanged();
            m_BoneGizmoController.OnGUI();

            Assert.That(m_SpriteSkin.boneTransforms[0].rotation, Is.EqualTo(new Quaternion(0f, 0f, 0.5560408f, 0.8311549f)).Using(quatCompare));
        }

        [Test]
        public void RotateBone_MultipleSelection()
        {
            m_HotAction = SkeletonAction.RotateBone;
            m_HotBoneID = m_SpriteSkin.boneTransforms[0].GetInstanceID();

            float deltaAngle;
            m_SkeletonView.DoRotateBone(Arg.Any<Vector3>(), Arg.Any<Vector3>(), out deltaAngle).Returns(x =>
                {
                    x[2] = 90f;
                    return true; 
                });

            var objectList = new List<Object>();

            foreach(var t in m_SpriteSkin.boneTransforms)
                objectList.Add(t.gameObject);

            Selection.objects = objectList.ToArray();

            m_BoneGizmoController.OnSelectionChanged();
            m_BoneGizmoController.OnGUI();

            Assert.That(m_SpriteSkin.boneTransforms[0].rotation, Is.EqualTo(new Quaternion(0f, 0f, 0.5560408f, 0.8311549f)).Using(quatCompare));
            Assert.That(m_SpriteSkin.boneTransforms[1].rotation, Is.EqualTo(new Quaternion(0f, 0f, 0.9997337f, -0.02307764f)).Using(quatCompare));
        }

        [Test]
        public void RotateBone_MultipleSelection_BonesNotInSamePlane()
        {
            m_SpriteSkin.boneTransforms[1].Rotate(Vector3.up, 45f);

            m_HotAction = SkeletonAction.RotateBone;
            m_HotBoneID = m_SpriteSkin.boneTransforms[0].GetInstanceID();

            float deltaAngle;
            m_SkeletonView.DoRotateBone(Arg.Any<Vector3>(), Arg.Any<Vector3>(), out deltaAngle).Returns(x =>
                {
                    x[2] = 90f;
                    return true; 
                });

            var objectList = new List<Object>();

            foreach(var t in m_SpriteSkin.boneTransforms)
                objectList.Add(t.gameObject);

            Selection.objects = objectList.ToArray();

            m_BoneGizmoController.OnSelectionChanged();
            m_BoneGizmoController.OnGUI();

            Assert.That(m_SpriteSkin.boneTransforms[0].rotation, Is.EqualTo(new Quaternion(0f, 0f, 0.5560408f, 0.8311549f)).Using(quatCompare));
            Assert.That(m_SpriteSkin.boneTransforms[1].rotation, Is.EqualTo(new Quaternion(-0.008831382f, 0.3825816f, 0.9236336f, -0.02132094f)).Using(quatCompare));
        }

        [Test]
        public void MoveBone_SingleSelection()
        {
            Vector3 deltaPosition;
            m_SkeletonView.DoMoveBone(out deltaPosition).Returns(x =>
            {
                x[0] = Vector3.up * 3f;
                return true;
            });

            Selection.activeGameObject = m_SpriteSkin.boneTransforms[0].gameObject;
            m_BoneGizmoController.OnSelectionChanged();
            m_BoneGizmoController.OnGUI();

            Assert.That(m_SpriteSkin.boneTransforms[0].position, Is.EqualTo(new Vector3(-1.219143f, 3.253849f, 0f)).Using(vec3Compare));
        }

        [Test]
        public void MoveBone_MultipleSelection()
        {
            Vector3 deltaPosition;
            m_SkeletonView.DoMoveBone(out deltaPosition).Returns(x =>
            {
                x[0] = Vector3.up * 3f;
                return true;
            });

            m_SpriteSkin.boneTransforms[1].parent = null;

            var objectList = new List<Object>();

            foreach (var t in m_SpriteSkin.boneTransforms)
                objectList.Add(t.gameObject);

            Selection.objects = objectList.ToArray();

            m_BoneGizmoController.OnSelectionChanged();
            m_BoneGizmoController.OnGUI();
            
            Assert.That(m_SpriteSkin.boneTransforms[0].position, Is.EqualTo(new Vector3(-1.219143f, 3.253849f, 0f)).Using(vec3Compare));
            Assert.That(m_SpriteSkin.boneTransforms[1].position, Is.EqualTo(new Vector3(-0.659342f, 3.022707f, 0f)).Using(vec3Compare));
        }

        [Test]
        public void SelectBone_SingleSelection()
        {            
            m_HoveredBoneID = m_SpriteSkin.boneTransforms[0].GetInstanceID();

            int id;
            bool additive;
            m_SkeletonView.DoSelectBone(out id, out additive).Returns(x =>
            {
                x[0] = m_HoveredBoneID;
                x[1] = false;
                return true;
            });

            m_BoneGizmoController.OnSelectionChanged();
            m_BoneGizmoController.OnGUI();

            Assert.IsTrue(Selection.Contains(m_SpriteSkin.boneTransforms[0].gameObject));
            Assert.IsFalse(Selection.Contains(m_SpriteSkin.boneTransforms[1].gameObject));

            m_HoveredBoneID = m_SpriteSkin.boneTransforms[1].GetInstanceID();

            m_BoneGizmoController.OnSelectionChanged();
            m_BoneGizmoController.OnGUI();

            Assert.IsFalse(Selection.Contains(m_SpriteSkin.boneTransforms[0].gameObject));
            Assert.IsTrue(Selection.Contains(m_SpriteSkin.boneTransforms[1].gameObject));
        }

        [Test]
        public void SelectBone_ToggleSelection()
        {
            m_HoveredBoneID = m_SpriteSkin.boneTransforms[0].GetInstanceID();

            int id;
            bool additive;
            m_SkeletonView.DoSelectBone(out id, out additive).Returns(x =>
            {
                x[0] = m_HoveredBoneID;
                x[1] = true;
                return true;
            });

            m_BoneGizmoController.OnSelectionChanged();
            m_BoneGizmoController.OnGUI();

            Assert.IsTrue(Selection.Contains(m_SpriteSkin.boneTransforms[0].gameObject));

            m_BoneGizmoController.OnSelectionChanged();
            m_BoneGizmoController.OnGUI();

            Assert.IsFalse(Selection.Contains(m_SpriteSkin.boneTransforms[0].gameObject));
        }
    }
}