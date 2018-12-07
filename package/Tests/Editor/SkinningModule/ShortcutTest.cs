using System;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.Experimental.U2D.Animation.Test.SkinningModuleTests
{
    public class ShortcutSpriteSheetTest : SkinningModuleTestBase
    {
        [Test]
        public void DefaultMode_IsSpriteSheetMode()
        {
            Assert.AreEqual(SkinningMode.SpriteSheet, skinningCache.mode);
        }

        [Test]
        public void DefaultTool_IsEditPoseTool()
        {
            var selectedTool = skinningCache.selectedTool as SkeletonToolWrapper;
            Assert.IsNotNull(selectedTool);
            Assert.AreEqual(selectedTool.mode, SkeletonMode.EditPose);
        }

        [Test]
        public void ActivateDisablePoseShortcut_DisablesPose()
        {
            var sprite = skinningCache.GetSprites()[0];
            skinningCache.events.selectedSpriteChanged.Invoke(sprite);
            sprite.GetSkeleton().SetPosePreview();
            Assert.IsTrue(sprite.GetSkeleton().isPosePreview);
            KeyboardEvent(KeyCode.Alpha1, true);
            Assert.IsFalse(sprite.GetSkeleton().isPosePreview);
        }

        [Test]
        public void SpriteSheet_ActivateCharacterModeShortcut_DoesNotSwitchCharacterMode()
        {
            KeyboardEvent(KeyCode.Alpha2, true);
            Assert.AreEqual(SkinningMode.SpriteSheet, skinningCache.mode);
        }

        [Test]
        public void ActivateEditJointShortcut_SwitchesToEditJointTool()
        {
            KeyboardEvent(KeyCode.W, true);
            var selectedTool = skinningCache.selectedTool as SkeletonToolWrapper;
            Assert.IsNotNull(selectedTool);
            Assert.AreEqual(selectedTool.mode, SkeletonMode.EditJoints);
        }

        [Test]
        public void ActivateEditJointShortcutAndEditPoseShortcut_SwitchesToEditPoseTool()
        {
            KeyboardEvent(KeyCode.W, true);
            KeyboardEvent(KeyCode.Q, true);
            var selectedTool = skinningCache.selectedTool as SkeletonToolWrapper;
            Assert.IsNotNull(selectedTool);
            Assert.AreEqual(selectedTool.mode, SkeletonMode.EditPose);
        }

        [Test]
        public void ActivateEditJointShortcutAndUndo_SwitchesToEditPoseTool()
        {
            KeyboardEvent(KeyCode.W, true);
            Undo.PerformUndo();
            var selectedTool = skinningCache.selectedTool as SkeletonToolWrapper;
            Assert.IsNotNull(selectedTool);
            Assert.AreEqual(selectedTool.mode, SkeletonMode.EditPose);
        }

        [Test]
        public void ActivateCreateBoneShortcut_SwitchesToCreateBoneTool()
        {
            KeyboardEvent(KeyCode.E, true);
            var selectedTool = skinningCache.selectedTool as SkeletonToolWrapper;
            Assert.IsNotNull(selectedTool);
            Assert.AreEqual(selectedTool.mode, SkeletonMode.CreateBone);
        }

        [Test]
        public void ActivateSplitBoneShortcut_SwitchesToSplitBoneTool()
        {
            KeyboardEvent(KeyCode.R, true);
            var selectedTool = skinningCache.selectedTool as SkeletonToolWrapper;
            Assert.IsNotNull(selectedTool);
            Assert.AreEqual(selectedTool.mode, SkeletonMode.SplitBone);
        }

        [Test]
        public void ActivateReparentBoneShortcut_SwitchesToReparentBoneTool()
        {
            KeyboardEvent(KeyCode.T, true);
            var selectedTool = skinningCache.selectedTool as BoneReparentTool;
            Assert.IsNotNull(selectedTool);
        }

        [Test]
        public void ActivateMeshSelectionShortcut_SwitchesToMeshSelectionTool()
        {
            KeyboardEvent(KeyCode.A, true);
            var selectedTool = skinningCache.selectedTool as MeshToolWrapper;
            Assert.IsNotNull(selectedTool);
            Assert.AreEqual(selectedTool.meshMode, SpriteMeshViewMode.EditGeometry);
        }

        [Test]
        public void ActivateCreateVertexShortcut_SwitchesToCreateVertexTool()
        {
            KeyboardEvent(KeyCode.D, true);
            var selectedTool = skinningCache.selectedTool as MeshToolWrapper;
            Assert.IsNotNull(selectedTool);
            Assert.AreEqual(selectedTool.meshMode, SpriteMeshViewMode.CreateVertex);
        }

        [Test]
        public void ActivateCreateEdgeShortcut_SwitchesToCreateEdgeTool()
        {
            KeyboardEvent(KeyCode.G, true);
            var selectedTool = skinningCache.selectedTool as MeshToolWrapper;
            Assert.IsNotNull(selectedTool);
            Assert.AreEqual(selectedTool.meshMode, SpriteMeshViewMode.CreateEdge);
        }

        [Test]
        public void ActivateSplitEdgeShortcut_SwitchesToSplitEdgeTool()
        {
            KeyboardEvent(KeyCode.H, true);
            var selectedTool = skinningCache.selectedTool as MeshToolWrapper;
            Assert.IsNotNull(selectedTool);
            Assert.AreEqual(selectedTool.meshMode, SpriteMeshViewMode.SplitEdge);
        }

        [Test]
        public void ActivateGenerateGeometryShortcut_SwitchesToGenerateGeometryTool()
        {
            KeyboardEvent(KeyCode.A, true);
            var selectedTool = skinningCache.selectedTool as GenerateGeometryTool;
            Assert.IsNotNull(selectedTool);
        }

        [Test]
        public void ActivateWeightPainterShortcut_SwitchesToWeightPainterTool()
        {
            KeyboardEvent(KeyCode.X, true);
            var selectedTool = skinningCache.selectedTool as WeightPainterToolWrapper;
            Assert.IsNotNull(selectedTool);
            Assert.AreEqual(selectedTool.paintMode, WeightPainterMode.Slider);
        }

        [Test]
        public void ActivateWeightBrushShortcut_SwitchesToWeightBrushTool()
        {
            KeyboardEvent(KeyCode.C, true);
            var selectedTool = skinningCache.selectedTool as WeightPainterToolWrapper;
            Assert.IsNotNull(selectedTool);
            Assert.AreEqual(selectedTool.paintMode, WeightPainterMode.Brush);
        }

        [Test]
        public void Spritesheet_ActivateBoneInfluenceShortcut_DoesNotSwitchesToBoneInfluenceTool()
        {
            KeyboardEvent(KeyCode.V, true);
            var boneInfluenceTool = skinningCache.selectedTool as SpriteBoneInfluenceTool;
            Assert.IsNull(boneInfluenceTool);
            var selectedTool = skinningCache.selectedTool as SkeletonToolWrapper;
            Assert.IsNotNull(selectedTool);
            Assert.AreEqual(selectedTool.mode, SkeletonMode.EditPose);
        }

        [Test]
        public void ActivateGenerateWeightsShortcut_SwitchesToGenerateWeightsTool()
        {
            KeyboardEvent(KeyCode.Z, true);
            var selectedTool = skinningCache.selectedTool as GenerateWeightsTool;
            Assert.IsNotNull(selectedTool);
        }

        [Test]
        public void ActivatePasteShortcut_SwitchesToPasteTool()
        {
            KeyboardEvent(KeyCode.B, true);
            var selectedTool = skinningCache.selectedTool as CopyTool;
            Assert.IsNotNull(selectedTool);
        }

        [Test]
        public void ActivateVisibilityShortcut_SwitchesToVisibilityTool()
        {
            KeyboardEvent(KeyCode.P, true);
            var selectedTool = skinningCache.selectedTool as VisibilityTool;
            Assert.IsNotNull(selectedTool);
        }

        [TestCase(KeyCode.P, typeof(VisibilityTool))]
        [TestCase(KeyCode.B, typeof(CopyTool))]
        public void ReactivateHorizontalToolShortcut_SwitchesToPreviousTool(KeyCode key, Type type)
        {
            var previousTool = skinningCache.selectedTool;
            KeyboardEvent(key, true);
            Assert.IsTrue(skinningCache.selectedTool.GetType() == type);
            KeyboardEvent(key, true);
            Assert.AreSame(previousTool, skinningCache.selectedTool);
        }

        private void KeyboardEvent(KeyCode keyCode, bool shift)
        {
            var ev = new Event();
            ev.keyCode = keyCode;
            ev.shift = shift;
            ev.type = EventType.KeyDown;
            m_Window.SendEvent(ev);
            ev.type = EventType.KeyUp;
            m_Window.SendEvent(ev);
        }
    }

    public class ShortcutCharacterTest : SkinningModuleCharacterTestBase
    {
        [Test]
        public void DefaultMode_IsCharacterMode()
        {
            Assert.AreEqual(SkinningMode.Character, skinningCache.mode);
        }

        [Test]
        public void Character_ActivateCharacterModeShortcut_SwitchesCharacterMode()
        {
            KeyboardEvent(KeyCode.Alpha2, true);
            Assert.AreEqual(SkinningMode.SpriteSheet, skinningCache.mode);
        }

        [Test]
        public void Character_ActivateBoneInfluenceShortcut_SwitchesToBoneInfluenceTool()
        {
            KeyboardEvent(KeyCode.V, true);
            var boneInfluenceTool = skinningCache.selectedTool as SpriteBoneInfluenceTool;
            Assert.IsNotNull(boneInfluenceTool);
        }

        private void KeyboardEvent(KeyCode keyCode, bool shift)
        {
            var ev = new Event();
            ev.keyCode = keyCode;
            ev.shift = shift;
            ev.type = EventType.KeyDown;
            m_Window.SendEvent(ev);
            ev.type = EventType.KeyUp;
            m_Window.SendEvent(ev);
        }
    }
}
