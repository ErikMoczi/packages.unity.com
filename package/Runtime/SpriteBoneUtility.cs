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

    public class SpriteBoneUtility
    {

        static GameObject CreateBoneGO(string name, GameObject parent, Vector3 position, Quaternion rotation)
        {
            var go = new GameObject(name);
            var tr = go.transform;
            if (parent != null)
                tr.SetParent(parent.transform);
            tr.localPosition = position;
            tr.localRotation = rotation;
            tr.localScale = Vector3.one;
            return go;
        }

        static string GetPath(int index, SpriteBone[] spriteBones)
        {
            string path = "";
            while (index != -1)
            {
                SpriteBone spriteBone = spriteBones[index];
                string spriteBoneName = spriteBone.name;
                if (spriteBone.parentId != -1)
                {
                    if (path.Length > 0)
                        path = spriteBoneName + "/" + path;
                    else
                        path = spriteBoneName;
                }
                index = spriteBone.parentId;
            }

            return path;
        }

        static bool HasValidBoneTransforms(SpriteBone[] spriteBones, Transform[] boneTransforms)
        {
            if (boneTransforms != null && boneTransforms.Length > 0)
            {
                foreach (var boneTransform in boneTransforms)
                {
                    if (boneTransform == null)
                        return false;
                }
                return boneTransforms.Length == spriteBones.Length;
            }
            return false;
        }

        static NativeArray<Matrix4x4> PrepareBoneTransformMatrixArray(Transform[] boneTransforms)
        {
            var matrixArray = new NativeArray<Matrix4x4>(boneTransforms.Length, Allocator.TempJob);
            int index = 0;
            foreach (var boneTransform in boneTransforms)
            {
                var boneTransformMatrix = boneTransform.localToWorldMatrix;
                matrixArray[index++] = boneTransformMatrix;
            }
            return matrixArray;
        }

        static bool GatherBones(SpriteBone[] spriteBones, Transform transform, ref Transform[] boneTransforms)
        {
            for (int i = 1; i < spriteBones.Length; i++)
            {
                var spriteBonePath = SpriteBoneUtility.GetPath(i, spriteBones);
                var toAdd = transform.Find(spriteBonePath);
                if (toAdd == null)
                {
                    boneTransforms = null;
                    return false;
                }
                boneTransforms[i] = toAdd;
            }
            return true;
        }

        static GameObject CreateBoneObjects(SpriteBone[] spriteBones, GameObject skin)
        {
            List<GameObject> gos = new List<GameObject>();
            GameObject rootObject = null;
            foreach (var bone in spriteBones)
            {
                if (bone.parentId < 0)
                {
                    rootObject = CreateBoneGO(bone.name, skin, bone.position, bone.rotation);
                    gos.Add(rootObject);
                }
                else
                {
                    var newBone = CreateBoneGO(bone.name, gos[bone.parentId], bone.position, bone.rotation);
                    gos.Add(newBone);
                }
            }
            return rootObject;
        }

        public static GameObject CreateSkeleton(SpriteBone[] spriteBones, GameObject go, Transform rootBone)
        {
            if (spriteBones == null)
                throw new ArgumentNullException("spriteBones is null");
            if (go == null)
                throw new ArgumentNullException("go is null");

            if (rootBone != null)
                GameObject.DestroyImmediate(rootBone.gameObject);
            
            return CreateBoneObjects(spriteBones, go);
        }

        public static void ResetBindPose(SpriteBone[] spriteBones, Transform[] boneTransforms)
        {
            if (spriteBones.Length != boneTransforms.Length)
                throw new ArgumentException(String.Format("'spriteBones' with length of {0} must be same as 'boneTransforms' length of {1}", spriteBones.Length, boneTransforms.Length));

            for (int i = 0; i < boneTransforms.Length; ++i)
            {
                var tr = boneTransforms[i];
                var bone = spriteBones[i];
                tr.localPosition = bone.position;
                tr.localRotation = bone.rotation;
                tr.localScale = Vector3.one;
            }
        }

        internal static Transform[] Rebind(Transform rootBone, SpriteBone[] spriteBones)
        {
            if (spriteBones == null)
                throw new ArgumentNullException("spriteBones is null");
            if (rootBone == null)
                throw new ArgumentNullException("rootBone is null");

            int boneCount = spriteBones.Length;
            Transform[] transforms = null;
            if (boneCount > 0)
            {
                transforms = new Transform[boneCount];
                transforms[0] = rootBone;
                SpriteBoneUtility.GatherBones(spriteBones, rootBone, ref transforms);
            }
            return transforms;
        }

        internal static JobHandle Deform(Sprite sprite, NativeArray<Vector3> deformableVertices, Matrix4x4 rootInv, Transform[] boneTransforms)
        {
            if (sprite == null)
                throw new ArgumentNullException("Sprite asset of spriteRenderer is null");
            if (boneTransforms == null)
                throw new ArgumentNullException("boneTransforms is null");
            if (!HasValidBoneTransforms(sprite.GetBones(), boneTransforms))
                throw new ArgumentException("boneTransforms are invalid");

            var bindPoses = sprite.GetBindPoses();
            if (bindPoses.Length != boneTransforms.Length)
                throw new InvalidOperationException("boneTransforms must have same size as bindPoses");

            var worldBones = PrepareBoneTransformMatrixArray(boneTransforms);
            var preparedBones = new NativeArray<Matrix4x4>(worldBones.Length, Allocator.TempJob);
            var inVertices = sprite.GetVertexAttribute<Vector3>(VertexAttribute.Position);
            var boneWeights = sprite.GetBoneWeights();

            var boneJob = new BoneJob()
            {
                rootInv = rootInv,
                bindPoses = bindPoses,
                bones = worldBones,
                output = preparedBones
            };
            var boneFence = boneJob.Schedule();

            var skinJob = new SkinJob()
            {
                influenceCount = 4,
                influences = boneWeights,
                vertices = inVertices,
                bones = preparedBones,
                output = deformableVertices
            };

            JobHandle skinFence = skinJob.Schedule(boneFence);
            return skinFence;
        }
    }
}
