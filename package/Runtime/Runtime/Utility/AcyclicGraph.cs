

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine.Assertions;

namespace Unity.Tiny
{
    internal interface INode { }

    [Serializable]
    internal struct AcyclicGraphConnection
    {
        public int Source;
        public int Target;
    }
    
    internal class AcyclicGraph<T>
    {
        internal class Node : INode
        {
            public int Index { get; set; }
            public T Data { get; set; }

            private readonly List<Node> m_Connections = new List<Node>();
        
            public IEnumerable<Node> Connections => m_Connections;

            public void AddConnection(Node node)
            {
                if (m_Connections.Contains(node))
                {
                    return;
                }
            
                m_Connections.Add(node);
            }

            public void RemoveConnection(Node node)
            {
                m_Connections.Remove(node);
            }
        }

        private readonly List<Node> m_Nodes = new List<Node>();
        
        public ReadOnlyCollection<Node> Nodes => m_Nodes.AsReadOnly();

        /// <summary>
        /// Get or add a node to the graph
        /// </summary>
        /// <param name="data">The content to add</param>
        /// <returns>The INode handle</returns>
        public INode GetOrAdd(T data)
        {
            var node = Get(data) as Node;

            if (null != node)
            {
                return node;
            }
            
            node = new Node {Index = m_Nodes.Count, Data = data};
            m_Nodes.Add(node);
            
            return node;
        }
        
        /// <summary>
        /// Adds a node to the graph
        /// </summary>
        /// <param name="data">The content to add</param>
        /// <returns>The INode handle</returns>
        public INode Add(T data)
        {
            var node = new Node {Index = m_Nodes.Count, Data = data};
            m_Nodes.Add(node);
            return node;
        }

        /// <summary>
        /// Gets a node from the graph by its content
        /// </summary>
        /// <param name="data">The content to find</param>
        /// <returns></returns>
        public INode Get(T data)
        {
            return m_Nodes.FirstOrDefault(t => t.Data.Equals(data));
        }

        /// <summary>
        /// Removes a node from the graph based on its content
        /// </summary>
        /// <param name="data">The content to remove</param>
        public void Remove(T data)
        {
            var node = Get(data) as Node;

            if (null != node)
            {
                Remove(node);
            }
        }

        /// <summary>
        /// Removes a node from the graph
        /// </summary>
        /// <param name="node">The content to remove</param>
        public void Remove(INode node)
        {
            Assert.IsTrue(node is Node);
            var index = ((Node) node).Index;
            m_Nodes.RemoveAt(index);

            for (var i = index; i < m_Nodes.Count; i++)
            {
                m_Nodes[i].Index = i;
            }
        }

        /// <summary>
        /// Creates a strong dependency between a and b
        /// 'A' depends on 'B'
        /// </summary>
        /// <param name="a">The source node</param>
        /// <param name="b">The dependency</param>
        public void AddDirectedConnection(INode a, INode b)
        {
            Assert.IsTrue(a is Node && b is Node);
            ((Node) a).AddConnection((Node) b);
        }
    }
}

