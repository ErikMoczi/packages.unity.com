using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using UnityEngine.Experimental.U2D;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal static class MeshModuleUtility
    {
        public const float kEditorLineHeight = 18f;

        static internal Vector3 GUIToWorld(Vector3 guiPosition)
        {
            return GUIToWorld(guiPosition, Vector3.forward, Vector3.zero);
        }

        static internal Vector3 GUIToWorld(Vector3 guiPosition, Vector3 planeNormal, Vector3 planePos)
        {
            Vector3 worldPos = Handles.inverseMatrix.MultiplyPoint(guiPosition);

            if (Camera.current)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(guiPosition);

                planeNormal = Handles.matrix.MultiplyVector(planeNormal);

                planePos = Handles.matrix.MultiplyPoint(planePos);

                Plane plane = new Plane(planeNormal, planePos);

                float distance = 0f;

                if (plane.Raycast(ray, out distance))
                {
                    worldPos = Handles.inverseMatrix.MultiplyPoint(ray.GetPoint(distance));
                }
            }

            return worldPos;
        }

        public static Color AlphaMultiplied(this Color c, float multiplier)
        {
            return new Color(c.r, c.g, c.b, c.a * multiplier);
        }

        public static GUIContent[] GetBoneNameList(SpriteMeshData spriteMeshData)
        {
            List<GUIContent> names = new List<GUIContent>();

            for (int i = 0; i < spriteMeshData.bones.Count; i++)
            {
                var bone = spriteMeshData.bones[i];
                names.Add(new GUIContent(i + " " + bone.name));
            }

            return names.ToArray();
        }

        public static void UpdateLocalToWorldMatrices(List<SpriteBoneData> spriteBoneDataList, Matrix4x4 rootMatrix, ref Matrix4x4[] localToWorldMatrices)
        {
            if (localToWorldMatrices == null || localToWorldMatrices.Length != spriteBoneDataList.Count)
                localToWorldMatrices = new Matrix4x4[spriteBoneDataList.Count];

            bool[] calculatedMatrix = new bool[spriteBoneDataList.Count];

            var processedBoneCount = 0;
            while (processedBoneCount < spriteBoneDataList.Count)
            {
                int oldCount = processedBoneCount;

                for (var i = 0; i < spriteBoneDataList.Count; ++i)
                {
                    if (calculatedMatrix[i])
                        continue;

                    var sourceBone = spriteBoneDataList[i];
                    if (sourceBone.parentId != -1 && !calculatedMatrix[sourceBone.parentId])
                        continue;

                    var localToWorldMatrix = Matrix4x4.identity;
                    localToWorldMatrix.SetTRS(sourceBone.localPosition, sourceBone.localRotation, Vector3.one);

                    if (sourceBone.parentId == -1)
                        localToWorldMatrix = rootMatrix * localToWorldMatrix;
                    else if (calculatedMatrix[sourceBone.parentId])
                        localToWorldMatrix = localToWorldMatrices[sourceBone.parentId] * localToWorldMatrix;

                    localToWorldMatrices[i] = localToWorldMatrix;
                    calculatedMatrix[i] = true;
                    processedBoneCount++;
                }

                if (oldCount == processedBoneCount)
                    throw new ArgumentException("Invalid hierarchy detected");
            }
        }

        public static List<SpriteBoneData> CreateSpriteBoneData(List<SpriteBone> spriteBoneList, Matrix4x4 rootMatrix)
        {
            List<SpriteBoneData> spriteBoneDataList = new List<SpriteBoneData>(spriteBoneList.Count);

            foreach (var spriteBone in spriteBoneList)
            {
                spriteBoneDataList.Add(new SpriteBoneData()
                {
                    name = spriteBone.name,
                    parentId = spriteBone.parentId,
                    localPosition = spriteBone.position,
                    localRotation = spriteBone.rotation,
                    depth = spriteBone.position.z,
                    length = spriteBone.length
                });
            }

            Matrix4x4[] localToWorldMatrices = null;
            MeshModuleUtility.UpdateLocalToWorldMatrices(spriteBoneDataList, rootMatrix, ref localToWorldMatrices);

            for (int i = 0; i < spriteBoneDataList.Count; ++i)
            {
                var spriteBoneData = spriteBoneDataList[i];
                spriteBoneData.position = localToWorldMatrices[i].MultiplyPoint(Vector2.zero);
                spriteBoneData.endPosition = localToWorldMatrices[i].MultiplyPoint(Vector2.right * spriteBoneData.length);
            }

            return spriteBoneDataList;
        }

        public static void DrawMesh(Mesh mesh, Material material)
        {
            Debug.Assert(mesh != null);
            Debug.Assert(material != null);

            if (Event.current.type != EventType.Repaint)
                return;

            material.SetFloat("_AdjustLinearForGamma", PlayerSettings.colorSpace == ColorSpace.Linear ? 1.0f : 0.0f);
            material.SetPass(0);
            Graphics.DrawMeshNow(mesh, Handles.matrix * GUI.matrix);
        }

        public static Vector2 ClampPositionToRect(Vector2 position, Rect rect)
        {
            return new Vector2(Mathf.Clamp(position.x, rect.xMin, rect.xMax), Mathf.Clamp(position.y, rect.yMin, rect.yMax));
        }

        public static Vector2 MoveRectInsideFrame(Rect rect, Rect frame, Vector2 delta)
        {
            if (frame.size.x <= rect.size.x)
                delta.x = 0f;

            if (frame.size.y <= rect.size.y)
                delta.y = 0f;

            Vector2 min = rect.min + delta;
            Vector2 max = rect.max + delta;
            Vector2 size = rect.size;
            Vector2 position = rect.position;

            max.x = Mathf.Clamp(max.x, frame.min.x, frame.max.x);
            max.y = Mathf.Clamp(max.y, frame.min.y, frame.max.y);

            min = max - size;

            min.x = Mathf.Clamp(min.x, frame.min.x, frame.max.x);
            min.y = Mathf.Clamp(min.y, frame.min.y, frame.max.y);

            max = min + size;

            rect.min = min;
            rect.max = max;

            delta = rect.position - position;

            return delta;
        }

        public static bool SegmentIntersection(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, ref Vector2 point)
        {
            Vector2 s1 = p1 - p0;
            Vector2 s2 = p3 - p2;

            float s, t, determinant;
            determinant = (s1.x * s2.y - s2.x * s1.y);

            if (Mathf.Approximately(determinant, 0f))
                return false;

            s = (-s1.y * (p0.x - p2.x) + s1.x * (p0.y - p2.y)) / determinant;
            t = (s2.x * (p0.y - p2.y) - s2.y * (p0.x - p2.x)) / determinant;

            if (s >= 0f && s <= 1f && t >= 0f && t <= 1f)
            {
                point = p0 + (t * s1);
                return true;
            }

            return false;
        }

        //https://gamedev.stackexchange.com/a/49370
        public static void Barycentric(Vector2 p, Vector2 a, Vector2 b, Vector2 c, out Vector3 coords)
        {
            Vector2 v0 = b - a, v1 = c - a, v2 = p - a;
            float d00 = Vector2.Dot(v0, v0);
            float d01 = Vector2.Dot(v0, v1);
            float d11 = Vector2.Dot(v1, v1);
            float d20 = Vector2.Dot(v2, v0);
            float d21 = Vector2.Dot(v2, v1);
            float invDenom = 1f / (d00 * d11 - d01 * d01);
            coords.y = (d11 * d20 - d01 * d21) * invDenom;
            coords.z = (d00 * d21 - d01 * d20) * invDenom;
            coords.x = 1f - coords.y - coords.z;
        }

        public static Quaternion NormalizeQuaternion(Quaternion q)
        {
            Vector4 v = new Vector4(q.x, q.y, q.z, q.w).normalized;
            return new Quaternion(v.x, v.y, v.z, v.w);
        }
    }
}
