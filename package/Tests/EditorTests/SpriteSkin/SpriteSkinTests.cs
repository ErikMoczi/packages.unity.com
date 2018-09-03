using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine.U2D;
using UnityEngine.Experimental.U2D;
using UnityEngine.Experimental.U2D.Animation;
using UnityEngine.Experimental.U2D.Common;
using Unity.Jobs;

namespace UnityEditor.Experimental.U2D.Animation.Test.SpriteSkin
{
    public class SpriteSkinTests
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

        private readonly Vector3[] kRootRotateDeformedVertices =
        {
            new Vector3( -1.010294f, 2.572992f, 0f ),
            new Vector3( -1.030294f, 2.752992f, 0f ),
            new Vector3( -1.090294f, 2.752992f, 0f ),
            new Vector3( -1.360294f, 2.202992f, 0f ),
            new Vector3( -1.540294f, 2.112992f, 0f ),
            new Vector3( -1.740294f, 1.982992f, 0f ),
            new Vector3( -1.750294f, 1.952992f, 0f ),
            new Vector3( -1.750294f, 1.792992f, 0f ),
            new Vector3( -1.680294f, 1.802992f, 0f ),
            new Vector3( -1.690294f, 1.592992f, 0f ),
            new Vector3( -1.650294f, 1.572992f, 0f ),
            new Vector3( -1.630294f, 1.572992f, 0f ),
            new Vector3( -1.580294f, 1.652992f, 0f ),
            new Vector3( -1.580294f, 1.282992f, 0f ),
            new Vector3( -1.750294f, 1.112992f, 0f ),
            new Vector3( -1.750294f, 1.042992f, 0f ),
            new Vector3( -1.700294f, 0.972992f, 0f ),
            new Vector3( -1.180294f, 0.802992f, 0f ),
            new Vector3( -1.220294f, 0.5829921f, 0f ),
            new Vector3( -1.550294f, 0.382992f, 0f ),
            new Vector3( -1.550294f, 0.3429921f, 0f ),
            new Vector3( -1.220294f, 0.362992f, 0f ),
            new Vector3( -1.310294f, 0.2429921f, 0f ),
            new Vector3( -1.340294f, 0.1929921f, 0f ),
            new Vector3( -1.050294f, 0.1929921f, 0f ),
            new Vector3( -0.970294f, 0.2029921f, 0f ),
            new Vector3( -0.750294f, 0.472992f, 0f ),
            new Vector3( -0.7502939f, 0.822992f, 0f ),
            new Vector3( -0.5502939f, 0.992992f, 0f ),
            new Vector3( -0.450294f, 1.222992f, 0f ),
            new Vector3( -0.4502939f, 1.682992f, 0f ),
            new Vector3( -0.530294f, 1.932992f, 0f ),
            new Vector3( -0.6202939f, 2.072992f, 0f ),
            new Vector3( -0.9102941f, 2.462992f, 0f )
        };

        private readonly Vector3[] kChildRotateDeformedVertices =
        {
            new Vector3( -0.6816357f, 1.782048f, 0f),
            new Vector3( -0.7016357f, 1.962048f, 0f),
            new Vector3( -0.7616357f, 1.962048f, 0f),
            new Vector3( -1.031636f, 1.412048f, 0f),
            new Vector3( -1.211636f, 1.322048f, 0f),
            new Vector3( -1.411636f, 1.192048f, 0f),
            new Vector3( -1.421636f, 1.162048f, 0f),
            new Vector3( -1.421636f, 1.002048f, 0f),
            new Vector3( -1.351636f, 1.012048f, 0f),
            new Vector3( -1.361636f, 0.8020483f, 0f),
            new Vector3( -1.321636f, 0.7820483f, 0f),
            new Vector3( -1.301636f, 0.7820483f, 0f),
            new Vector3( -1.251636f, 0.8620483f, 0f),
            new Vector3( -1.251636f, 0.4920483f, 0f),
            new Vector3( -1.421636f, 0.3220483f, 0f),
            new Vector3( -1.421636f, 0.2520483f, 0f),
            new Vector3( -1.371636f, 0.1820483f, 0f),
            new Vector3( -0.84188f, 0.02294878f, 0f),
            new Vector3( -0.890622f, 0.07894892f, 0f),
            new Vector3( -1.09f, 0.585f, 0f),
            new Vector3( -1.13f, 0.585f, 0f),
            new Vector3( -1.097388f, 0.2155563f, 0f),
            new Vector3( -1.23f, 0.345f, 0f),
            new Vector3( -1.28f, 0.375f, 0f),
            new Vector3( -1.228463f, 0.02196335f, 0f),
            new Vector3( -1.178291f, -0.08154059f, 0f),
            new Vector3( -0.6511877f, -0.2770902f, 0f),
            new Vector3( -0.4241534f, 0.02932463f, 0f),
            new Vector3( -0.2216356f, 0.2020484f, 0f),
            new Vector3( -0.1216356f, 0.4320484f, 0f),
            new Vector3( -0.1216356f, 0.8920484f, 0f),
            new Vector3( -0.2016356f, 1.142048f, 0f),
            new Vector3( -0.2916357f, 1.282048f, 0f),
            new Vector3( -0.5816357f, 1.672048f, 0f)
        };

