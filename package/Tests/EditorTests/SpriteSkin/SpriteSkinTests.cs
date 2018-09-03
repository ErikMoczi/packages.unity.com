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

    private readonly Vector3[] kDeformedVertices =
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

    private GameObject go1;
    private GameObject go2;

    private Sprite riggedSprite;
    private Sprite staticSprite;

    private Vector3Compare vec3Compare = new Vector3Compare();

    private static string kTestAssetsFolder = "UnityPackageManager/com.unity.2d.animation@1.0.1-experimental/Tests/EditorTests/SpriteSkin/Assets/";
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

    private void TestSimpleDeform(GameObject go, Sprite sprite)
    {
        GameObject rootBone = SpriteBoneUtility.CreateSkeleton(sprite.GetBones(), go, null);
        go.transform.GetChild(0).transform.Rotate(new Vector3(0, 0, 90.0f));
        var deformedVertices = Deform(go, rootBone.transform, sprite);
        for (var i = 0; i < deformedVertices.Length; ++i)
            Assert.That(deformedVertices[i], Is.EqualTo(kDeformedVertices[i]).Using(vec3Compare));
        deformedVertices.Dispose();
    }

    [Test]
    public void UnskinnedSprite_WillNotRebind()
    {
        Assert.IsNull( SpriteBoneUtility.Rebind(go1.transform, staticSprite.GetBones()) );
    }

    [Test]
    public void SkinnedSprite_WillRebind()
    {
        GameObject rootBone = SpriteBoneUtility.CreateSkeleton(riggedSprite.GetBones(), go1, null);
        Assert.IsNotNull(SpriteBoneUtility.Rebind(rootBone.transform, riggedSprite.GetBones()));
    }

    [Test]
    public void SkinnedSprite_VerifySimpleAnimation()
    {
        TestSimpleDeform(go1, riggedSprite);
    }

    [Test]
    public void SkinnedSprite_VerifyMultipleSprites_WithDeform()
    {
        TestSimpleDeform(go1, riggedSprite);
        TestSimpleDeform(go2, riggedSprite);
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

}

}