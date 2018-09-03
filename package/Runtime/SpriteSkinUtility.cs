using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Experimental.U2D.Animation
{
    internal enum SpriteSkinValidationResult
    {
        SpriteNotFound,
        SpriteHasNoSkinningInformation,
        RootTransformNotFound,
        InvalidTransformArray,
        InvalidTransformArrayLength,
        TransformArrayContainsNull,
        RootNotFoundInTransformArray,

        Ready
    }

    internal static class SpriteSkinUtility
    {
        internal static SpriteSkinValidationResult Validate(this SpriteSkin spriteSkin)
        {
            if (spriteSkin.spriteRenderer.sprite == null)
                return SpriteSkinValidationResult.SpriteNotFound;

            var bindPoses = spriteSkin.spriteRenderer.sprite.GetBindPoses();

            if (bindPoses.Length == 0)
                return SpriteSkinValidationResult.SpriteHasNoSkinningInformation;

            if (spriteSkin.rootBone == null)
                return SpriteSkinValidationResult.RootTransformNotFound;

            if (spriteSkin.boneTransforms == null)
                return SpriteSkinValidationResult.InvalidTransformArray;

            if (bindPoses.Length != spriteSkin.boneTransforms.Length)
                return SpriteSkinValidationResult.InvalidTransformArrayLength;

            var rootFound = false;
            foreach (var boneTransform in spriteSkin.boneTransforms)
            {
                if (boneTransform == null)
                    return SpriteSkinValidationResult.TransformArrayContainsNull;

                if (boneTransform == spriteSkin.rootBone)
                    rootFound = true;
            }

            if (!rootFound)
                return SpriteSkinValidationResult.RootNotFoundInTransformArray;

            return SpriteSkinValidationResult.Ready;
        }

        internal static void CreateBoneHierarchy(this SpriteSkin spriteSkin)
        {
            if (spriteSkin.spriteRenderer.sprite == null)
                throw new InvalidOperationException("SpriteRenderer has no Sprite set");

            var spriteBones = spriteSkin.spriteRenderer.sprite.GetBones();
            var transforms = new List<Transform>();
            Transform root = null;

            //TODO: This code expects sprite bones to be in hierarchical order. Fix so it can generate from any order.
            foreach (var bone in spriteBones)
            {
                var parent = spriteSkin.transform;

                if (bone.parentId >= 0)
                    parent = transforms[bone.parentId].transform;

                var newGameObject = CreateGameObject(bone.name, parent, bone.position, bone.rotation);

                transforms.Add(newGameObject.transform);

                if (bone.parentId < 0 && root == null)
                    root = newGameObject.transform;
            }

            spriteSkin.rootBone = root;
            spriteSkin.boneTransforms = transforms.ToArray();
        }

        private static GameObject CreateGameObject(string name, Transform parent, Vector3 position, Quaternion rotation)
        {
            var go = new GameObject(name);
            var transform = go.transform;
            transform.SetParent(parent);
            transform.localPosition = position;
            transform.localRotation = rotation;
            transform.localScale = Vector3.one;
            return go;
        }

        internal static void ResetBindPose(this SpriteSkin spriteSkin)
        {
            if (!spriteSkin.isValid)
                throw new InvalidOperationException("SpriteSkin is not valid");

            var spriteBones = spriteSkin.spriteRenderer.sprite.GetBones();
            var boneTransforms = spriteSkin.boneTransforms;

            for (int i = 0; i < boneTransforms.Length; ++i)
            {
                var boneTransform = boneTransforms[i];
                var spriteBone = spriteBones[i];

                if (spriteBone.parentId != -1)
                {
                    boneTransform.localPosition = spriteBone.position;
                    boneTransform.localRotation = spriteBone.rotation;
                    boneTransform.localScale = Vector3.one;
                }
            }
        }

        //TODO: Add other ways to find the transforms in case the named path fails
        internal static void Rebind(this SpriteSkin spriteSkin)
        {
            if (spriteSkin.spriteRenderer.sprite == null)
                throw new ArgumentException("SpriteRenderer has no Sprite set");
            if (spriteSkin.rootBone == null)
                throw new ArgumentException("SpriteSkin has no rootBone");

            var spriteBones = spriteSkin.spriteRenderer.sprite.GetBones();
            var boneTransforms = new List<Transform>();

            for (int i = 0; i < spriteBones.Length; ++i)
            {
                var boneTransformPath = CalculateBoneTransformPath(i, spriteBones);
                var boneTransform = spriteSkin.rootBone.Find(boneTransformPath);

                boneTransforms.Add(boneTransform);
            }

            spriteSkin.boneTransforms = boneTransforms.ToArray();
        }

        private static string CalculateBoneTransformPath(int index, SpriteBone[] spriteBones)
        {
            var path = "";

            while (index != -1)
            {
                var spriteBone = spriteBones[index];
                var spriteBoneName = spriteBone.name;
                if (spriteBone.parentId != -1)
                {
                    if (string.IsNullOrEmpty(path))
                        path = spriteBoneName;
                    else
                        path = spriteBoneName + "/" + path;
                }
                index = spriteBone.parentId;
            }

            return path;
        }

        internal static int CalculateTransformHash(this SpriteSkin spriteSkin)
        {
            int bits = 0;
            int boneTransformHash = spriteSkin.transform.localToWorldMatrix.GetHashCode() >> bits;
            bits++;
            foreach (var transform in spriteSkin.boneTransforms)
            {
                boneTransformHash ^= (transform.localToWorldMatrix.GetHashCode() >> bits);
                bits = (bits + 1) % 8;
            }
            return boneTransformHash;
        }

        internal static JobHandle CalculateBounds(NativeArray<Vector3> vertices, NativeArray<Vector3> minMax, JobHandle parentJob)
        {
            var boundsJob = new AABBJob()
            {
                vertices = vertices,
                minMax = minMax
            };

            JobHandle boundsFence = boundsJob.Schedule(parentJob);
            return boundsFence;
        }

        internal static JobHandle Deform(NativeSlice<Vector3> inputVertices, NativeArray<BoneWeight> boneWeights, Matrix4x4 worldToLocalMatrix,
            NativeArray<Matrix4x4> bindPoses, NativeArray<Matrix4x4> transformMatrices, NativeArray<Vector3> outputVertices)
        {
            if (bindPoses.Length != transformMatrices.Length)
                throw new InvalidOperationException("Invalid TransformMatrices array length.");
            if (boneWeights.Length != inputVertices.Length)
                throw new InvalidOperationException("Invalid BoneWeight array length");
            if (outputVertices.Length != inputVertices.Length)
                throw new InvalidOperationException("Invalid output Vertices array length");

            var skinningMatrices = new NativeArray<Matrix4x4>(transformMatrices.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            var boneJob = new BoneJob()
            {
                rootInv = worldToLocalMatrix,
                bindPoses = bindPoses,
                bones = transformMatrices,
                output = skinningMatrices
            };

            var skinJob = new SkinJob()
            {
                influenceCount = 4,
                influences = boneWeights,
                vertices = inputVertices,
                bones = skinningMatrices,
                output = outputVertices
            };

            return skinJob.Schedule(boneJob.Schedule());
        }
    }
}
