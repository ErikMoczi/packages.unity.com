using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace Unity.Tiny
{
    internal abstract class SceneGraphBase : ISceneGraph
    {
        public List<ISceneGraphNode> Roots { get; } = new List<ISceneGraphNode>();

        public bool Changed { get; private set; }

        /// <summary>
        /// Implement this method to handle duplication of a single node
        ///
        /// @NOTE You must handling adding to the graph in this method
        /// </summary>
        /// <param name="source">The source node being duplicated</param>
        /// <param name="parent">The parent the node should be placed under</param>
        /// <returns></returns>
        protected abstract ISceneGraphNode CreateNode(ISceneGraphNode source, ISceneGraphNode parent);

        /// <summary>
        /// Implement this method to handle an optional remap step for a duplicated graph
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        protected virtual void Remap(ISceneGraphNode source, ISceneGraphNode target)
        {
            
        }

        /// <summary>
        /// Invoked when inserting a node to the graph
        ///
        /// This can happen during creation OR moving from one graph to another
        /// </summary>
        /// <param name="node">The node being inserted</param>
        /// <param name="parent">The parent the node is being inserted under</param>
        protected virtual void OnInsertNode(ISceneGraphNode node, ISceneGraphNode parent)
        {
        }

        /// <summary>
        /// Invoked when removing a node from the graph
        ///
        /// This can happen during deletion OR moving from one graph to another
        /// </summary>
        /// <param name="node">The node being inserted</param>
        protected virtual void OnRemoveNode(ISceneGraphNode node)
        {
        }
        
        
        /// <summary>
        /// Invoked when deleting a node from the graph
        ///
        /// @note This is NOT the same as a removal
        /// </summary>
        /// <param name="node">The node being deleted</param>
        protected virtual void OnDeleteNode(ISceneGraphNode node)
        {
        }

        /// <inheritdoc />
        public void Add(IEnumerable<ISceneGraphNode> nodes)
        {
            foreach (var node in nodes)
            {
                Add(node);
            }
        }

        public void Add(ISceneGraphNode node, ISceneGraphNode parent = null)
        {
            Insert(-1, node, parent);
        }

        public void Insert(int siblingIndex, IEnumerable<ISceneGraphNode> nodes, ISceneGraphNode parent = null)
        {
            foreach (var node in nodes)
            {
                if (node.IsAncestorOrParentOf(parent))
                {
                    continue;
                }

                Insert(siblingIndex, node, parent);
                siblingIndex += siblingIndex < 0 ? 0 : 1;
            }
        }
        
        /// <inheritdoc />
        public void Insert(int siblingIndex, ISceneGraphNode node, ISceneGraphNode parent = null)
        {
            if (node.IsAncestorOrParentOf(parent))
            {
                // The node is already an ancestor of the given node. Bail out.
                return;
            }

            if (this != node.Graph)
            {
                // Remove this node from the previous graph; if any
                node.Graph.Remove(node.Graph.Roots, node);
                
                // We are moving from another graph, remap our descendants to this new graph 
                foreach (var descendant in node.GetDescendants())
                {
                    descendant.Graph = this;
                }
            }
            
            // Remove from previous parent or roots; if any
            if (null != node.Parent)
            {
                node.Parent.Children.Remove(node);
            }
            else
            {
                Roots.Remove(node);
            }
            
            node.Parent = parent;
            InsertOrAdd(null == node.Parent ? Roots : node.Parent.Children, siblingIndex, node);

            // Handle any custom operations in this callback
            OnInsertNode(node, parent);
            Changed = true;
        }
        
        public bool Delete(ISceneGraphNode node)
        {
            if (null == node)
            {
                return false;
            }
            
            Assert.IsTrue(this == node.Graph);

            if (!Remove(Roots, node))
            {
                return false;
            }

            foreach (var n in node.GetDescendants())
            {
                OnDeleteNode(n);
            }
            
            return true;
        }

        /// <inheritdoc />
        public bool Remove(List<ISceneGraphNode> inspect, ISceneGraphNode node)
        {
            if (inspect.Remove(node))
            {
                OnRemoveNode(node);
                Changed = true;
                return true;
            }

            foreach (var n in inspect)
            {
                if (Remove(n.Children, node))
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        /// <summary>
        /// Duplicates all root(s) of the given nodes
        /// </summary>
        /// <param name="candidates">Set of nodes to duplicate</param>
        public List<ISceneGraphNode> Duplicate(List<ISceneGraphNode> candidates)
        {
            // Only duplicate the root(s) from the given candidates
            var duplicates = candidates
                .Where(node => !node.IsDescendantOrChildOf(candidates))
                .Select(Duplicate)
                .ToList();
            
            return duplicates;
        }

        /// <inheritdoc />
        /// <summary>
        /// Duplicates the given node and all of it's children
        /// </summary>
        public ISceneGraphNode Duplicate(ISceneGraphNode source)
        {
            // Create the new node and all children from the source
            var target = CreateNodes(source, source.Parent);

            Remap(source, target);

            return target;
        }
        
        /// <summary>
        /// Clones
        /// </summary>
        /// <param name="source"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        private ISceneGraphNode CreateNodes(ISceneGraphNode source, ISceneGraphNode parent)
        {
            // @NOTE Adding to the graph should be handled in `CreateNode` implementation
            var node = CreateNode(source, parent);
            
            foreach (var child in source.Children)
            {
                CreateNodes(child, node);
            }

            return node;
        }
        
        /// <summary>
        /// Helper method to `Insert` the item if the given index is valid; otherwise `Add` will be used
        /// </summary>
        private static void InsertOrAdd<T>(IList<T> list, int index, T item)
        {
            list.Remove(item);
            
            if (index < 0 || index >= list.Count)
            {
                list.Add(item);
            }
            else
            {
                list.Insert(index, item);
            }
        }

        /// <summary>
        /// Clears the internal changed flag on the graph
        /// </summary>
        public void ClearChanged()
        {
            Changed = false;
        }
    }
}