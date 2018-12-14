using System;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.Experimental.U2D.Animation.Test.SkinningModuleTests
{
    public class CopySpriteSheetTest : SkinningModuleFullFakeTestBase
    {
        private CopyTool m_CopyTool;
        private static readonly string kDefaultSpriteCopyString =
            @"{""pixelsPerUnit"":100.0,""copyData"":[{""spriteName"":"""",""spriteBones"":[{""m_Name"":""Bone-1-1"",""m_Position"":{""x"":0.0,""y"":0.0,""z"":0.0},""m_Rotation"":{""x"":0.0,""y"":0.0,""z"":0.0,""w"":1.0},""m_Length"":20.0,""m_ParentId"":-1},{""m_Name"":""Bone-1-2"",""m_Position"":{""x"":20.0,""y"":0.0,""z"":0.0},""m_Rotation"":{""x"":0.0,""y"":0.0,""z"":0.7071068286895752,""w"":-0.7071068286895752},""m_Length"":50.0,""m_ParentId"":0}],""vertices"":[{""m_Position"":{""x"":0.0,""y"":0.0},""m_EditableBoneWeight"":{""m_Channels"":[{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0}]}},{""m_Position"":{""x"":0.0,""y"":100.0},""m_EditableBoneWeight"":{""m_Channels"":[{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0}]}},{""m_Position"":{""x"":100.0,""y"":100.0},""m_EditableBoneWeight"":{""m_Channels"":[{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0}]}},{""m_Position"":{""x"":100.0,""y"":0.0},""m_EditableBoneWeight"":{""m_Channels"":[{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0}]}}],""indices"":[0,1,2,0,2,3],""edges"":[{""m_Index1"":0,""m_Index2"":1},{""m_Index1"":1,""m_Index2"":2},{""m_Index1"":2,""m_Index2"":3},{""m_Index1"":3,""m_Index2"":0}],""boneWeightNames"":[""Bone-1-1"",""Bone-1-2""]}]}";

        private static readonly string kAllSpriteCopyString =
            @"{""pixelsPerUnit"":100.0,""copyData"":[{""spriteName"":""Sprite1"",""spriteBones"":[{""m_Name"":""Bone-1-1"",""m_Position"":{""x"":0.0,""y"":0.0,""z"":0.0},""m_Rotation"":{""x"":0.0,""y"":0.0,""z"":0.0,""w"":1.0},""m_Length"":20.0,""m_ParentId"":-1},{""m_Name"":""Bone-1-2"",""m_Position"":{""x"":20.0,""y"":0.0,""z"":0.0},""m_Rotation"":{""x"":0.0,""y"":0.0,""z"":0.7071068286895752,""w"":-0.7071068286895752},""m_Length"":50.0,""m_ParentId"":0}],""vertices"":[{""m_Position"":{""x"":0.0,""y"":0.0},""m_EditableBoneWeight"":{""m_Channels"":[{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0}]}},{""m_Position"":{""x"":0.0,""y"":100.0},""m_EditableBoneWeight"":{""m_Channels"":[{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0}]}},{""m_Position"":{""x"":100.0,""y"":100.0},""m_EditableBoneWeight"":{""m_Channels"":[{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0}]}},{""m_Position"":{""x"":100.0,""y"":0.0},""m_EditableBoneWeight"":{""m_Channels"":[{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0}]}}],""indices"":[0,1,2,0,2,3],""edges"":[{""m_Index1"":0,""m_Index2"":1},{""m_Index1"":1,""m_Index2"":2},{""m_Index1"":2,""m_Index2"":3},{""m_Index1"":3,""m_Index2"":0}],""boneWeightNames"":[""Bone-1-1"",""Bone-1-2""]},{""spriteName"":""Sprite2"",""spriteBones"":[{""m_Name"":""Bone-2-1"",""m_Position"":{""x"":100.0,""y"":0.0,""z"":0.0},""m_Rotation"":{""x"":0.0,""y"":0.0,""z"":0.0,""w"":1.0},""m_Length"":20.0,""m_ParentId"":-1},{""m_Name"":""Bone-2-2"",""m_Position"":{""x"":120.0,""y"":0.0,""z"":0.0},""m_Rotation"":{""x"":0.0,""y"":0.0,""z"":0.7071068286895752,""w"":-0.7071068286895752},""m_Length"":50.0,""m_ParentId"":0}],""vertices"":[{""m_Position"":{""x"":100.0,""y"":0.0},""m_EditableBoneWeight"":{""m_Channels"":[{""m_Enabled"":true,""m_BoneIndex"":0,""m_Weight"":1.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0}]}},{""m_Position"":{""x"":100.0,""y"":100.0},""m_EditableBoneWeight"":{""m_Channels"":[{""m_Enabled"":true,""m_BoneIndex"":1,""m_Weight"":0.5},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0}]}},{""m_Position"":{""x"":190.0,""y"":50.0},""m_EditableBoneWeight"":{""m_Channels"":[{""m_Enabled"":true,""m_BoneIndex"":0,""m_Weight"":0.5},{""m_Enabled"":true,""m_BoneIndex"":1,""m_Weight"":0.5},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0},{""m_Enabled"":false,""m_BoneIndex"":0,""m_Weight"":0.0}]}}],""indices"":[0,1,2],""edges"":[{""m_Index1"":0,""m_Index2"":1},{""m_Index1"":1,""m_Index2"":2},{""m_Index1"":2,""m_Index2"":0}],""boneWeightNames"":[""Bone-2-1"",""Bone-2-2""]}]}";


        public override void DoOtherSetup()
        {
            var sprite = skinningCache.GetSprites()[0];
            skinningCache.events.selectedSpriteChanged.Invoke(sprite);
            skinningCache.events.boneSelectionChanged.Invoke();

            m_CopyTool = skinningCache.GetTool(Tools.CopyPaste) as CopyTool;
            EditorGUIUtility.systemCopyBuffer = "";
        }

        public override void DoOtherTeardown()
        {
            EditorGUIUtility.systemCopyBuffer = "";
        }

        [Test]
        public void SelectedSprite_DoCopy_CopiesToSystemCopyBuffer()
        {
            m_CopyTool.OnCopyActivated();
            Assert.IsFalse(String.IsNullOrEmpty(EditorGUIUtility.systemCopyBuffer));
            Assert.AreEqual(kDefaultSpriteCopyString, EditorGUIUtility.systemCopyBuffer);
        }

        [Test]
        public void NoSelectedSprite_DoCopy_CopiesAllToSystemCopyBuffer()
        {
            skinningCache.events.selectedSpriteChanged.Invoke(null);

            m_CopyTool.OnCopyActivated();
            Assert.IsFalse(String.IsNullOrEmpty(EditorGUIUtility.systemCopyBuffer));
            Assert.AreEqual(kAllSpriteCopyString, EditorGUIUtility.systemCopyBuffer);
        }

        [Test]
        public void NothingCopied_PasteActivated_NothingIsPasted()
        {
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, TextContent.copyError2);
            Assert.IsTrue(String.IsNullOrEmpty(EditorGUIUtility.systemCopyBuffer));
            m_CopyTool.OnPasteActivated(true, true, false, false);
            m_CopyTool.OnCopyActivated();
            Assert.IsFalse(String.IsNullOrEmpty(EditorGUIUtility.systemCopyBuffer));
            Assert.AreEqual(kDefaultSpriteCopyString, EditorGUIUtility.systemCopyBuffer);
        }

        [Test]
        public void SelectedSprite_DoCopyAndPasteBones_PastesToSprite()
        {
            m_CopyTool.OnCopyActivated();

            var sprite = skinningCache.GetSprites()[1];
            skinningCache.events.selectedSpriteChanged.Invoke(sprite);

            var skeletonCopyCache = sprite.GetSkeleton();
            Assert.AreEqual(2, skeletonCopyCache.BoneCount);
            Assert.AreEqual("Bone-2-1", skeletonCopyCache.bones[0].name);
            Assert.AreEqual("Bone-2-2", skeletonCopyCache.bones[1].name);

            m_CopyTool.OnPasteActivated(true, false, false, false);

            var skeletonPasteCache = sprite.GetSkeleton();
            Assert.AreEqual(2, skeletonPasteCache.BoneCount);
            Assert.AreEqual("Bone-1-1", skeletonPasteCache.bones[0].name);
            Assert.AreEqual(0, (new Vector3(100f, 0, 0) - skeletonPasteCache.bones[0].position).magnitude, 0.0001f);
            Assert.AreEqual(0, (new Vector3(0, 0, 0) - skeletonPasteCache.bones[0].rotation.eulerAngles).magnitude, 0.0001f);
            Assert.AreEqual("Bone-1-2", skeletonPasteCache.bones[1].name);
            Assert.AreEqual(0, (new Vector3(120f, 0, 0) - skeletonPasteCache.bones[1].position).magnitude, 0.0001f);
            Assert.AreEqual(0, (new Vector3(0, 0, 270f) - skeletonPasteCache.bones[1].rotation.eulerAngles).magnitude, 0.0001f);
        }

        [Test]
        public void SelectedSprite_DoCopyAndPasteBones_FlipX_PastesToSprite()
        {
            m_CopyTool.OnCopyActivated();

            var sprite = skinningCache.GetSprites()[1];
            skinningCache.events.selectedSpriteChanged.Invoke(sprite);

            var skeletonCopyCache = sprite.GetSkeleton();
            Assert.AreEqual(2, skeletonCopyCache.BoneCount);
            Assert.AreEqual("Bone-2-1", skeletonCopyCache.bones[0].name);
            Assert.AreEqual("Bone-2-2", skeletonCopyCache.bones[1].name);

            m_CopyTool.OnPasteActivated(true, false, true, false);

            var skeletonPasteCache = sprite.GetSkeleton();
            Assert.AreEqual(2, skeletonPasteCache.BoneCount);
            Assert.AreEqual("Bone-1-1", skeletonPasteCache.bones[0].name);
            Assert.AreEqual(0, (new Vector3(200f, 0, 0) - skeletonPasteCache.bones[0].position).magnitude, 0.0001f);
            Assert.AreEqual(0, (new Vector3(0, 0, 180f) - skeletonPasteCache.bones[0].rotation.eulerAngles).magnitude, 0.0001f);
            Assert.AreEqual("Bone-1-2", skeletonPasteCache.bones[1].name);
            Assert.AreEqual(0, (new Vector3(180f, 0, 0) - skeletonPasteCache.bones[1].position).magnitude, 0.0001f);
            Assert.AreEqual(0, (new Vector3(0, 0, 270f) - skeletonPasteCache.bones[1].rotation.eulerAngles).magnitude, 0.0001f);
        }

        [Test]
        public void SelectedSprite_DoCopyAndPasteBones_FlipY_PastesToSprite()
        {
            m_CopyTool.OnCopyActivated();

            var sprite = skinningCache.GetSprites()[1];
            skinningCache.events.selectedSpriteChanged.Invoke(sprite);

            var skeletonCopyCache = sprite.GetSkeleton();
            Assert.AreEqual(2, skeletonCopyCache.BoneCount);
            Assert.AreEqual("Bone-2-1", skeletonCopyCache.bones[0].name);
            Assert.AreEqual("Bone-2-2", skeletonCopyCache.bones[1].name);

            m_CopyTool.OnPasteActivated(true, false, false, true);

            var skeletonPasteCache = sprite.GetSkeleton();
            Assert.AreEqual(2, skeletonPasteCache.BoneCount);
            Assert.AreEqual("Bone-1-1", skeletonPasteCache.bones[0].name);
            Assert.AreEqual(0, (new Vector3(100f, 100f, 0) - skeletonPasteCache.bones[0].position).magnitude, 0.0001f);
            Assert.AreEqual(0, (new Vector3(0, 0, 0f) - skeletonPasteCache.bones[0].rotation.eulerAngles).magnitude, 0.0001f);
            Assert.AreEqual("Bone-1-2", skeletonPasteCache.bones[1].name);
            Assert.AreEqual(0, (new Vector3(120f, 100f, 0) - skeletonPasteCache.bones[1].position).magnitude, 0.0001f);
            Assert.AreEqual(0, (new Vector3(0, 0, 90f) - skeletonPasteCache.bones[1].rotation.eulerAngles).magnitude, 0.0001f);
        }

        [Test]
        public void SelectedSprite_DoCopyAndPasteBones_FlipXY_PastesToSprite()
        {
            m_CopyTool.OnCopyActivated();

            var sprite = skinningCache.GetSprites()[1];
            skinningCache.events.selectedSpriteChanged.Invoke(sprite);

            var skeletonCopyCache = sprite.GetSkeleton();
            Assert.AreEqual(2, skeletonCopyCache.BoneCount);
            Assert.AreEqual("Bone-2-1", skeletonCopyCache.bones[0].name);
            Assert.AreEqual("Bone-2-2", skeletonCopyCache.bones[1].name);

            m_CopyTool.OnPasteActivated(true, false, true, true);

            var skeletonPasteCache = sprite.GetSkeleton();
            Assert.AreEqual(2, skeletonPasteCache.BoneCount);
            Assert.AreEqual("Bone-1-1", skeletonPasteCache.bones[0].name);
            Assert.AreEqual(0, (new Vector3(200f, 100f, 0) - skeletonPasteCache.bones[0].position).magnitude, 0.0001f);
            Assert.AreEqual(0, (new Vector3(0, 0, 180f) - skeletonPasteCache.bones[0].rotation.eulerAngles).magnitude, 0.0001f);
            Assert.AreEqual("Bone-1-2", skeletonPasteCache.bones[1].name);
            Assert.AreEqual(0, (new Vector3(180f, 100f, 0) - skeletonPasteCache.bones[1].position).magnitude, 0.0001f);
            Assert.AreEqual(0, (new Vector3(0, 0, 90f) - skeletonPasteCache.bones[1].rotation.eulerAngles).magnitude, 0.0001f);
        }

        [Test]
        public void SelectedSprite_DoCopyAndPasteMesh_PastesToSprite()
        {
            m_CopyTool.OnCopyActivated();

            var sprite = skinningCache.GetSprites()[1];
            skinningCache.events.selectedSpriteChanged.Invoke(sprite);

            var meshCopyCache = sprite.GetMesh();
            Assert.AreEqual(3, meshCopyCache.vertexCount);

            m_CopyTool.OnPasteActivated(false, true, false, false);

            var meshPasteCache = sprite.GetMesh();
            Assert.AreEqual(4, meshPasteCache.vertexCount);
            Assert.AreEqual(0, (new Vector2(0, 0) - meshPasteCache.vertices[0].position).magnitude, 0.0001f);
            Assert.AreEqual(0, (new Vector2(0, 100f) - meshPasteCache.vertices[1].position).magnitude, 0.0001f);
            Assert.AreEqual(0, (new Vector2(100f, 100f) - meshPasteCache.vertices[2].position).magnitude, 0.0001f);
            Assert.AreEqual(0, (new Vector2(100f, 0) - meshPasteCache.vertices[3].position).magnitude, 0.0001f);
            Assert.AreEqual(6, meshPasteCache.indices.Count);
            Assert.AreEqual(4, meshPasteCache.edges.Count);
        }

        [Test]
        public void SelectedSprite_DoCopyAndPasteMesh_FlipX_PastesToSprite()
        {
            m_CopyTool.OnCopyActivated();

            var sprite = skinningCache.GetSprites()[1];
            skinningCache.events.selectedSpriteChanged.Invoke(sprite);

            var meshCopyCache = sprite.GetMesh();
            Assert.AreEqual(3, meshCopyCache.vertexCount);

            m_CopyTool.OnPasteActivated(false, true, true, false);

            var meshPasteCache = sprite.GetMesh();
            Assert.AreEqual(4, meshPasteCache.vertexCount);
            Assert.AreEqual(0, (new Vector2(100f, 0) - meshPasteCache.vertices[0].position).magnitude, 0.0001f);
            Assert.AreEqual(0, (new Vector2(100f, 100f) - meshPasteCache.vertices[1].position).magnitude, 0.0001f);
            Assert.AreEqual(0, (new Vector2(0f, 100f) - meshPasteCache.vertices[2].position).magnitude, 0.0001f);
            Assert.AreEqual(0, (new Vector2(0f, 0) - meshPasteCache.vertices[3].position).magnitude, 0.0001f);
            Assert.AreEqual(6, meshPasteCache.indices.Count);
            Assert.AreEqual(4, meshPasteCache.edges.Count);
        }

        [Test]
        public void SelectedSprite_DoCopyAndPasteMesh_FlipY_PastesToSprite()
        {
            m_CopyTool.OnCopyActivated();

            var sprite = skinningCache.GetSprites()[1];
            skinningCache.events.selectedSpriteChanged.Invoke(sprite);

            var meshCopyCache = sprite.GetMesh();
            Assert.AreEqual(3, meshCopyCache.vertexCount);

            m_CopyTool.OnPasteActivated(false, true, false, true);

            var meshPasteCache = sprite.GetMesh();
            Assert.AreEqual(4, meshPasteCache.vertexCount);
            Assert.AreEqual(0, (new Vector2(0, 100f) - meshPasteCache.vertices[0].position).magnitude, 0.0001f);
            Assert.AreEqual(0, (new Vector2(0, 0) - meshPasteCache.vertices[1].position).magnitude, 0.0001f);
            Assert.AreEqual(0, (new Vector2(100f, 0) - meshPasteCache.vertices[2].position).magnitude, 0.0001f);
            Assert.AreEqual(0, (new Vector2(100f, 100f) - meshPasteCache.vertices[3].position).magnitude, 0.0001f);
            Assert.AreEqual(6, meshPasteCache.indices.Count);
            Assert.AreEqual(4, meshPasteCache.edges.Count);
        }

        [Test]
        public void SelectedSprite_DoCopyAndPasteMesh_FlipXY_PastesToSprite()
        {
            m_CopyTool.OnCopyActivated();

            var sprite = skinningCache.GetSprites()[1];
            skinningCache.events.selectedSpriteChanged.Invoke(sprite);

            var meshCopyCache = sprite.GetMesh();
            Assert.AreEqual(3, meshCopyCache.vertexCount);

            m_CopyTool.OnPasteActivated(false, true, true, true);

            var meshPasteCache = sprite.GetMesh();
            Assert.AreEqual(4, meshPasteCache.vertexCount);
            Assert.AreEqual(0, (new Vector2(100f, 100f) - meshPasteCache.vertices[0].position).magnitude, 0.0001f);
            Assert.AreEqual(0, (new Vector2(100f, 0) - meshPasteCache.vertices[1].position).magnitude, 0.0001f);
            Assert.AreEqual(0, (new Vector2(0f, 0) - meshPasteCache.vertices[2].position).magnitude, 0.0001f);
            Assert.AreEqual(0, (new Vector2(0f, 100f) - meshPasteCache.vertices[3].position).magnitude, 0.0001f);
            Assert.AreEqual(6, meshPasteCache.indices.Count);
            Assert.AreEqual(4, meshPasteCache.edges.Count);
        }
    }
}
