using UnityEngine;
using UnityEngine.U2D;
using System;
using System.Collections.Generic;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal class SpriteMeshController
    {
        private const float kSnapDistance = 10f;

        private struct EdgeIntersectionResult
        {
            public int startVertexIndex;
            public int endVertexIndex;
            public int intersectEdgeIndex;
            public Vector2 endPosition;
        }

        public ISpriteMeshView spriteMeshView { get; set; }
        public SpriteMeshData spriteMeshData { get; set; }
        public ISelection selection { get; set; }
        public IUndoObject undoObject { get; set; }
        public ITriangulator triangulator { get; set; }

        public void OnGUI()
        {
            Debug.Assert(spriteMeshView != null);
            Debug.Assert(spriteMeshData != null);
            Debug.Assert(selection != null);
            Debug.Assert(undoObject != null);

            spriteMeshView.selection = selection;
            spriteMeshView.frame = spriteMeshData.frame;
            spriteMeshView.SetupLayout();
            spriteMeshView.CancelMode();

            DrawEdges();
            PreviewCreateVertex();
            PreviewCreateEdge();
            PreviewSplitEdge();
            DrawVertices();

            LayoutVertices();
            LayoutEdges();

            HandleSplitEdge();
            HandleCreateEdge();
            HandleCreateVertex();
            HandleSelectVertex();
            HandleMoveVertex();
            HandleSelectEdge();
            HandleMoveEdge();
            HandleRemoveEdge();
            HandleRemoveVertices();
        }

        private void LayoutVertices()
        {
            for (int i = 0; i < spriteMeshData.vertices.Count; i++)
            {
                Vector2 position = spriteMeshData.vertices[i].position;
                spriteMeshView.LayoutVertex(position, i);
            }
        }

        private void LayoutEdges()
        {
            if (spriteMeshView.hoveredVertex != -1)
                return;

            for (int i = 0; i < spriteMeshData.edges.Count; i++)
            {
                Edge edge = spriteMeshData.edges[i];
                Vector2 startPosition = spriteMeshData.vertices[edge.index1].position;
                Vector2 endPosition = spriteMeshData.vertices[edge.index2].position;

                spriteMeshView.LayoutEdge(startPosition, endPosition, i);
            }
        }

        private void DrawEdges()
        {
            UpdateEdgeInstersection();

            spriteMeshView.BeginDrawEdges();

            for (int i = 0; i < spriteMeshData.edges.Count; ++i)
            {
                if (SkipDrawEdge(i))
                    continue;

                Edge edge = spriteMeshData.edges[i];
                Vector2 startPosition = spriteMeshData.vertices[edge.index1].position;
                Vector2 endPosition = spriteMeshData.vertices[edge.index2].position;

                if (selection.IsSelected(edge.index1) && selection.IsSelected(edge.index2))
                    spriteMeshView.DrawEdgeSelected(startPosition, endPosition);
                else
                    spriteMeshView.DrawEdge(startPosition, endPosition);
            }

            if (spriteMeshView.IsActionActive(MeshEditorAction.SelectEdge))
            {
                Edge hoveredEdge = spriteMeshData.edges[spriteMeshView.hoveredEdge];
                Vector2 startPosition = spriteMeshData.vertices[hoveredEdge.index1].position;
                Vector2 endPosition = spriteMeshData.vertices[hoveredEdge.index2].position;

                spriteMeshView.DrawEdgeHovered(startPosition, endPosition);
            }

            spriteMeshView.EndDrawEdges();
        }

        private bool SkipDrawEdge(int edgeIndex)
        {
            return edgeIndex == -1 ||
                spriteMeshView.IsActionActive(MeshEditorAction.SelectEdge) && spriteMeshView.hoveredEdge == edgeIndex ||
                spriteMeshView.IsActionActive(MeshEditorAction.CreateVertex) && spriteMeshView.hoveredEdge == edgeIndex ||
                spriteMeshView.IsActionActive(MeshEditorAction.SplitEdge) && spriteMeshView.closestEdge == edgeIndex ||
                spriteMeshView.IsActionActive(MeshEditorAction.CreateEdge) && edgeIndex == m_EdgeIntersectionResult.intersectEdgeIndex;
        }

        private void PreviewCreateVertex()
        {
            if (spriteMeshView.mode == SpriteMeshViewMode.CreateVertex &&
                spriteMeshView.IsActionActive(MeshEditorAction.CreateVertex))
            {
                Vector2 clampedMousePos = ClampToFrame(spriteMeshView.mouseWorldPosition);

                if (spriteMeshView.hoveredEdge != -1)
                {
                    Edge edge = spriteMeshData.edges[spriteMeshView.hoveredEdge];

                    spriteMeshView.BeginDrawEdges();

                    spriteMeshView.DrawEdge(spriteMeshData.vertices[edge.index1].position, clampedMousePos);
                    spriteMeshView.DrawEdge(spriteMeshData.vertices[edge.index2].position, clampedMousePos);

                    spriteMeshView.EndDrawEdges();
                }

                spriteMeshView.DrawVertex(clampedMousePos);
            }
        }

        private void PreviewCreateEdge()
        {
            if (!spriteMeshView.IsActionActive(MeshEditorAction.CreateEdge))
                return;

            spriteMeshView.BeginDrawEdges();

            spriteMeshView.DrawEdge(spriteMeshData.vertices[m_EdgeIntersectionResult.startVertexIndex].position, m_EdgeIntersectionResult.endPosition);

            if (m_EdgeIntersectionResult.intersectEdgeIndex != -1)
            {
                Edge intersectingEdge = spriteMeshData.edges[m_EdgeIntersectionResult.intersectEdgeIndex];
                spriteMeshView.DrawEdge(spriteMeshData.vertices[intersectingEdge.index1].position, m_EdgeIntersectionResult.endPosition);
                spriteMeshView.DrawEdge(spriteMeshData.vertices[intersectingEdge.index2].position, m_EdgeIntersectionResult.endPosition);
            }

            spriteMeshView.EndDrawEdges();

            if (m_EdgeIntersectionResult.endVertexIndex == -1)
                spriteMeshView.DrawVertex(m_EdgeIntersectionResult.endPosition);
        }

        private void PreviewSplitEdge()
        {
            if (!spriteMeshView.IsActionActive(MeshEditorAction.SplitEdge))
                return;

            Vector2 clampedMousePos = ClampToFrame(spriteMeshView.mouseWorldPosition);

            Edge closestEdge = spriteMeshData.edges[spriteMeshView.closestEdge];

            spriteMeshView.BeginDrawEdges();

            spriteMeshView.DrawEdge(spriteMeshData.vertices[closestEdge.index1].position, clampedMousePos);
            spriteMeshView.DrawEdge(spriteMeshData.vertices[closestEdge.index2].position, clampedMousePos);

            spriteMeshView.EndDrawEdges();

            spriteMeshView.DrawVertex(clampedMousePos);
        }

        private void DrawVertices()
        {
            for (int i = 0; i < spriteMeshData.vertices.Count; i++)
            {
                Vector3 position = spriteMeshData.vertices[i].position;

                if (selection.IsSelected(i))
                    spriteMeshView.DrawVertexSelected(position);
                else if (i == spriteMeshView.hoveredVertex && spriteMeshView.IsActionHot(MeshEditorAction.None))
                    spriteMeshView.DrawVertexHovered(position);
                else
                    spriteMeshView.DrawVertex(position);
            }
        }

        private void HandleSelectVertex()
        {
            bool additive;
            if (spriteMeshView.DoSelectVertex(out additive))
                SelectVertex(spriteMeshView.hoveredVertex, additive);
        }

        private void HandleSelectEdge()
        {
            bool additive;
            if (spriteMeshView.DoSelectEdge(out additive))
                SelectEdge(spriteMeshView.hoveredEdge, additive);
        }

        private void HandleMoveVertex()
        {
            Vector2 delta;
            if (spriteMeshView.DoMoveVertex(out delta))
                MoveSelectedVertices(delta);
        }

        private void HandleCreateVertex()
        {
            if (spriteMeshView.DoCreateVertex())
                CreateVertex(spriteMeshView.mouseWorldPosition, spriteMeshView.hoveredEdge);
        }

        private void HandleSplitEdge()
        {
            if (spriteMeshView.DoSplitEdge())
                SplitEdge(spriteMeshView.mouseWorldPosition, spriteMeshView.closestEdge);
        }

        private void HandleCreateEdge()
        {
            if (spriteMeshView.DoCreateEdge())
                CreateEdge(spriteMeshView.mouseWorldPosition, spriteMeshView.hoveredVertex, spriteMeshView.hoveredEdge);
        }

        private void HandleMoveEdge()
        {
            Vector2 delta;
            if (spriteMeshView.DoMoveEdge(out delta))
                MoveSelectedVertices(delta);
        }

        private void HandleRemoveEdge()
        {
            Edge edge;
            if (GetSelectedEdge(out edge) && spriteMeshView.DoRemove())
                RemoveEdge(edge);
        }

        private void HandleRemoveVertices()
        {
            if (spriteMeshView.DoRemove())
                RemoveSelectedVertices();
        }

        private void CreateVertex(Vector2 position, int edgeIndex)
        {
            position = MeshModuleUtility.ClampPositionToRect(position, spriteMeshData.frame);
            undoObject.RegisterCompleteObjectUndo("Create Vertex");

            BoneWeight boneWeight = new BoneWeight();

            Vector3Int indices;
            Vector3 barycentricCoords;
            if (spriteMeshData.FindTriangle(position, out indices, out barycentricCoords))
            {
                EditableBoneWeight bw1 = spriteMeshData.vertices[indices.x].editableBoneWeight;
                EditableBoneWeight bw2 = spriteMeshData.vertices[indices.y].editableBoneWeight;
                EditableBoneWeight bw3 = spriteMeshData.vertices[indices.z].editableBoneWeight;

                EditableBoneWeight result = new EditableBoneWeight();

                foreach (BoneWeightChannel channel in bw1)
                {
                    if (!channel.enabled)
                        continue;

                    BoneWeightData data = channel.boneWeightData;
                    data.weight *= barycentricCoords.x;

                    if (data.weight > 0f)
                        result.AddChannel(data, true);
                }

                foreach (BoneWeightChannel channel in bw2)
                {
                    if (!channel.enabled)
                        continue;

                    BoneWeightData data = channel.boneWeightData;
                    data.weight *= barycentricCoords.y;

                    if (data.weight > 0f)
                        result.AddChannel(data, true);
                }

                foreach (BoneWeightChannel channel in bw3)
                {
                    if (!channel.enabled)
                        continue;

                    BoneWeightData data = channel.boneWeightData;
                    data.weight *= barycentricCoords.z;

                    if (data.weight > 0f)
                        result.AddChannel(data, true);
                }

                result.UnifyChannelsWithSameBoneIndex();
                result.FilterChannels(0f);
                result.ClampChannels(4, true);

                boneWeight = result.ToBoneWeight(true);
            }
            else if (edgeIndex != -1)
            {
                Edge edge = spriteMeshData.edges[edgeIndex];
                Vector2 pos1 = spriteMeshData.vertices[edge.index1].position;
                Vector2 pos2 = spriteMeshData.vertices[edge.index2].position;
                Vector2 dir1 = (position - pos1);
                Vector2 dir2 = (pos2 - pos1);
                float t = Vector2.Dot(dir1, dir2.normalized) / dir2.magnitude;
                t = Mathf.Clamp01(t);
                BoneWeight bw1 = spriteMeshData.vertices[edge.index1].editableBoneWeight.ToBoneWeight(true);
                BoneWeight bw2 = spriteMeshData.vertices[edge.index2].editableBoneWeight.ToBoneWeight(true);

                boneWeight = EditableBoneWeightUtility.Lerp(bw1, bw2, t);
            }

            spriteMeshData.CreateVertex(position, edgeIndex);
            spriteMeshData.vertices[spriteMeshData.vertices.Count - 1].editableBoneWeight.SetFromBoneWeight(boneWeight);
            spriteMeshData.Triangulate(triangulator);
        }

        private void SelectVertex(int index, bool additiveToggle)
        {
            if (index < 0)
                throw new ArgumentException("Index out of range");

            bool selected = selection.IsSelected(index);
            if (selected)
            {
                if (additiveToggle)
                {
                    undoObject.RegisterCompleteObjectUndo("Selection");
                    selection.Select(index, false);
                }
            }
            else
            {
                undoObject.RegisterCompleteObjectUndo("Selection");

                if (!additiveToggle)
                    ClearSelection();

                selection.Select(index, true);
            }

            undoObject.IncrementCurrentGroup();
        }

        private void SelectEdge(int index, bool additiveToggle)
        {
            Debug.Assert(index >= 0);

            Edge edge = spriteMeshData.edges[index];

            undoObject.RegisterCompleteObjectUndo("Selection");

            bool selected = selection.IsSelected(edge.index1) && selection.IsSelected(edge.index2);
            if (selected)
            {
                if (additiveToggle)
                {
                    selection.Select(edge.index1, false);
                    selection.Select(edge.index2, false);
                }
            }
            else
            {
                if (!additiveToggle)
                    ClearSelection();

                selection.Select(edge.index1, true);
                selection.Select(edge.index2, true);
            }

            undoObject.IncrementCurrentGroup();
        }

        private void ClearSelection()
        {
            undoObject.RegisterCompleteObjectUndo("Selection");
            selection.Clear();
        }

        private void MoveSelectedVertices(Vector2 delta)
        {
            delta = MeshModuleUtility.MoveRectInsideFrame(CalculateRectFromSelection(), spriteMeshData.frame, delta);

            undoObject.RegisterCompleteObjectUndo("Move Vertices");

            foreach (int index in selection)
            {
                Vector2 v = spriteMeshData.vertices[index].position;
                spriteMeshData.SetVertexPosition(index, ClampToFrame(v + delta));
            }

            spriteMeshData.Triangulate(triangulator);
        }

        private void CreateEdge(Vector2 position, int hoveredVertexIndex, int hoveredEdgeIndex)
        {
            position = ClampToFrame(position);
            EdgeIntersectionResult edgeIntersectionResult = CalculateEdgeIntersection(selection.single, hoveredVertexIndex, hoveredEdgeIndex, position);

            undoObject.RegisterCompleteObjectUndo("Create Edge");

            int selectIndex = -1;

            if (edgeIntersectionResult.endVertexIndex == -1)
            {
                CreateVertex(edgeIntersectionResult.endPosition, edgeIntersectionResult.intersectEdgeIndex);
                spriteMeshData.CreateEdge(selection.single, spriteMeshData.vertices.Count - 1);
                selectIndex = spriteMeshData.vertices.Count - 1;
            }
            else
            {
                spriteMeshData.CreateEdge(selection.single, edgeIntersectionResult.endVertexIndex);
                spriteMeshData.Triangulate(triangulator);
                selectIndex = edgeIntersectionResult.endVertexIndex;
            }

            ClearSelection();
            selection.Select(selectIndex, true);

            undoObject.IncrementCurrentGroup();
        }

        private void SplitEdge(Vector2 position, int edgeIndex)
        {
            undoObject.RegisterCompleteObjectUndo("Split Edge");

            Vector2 clampedMousePos = ClampToFrame(position);

            CreateVertex(clampedMousePos, edgeIndex);

            undoObject.IncrementCurrentGroup();
        }

        private bool GetSelectedEdge(out Edge edge)
        {
            edge = default(Edge);

            if (selection.Count != 2)
                return false;

            int index1 = 0;
            int index2 = 0;

            using (IEnumerator<int> enumerator = selection.GetEnumerator())
            {
                enumerator.MoveNext();
                index1 = enumerator.Current;
                enumerator.MoveNext();
                index2 = enumerator.Current;
            }

            edge = new Edge(index1, index2);

            if (!spriteMeshData.edges.Contains(edge))
                return false;

            return true;
        }

        private void RemoveEdge(Edge edge)
        {
            undoObject.RegisterCompleteObjectUndo("Remove Edge");
            spriteMeshData.RemoveEdge(edge);
            spriteMeshData.Triangulate(triangulator);
        }

        private void RemoveSelectedVertices()
        {
            undoObject.RegisterCompleteObjectUndo("Remove Vertices");

            spriteMeshData.RemoveVertex(selection);
            spriteMeshData.Triangulate(triangulator);
            selection.Clear();
        }

        private Vector2 ClampToFrame(Vector2 position)
        {
            return MeshModuleUtility.ClampPositionToRect(position, spriteMeshData.frame);
        }

        private Rect CalculateRectFromSelection()
        {
            Rect rect = new Rect();

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            foreach (int index in selection)
            {
                Vector2 v = spriteMeshData.vertices[index].position;

                min.x = Mathf.Min(min.x, v.x);
                min.y = Mathf.Min(min.y, v.y);

                max.x = Mathf.Max(max.x, v.x);
                max.y = Mathf.Max(max.y, v.y);
            }

            rect.min = min;
            rect.max = max;

            return rect;
        }

        private void UpdateEdgeInstersection()
        {
            if (selection.Count == 1)
                m_EdgeIntersectionResult = CalculateEdgeIntersection(selection.single, spriteMeshView.hoveredVertex, spriteMeshView.hoveredEdge, ClampToFrame(spriteMeshView.mouseWorldPosition));
        }

        private EdgeIntersectionResult CalculateEdgeIntersection(int vertexIndex, int hoveredVertexIndex, int hoveredEdgeIndex, Vector2 targetPosition)
        {
            Debug.Assert(vertexIndex >= 0);

            EdgeIntersectionResult edgeIntersection = new EdgeIntersectionResult();

            edgeIntersection.startVertexIndex = vertexIndex;
            edgeIntersection.endVertexIndex = hoveredVertexIndex;
            edgeIntersection.endPosition = targetPosition;
            edgeIntersection.intersectEdgeIndex = -1;

            Vector2 startPoint = spriteMeshData.vertices[edgeIntersection.startVertexIndex].position;

            bool intersectsEdge = false;
            int lastIntersectingEdgeIndex = -1;

            do
            {
                lastIntersectingEdgeIndex = edgeIntersection.intersectEdgeIndex;

                if (intersectsEdge)
                {
                    Vector2 dir = edgeIntersection.endPosition - startPoint;
                    edgeIntersection.endPosition += dir.normalized * 10f;
                }

                intersectsEdge = SegmentIntersectsEdge(startPoint, edgeIntersection.endPosition, vertexIndex, ref edgeIntersection.endPosition, out edgeIntersection.intersectEdgeIndex);

                //if we are hovering a vertex and intersect an edge indexing it we forget about the intersection
                if (intersectsEdge && spriteMeshData.edges[edgeIntersection.intersectEdgeIndex].Contains(edgeIntersection.endVertexIndex))
                {
                    edgeIntersection.intersectEdgeIndex = -1;
                    intersectsEdge = false;
                    edgeIntersection.endPosition = spriteMeshData.vertices[edgeIntersection.endVertexIndex].position;
                }

                if (intersectsEdge)
                {
                    edgeIntersection.endVertexIndex = -1;

                    Edge intersectingEdge = spriteMeshData.edges[edgeIntersection.intersectEdgeIndex];
                    Vector2 newPointScreen = spriteMeshView.WorldToScreen(edgeIntersection.endPosition);
                    Vector2 edgeV1 = spriteMeshView.WorldToScreen(spriteMeshData.vertices[intersectingEdge.index1].position);
                    Vector2 edgeV2 = spriteMeshView.WorldToScreen(spriteMeshData.vertices[intersectingEdge.index2].position);

                    if ((newPointScreen - edgeV1).magnitude <= kSnapDistance)
                        edgeIntersection.endVertexIndex = intersectingEdge.index1;
                    else if ((newPointScreen - edgeV2).magnitude <= kSnapDistance)
                        edgeIntersection.endVertexIndex = intersectingEdge.index2;

                    if (edgeIntersection.endVertexIndex != -1)
                    {
                        edgeIntersection.intersectEdgeIndex = -1;
                        intersectsEdge = false;
                        edgeIntersection.endPosition = spriteMeshData.vertices[edgeIntersection.endVertexIndex].position;
                    }
                }
            }
            while (intersectsEdge && lastIntersectingEdgeIndex != edgeIntersection.intersectEdgeIndex);

            edgeIntersection.intersectEdgeIndex = intersectsEdge ? edgeIntersection.intersectEdgeIndex : hoveredEdgeIndex;

            if (edgeIntersection.endVertexIndex != -1 && !intersectsEdge)
                edgeIntersection.endPosition = spriteMeshData.vertices[edgeIntersection.endVertexIndex].position;

            return edgeIntersection;
        }

        private bool SegmentIntersectsEdge(Vector2 p1, Vector2 p2, int ignoreIndex, ref Vector2 point, out int intersectingEdgeIndex)
        {
            intersectingEdgeIndex = -1;

            float sqrDistance = float.MaxValue;

            for (int i = 0; i < spriteMeshData.edges.Count; i++)
            {
                Edge edge = spriteMeshData.edges[i];
                Vector2 v1 = spriteMeshData.vertices[edge.index1].position;
                Vector2 v2 = spriteMeshData.vertices[edge.index2].position;
                Vector2 pointTmp = Vector2.zero;

                if (!edge.Contains(ignoreIndex) && MeshModuleUtility.SegmentIntersection(p1, p2, v1, v2, ref pointTmp))
                {
                    float sqrMagnitude = (pointTmp - p1).sqrMagnitude;
                    if (sqrMagnitude < sqrDistance)
                    {
                        sqrDistance = sqrMagnitude;
                        intersectingEdgeIndex = i;
                        point = pointTmp;
                    }
                }
            }

            return intersectingEdgeIndex != -1;
        }

        private EdgeIntersectionResult m_EdgeIntersectionResult;
    }
}
