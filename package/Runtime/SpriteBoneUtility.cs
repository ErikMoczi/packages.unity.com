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

        struct BoneJob : IJob
        {
            // Root's worldToLocalMatrix
            [ReadOnly]
            public Matrix4x4 rootInv;
            // this is loaded from SharedMeshData
            [ReadOnly]
            public NativeArray<Matrix4x4> bindPoses;
            // this is each of the bond's localToWorld
            [ReadOnly]
            [DeallocateOnJobCompletionAttribute]
            public NativeArray<Matrix4x4> bones;

            public NativeArray<Matrix4x4> output;

            public void Execute()
            {
                for (var i = 0; i < bones.Length; i++)
                {
                    output[i] = rootInv * bones[i] * bindPoses[i];
                }
            }
        }

        struct SkinJob : IJob
        {
            public int influenceCount;
            [ReadOnly]
            public NativeArray<BoneWeight> influences;
            [ReadOnly]
            public NativeSlice<Vector3> vertices;
            [ReadOnly]
            [DeallocateOnJobCompletionAttribute]
            public NativeArray<Matrix4x4> bones;

            public NativeArray<Vector3> output;

            public void Execute()
            {
                var deformFunc = (new System.Action<int>[] { Deform1, Deform2, null, Deform4 })[influenceCount - 1];
                for (var i = 0; i < vertices.Length; i++)
                {
                    deformFunc(i);
                }
            }

            void Deform1(int i)
            {
                int bone0 = influences[i].boneIndex0;
                output[i] = bones[bone0].MultiplyPoint3x4(vertices[i]) * influences[i].weight0;
            }

            void Deform2(int i)
            {
                int bone0 = influences[i].boneIndex0;
                int bone1 = influences[i].boneIndex1;
                output[i] =
                    bones[bone0].MultiplyPoint3x4(vertices[i]) * influences[i].weight0 +
                    bones[bone1].MultiplyPoint3x4(vertices[i]) * influences[i].weight1;
            }

            void Deform4(int i)
            {
                int bone0 = influences[i].boneIndex0;
                int bone1 = influences[i].boneIndex1;
                int bone2 = influences[i].boneIndex2;
                int bone3 = influences[i].boneIndex3;
                output[i] =
                    bones[bone0].MultiplyPoint3x4(vertices[i]) * influences[i].weight0 +
                    bones[bone1].MultiplyPoint3x4(vertices[i]) * influences[i].weight1 +
                    bones[bone2].MultiplyPoint3x4(vertices[i]) * influences[i].weight2 +
                    bones[bone3].MultiplyPoint3x4(vertices[i]) * influences[i].weight3;
            }
        }

        static GameObject CreateBoneGO(string name, GameObject parent, Vector3 position, Quaternion rotation)
        {
            var go = new GameObject(name);
            var tr = go.transform;
            if (parent != null)
                tr.SetParent(parent.transform);
            tr.position = position;
            tr.rotation = rotation;
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

        static GameObject CreateBoneObjects(SpriteBone[] spriteBones)
        {
            List<GameObject> gos = new List<GameObject>();
            GameObject rootObject = null;
            foreach (var bone in spriteBones)
            {
                if (bone.parentId < 0)
                {
                    rootObject = CreateBoneGO(bone.name, null, bone.position, bone.rotation);
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

        public static Transform[] Rebind(Transform rootBone, SpriteBone[] spriteBones)
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

        public static GameObject CreateSkeleton(SpriteBone[] spriteBones, GameObject go, Transform rootBone)
        {
            if (spriteBones == null)
                throw new ArgumentNullException("spriteBones is null");
            if (go == null)
                throw new ArgumentNullException("go is null");

            if (rootBone != null)
                GameObject.DestroyImmediate(rootBone.gameObject);

            var rootObject = CreateBoneObjects(spriteBones);
            Debug.Assert(rootObject != null);
            rootObject.transform.SetParent(go.transform);
            rootObject.transform.localPosition = spriteBones[0].position;
            rootObject.transform.localRotation = spriteBones[0].rotation;
            return rootObject;
        }

        public static JobHandle Deform(Sprite sprite, NativeArray<Vector3> deformableVertices, Matrix4x4 rootInv, Transform[] boneTransforms)
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
