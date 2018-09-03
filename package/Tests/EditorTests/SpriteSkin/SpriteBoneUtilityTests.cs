using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine.U2D;
using UnityEngine.Experimental.U2D;
using UnityEngine.Experimental.U2D.Animation;

namespace UnityEditor.Experimental.U2D.Animation.Test.SpriteBoneUtilityTests
{
    public class SpriteBoneUtilityTests
    {
        private class Vector3Compare : IEqualityComparer<Vector3>
        {
            public bool Equals(Vector3 a, Vector3 b)
            {
                return Vector3.Distance(a, b) < Epsilon;
            }
            public int GetHashCode(Vector3 v)
            {
                return Mathf.RoundToInt(v.x) ^ Mathf.RoundToInt(v.y) ^ Mathf.RoundToInt(v.z);
            }
            private static readonly float Epsilon = 0.001f;
        }

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

        private static SceneView s_SceneView;
        private GameObject go;
        private Sprite riggedSprite;

        private Vector3Compare vec3Compare = new Vector3Compare();
        private QuaternionCompare quatCompare = new QuaternionCompare();

        private static string kTestAssetsFolder = "Packages/com.unity.2d.animation/Tests/EditorTests/SpriteSkin/Assets/";
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
            go = new GameObject("TestObject", typeof(SpriteRenderer));
            go.transform.position = Vector3.zero;

            riggedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Temp/bird.png");
            go.GetComponent<SpriteRenderer>().sprite = riggedSprite;
            go.AddComponent<UnityEngine.Experimental.U2D.Animation.SpriteSkin>();
        }

        [TearDown]
        public void Teardown()
        {
            GameObject.DestroyImmediate(go);
        }

        [Test]
        public void CreateSkeleton_CreatesValidSkeletonFromBones()
        {
            var bones = riggedSprite.GetBones();
            var rootBoneGO = SpriteBoneUtility.CreateSkeleton(bones, go, null);
            go.GetComponent<UnityEngine.Experimental.U2D.Animation.SpriteSkin>().rootBone = rootBoneGO.transform;

            Assert.AreEqual(1, go.transform.childCount);
            var transforms = go.transform.GetComponentsInChildren<Transform>();
            Assert.AreEqual(4, transforms.Length); // GameObject + 3 Bones

            Assert.AreEqual("TestObject", transforms[0].gameObject.name);
            Assert.AreEqual("root", transforms[1].gameObject.name);
            Assert.That(new Vector3(-1.219143f, 0.253849f, 0.0f), Is.EqualTo(transforms[1].position).Using(vec3Compare));
            Assert.That(new Quaternion(0f, 0f, -0.1945351f, 0.9808956f), Is.EqualTo(transforms[1].rotation).Using(quatCompare));
            Assert.AreEqual("bone_HGS", transforms[2].gameObject.name);
            Assert.That(new Vector3(-0.659342f, 0.0227064f, 0f), Is.EqualTo(transforms[2].position).Using(vec3Compare));
            Assert.That(new Quaternion(0f, 0f, 0.01651449f, 0.9998637f), Is.EqualTo(transforms[2].rotation).Using(quatCompare));
            Assert.AreEqual("bone_HGS_KTR", transforms[3].gameObject.name);
            Assert.That(new Vector3(0.7444712f, 0.08755364f, 0.0f), Is.EqualTo(transforms[3].position).Using(vec3Compare));
            Assert.That(new Quaternion(0f, 0f, 0.003135923f, 0.9999951f), Is.EqualTo(transforms[3].rotation).Using(quatCompare));
        }
    }
}
