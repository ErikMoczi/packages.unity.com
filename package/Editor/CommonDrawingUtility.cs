using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace UnityEditor.Experimental.U2D.Animation
{
    [InitializeOnLoad]
    internal class CommonDrawingUtility
    {
        public static readonly Color kSpriteBorderColor = new Color(0.25f, 0.5f, 1f, 0.75f);

        static MethodInfo ApplyWireMaterial;
        static CommonDrawingUtility()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.ManifestModule.Name == "UnityEditor.dll")
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Namespace == "UnityEditor" && type.Name == "HandleUtility")
                        {
                            ApplyWireMaterial = type.GetMethod("ApplyWireMaterial", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[0], null);
                            break;
                        }
                    }
                    break;
                }
            }
            if (ApplyWireMaterial == null)
                Debug.LogError("HandleUtility.ApplyWireFrame method not found");
        }

        public static void DrawLine(Vector3 p1, Vector3 p2, Vector3 normal, float width)
        {
            DrawLine(p1, p2, normal, width, width);
        }

        public static void DrawLine(Vector3 p1, Vector3 p2, Vector3 normal, float widthP1, float widthP2)
        {
            DrawLine(p1, p2, normal, widthP1, widthP2, Handles.color);
        }

        public static void DrawLine(Vector3 p1, Vector3 p2, Vector3 normal, float widthP1, float widthP2, Color color)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Vector3 right = Vector3.Cross(normal, p2 - p1).normalized;

            Shader.SetGlobalFloat("_HandleSize", 1);

            ApplyWireMaterial.Invoke(null, null);
            GL.PushMatrix();
            GL.MultMatrix(Handles.matrix);
            GL.Begin(4);
            GL.Color(color);
            GL.Vertex(p1 + right * widthP1 * 0.5f);
            GL.Vertex(p1 - right * widthP1 * 0.5f);
            GL.Vertex(p2 - right * widthP2 * 0.5f);
            GL.Vertex(p1 + right * widthP1 * 0.5f);
            GL.Vertex(p2 - right * widthP2 * 0.5f);
            GL.Vertex(p2 + right * widthP2 * 0.5f);
            GL.End();
            GL.PopMatrix();
        }

        public static void BeginLines(Color color)
        {
            ApplyWireMaterial.Invoke(null, null);
            GL.PushMatrix();
            GL.MultMatrix(Handles.matrix);
            GL.Begin(GL.LINES);
            GL.Color(color);
        }

        public static void BeginSolidLines()
        {
            ApplyWireMaterial.Invoke(null, null);
            GL.PushMatrix();
            GL.MultMatrix(Handles.matrix);
            GL.Begin(GL.TRIANGLES);
        }

        public static void EndLines()
        {
            GL.End();
            GL.PopMatrix();
        }

        public static void DrawLine(Vector3 p1, Vector3 p2)
        {
            GL.Vertex(p1);
            GL.Vertex(p2);
        }

        public static void DrawSolidLine(float width, Vector3 p1, Vector3 p2)
        {
            DrawSolidLine(p1, p2, Vector3.forward, width, width);
        }

        public static void DrawSolidLine(Vector3 p1, Vector3 p2, Vector3 normal, float widthP1, float widthP2)
        {
            GL.Color(Handles.color);

            Vector3 right = Vector3.Cross(normal, p2 - p1).normalized;

            GL.Vertex(p1 + right * widthP1 * 0.5f);
            GL.Vertex(p1 - right * widthP1 * 0.5f);
            GL.Vertex(p2 - right * widthP2 * 0.5f);
            GL.Vertex(p1 + right * widthP1 * 0.5f);
            GL.Vertex(p2 - right * widthP2 * 0.5f);
            GL.Vertex(p2 + right * widthP2 * 0.5f);
        }

        public static void DrawBox(Rect position)
        {
            Vector3[] points = new Vector3[5];
            int i = 0;
            points[i++] = new Vector3(position.xMin, position.yMin, 0f);
            points[i++] = new Vector3(position.xMax, position.yMin, 0f);
            points[i++] = new Vector3(position.xMax, position.yMax, 0f);
            points[i++] = new Vector3(position.xMin, position.yMax, 0f);

            DrawLine(points[0], points[1]);
            DrawLine(points[1], points[2]);
            DrawLine(points[2], points[3]);
            DrawLine(points[3], points[0]);
        }

        public static void DrawTriangleLines(List<Vector3> vertices, List<int> indices, float width, Color color)
        {
            if (Event.current.type != EventType.Repaint || vertices.Count < 3 || indices.Count < 3)
                return;

            BeginLines(color);

            for (int i = 0; i < indices.Count; i += 3)
            {
                int i1 = indices[i];
                int i2 = indices[i + 1];
                int i3 = indices[i + 2];

                if (i1 < vertices.Count && i2 < vertices.Count && i3 < vertices.Count)
                {
                    DrawLine(vertices[i1], vertices[i2]);
                    DrawLine(vertices[i2], vertices[i3]);
                    DrawLine(vertices[i1], vertices[i3]);
                }
            }

            EndLines();
        }

        public static Color CalculateNiceColor(int index, int numColors)
        {
            numColors = Mathf.Clamp(numColors, 1, int.MaxValue);

            float hueAngleStep = 360f / (float)numColors;
            float hueLoopOffset = hueAngleStep * 0.5f;

            float hueAngle = index * hueAngleStep;
            float loops = (int)(hueAngle / 360f);
            float hue = ((hueAngle % 360f + (loops * hueLoopOffset % 360f)) / 360f);

            return Color.HSVToRGB(hue, 1f, 1f);
        }

        public static void DrawRect(Rect rect, Vector3 position, Quaternion rotation, Color color, float rectAlpha, float outlineAlpha)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Vector3[] corners = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                Vector3 point = GetLocalRectPoint(rect, i);
                corners[i] = rotation * point + position;
            }

            Vector3[] points = new Vector3[]
            {
                corners[0],
                corners[1],
                corners[2],
                corners[3],
                corners[0]
            };

            Color l_color = Handles.color;
            Handles.color = color;

            Vector2 offset = new Vector2(1f, 1f);

            if (!Camera.current)
            {
                offset.y *= -1;
            }

            Handles.DrawSolidRectangleWithOutline(points, new Color(1f, 1f, 1f, rectAlpha), new Color(1f, 1f, 1f, outlineAlpha));

            Handles.color = l_color;
        }

        private static Vector2 GetLocalRectPoint(Rect rect, int index)
        {
            switch (index)
            {
                case (0): return new Vector2(rect.xMin, rect.yMax);
                case (1): return new Vector2(rect.xMax, rect.yMax);
                case (2): return new Vector2(rect.xMax, rect.yMin);
                case (3): return new Vector2(rect.xMin, rect.yMin);
            }
            return Vector3.zero;
        }
    }
}
