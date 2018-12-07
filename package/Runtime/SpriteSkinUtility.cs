using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.U2D.Animation
{
    internal enum SpriteSkinValidationResult
    {
        SpriteNotFound,
        SpriteHasNoSkinningInformation,
        SpriteHasNoWeights,
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
            var sprite = spriteSkin.spriteRenderer.sprite; 
            if (sprite == null)
                return SpriteSkinValidationResult.SpriteNotFound;

            var bindPoses = sprite.GetBindPoses();

            if (bindPoses.Length == 0)
                return SpriteSkinValidationResult.SpriteHasNoSkinningInformation;

            if (sprite.GetBoneWeights().Length == 0)
                return SpriteSkinValidationResult.SpriteHasNoWeights;

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
            var transforms = new Transform[spriteBones.Length];
            Transform root = null;

            for (int i = 0; i < spriteBones.Length; ++i)
            {
                CreateGameObject(i, spriteBones, transforms, spriteSkin.transform);
                if (spriteBones[i].parentId < 0 && root == null)
                    root = transforms[i];
            }

            spriteSkin.rootBone = root;
            spriteSkin.boneTransforms = transforms;
        }

        private static void CreateGameObject(int index, SpriteBone[] spriteBones, Transform[] transforms, Transform root)
        {
            if (transforms[index] == null)
            {
                var spriteBone = spriteBones[index];
                if (spriteBone.parentId >= 0)
                    CreateGameObject(spriteBone.parentId, spriteBones, transforms, root);

                var go = new GameObject(spriteBone.name);
                var transform = go.transform;
                if (spriteBone.parentId >= 0)
                    transform.SetParent(transforms[spriteBone.parentId]);
                else
                    transform.SetParent(root);
                transform.localPosition = spriteBone.position;
                transform.localRotation = spriteBone.rotation;
                transform.localScale = Vector3.one;
                transforms[index] = transform;
            }
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

        internal static Vector3[] Bake(this SpriteSkin spriteSkin)
        {
            if (!spriteSkin.isValid)
                throw new Exception("Bake error: invalid SpriteSkin");

            var sprite = spriteSkin.spriteRenderer.sprite;
            var boneTransforms = spriteSkin.boneTransforms;
            var bindPoses = sprite.GetBindPoses();
            var boneWeights = sprite.GetBoneWeights();
            var outputVertices = new NativeArray<Vector3>(sprite.GetVertexCount(), Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var transformMatrices = new NativeArray<Matrix4x4>(boneTransforms.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var vertices = sprite.GetVertexAttribute<Vector3>(VertexAttribute.Position);
            if (vertices.Length == 0 || transformMatrices.Length == 0 || boneWeights.Length == 0 || outputVertices.Length == 0)
                return new Vector3[0];
            for (int i = 0; i < boneTransforms.Length; ++i)
                transformMatrices[i] = boneTransforms[i].localToWorldMatrix;

            var jobHandle = SpriteSkinUtility.Deform(vertices, boneWeights, Matrix4x4.identity, bindPoses, transformMatrices, outputVertices);
            jobHandle.Complete();

            var result = outputVertices.ToArray();

            outputVertices.Dispose();

            return result;
        }

        internal static void CalculateBounds(this SpriteSkin spriteSkin)
        {
            Debug.Assert(spriteSkin.isValid);

            var rootBone = spriteSkin.rootBone;
            var deformedVertices = spriteSkin.Bake();
            var bounds = new Bounds();

            if (deformedVertices.Length > 0)
            {
                bounds.min = rootBone.InverseTransformPoint(deformedVertices[0]);
                bounds.max = bounds.min;
            }

            foreach(var v in deformedVertices)
                bounds.Encapsulate(rootBone.InverseTransformPoint(v));

            bounds.extents = Vector3.Scale(bounds.extents, new Vector3(1.25f, 1.25f, 1f)); 
            
            spriteSkin.bounds = bounds;
        }
    }
}
