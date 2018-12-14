using System.Collections.Generic;
using System.Linq;

namespace Unity.Tiny
{
    internal interface ISceneGraphNode
    {
        /// <summary>
        /// The direct parent of this node
        /// </summary>
        ISceneGraphNode Parent { get; set; }
        
        /// <summary>
        /// The direct children of this node
        /// </summary>
        List<ISceneGraphNode> Children { get; }
        
        /// <summary>
        /// The graph that this node belongs to
        /// </summary>
        ISceneGraph Graph { get; set; }
        
        /// <summary>
        /// Returns true if the node is active and enabled in the hierarchy
        /// </summary>
        bool EnabledInHierarchy { get; }
    }
    
    /// <summary>
    /// Graph node utility methods at the interface level
    /// </summary>
    internal static class GraphNodeExtensions
    {
        /// <summary>
        /// Returns true if the node is a direct child of the given node
        /// </summary>
        public static bool IsChildOf(this ISceneGraphNode self, ISceneGraphNode node)
        {
            return null != node && node.Children.Contains(self);
        }
        
        /// <summary>
        /// Returns true if the node is a direct child or any level descendant of the given node
        /// </summary>
        public static bool IsDescendantOrChildOf(this ISceneGraphNode self, ISceneGraphNode node)
        {
            var parent = self.Parent;

            while (null != parent)
            {
                if (node == parent)
                {
                    return true;
                }
                
                parent = parent.Parent;
            }

            return false;
        }
        
        /// <summary>
        /// Returns true if the node is a direct child or any level descendant of the given nodes
        /// </summary>
        public static bool IsDescendantOrChildOf(this ISceneGraphNode self, List<ISceneGraphNode> candidates)
        {
            var parents = ListPool<ISceneGraphNode>.Get();
            try
            {
                var parent = self.Parent;

                while (null != parent)
                {
                    parents.Add(parent);
                    parent = parent.Parent;
                }

                return parents.Intersect(candidates).Any();
            }
            finally
            {
                ListPool<ISceneGraphNode>.Release(parents);
            }
        }
        
        /// <summary>
        /// Returns true if the node is a direct parent or any level ancestor of the given node
        /// </summary>
        public static bool IsAncestorOrParentOf(this ISceneGraphNode self, ISceneGraphNode node)
        {
            if (self == node)
            {
                return true;
            }

            foreach(var child in self.Children)
            {
                if (child == node || child.IsAncestorOrParentOf(node))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        public static void SetParent(this ISceneGraphNode self, ISceneGraphNode parent)
        {
            self.SetParent(-1, parent);
        }
        
        public static void SetParent(this ISceneGraphNode self, int siblingIndex, ISceneGraphNode parent)
        {
            // Cannot SetParent on a node that is part of the children.
            if (self.IsAncestorOrParentOf(parent))
            {
                return;
            }

            // We defer the operations to the owning graph, since the current node and the parent node may not be in the
            // same graph.
            if (null == parent)
            {
                self.Graph.Insert(siblingIndex, self);
            }
            else
            {
                parent.Graph.Insert(siblingIndex, self, parent);
            }
        }
        
        /// <summary>
        /// Enumerates all ancestors
        ///
        /// @NOTE Includes self (should the naming be changed?)
        /// </summary>
        public static IEnumerable<ISceneGraphNode> GetAncestors(this ISceneGraphNode node)
        {
            while (null != node)
            {
                yield return node;
                node = node.Parent;
            }
        }
        
        /// <summary>
        /// Gathers all descendants and adds them to the given collection
        ///
        /// @NOTE Includes self (should the naming be changed?)
        /// </summary>
        public static IEnumerable<ISceneGraphNode> GetDescendants(this ISceneGraphNode node)
        {
            yield return node;

            foreach (var child in node.Children)
            {
                foreach (var descendant in child.GetDescendants())
                {
                    yield return descendant;
                }
            }
        }
        
        /// <summary>
        /// Gathers all descendants of the given nodes
        ///
        /// @NOTE Includes self (should the naming be changed?)
        /// </summary>
        public static IEnumerable<ISceneGraphNode> GetDescendants(this IEnumerable<ISceneGraphNode> nodes)
        {
            return nodes.SelectMany(node => node.GetDescendants());
        }

        public static TNode GetFirstAncestorOfType<TNode>(this ISceneGraphNode node)
            where TNode : class
        {
            if (null == node)
            {
                return null;
            }
            
            var parent = node.Parent;

            while (null != parent)
            {
                if (parent is TNode typed)
                {
                    return typed;
                }

                parent = parent.Parent;
            }

            return null;
        }
        
        public static int SiblingIndex(this ISceneGraphNode node)
        {
            return node.Parent?.Children.IndexOf(node) ?? node.Graph.Roots.IndexOf(node);
        }
    }
}