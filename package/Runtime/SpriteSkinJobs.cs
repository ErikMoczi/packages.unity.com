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

    public struct BoneJob : IJob
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

    public struct SkinJob : IJob
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

    public struct AABBJob : IJob
    {
        [ReadOnly]
        public NativeArray<Vector3> vertices;

        public NativeArray<Vector3> minMax;

        public void Execute()
        {
            Debug.Assert(minMax.Length == 2);
            Debug.Assert(vertices.Length > 0);

            minMax[0] = minMax[1] = vertices[0];
            for (var i = 1; i < vertices.Length; i++)
            {
                minMax[0] = Vector3.Min(minMax[0], vertices[i]);
                minMax[1] = Vector3.Max(minMax[1], vertices[i]);
            }
        }
    }

}