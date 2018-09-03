using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal static class MeshModuleUtility
    {
        public const float kEditorLineHeight = 18f;

        // These Don't belong here.
        public static float DistanceToSegment(Vector3 p1, Vector3 p2)
        {
            p1 = HandleUtility.WorldToGUIPoint(p1);
            p2 = HandleUtility.WorldToGUIPoint(p2);

            Vector2 point = Event.current.mousePosition;

            float retval = HandleUtility.DistancePointToLineSegment(point, p1, p2);
            if (retval < 0) retval = 0.0f;
            return retval;
        }

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
    }
}
