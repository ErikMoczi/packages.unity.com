using UnityEngine;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal static class BoneDrawingUtility
    {
        public static float GetBoneRadius(float scale = 1.0f)
        {
            return 10f * scale / Handles.matrix.GetColumn(0).magnitude;
        }

        public static void DrawBoneNodeOutline(Vector3 position, Color color, float scale = 1.0f)
        {
            Color c = Handles.color;
            Handles.color = color;

            Handles.DrawSolidDisc(position, Vector3.back, GetBoneRadius(scale) * 0.5f);

            Handles.color = c;
        }

        public static void DrawBoneNode(Vector3 position, Color color, float scale = 1.0f)
        {
            Color c = Handles.color;
            Handles.color = color;

            Handles.DrawSolidDisc(position, Vector3.back, GetBoneRadius(scale) * 0.3f);

            Handles.color = c;
        }

        public static void DrawBoneBody(Vector3 startPos, Vector3 endPos, Color color, float scale = 1.0f)
        {
            Color c = Handles.color;
            Handles.color = color;

            CommonDrawingUtility.DrawLine(startPos, endPos, Vector3.back, GetBoneRadius(scale), GetBoneRadius(scale) * 0.05f);

            Handles.color = c;
        }

        public static void DrawBoneOutline(Vector3 startPos, Vector3 endPos, Color color, float scale = 1.0f)
        {
            Color c = Handles.color;
            Handles.color = color;

            CommonDrawingUtility.DrawLine(startPos, endPos, Vector3.back, GetBoneRadius(scale) * 1.25f, GetBoneRadius(scale) * 0.25f);
            Handles.DrawSolidDisc(startPos, Vector3.back, 0.5f * GetBoneRadius(scale) * 1.25f);
            Handles.DrawSolidDisc(endPos, Vector3.back, 0.5f * GetBoneRadius(scale) * 0.25f);

            Handles.color = c;
        }

        public static void DrawParentLink(Vector3 startPos, Vector3 endPos, Color color, float scale = 1.0f)
        {
            CommonDrawingUtility.DrawLine(startPos, endPos, Vector3.back, GetBoneRadius(scale) * 0.1f, GetBoneRadius(scale) * 0.1f, color);
            
            var dir = (startPos - endPos).normalized;
            var arrowTipPoint = Vector3.Lerp(startPos, endPos, 0.5f);
            var arrowWingPoint = arrowTipPoint + dir * GetBoneRadius(scale);

            // Arrow
            CommonDrawingUtility.DrawLine(arrowWingPoint, arrowTipPoint, Vector3.back, GetBoneRadius(0.7f), 0.0f, color);
        }
    }
}
