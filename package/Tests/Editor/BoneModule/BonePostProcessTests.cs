using System.IO;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Experimental.U2D;

using NUnit.Framework;

namespace UnityEditor.Experimental.U2D.Animation.Test.Bone
{
    // These tests verify asset import process triggering the bone post processing correctly.
    // Both test assets are hand weaved to have rigid angle.

    [TestFixture]
    public class BonePostProcessTests
    {
        private static string kTestAssetsFolder = "Packages/com.unity.2d.animation/Tests/Editor/BoneModule/Assets/";
        private static string kTestTempFolder = "Assets/Temp/";

        private static int kFixedTestAssetPpu = 100;
        private static Vector3 kFixedTestAssetPivotPointInPixels = new Vector3(16.0f, 16.0f);
        private static SpriteBone[] k3BoneExpected = new SpriteBone[3]
        {
            new SpriteBone()
            {
                name = "root",
                parentId = -1,
                position = (Vector3.zero - kFixedTestAssetPivotPointInPixels) / kFixedTestAssetPpu,
                rotation = Quaternion.Euler(0.0f, 0.0f, 90.0f),
                length = 1.0f / kFixedTestAssetPpu
            },
            new SpriteBone()
            {
                name = "child1",
                parentId = 0,
                position = Vector3.up / kFixedTestAssetPpu,
                rotation = Quaternion.identity,
                length = 0.5f / kFixedTestAssetPpu
            },
            new SpriteBone()
            {
                name = "child2",
                parentId = 1,
                position = Vector3.right / kFixedTestAssetPpu,
                rotation = Quaternion.Euler(0.0f, 0.0f, 225.0f),
                length = 1.5f / kFixedTestAssetPpu
            }
        };

        private static SpriteBone[] kComplexBoneExpected = new SpriteBone[5]
        {
            new SpriteBone()
            {
                name = "root",
                parentId = -1,
                position = ((Vector3)Vector2.one - kFixedTestAssetPivotPointInPixels) / kFixedTestAssetPpu,
                rotation = Quaternion.Euler(0.0f, 0.0f, 30.0f),
                length = 1.0f / kFixedTestAssetPpu
            },
            new SpriteBone()
            {
                name = "child1",
                parentId = 0,
                position = Vector3.up / kFixedTestAssetPpu,
                rotation = Quaternion.Euler(0.0f, 0.0f, 60.0f),
                length = 1.5f / kFixedTestAssetPpu
            },
            new SpriteBone()
            {
                name = "child2",
                parentId = 1,
                position = Vector3.right / kFixedTestAssetPpu,
                rotation = Quaternion.identity,
                length = 1.5f / kFixedTestAssetPpu
            },
            new SpriteBone()
            {
                name = "child3",
                parentId = 1,
                position = Vector3.left / kFixedTestAssetPpu,
                rotation = Quaternion.Euler(0.0f, 0.0f, 120.0f),
                length = 2.5f / kFixedTestAssetPpu
            },
            new SpriteBone()
            {
                name = "child4",
                parentId = 3,
                position = Vector3.up / kFixedTestAssetPpu,
                rotation = Quaternion.identity,
                length = 1.0f / kFixedTestAssetPpu
            }
        };

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
        
        private static void CalculateBindPose(SpriteBone[] spriteBones, out Matrix4x4[] expectedBindPoses)
        {
            expectedBindPoses = new Matrix4x4[spriteBones.Length];
            for (int i = 0; i < spriteBones.Length; ++i)
            {
                var sp = spriteBones[i];

                // Calculate bind poses
                var worldPosition = Vector3.zero;
                var worldRotation = Quaternion.identity;

                Vector3 position = (Vector2)sp.position;

                if (sp.parentId == -1)
                {
                    worldPosition = position;
                    worldRotation = sp.rotation;
                }
                else
                {
                    var parentBindPose = expectedBindPoses[sp.parentId];
                    var invParentBindPose = Matrix4x4.Inverse(parentBindPose);

                    worldPosition = invParentBindPose.MultiplyPoint(position);
                    worldRotation = sp.rotation * invParentBindPose.rotation;
                }

                // Practically Matrix4x4.SetTRInverse
                Matrix4x4 mat = Matrix4x4.identity;
                mat = Matrix4x4.Rotate(Quaternion.Inverse(worldRotation));
                mat = mat * Matrix4x4.Translate(-worldPosition);

                expectedBindPoses[i] = mat;
            }
        }

        private static void VerifyApproximatedSpriteBones(SpriteBone[] expected, SpriteBone[] actual)
        {
            const double kLooseEqual = 0.001;
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

            yield return new TestCaseData(testname, filename, new SpriteBone[0]);
            
            testname = "3bone_sp";
            filename = "3bone.png";

            yield return new TestCaseData(testname, filename, k3BoneExpected);
            
            testname = "complex_sp";
            filename = "complex.png";
            
            yield return new TestCaseData(testname, filename, kComplexBoneExpected);
        }

        private static IEnumerable<TestCaseData> BindPoseCases()
        {
            var testname = "nobone_bp";
            var filename = "nobone.png";
            var expectedBindPoses = new Matrix4x4[0];
            yield return new TestCaseData(testname, filename, expectedBindPoses);
            
            testname = "3bone_bp";
            filename = "3bone.png";
            CalculateBindPose(k3BoneExpected, out expectedBindPoses);

            yield return new TestCaseData(testname, filename, expectedBindPoses);
            
            testname = "complex_bp";
            filename = "complex.png";
            CalculateBindPose(kComplexBoneExpected, out expectedBindPoses);
            
            yield return new TestCaseData(testname, filename, expectedBindPoses);
        }
        
        [Test, TestCaseSource("SpriteBoneCases")]
        public void PostProcessProducedRuntimeSpriteBones(string testname, string filename, SpriteBone[] expectedRuntimeBones)
        {
            string tempPath = kTestTempFolder + testname + "/";
            string spritePath = tempPath + filename;

            // We create a new folder and clone the test asset into the new folder to simulate a "import a sprite with metadata" process.
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