        private GameObject go1;
        private GameObject go2;

        private Sprite riggedSprite;
        private Sprite staticSprite;

        private Vector3Compare vec3Compare = new Vector3Compare();

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
            CloneSpriteForTest("star.png");
        }

        private static void CloneSpriteForTest(string filename)
        {
            ValidateDirectory(kTestTempFolder);
            // var filename = Path.GetFileName(path);

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
            go1 = new GameObject("TestObject1");
            go2 = new GameObject("TestObject2");

            riggedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Temp/bird.png");
            staticSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Temp/star.png");
        }

        [TearDown]
        public void Teardown()
        {
            GameObject.DestroyImmediate(go2);
            GameObject.DestroyImmediate(go1);
        }

        private NativeArray<Vector3> Deform(GameObject go, Transform rootBone, Sprite sprite)
        {
            var deformedVertices = new NativeArray<Vector3>(sprite.GetVertexCount(), Allocator.Persistent);
            Transform[] transforms = SpriteBoneUtility.Rebind(rootBone.transform, sprite.GetBones());
            var handle = SpriteBoneUtility.Deform(sprite, deformedVertices, go.transform.worldToLocalMatrix, transforms);
            handle.Complete();
            return deformedVertices;
        }

        private Bounds DeformWithAABB(GameObject go, Transform rootBone, Sprite sprite)
        {
            var minMax = new NativeArray<Vector3>(2, Allocator.Temp);
            var deformedVertices = new NativeArray<Vector3>(sprite.GetVertexCount(), Allocator.Persistent);
            Transform[] transforms = SpriteBoneUtility.Rebind(rootBone.transform, sprite.GetBones());
            var deformJobHandle = SpriteBoneUtility.Deform(sprite, deformedVertices, go.transform.worldToLocalMatrix, transforms);
            var boundsHandle = SpriteBoneUtility.CalculateBounds(deformedVertices, minMax, deformJobHandle);
            boundsHandle.Complete();

            Bounds bounds = new Bounds();
            bounds.SetMinMax(minMax[0], minMax[1]);
            deformedVertices.Dispose();
            minMax.Dispose();
            return bounds;
        }

        private void TestRootRotateDeform(GameObject go, Sprite sprite)
        {
            GameObject rootBone = SpriteBoneUtility.CreateSkeleton(sprite.GetBones(), go, null);
            go.transform.GetChild(0).transform.Rotate(new Vector3(0, 0, 90.0f));
            var deformedVertices = Deform(go, rootBone.transform, sprite);
            for (var i = 0; i < deformedVertices.Length; ++i)
                Assert.That(deformedVertices[i], Is.EqualTo(kRootRotateDeformedVertices[i]).Using(vec3Compare));
            deformedVertices.Dispose();
        }

        private void TestChildRotateDeform(GameObject go, Sprite sprite)
        {
            GameObject rootBone = SpriteBoneUtility.CreateSkeleton(sprite.GetBones(), go, null);
            go.transform.GetChild(0).GetChild(0).transform.Rotate(new Vector3(0, 0, 90.0f));
            var deformedVertices = Deform(go, rootBone.transform, sprite);
            for (var i = 0; i < deformedVertices.Length; ++i)
                Assert.That(deformedVertices[i], Is.EqualTo(kChildRotateDeformedVertices[i]).Using(vec3Compare));
            deformedVertices.Dispose();
        }

        private void TestAABBOnDeformAndReset(GameObject go, Sprite sprite)
        {
            GameObject rootBone = SpriteBoneUtility.CreateSkeleton(sprite.GetBones(), go, null);
            Transform[] transforms = SpriteBoneUtility.Rebind(rootBone.transform, sprite.GetBones());
            int bindPoseHash = SpriteBoneUtility.BoneTransformsHash(transforms);
            var staticBounds = DeformWithAABB(go, rootBone.transform, sprite);

            go.transform.GetChild(0).GetChild(0).transform.Rotate(new Vector3(0, 0, 90.0f));
            var dynamicBounds = DeformWithAABB(go, rootBone.transform, sprite);
            int animatedHash = SpriteBoneUtility.BoneTransformsHash(transforms);
            Assert.That(dynamicBounds.center, !Is.EqualTo(staticBounds.center).Using(vec3Compare));
            Assert.That(dynamicBounds.center, !Is.EqualTo(staticBounds.extents).Using(vec3Compare));
            Assert.That(bindPoseHash, !Is.EqualTo(animatedHash));

            SpriteBoneUtility.ResetBindPose(sprite.GetBones(), transforms);
            var staticBoundsAfterReset = DeformWithAABB(go, rootBone.transform, sprite);
            int bindPoseHashAfterReset = SpriteBoneUtility.BoneTransformsHash(transforms);
            Assert.That(staticBoundsAfterReset.center, Is.EqualTo(staticBounds.center).Using(vec3Compare));
            Assert.That(staticBoundsAfterReset.extents, Is.EqualTo(staticBounds.extents).Using(vec3Compare));
            Assert.That(bindPoseHash, Is.EqualTo(bindPoseHashAfterReset));
        }

        [Test]
        public void UnskinnedSprite_WillNotRebind()
        {
            Assert.IsNull(SpriteBoneUtility.Rebind(go1.transform, staticSprite.GetBones()));
        }

        [Test]
        public void SkinnedSprite_WillRebind()
        {
            GameObject rootBone = SpriteBoneUtility.CreateSkeleton(riggedSprite.GetBones(), go1, null);
            Assert.IsNotNull(SpriteBoneUtility.Rebind(rootBone.transform, riggedSprite.GetBones()));
        }

        [Test]
        public void SkinnedSprite_VerifyRootRotateAnimation()
        {
            TestRootRotateDeform(go1, riggedSprite);
        }

        [Test]
        public void SkinnedSprite_VerifyChildRotateAnimation()
        {
            TestChildRotateDeform(go1, riggedSprite);
        }

        [Test]
        public void SkinnedSprite_VerifyMultipleRootRotate_WithDeform()
        {
            TestRootRotateDeform(go1, riggedSprite);
            TestRootRotateDeform(go2, riggedSprite);
        }

        [Test]
        public void SkinnedSprite_VerifyMultipleChildRotate_WithDeform()
        {
            TestChildRotateDeform(go1, riggedSprite);
            TestChildRotateDeform(go2, riggedSprite);
        }

        [Test]
        public void SkinnedSprite_VerifyAABB()
        {
            TestAABBOnDeformAndReset(go1, riggedSprite);
            TestAABBOnDeformAndReset(go2, riggedSprite);
        }

        [Test]
        public void SkinnedSprite_VerifyBindPoseRetainsVertexData()
        {
            GameObject rootBone = SpriteBoneUtility.CreateSkeleton(riggedSprite.GetBones(), go1, null);
            SpriteBoneUtility.Rebind(rootBone.transform, riggedSprite.GetBones());
            var expectedVertices = SpriteDataAccessExtensions.GetVertexAttribute<Vector3>(riggedSprite, UnityEngine.Experimental.Rendering.VertexAttribute.Position);
            var deformedVertices = Deform(go1, rootBone.transform, riggedSprite);

            for (var i = 0; i < deformedVertices.Length; ++i)
                Assert.That(deformedVertices[i], Is.EqualTo(expectedVertices[i]).Using(vec3Compare));
            deformedVertices.Dispose();
        }

        [Test]
        public void SkinnedSprite_ResetBindPose()
        {
            List<Vector3> positions = new List<Vector3>();
            GameObject rootBone = SpriteBoneUtility.CreateSkeleton(riggedSprite.GetBones(), go1, null);
            Transform[] transforms = SpriteBoneUtility.Rebind(rootBone.transform, riggedSprite.GetBones());

            foreach (var transform in transforms)
                positions.Add(transform.position);

            // Simple Deform. 
            go1.transform.GetChild(0).transform.Translate(new Vector3(0, 1.0f, 0));
            go1.transform.GetChild(0).transform.Rotate(new Vector3(0, 0, 90.0f));
            var deformedVertices = Deform(go1, rootBone.transform, riggedSprite);
            for (int i = 0; i < transforms.Length; ++i)
                Assert.That(transforms[i].position, !Is.EqualTo(positions[i]).Using(vec3Compare));

            // Reset BindPose. 
            SpriteBoneUtility.ResetBindPose(riggedSprite.GetBones(), transforms);
            for (int i = 0; i < transforms.Length; ++i)
                Assert.That(transforms[i].position, Is.EqualTo(positions[i]).Using(vec3Compare));
            deformedVertices.Dispose();

        }

    }

}