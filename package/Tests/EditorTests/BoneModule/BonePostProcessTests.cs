using System.IO;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Experimental.U2D;

using NUnit.Framework;

namespace UnityEditor.Experimental.U2D.Animation.Test.Bone
{
    [TestFixture]
    public class BonePostProcessTests
    {
        private static string kTestAssetsFolder = "Packages/com.unity.2d.animation/Tests/EditorTests/BoneModule/Assets/";
        private static string kTestTempFolder = "Assets/Temp/";
        
        [OneTimeTearDown]
        public void FullTeardown()
        {
            // Delete cloned sprites
            AssetDatabase.DeleteAsset(Path.GetDirectoryName(kTestTempFolder));
        }

        private static void ValidateDirectory(string path)
        {
            var dirPath = Path.GetDirectoryName(path);

            if (Directory.Exists(dirPath) == false)
                Directory.CreateDirectory(dirPath);
        }

        private static void CloneSpriteForTest(string path)
        {
            var filename = Path.GetFileName(path);

            File.Copy(kTestAssetsFolder + filename, path);
            File.Copy(kTestAssetsFolder + filename + ".meta", path + ".meta");

            AssetDatabase.Refresh();
        }
        
        private static Vector3 GetPositionInWorld(Vector3 positionInMesh, Vector3 pivot, float ppu)
        {
            return (positionInMesh - pivot) / ppu;
        }

        private static Matrix4x4 CalculateBindPose(Vector3 position, Quaternion rotation)
        {
            var m = Matrix4x4.identity;
            m = Matrix4x4.Rotate(Quaternion.Inverse(rotation));
            m = m * Matrix4x4.Translate(-position);

            return m;
        }

        private static void VerifyApproximatedSpriteBones(SpriteBone[] expected, SpriteBone[] actual)
        {
            const double kLooseEqual = 0.01;
            Assert.AreEqual(expected.Length, actual.Length);
            for (var i = 0; i < expected.Length; ++i)
            {
                var expectedBone = expected[i];
                var actualBone = actual[i];

                Assert.AreEqual(expectedBone.name, actualBone.name, "Name not matched at #{0}", i);
                Assert.AreEqual(expectedBone.parentId, actualBone.parentId, "ParentId not matched at #{0}", i);
                Assert.AreEqual(expectedBone.length, actualBone.length, kLooseEqual, "Length not matched at #{0}", i);
                Assert.AreEqual(expectedBone.position.x, actualBone.position.x, kLooseEqual, "Position X not matched at #{0}", i);
                Assert.AreEqual(expectedBone.position.y, actualBone.position.y, kLooseEqual, "Position Y not matched at #{0}", i);
                Assert.AreEqual(expectedBone.position.z, actualBone.position.z, kLooseEqual, "Position Z not matched at #{0}", i);

                var expectedEuler = expectedBone.rotation.eulerAngles;
                var actualEuler = actualBone.rotation.eulerAngles;
                Assert.AreEqual(expectedEuler.x, actualEuler.x, kLooseEqual, "Rotation X not matched at #{0}", i);
                Assert.AreEqual(expectedEuler.y, actualEuler.y, kLooseEqual, "Rotation Y not matched at #{0}", i);
                Assert.AreEqual(expectedEuler.z, actualEuler.z, kLooseEqual, "Rotation Z not matched at #{0}", i);
            }
        }

        private static void VerifyApproximatedBindPoses(Matrix4x4[] expected, Matrix4x4[] actual)
        {
            Assert.AreEqual(expected.Length, actual.Length);
            for (var i = 0; i < expected.Length; ++i)
                Assert.IsTrue(expected[i] == actual[i]);
        }
        
