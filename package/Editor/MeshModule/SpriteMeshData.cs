using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.U2D;

namespace UnityEditor.Experimental.U2D.Animation
{
    [Serializable]
    internal class SpriteBoneData
    {
        public string name;
        public int parentId;
        public Vector2 localPosition;
        public Quaternion localRotation;
        public Vector2 position;
        public Vector2 endPosition;
        public float depth;
        public float length;
    }

    [Serializable]
    internal class SpriteMeshData
    {
        public GUID spriteID;
        public Rect frame;
        public Vector2 pivot;
        public List<Vertex2D> vertices = new List<Vertex2D>();
        public List<int> indices = new List<int>();
        public List<SpriteBoneData> bones = new List<SpriteBoneData>();
        public List<Edge> edges = new List<Edge>();

        public void CreateVertex(Vector2 position)
        {
            CreateVertex(position, -1);
        }

        public void CreateVertex(Vector2 position, int edgeIndex)
        {
            vertices.Add(new Vertex2D(position));

            if (edgeIndex != -1)
            {
                Edge edge = edges[edgeIndex];
                RemoveEdge(edge);
                CreateEdge(edge.index1, vertices.Count - 1);
                CreateEdge(edge.index2, vertices.Count - 1);
            }
        }

        public void CreateEdge(int index1, int index2)
        {
            Debug.Assert(index1 >= 0);
            Debug.Assert(index2 >= 0);
            Debug.Assert(index1 < vertices.Count);
            Debug.Assert(index2 < vertices.Count);
            Debug.Assert(index1 != index2);

            Edge newEdge = new Edge(index1, index2);

            if (!edges.Contains(newEdge))
                edges.Add(newEdge);
        }

        public void RemoveVertex(int index)
        {
            //We need to delete the edges that reference the index
            List<Edge> edgesWithIndex;
            if (FindEdgesContainsIndex(index, out edgesWithIndex))
            {
                //If there are 2 edges referencing the same index we are removing, we can create a new one that connects the endpoints ("Unsplit").
                if (edgesWithIndex.Count == 2)
                {
                    Edge first = edgesWithIndex[0];
                    Edge second = edgesWithIndex[1];

                    int index1 = first.index1 != index ? first.index1 : first.index2;
                    int index2 = second.index1 != index ? second.index1 : second.index2;

                    CreateEdge(index1, index2);
                }

                //remove found edges
                for (int i = 0; i < edgesWithIndex.Count; i++)
                {
                    RemoveEdge(edgesWithIndex[i]);
                }
            }

            //Fix indices in edges greater than the one we are removing
            for (int i = 0; i < edges.Count; i++)
            {
                Edge edge = edges[i];

                if (edge.index1 > index)
                    edge.index1--;
                if (edge.index2 > index)
                    edge.index2--;

                edges[i] = edge;
            }

            vertices.RemoveAt(index);
        }

        public void RemoveVertex(IEnumerable<int> indices)
        {
            List<int> sortedIndexList = new List<int>(indices);

            if (sortedIndexList.Count == 0)
                return;

            sortedIndexList.Sort();

            for (int i = sortedIndexList.Count - 1; i >= 0; --i)
            {
                RemoveVertex(sortedIndexList[i]);
            }
        }

        public void RemoveEdge(Edge edge)
        {
            edges.Remove(edge);
        }

        public bool FindEdgesContainsIndex(int index, out List<Edge> result)
        {
            bool found = false;

            result = new List<Edge>();

            for (int i = 0; i < edges.Count; ++i)
            {
                Edge edge = edges[i];

                if (edge.Contains(index))
                {
                    found = true;
                    result.Add(edge);
                }
            }

            return found;
        }

        public void SetVertexPosition(int index, Vector2 position)
        {
            vertices[index].position = position;
        }
    }
}
