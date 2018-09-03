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

        private IBoneGizmoView m_BoneGizmoView;
        private IBoneGizmoToggle m_BoneGizmoToggle;
        private IUndoObject m_UndoObject;
        private BoneGizmoController m_BoneGizmoController;
        private Sprite m_SkinnedSprite;
        private SpriteSkin m_SpriteSkin;

        private static string kTestAssetsFolder = "Packages/com.unity.2d.animation/Tests/Editor/SpriteSkin/Assets/";
        private static string kTestTempFolder = "Assets/Temp/";

        [OneTimeTearDown]
        public void FullTeardown()
        {
            // Delete cloned sprites
            AssetDatabase.DeleteAsset(Path.GetDirectoryName(kTestTempFolder));
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            CloneSpriteForTest("bird.png");
        }

        private static void CloneSpriteForTest(string filename)
        {
            ValidateDirectory(kTestTempFolder);

            File.Copy(kTestAssetsFolder + filename, kTestTempFolder + filename);
            File.Copy(kTestAssetsFolder + filename + ".meta", kTestTempFolder + filename + ".meta");

            AssetDatabase.Refresh();
        }

        private static void ValidateDirectory(string path)
        {
            var dirPath = Path.GetDirectoryName(path);

            if (Directory.Exists(dirPath) == false)
                Directory.CreateDirectory(dirPath);
        }


        [SetUp]
        public void Setup()
        {
            var go = new GameObject("TestObject");
            m_SkinnedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Temp/bird.png");
            m_SpriteSkin = go.AddComponent<SpriteSkin>();
            m_SpriteSkin.spriteRenderer.sprite = m_SkinnedSprite;
            m_SpriteSkin.CreateBoneHierarchy();

            m_BoneGizmoView = Substitute.For<IBoneGizmoView>();
            m_BoneGizmoView.IsBoneVisible(Arg.Any<Transform>(), Arg.Any<float>(), Arg.Any<float>()).Returns(x => { return true; });
            m_BoneGizmoView.IsActionHot(BoneGizmoAction.None).Returns(x => { return true; });
            m_BoneGizmoView.CanLayout().Returns(x => { return true; });

            m_UndoObject = Substitute.For<IUndoObject>();
            m_BoneGizmoToggle = Substitute.For<IBoneGizmoToggle>();
            m_BoneGizmoToggle.enableGizmos.Returns(x => { return true; });

            m_BoneGizmoController = new BoneGizmoController(m_BoneGizmoView, m_UndoObject, m_BoneGizmoToggle);
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
            float deltaAngle;
            m_BoneGizmoView.DoBoneRotation(m_SpriteSkin.boneTransforms[0], out deltaAngle).Returns(x =>
                {
                    x[1] = 90f;
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
            float deltaAngle;
            m_BoneGizmoView.DoBoneRotation(m_SpriteSkin.boneTransforms[1], out deltaAngle).Returns(x =>
            {
                x[1] = 90f;
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

            float deltaAngle;
            m_BoneGizmoView.DoBoneRotation(m_SpriteSkin.boneTransforms[1], out deltaAngle).Returns(x =>
            {
                x[1] = 90f;
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
            m_BoneGizmoView.DoBonePosition(m_SpriteSkin.boneTransforms[0], out deltaPosition).Returns(x =>
            {
                x[1] = Vector3.up * 3f;
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
            m_BoneGizmoView.DoBonePosition(m_SpriteSkin.boneTransforms[0], out deltaPosition).Returns(x =>
            {
                x[1] = Vector3.up * 3f;
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
            BoneGizmoSelectionMode selectionMode;
            m_BoneGizmoView.DoSelection(m_SpriteSkin.boneTransforms[0], out selectionMode).Returns(x =>
            {
                x[1] = BoneGizmoSelectionMode.Single;
                return true;
            });
            m_BoneGizmoView.DoSelection(m_SpriteSkin.boneTransforms[1], out selectionMode).Returns(x =>
            {
                x[1] = BoneGizmoSelectionMode.Single;
                return false;
            });

            m_BoneGizmoController.OnSelectionChanged();
            m_BoneGizmoController.OnGUI();

            Assert.IsTrue(Selection.Contains(m_SpriteSkin.boneTransforms[0].gameObject));
            Assert.IsFalse(Selection.Contains(m_SpriteSkin.boneTransforms[1].gameObject));

            m_BoneGizmoView.DoSelection(m_SpriteSkin.boneTransforms[0], out selectionMode).Returns(x =>
            {
                x[1] = BoneGizmoSelectionMode.Single;
                return false;
            });
            m_BoneGizmoView.DoSelection(m_SpriteSkin.boneTransforms[1], out selectionMode).Returns(x =>
            {
                x[1] = BoneGizmoSelectionMode.Single;
                return true;
            });

            m_BoneGizmoController.OnSelectionChanged();
            m_BoneGizmoController.OnGUI();

            Assert.IsFalse(Selection.Contains(m_SpriteSkin.boneTransforms[0].gameObject));
            Assert.IsTrue(Selection.Contains(m_SpriteSkin.boneTransforms[1].gameObject));
        }

        [Test]
        public void SelectBone_ToggleSelection()
        {
            BoneGizmoSelectionMode selectionMode;
            m_BoneGizmoView.DoSelection(m_SpriteSkin.boneTransforms[0], out selectionMode).Returns(x =>
            {
                x[1] = BoneGizmoSelectionMode.Toggle;
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
