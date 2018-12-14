using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal static class SpriteCacheExtensions
    {
        public static MeshCache GetMesh(this SpriteCache sprite)
        {
            if (sprite != null)
                return sprite.skinningCache.GetMesh(sprite);
            return null;
        }

        public static MeshPreviewCache GetMeshPreview(this SpriteCache sprite)
        {
            if (sprite != null)
                return sprite.skinningCache.GetMeshPreview(sprite);
            return null;
        }

        public static SkeletonCache GetSkeleton(this SpriteCache sprite)
        {
            if (sprite != null)
                return sprite.skinningCache.GetSkeleton(sprite);
            return null;
        }

        public static CharacterPartCache GetCharacterPart(this SpriteCache sprite)
        {
            if (sprite != null)
                return sprite.skinningCache.GetCharacterPart(sprite);
            return null;
        }

        public static bool IsVisible(this SpriteCache sprite)
        {
            var isVisible = true;
            var characterPart = sprite.GetCharacterPart();

            if (sprite.skinningCache.mode == SkinningMode.Character && characterPart != null)
                isVisible = characterPart.isVisible;

            return isVisible;
        }

        public static Matrix4x4 GetLocalToWorldMatrixFromMode(this SpriteCache sprite)
        {
            var skinningCache = sprite.skinningCache;

            if (skinningCache.mode == SkinningMode.SpriteSheet)
                return sprite.localToWorldMatrix;

            var characterPart = sprite.GetCharacterPart();

            Debug.Assert(characterPart != null);

            return characterPart.localToWorldMatrix;
        }

        public static BoneCache[] GetBonesFromMode(this SpriteCache sprite)
        {
            var skinningCache = sprite.skinningCache;

            if (skinningCache.mode == SkinningMode.SpriteSheet)
                return sprite.GetSkeleton().bones;

            var characterPart = sprite.GetCharacterPart();
            Debug.Assert(characterPart != null);
            return characterPart.bones;
        }

        public static void UpdateMesh(this SpriteCache sprite, BoneCache[] bones)
        {
            var mesh = sprite.GetMesh();
            var previewMesh = sprite.GetMeshPreview();

            Debug.Assert(mesh != null);
            Debug.Assert(previewMesh != null);

            mesh.bones = bones;

            previewMesh.SetWeightsDirty();
        }

        public static void SmoothFill(this SpriteCache sprite)
        {
            var mesh = sprite.GetMesh();

            if (mesh == null)
                return;

            var controller = new SpriteMeshDataController();
            controller.spriteMeshData = mesh;
            controller.SmoothFill();
        }

        public static void RestoreBindPose(this SpriteCache sprite)
        {
            var skinningCache = sprite.skinningCache;
            var skeleton = sprite.GetSkeleton();
            Debug.Assert(skeleton != null);
            skeleton.RestoreDefaultPose();
            skinningCache.events.skeletonPreviewPoseChanged.Invoke(skeleton);
        }

        public static bool AssociateAllBones(this SpriteCache sprite)
        {
            var skinningCache = sprite.skinningCache;

            if (skinningCache.mode == SkinningMode.SpriteSheet)
                return false;

            var character = skinningCache.character;
            Debug.Assert(character != null);
            Debug.Assert(character.skeleton != null);

            var characterPart = sprite.GetCharacterPart();

            Debug.Assert(characterPart != null);

            var bones = character.skeleton.bones.Where(x => x.isVisible).ToArray();
            characterPart.bones = bones;

            characterPart.sprite.UpdateMesh(bones);

            return true;
        }

        public static void DeassociateUnusedBones(this SpriteCache sprite)
        {
            var skinningCache = sprite.skinningCache;

            Debug.Assert(skinningCache.mode == SkinningMode.Character);

            var characterPart = sprite.GetCharacterPart();

            Debug.Assert(characterPart != null);

            characterPart.DeassociateUnusedBones();
        }

        public static void DeassociateAllBones(this SpriteCache sprite)
        {
            var skinningCache = sprite.skinningCache;

            if (skinningCache.mode == SkinningMode.SpriteSheet)
                return;

            var part = sprite.GetCharacterPart();
            if (part.bones.Length == 0)
                return;

            Debug.Assert(part.sprite != null);

            part.bones = new BoneCache[0];
            part.sprite.UpdateMesh(part.bones);

            skinningCache.events.characterPartChanged.Invoke(part);
        }
    }
}
