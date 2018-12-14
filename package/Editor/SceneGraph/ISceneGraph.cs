using System.Collections.Generic;

namespace Unity.Tiny
{
    internal interface ISceneGraph
    {
        List<ISceneGraphNode> Roots { get; }

        /// <summary>
        /// Adds the given node to the graph
        /// </summary>
        /// <param name="node">The node to add</param>
        /// <param name="parent">The parent to add under</param>
        void Add(ISceneGraphNode node, ISceneGraphNode parent = null);
        
        /// <summary>
        /// Adds the given nodes to the graph
        /// </summary>
        /// <param name="nodes"></param>
        void Add(IEnumerable<ISceneGraphNode> nodes);
        
        /// <summary>
        /// Inserts the given node into the graph
        /// </summary>
        /// <param name="siblingIndex">The sibling index to insert at</param>
        /// <param name="node">The node to insert</param>
        /// <param name="parent">The parent to insert under</param>
        void Insert(int siblingIndex, ISceneGraphNode node, ISceneGraphNode parent = null);

        /// <summary>
        /// Inserts the given set of nodes into the graph
        /// </summary>
        /// <param name="siblingIndex">The sibling index to insert at</param>
        /// <param name="nodes">The nodes to insert</param>
        /// <param name="parent">The parent to insert under</param>
        void Insert(int siblingIndex, IEnumerable<ISceneGraphNode> nodes, ISceneGraphNode parent = null);
        
        /// <summary>
        /// Removes the given node from the graph starting at the given roots
        ///
        /// @NOTE The node can be one of the roots and it will be removed
        /// </summary>
        /// <param name="inspect">List of roots to recurse from</param>
        /// <param name="node">The node to remove</param>
        /// <returns>True if the element was removed; false otherwise</returns>
        bool Remove(List<ISceneGraphNode> inspect, ISceneGraphNode node);

        /// <summary>
        /// Deletes the given node from the graph
        /// </summary>
        /// <param name="node">The node to remove</param>
        /// <returns>True if the node was deleted; false otherwise</returns>
        bool Delete(ISceneGraphNode node);

        /// <summary>
        /// Duplicates the given node
        /// </summary>
        /// <param name="node">The node to duplicate</param>
        /// <returns></returns>
        ISceneGraphNode Duplicate(ISceneGraphNode node);
        
        /// <summary>
        /// Duplicates the given nodes
        /// </summary>
        /// <param name="candidates"></param>
        /// <returns></returns>
        List<ISceneGraphNode> Duplicate(List<ISceneGraphNode> candidates);
    }
}