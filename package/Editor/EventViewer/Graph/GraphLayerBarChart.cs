
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace EditorDiagnostics
{
    public class GraphLayerBarChartMesh : GraphLayerBase
    {
        Mesh mesh;
        List<Vector3> verts = new List<Vector3>();
        List<int> indices = new List<int>();
        List<Color32> colors = new List<Color32>();

        Rect bounds;
        Vector2 gridSize;

        public GraphLayerBarChartMesh(int stream, string name, string desc, Color color) : base(stream, name, desc, color) { }
        private void AddQuadToMesh(float left, float right, float bot, float top)
        {
            float xLeft = bounds.xMin + left * gridSize.x;
            float xRight = bounds.xMin + right * gridSize.x;
            float yBot = bounds.yMax - bot * gridSize.y;
            float yTop = bounds.yMax - top * gridSize.y;

            int start = verts.Count;
            verts.Add(new Vector3(xLeft, yBot, 0));
            verts.Add(new Vector3(xLeft, yTop, 0));
            verts.Add(new Vector3(xRight, yTop, 0));
            verts.Add(new Vector3(xRight, yBot, 0));

            indices.Add(start);
            indices.Add(start + 1);
            indices.Add(start + 2);

            indices.Add(start);
            indices.Add(start + 2);
            indices.Add(start + 3);
        }

        public override void Draw(EventDataCollection.PlayerSession.DataSet e, Rect r, int startFrame, int frameCount, int inspectFrame, bool expanded, Material mat, int maxValue)
        {
            var stream = e.GetStream(m_stream);
            if (stream != null && stream.samples.Count > 0)
            {
                mat.color = m_color;

                if (mesh == null)
                    mesh = new Mesh();
                verts.Clear();
                indices.Clear();
                colors.Clear();
                var endTime = startFrame + frameCount;

                bounds = new Rect(r);
                gridSize.x = bounds.width / (float)frameCount;
                gridSize.y = bounds.height / maxValue;

                int previousFrameNumber = endTime;
                int currentFrame = endTime;
                
                for (int i = stream.samples.Count - 1; i >= 0 && currentFrame > startFrame; --i)
                {
                    currentFrame = stream.samples[i].frame;
                    var frame = Mathf.Max(currentFrame, startFrame);
                    if (stream.samples[i].value > 0)
                    {
                        AddQuadToMesh(frame - startFrame, previousFrameNumber - startFrame, 0, stream.samples[i].value);
                    }
                    previousFrameNumber = frame;
                }
               
                if (verts.Count > 0)
                {
                    mesh.Clear(true);
                    mesh.SetVertices(verts);
                    mesh.triangles = indices.ToArray();
                    mat.SetPass(0);
                    Graphics.DrawMeshNow(mesh, Vector3.zero, Quaternion.identity);
                }
            }
        }
    }
}