        private static IEnumerable<TestCaseData> SpriteBoneCases()
        {
            var testname = "nobone_sp";
            var filename = "nobone.png";
            var expectedRuntimeBones = new SpriteBone[0];
            yield return new TestCaseData(testname, filename, expectedRuntimeBones);
            
            testname = "3bone_sp";
            filename = "3bone.png";
            var ppu = 100;
            var pivotPointInPixels = new Vector3(16.0f, 16.0f);

            expectedRuntimeBones = new SpriteBone[3]
            {
                new SpriteBone()
                {
                    name = "root",
                    parentId = -1,
                    position = (Vector3.zero - pivotPointInPixels) / ppu,
                    rotation = Quaternion.identity,
                    length = 1.0f / ppu
                },
                new SpriteBone()
                {
                    name = "child1",
                    parentId = 0,
                    position = (Vector3.right - pivotPointInPixels) / ppu,
                    rotation = Quaternion.Euler(0.0f, 0.0f, 90.0f),
                    length = 0.5f / ppu
                },
                new SpriteBone()
                {
                    name = "child2",
                    parentId = 1,
                    position = (Vector3.one - pivotPointInPixels) / ppu,
                    rotation = Quaternion.Euler(0.0f, 0.0f, 45.0f),
                    length = 1.5f / ppu
                }
            };

            yield return new TestCaseData(testname, filename, expectedRuntimeBones);
            
            testname = "complex_sp";
            filename = "complex.png";
            ppu = 100;
            pivotPointInPixels = new Vector3(16.0f, 16.0f);

            expectedRuntimeBones = new SpriteBone[5]
            {
                new SpriteBone()
                {
                    name = "root",
                    parentId = -1,
                    position = (Vector3.zero - pivotPointInPixels) / ppu,
                    rotation = Quaternion.identity,
                    length = 1.0f / ppu
                },
                new SpriteBone()
                {
                    name = "child1",
                    parentId = 0,
                    position = (Vector3.right - pivotPointInPixels) / ppu,
                    rotation = Quaternion.Euler(0.0f, 0.0f, 90.0f),
                    length = 0.5f / ppu
                },
                new SpriteBone()
                {
                    name = "child2",
                    parentId = 1,
                    position = (Vector3.one - pivotPointInPixels) / ppu,
                    rotation = Quaternion.Euler(0.0f, 0.0f, 45.0f),
                    length = 1.5f / ppu
                },
                new SpriteBone()
                {
                    name = "child3",
                    parentId = 0,
                    position = (Vector3.up - pivotPointInPixels) / ppu,
                    rotation = Quaternion.Euler(0.0f, 0.0f, 275.0f),
                    length = 2.5f / ppu
                },
                new SpriteBone()
                {
                    name = "child4",
                    parentId = 3,
                    position = ((Vector3.one * 2) - pivotPointInPixels) / ppu,
                    rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f),
                    length = 1.0f / ppu
                }
            };

            yield return new TestCaseData(testname, filename, expectedRuntimeBones);
        }

        private static IEnumerable<TestCaseData> BindPoseCases()
        {
            var testname = "nobone_bp";
            var filename = "nobone.png";
            var expectedBindPoses = new Matrix4x4[0];
            yield return new TestCaseData(testname, filename, expectedBindPoses);
            
            testname = "3bone_bp";
            filename = "3bone.png";
            var ppu = 100;
            var pivotPointInPixels = new Vector3(16.0f, 16.0f);
    
            expectedBindPoses = new Matrix4x4[3]
            {
                CalculateBindPose(GetPositionInWorld(Vector3.zero, pivotPointInPixels, ppu), Quaternion.identity),
                CalculateBindPose(GetPositionInWorld(Vector3.right, pivotPointInPixels, ppu), Quaternion.Euler(0.0f, 0.0f, 90.0f)),
                CalculateBindPose(GetPositionInWorld(Vector3.one, pivotPointInPixels, ppu), Quaternion.Euler(0.0f, 0.0f, 45.0f))
            };
            yield return new TestCaseData(testname, filename, expectedBindPoses);
            
            testname = "complex_bp";
            filename = "complex.png";
            ppu = 100;
            pivotPointInPixels = new Vector3(16.0f, 16.0f);

            expectedBindPoses = new Matrix4x4[5]
            {
                CalculateBindPose(GetPositionInWorld(Vector3.zero, pivotPointInPixels, ppu), Quaternion.identity),
                CalculateBindPose(GetPositionInWorld(Vector3.right, pivotPointInPixels, ppu), Quaternion.Euler(0.0f, 0.0f, 90.0f)),
                CalculateBindPose(GetPositionInWorld(Vector3.one, pivotPointInPixels, ppu), Quaternion.Euler(0.0f, 0.0f, 45.0f)),
                CalculateBindPose(GetPositionInWorld(Vector3.up, pivotPointInPixels, ppu), Quaternion.Euler(0.0f, 0.0f, 275.0f)),
                CalculateBindPose(GetPositionInWorld(Vector3.one * 2, pivotPointInPixels, ppu), Quaternion.Euler(0.0f, 0.0f, 0.0f))
            };
            yield return new TestCaseData(testname, filename, expectedBindPoses);
        }
        
        [Test, TestCaseSource("SpriteBoneCases")]
        public void PostProcessProducedRuntimeSpriteBones(string testname, string filename, SpriteBone[] expectedRuntimeBones)
        {
            string tempPath = kTestTempFolder + testname + "/";
            string spritePath = tempPath + filename;

            ValidateDirectory(tempPath);
            CloneSpriteForTest(spritePath);

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

            var runtimeBones = sprite.GetBones();
            VerifyApproximatedSpriteBones(expectedRuntimeBones, runtimeBones);
        }

        [Test, TestCaseSource("BindPoseCases")]
        public void PostProcessProducedBindPoses(string testname, string filename, Matrix4x4[] expectedBindPoses)
        {
            string tempPath = kTestTempFolder + testname + "/";
            string spritePath = tempPath + filename;

            ValidateDirectory(tempPath);
            CloneSpriteForTest(spritePath);

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

            var bp = sprite.GetBindPoses();
            VerifyApproximatedBindPoses(expectedBindPoses, bp.ToArray());
        }
    }
}
